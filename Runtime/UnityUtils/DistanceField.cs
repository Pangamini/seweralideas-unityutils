#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// The exact squared Euclidean distance, in texels, from every texel of a grid to the nearest seed. Seeds are texels with a value of
    /// zero (see <see cref="Cells"/>; <see cref="Reset"/> clears the grid to "no seed"). The grid can be any size.
    /// <para>
    /// The solve is the lower envelope of parabolas of Felzenszwalb and Huttenlocher, "Distance Transforms of Sampled Functions" (2012;
    /// "Marching Parabolas" in https://prideout.net/blog/distance_fields/): the one-dimensional transform along every row, then along every
    /// column. Each pass is one parallel job over the lines, and the whole solve is scheduled without waiting for anything.
    /// </para>
    /// <para>
    /// A signed solve takes the seeds as the inside of a shape: texels outside get the (positive) squared distance to the nearest inside
    /// texel, texels inside the negative of the squared distance to the nearest outside one. A texel's own distance is measured between texel
    /// centers, so the edge of a shape lies half a texel from the centers on either side.
    /// </para>
    /// </summary>
    public sealed class DistanceField : INativeDisposable
    {
        /// <summary>The value of a texel that is not a seed, and of one no seed reaches (everything, if there are no seeds).</summary>
        public const float Unreached = 1e20f;

        private const int CellsPerBatch = 1024; // about how many cells a worker takes at a time

        private readonly int _width;
        private readonly int _height;
        private NativeArray<float> _cells;
        private JobHandle _jobs; // the last job that uses the cells

        public int Width => _width;
        public int Height => _height;

        /// <summary>
        /// The grid, row by row (x + y * Width). Write a zero for each seed; jobs that do must depend on the handle of <see cref="ScheduleReset"/>
        /// and come before <see cref="ScheduleSolve"/>. After a solve it holds the squared distances.
        /// </summary>
        public NativeArray<float> Cells => _cells;

        public DistanceField(int width, int height)
        {
            if(width < 1 || height < 1)
                throw new System.ArgumentException("DistanceField size must be larger than 0");

            _width = width;
            _height = height;
            _cells = new NativeArray<float>(width * height, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Reset();
        }

        /// <summary>Clears the grid to no seeds, now.</summary>
        public void Reset() => ScheduleReset().Complete();

        /// <summary>
        /// Clears the grid to no seeds, as a job that comes after the solve in progress, if any, and <paramref name="dependsOn"/>.
        /// Seeds are put in by jobs that depend on the returned handle.
        /// </summary>
        public JobHandle ScheduleReset(JobHandle dependsOn = default)
        {
            _jobs = new FillJob { cells = _cells, value = Unreached }.Schedule(_cells.Length, CellsPerBatch, JobHandle.CombineDependencies(dependsOn, _jobs));
            return _jobs;
        }

        /// <summary>
        /// Solves the grid: each texel becomes its squared distance to the nearest seed. Signed, texels outside the seeds are positive and
        /// the seeds themselves (and the texels inside them) negative, see the class. Solve a grid once after each <see cref="Reset"/>.
        /// </summary>
        /// <returns>The handle of the last job: the cells are final when it is complete.</returns>
        public JobHandle ScheduleSolve(bool signed, JobHandle dependsOn = default)
        {
            JobHandle start = JobHandle.CombineDependencies(dependsOn, _jobs);

            if(!signed)
            {
                _jobs = ScheduleAxes(_cells, start);
                return _jobs;
            }

            // The distance of the texels inside to the nearest one outside is the same transform of the opposite seeds.
            var outside = new NativeArray<float>(_cells.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            JobHandle inverted = new InvertJob { source = _cells, target = outside }.Schedule(_cells.Length, CellsPerBatch, start);

            JobHandle solvedInside = ScheduleAxes(_cells, inverted);
            JobHandle solvedOutside = ScheduleAxes(outside, inverted);

            JobHandle combined = new CombineJob { main = _cells, outside = outside }
                .Schedule(_cells.Length, CellsPerBatch, JobHandle.CombineDependencies(solvedInside, solvedOutside));
            _jobs = outside.Dispose(combined);
            return _jobs;
        }

        // The transform along every row, then along every column.
        private JobHandle ScheduleAxes(NativeArray<float> cells, JobHandle dependsOn)
        {
            var rows = new AxisPassJob { cells = cells, length = _width, step = 1, lineStep = _width }
                .Schedule(_height, BatchOfLines(_width), dependsOn);
            return new AxisPassJob { cells = cells, length = _height, step = _width, lineStep = 1 }
                .Schedule(_width, BatchOfLines(_height), rows);
        }

        private static int BatchOfLines(int lineLength) => System.Math.Max(CellsPerBatch / lineLength, 1);

        public void Dispose()
        {
            _jobs.Complete();
            _cells.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return _cells.Dispose(JobHandle.CombineDependencies(inputDeps, _jobs));
        }

        [BurstCompile]
        private struct FillJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<float> cells;
            public float value;

            public void Execute(int index) => cells[index] = value;
        }

        [BurstCompile]
        private struct InvertJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> source;
            [WriteOnly] public NativeArray<float> target;

            public void Execute(int index) => target[index] = source[index] == 0f ? Unreached : 0f;
        }

        [BurstCompile]
        private struct CombineJob : IJobParallelFor
        {
            public NativeArray<float> main;
            [ReadOnly] public NativeArray<float> outside;

            // A texel inside (a seed) is 0 in the main transform; there, the distance to the outside, negated
            public void Execute(int index)
            {
                if(main[index] == 0f)
                    main[index] = -outside[index];
            }
        }

        // The one-dimensional transform of one line of the grid: line number `line`, `length` texels, `step` apart. A line is a row or a column.
        [BurstCompile]
        private struct AxisPassJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<float> cells;
            public int length;
            public int step;
            public int lineStep;

            public void Execute(int line)
            {
                int start = line * lineStep;
                int n = length;

                var f = new NativeArray<float>(n, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var v = new NativeArray<int>(n, Allocator.Temp, NativeArrayOptions.UninitializedMemory);   // the parabolas of the envelope: the texel of each
                var z = new NativeArray<float>(n + 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory); // ... and where each starts (the last, where the end is)

                for(int i = 0; i < n; i++)
                    f[i] = cells[start + i * step];

                // The lower envelope of the parabolas f(q) + (x - q)^2, left to right
                int k = 0;
                v[0] = 0;
                z[0] = -Unreached;
                z[1] = Unreached;
                for(int q = 1; q < n; q++)
                {
                    float s;
                    while(true)
                    {
                        int p = v[k];
                        s = ((f[q] + (float)q * q) - (f[p] + (float)p * p)) / (2f * q - 2f * p); // where the parabola of q passes the one of p
                        if(s > z[k])
                            break;
                        k--; // it passes it before p even starts: p is never lowest (z[0] is -infinity, so this never goes below 0)
                    }
                    k++;
                    v[k] = q;
                    z[k] = s;
                    z[k + 1] = Unreached;
                }

                // Read it off
                k = 0;
                for(int q = 0; q < n; q++)
                {
                    while(z[k + 1] < q)
                        k++;
                    float d = q - v[k];
                    cells[start + q * step] = d * d + f[v[k]];
                }

                f.Dispose();
                v.Dispose();
                z.Dispose();
            }
        }
    }
}
