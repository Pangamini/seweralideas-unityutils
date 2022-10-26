// Algorithm described at https://prideout.net/blog/distance_fields/

using SeweralIdeas.Utils;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace SeweralIdeas.UnityUtils
{
    public class DistanceField : INativeDisposable
    {
        private int m_width;
        private int m_height;
        private NativeArray<Cell> m_cells;
        private bool m_startedSolving = false;
        private JobHandle m_solveJobHandle = default;

        public int Width => m_width;
        public int Height => m_height;

        public bool IsEditable => !m_startedSolving;
        public bool IsSolved => m_startedSolving && m_solveJobHandle.IsCompleted;

        public int GetDistanceSquared(int x, int y) => m_cells[x + y * m_width].distSqr;
        public Cell GetValue(int x, int y) => m_cells[x + y * m_width];

        public NativeArray<Cell> nativeArray => m_cells;

        public Vector2Int Size => new Vector2Int(m_width, m_height);

        public struct Cell
        {
            public int distSqr;
            public int2 closestPoint;

            public static readonly Cell Default = new Cell()
            {
                distSqr = int.MaxValue,
                closestPoint = default
            };
        }

        public DistanceField(Vector2Int size) : this(size.x, size.y)
        {
        }

        public DistanceField(int width, int height)
        {
            if (width < 1 || height < 1)
                throw new System.ArgumentException("DistanceField size must be larger than 0");
            m_width = width;
            m_height = height;
            m_cells = new NativeArray<Cell>(m_width * m_height, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Reset();
        }

        public void Reset()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DistanceField.Reset");

            var job = new MemsetNativeArray<Cell>(m_cells, Cell.Default);
            job.Run(job.Length);

            m_startedSolving = false;
            m_solveJobHandle = default;
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void SetZero(int x, int y)
        {
            if (m_startedSolving)
                throw new System.InvalidOperationException("DistanceField locked");
            if (!CheckRange(x, y))
                throw new System.IndexOutOfRangeException("DistanceField index out of range");
            SetZero(x, y, m_cells, m_width);
        }


        public static void SetZero(int x, int y, NativeArray<Cell> cells, int m_width)
        {
            var cell = new Cell()
            {
                distSqr = 0,
                closestPoint = new int2(x, y)
            };
            cells[x + y * m_width] = cell;
        }

        public bool CheckRange(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < m_width && y < m_height);
        }

        private int CeilDiv(int x, int y)
        {
            return x / y + ((x % y != 0) ? 1 : 0);
        }

        const int cellsPerTask = 256;   // Empirically found to be the most efficient in various size cases (before jobs and burst tho)s
        public JobHandle ScheduleSolve(bool signed, JobHandle dependsOn = default)
        {
            if (m_startedSolving)
                throw new System.InvalidOperationException("DistanceField locked");

            m_startedSolving = true;

            int rowsPerTask = CeilDiv(cellsPerTask, m_width);
            int taskCount = CeilDiv(m_height, rowsPerTask);
            rowsPerTask = CeilDiv(m_height, taskCount);

            if (signed)
            {
                var negCells = new NativeArray<Cell>(m_cells.Length, Allocator.Persistent);
                var negateJob = new JobNegate(m_cells, negCells, Width).Schedule(m_cells.Length, 128, dependsOn);

                var solve1 = SolveHorizontal(rowsPerTask, m_width, m_height, m_cells, negateJob);
                var transpose1 = new TransposeJob(m_width, m_height, m_cells).Schedule(solve1);
                var solve2 = SolveHorizontal(rowsPerTask, m_width, m_height, m_cells, transpose1);
                var solveJobHandle = new TransposeJob(m_width, m_height, m_cells).Schedule(solve2);

                var solve1neg = SolveHorizontal(rowsPerTask, m_width, m_height, negCells, negateJob);
                var transpose1neg = new TransposeJob(m_width, m_height, negCells).Schedule(solve1neg);
                var solve2neg = SolveHorizontal(rowsPerTask, m_width, m_height, negCells, transpose1neg);
                var solveJobHandleNeg = new TransposeJob(m_width, m_height, negCells).Schedule(solve2neg);

                var solveJob = JobHandle.CombineDependencies(solveJobHandle, solveJobHandleNeg);
                var addJob = new JobCombine() { main = m_cells, neg = negCells }.Schedule(m_cells.Length, 128, solveJob);
                negCells.Dispose(addJob);
                m_solveJobHandle = addJob;
                return m_solveJobHandle;
            }
            else
            {
                var solve1 = SolveHorizontal(rowsPerTask, m_width, m_height, m_cells, dependsOn);
                var transpose1 = new TransposeJob(m_width, m_height, m_cells).Schedule(solve1);
                var solve2 = SolveHorizontal(rowsPerTask, m_width, m_height, m_cells, transpose1);
                var solveJobHandle = new TransposeJob(m_width, m_height, m_cells).Schedule(solve2);
                m_solveJobHandle = solveJobHandle;
                return m_solveJobHandle;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct JobNegate : IJobParallelFor
        {
            [ReadOnly] private NativeArray<Cell> input;
            [WriteOnly] private NativeArray<Cell> output;
            private int width;

            public JobNegate(NativeArray<Cell> input, NativeArray<Cell> output, int width)
            {
                this.input = input;
                this.output = output;
                this.width = width;
            }

            public void Execute(int i)
            {
                if (input[i].distSqr == 0)
                    output[i] = Cell.Default;
                else
                    SetZero(i % width, i / width, output, width);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct JobCombine : IJobParallelFor
        {
            public NativeArray<Cell> main;
            [ReadOnly] public NativeArray<Cell> neg;

            public void Execute(int index)
            {
                if (main[index].distSqr == 0)
                {
                    var negCell = neg[index];
                    negCell.distSqr *= -1;
                    main[index] = negCell;
                }
            }
        }

        private static JobHandle SolveHorizontal(int rowsPerTask, int width, int height, NativeArray<Cell> distances, JobHandle dependsOn = default)
        {
            if (rowsPerTask <= 0 || rowsPerTask >= height)
            {
                // single-threaded
                var job = new RowJob(0, height, width, distances);
                return job.Schedule(dependsOn);
            }

            else
            {
                int jobCount = (height / rowsPerTask) + (height % rowsPerTask == 0 ? 0 : 1);  // basically a ceil
                var jobs = new NativeArray<JobHandle>(jobCount, Allocator.TempJob);
                int jobIndex = 0;
                for (int i = 0; i < height; i += rowsPerTask)
                {
                    int rowStart = i;
                    int rowEnd = Mathf.Min(rowStart + rowsPerTask, height);
                    int rowCount = rowEnd - rowStart;

                    var handle = new RowJob(rowStart, rowCount, width, distances).Schedule(dependsOn);

                    jobs[jobIndex++] = handle;
                }

                var sharedHandle = JobHandle.CombineDependencies(jobs);
                jobs.Dispose();
                return sharedHandle;

            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct RowJob : IJob
        {
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            private NativeSlice<Cell> m_slice;
            private int m_rowCount;
            private int m_width;


            public RowJob(int firstRow, int rowCount, int width, NativeArray<Cell> distances)
            {
                this.m_slice = new NativeSlice<Cell>(distances, firstRow * width, width * rowCount);
                this.m_rowCount = rowCount;
                this.m_width = width;
            }

            private bool IsSet(Cell cell)
            {
                return cell.distSqr != int.MaxValue;
            }

            public void Execute()
            {
                var evaluated = new NativeArray<int>(m_width, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var m_nextEvaluated = new NativeArray<int>(m_width, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var m_newValues = new NativeArray<Cell>(m_width, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                try
                {
                    for (int rowIndex = 0; rowIndex < m_rowCount; ++rowIndex)
                    {
                        var rowSlice = new NativeSlice<Cell>(m_slice, rowIndex * m_width, m_width);
                        int m_evaluatedLength = 0;
                        int m_nextEvaluatedLength = 0;

                        // Expects values to be added ordered from lower to higher
                        void AddToEvaluate(int index)
                        {
                            // check last and next to last indices, if they don't contain this index already
                            if (m_nextEvaluatedLength > 0 && m_nextEvaluated[m_nextEvaluatedLength - 1] == index)
                                return;
                            if (m_nextEvaluatedLength > 1 && m_nextEvaluated[m_nextEvaluatedLength - 2] == index)
                                return;
                            if (m_nextEvaluatedLength > 2 && m_nextEvaluated[m_nextEvaluatedLength - 3] == index)
                                return;

#if DEBUG
                            //for (int i = 0; i < m_nextEvaluatedLength; ++i)
                            //    if (m_nextEvaluated[i] == index)
                            //    {
                            //        Debug.LogError("This shouldn't happen");
                            //        return;
                            //    }
#endif

                            m_nextEvaluated[m_nextEvaluatedLength] = index;
                            ++m_nextEvaluatedLength;
                        }

                        void SwapBuffers()
                        {
                            Swap(ref evaluated, ref m_nextEvaluated);
                            Swap(ref m_evaluatedLength, ref m_nextEvaluatedLength);
                        }

                        // pre-pass
                        for (int x = 0; x < m_width; ++x)
                        {

                            var val = rowSlice[x];
                            if (IsSet(val))
                            {

                                if ((x > 0))
                                {
                                    var leftVal = rowSlice[x - 1];
                                    if (leftVal.distSqr > val.distSqr || !IsSet(leftVal))
                                        AddToEvaluate(x - 1);
                                }
                                if ((x < m_width - 1))
                                {
                                    var rightVal = rowSlice[x + 1];
                                    if (rightVal.distSqr > val.distSqr || !IsSet(rightVal))
                                        AddToEvaluate(x + 1);
                                }
                            }
                            m_newValues[x] = rowSlice[x];
                        }

                        SwapBuffers();

                        int beta = 1;
                        // pass
                        while (m_evaluatedLength > 0)
                        {
                            m_nextEvaluatedLength = 0;
                            for (int evIndex = 0; evIndex < m_evaluatedLength; ++evIndex)
                            {
                                int x = evaluated[evIndex];
                                var val = rowSlice[x];
                                bool hasValueOnLeft = x != 0;
                                bool hasValueOnRight = x != (m_width - 1);

                                if (hasValueOnLeft)
                                {
                                    var cellLeft = rowSlice[x - 1];
                                    if (IsSet(cellLeft) && val.distSqr > cellLeft.distSqr + beta)
                                    {
                                        val.distSqr = cellLeft.distSqr + beta;
                                        val.closestPoint = cellLeft.closestPoint;
                                        if (hasValueOnRight)
                                            AddToEvaluate(x + 1);   // evaluate cell to the right in the next pass
                                    }
                                }

                                if (hasValueOnRight)
                                {
                                    var cellRight = rowSlice[x + 1];
                                    if (IsSet(cellRight) && val.distSqr > cellRight.distSqr + beta)
                                    {
                                        val.distSqr = cellRight.distSqr + beta;
                                        val.closestPoint = cellRight.closestPoint;
                                        if (hasValueOnLeft)
                                            AddToEvaluate(x - 1);   // evaluate cell to the left in the next pass
                                    }
                                }
                                m_newValues[x] = val;
                            }

                            for (int i = 0; i < m_width; ++i)
                                rowSlice[i] = m_newValues[i];

                            SwapBuffers();
                            beta += 2;
                        }
                    }
                }
                finally
                {
                    evaluated.Dispose();
                    m_nextEvaluated.Dispose();
                    m_newValues.Dispose();
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct TransposeJob : IJob
        {
            private int m_width;
            private int m_height;
            private NativeArray<Cell> m_distances;

            public TransposeJob(int width, int height, NativeArray<Cell> distances)
            {
                m_width = width;
                m_height = height;
                m_distances = distances;
            }

            public void Execute()
            {
                // simple transposition for squares
                if (m_width == m_height)
                {
                    for (int y = 0; y < m_height; ++y)
                    {
                        int row = y * m_width;
                        for (int x = y + 1; x < m_height; ++x)
                        {
                            var valA = m_distances[x + row];
                            m_distances[x + row] = m_distances[y + x * m_height];
                            m_distances[y + x * m_height] = valA;
                        }
                    }
                }
                else
                {
                    throw new System.NotImplementedException("Transposition of non-square matrix not implemented");
                }
            }
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            T store = a;
            a = b;
            b = store;
        }


        public void Dispose()
        {
            m_cells.Dispose();
            System.GC.SuppressFinalize(this);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            System.GC.SuppressFinalize(this);
            return m_cells.Dispose(inputDeps);
        }

        public JobHandle ScheduleGrab(NativeArray<Color> grab, JobHandle dependsOn = default)
        {
            var job = new GrabJobColor(new Vector2Int(m_width, m_height), m_cells, grab);
            return job.Schedule(dependsOn);
        }
        
        public JobHandle ScheduleGrab(NativeArray<Color32> grab, JobHandle dependsOn = default)
        {
            var job = new GrabJobColor32(new Vector2Int(m_width, m_height), m_cells, grab);
            return job.Schedule(dependsOn);
        }


        [BurstCompile]
        private struct GrabJobColor : IJobParallelFor
        {
            [ReadOnly] private Vector2Int size;
            [ReadOnly] private NativeArray<Cell> distanceField;
            [WriteOnly] private NativeArray<Color> grab;
            [ReadOnly] private float distMultiplier;
            [ReadOnly] private Vector2 texelSize;

            public GrabJobColor(Vector2Int size, NativeArray<Cell> distanceField, NativeArray<Color> grab)
            {
                int length = size.x * size.y;
                if (distanceField.Length != length || grab.Length != length)
                    throw new System.ArgumentException("NativeArrays don't have the correct size");

                this.size = size;
                this.distanceField = distanceField;
                this.grab = grab;

                distMultiplier = 1f / Mathf.Max(size.x, size.y);
                texelSize = new Vector2(1, 1) / size;
            }

            public JobHandle Schedule(JobHandle dependsOn = default)
            {
                return this.Schedule(distanceField.Length, 256, dependsOn);
            }

            void IJobParallelFor.Execute(int index)
            {
                var cell = distanceField[index];
                var relPos = new Vector2(cell.closestPoint.x, cell.closestPoint.y) * texelSize;
                grab[index] = new Color()
                {
                    r = math.sign(cell.distSqr) * math.sqrt(math.abs(cell.distSqr)) * distMultiplier,
                    g = relPos.x,
                    b = relPos.y,
                    a = 1
                };
            }
        }


        [BurstCompile]
        private struct GrabJobColor32 : IJobParallelFor
        {
            [ReadOnly] private Vector2Int size;
            [ReadOnly] private NativeArray<Cell> distanceField;
            [WriteOnly] private NativeArray<Color32> grab;
            [ReadOnly] private float distMultiplier;
            [ReadOnly] private Vector2 texelSize;

            public GrabJobColor32(Vector2Int size, NativeArray<Cell> distanceField, NativeArray<Color32> grab)
            {
                int length = size.x * size.y;
                if (distanceField.Length != length || grab.Length != length)
                    throw new System.ArgumentException("NativeArrays don't have the correct size");

                this.size = size;
                this.distanceField = distanceField;
                this.grab = grab;

                distMultiplier = 1f / Mathf.Max(size.x, size.y);
                texelSize = new Vector2(1, 1) / size;
            }

            public JobHandle Schedule(JobHandle dependsOn = default)
            {
                return this.Schedule(distanceField.Length, 256, dependsOn);
            }

            void IJobParallelFor.Execute(int index)
            {
                var cell = distanceField[index];
                var relPos = new Vector2(cell.closestPoint.x, cell.closestPoint.y) * texelSize;
                grab[index] = new Color()
                {
                    r = math.sign(cell.distSqr) * math.sqrt(math.abs(cell.distSqr)) * distMultiplier,
                    g = relPos.x,
                    b = relPos.y,
                    a = 1
                };
            }
        }
    }
}