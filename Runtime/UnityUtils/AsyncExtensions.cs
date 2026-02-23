using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// Wraps the task in try/catch with UnityEngine.Debug.LogException
        /// </summary>
        /// <param name="task"></param>
        public async static void ForgetSafe(this Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        /// <summary>
        /// Awaits the task, but throws OperationCanceledException if the token is canceled first.
        /// </summary>
        public async static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if (task.IsCompleted) 
                return await task; // fast path

            // Create a Task that completes when the token is canceled
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(task, tcs.Task);

                if (completed == tcs.Task)
                    throw new OperationCanceledException(cancellationToken);

                // Await the original task to propagate exceptions or return the result
                return await task;
            }
        }

        /// <summary>
        /// Awaits the task, but throws OperationCanceledException if the token is canceled first.
        /// </summary>
        public async static Task WaitAsync(this Task task, CancellationToken cancellationToken)
        {
            if (task.IsCompleted)
            {
                await task; // fast path
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(task, tcs.Task);

                if (completed == tcs.Task)
                    throw new OperationCanceledException(cancellationToken);

                await task; // propagate exceptions
            }
        }
    }
}
