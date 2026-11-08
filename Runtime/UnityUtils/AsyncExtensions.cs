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
            catch( OperationCanceledException )
            {
            }
            catch( Exception e )
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Wraps the valuetask in try/catch with UnityEngine.Debug.LogException
        /// </summary>
        /// <param name="task"></param>
        public async static void ForgetSafe(this ValueTask task)
        {
            try
            {
                await task;
            }
            catch( OperationCanceledException )
            {
            }
            catch( Exception e )
            {
                Debug.LogException(e);
            }
        }
        
        /// <summary>
        /// Wraps the valuetask in try/catch with UnityEngine.Debug.LogException
        /// </summary>
        /// <param name="task"></param>
        public async static void ForgetSafe(this Awaitable task)
        {
            try
            {
                await task;
            }
            catch( OperationCanceledException )
            {
            }
            catch( Exception e )
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Awaits the task, but throws OperationCanceledException if the token is canceled first.
        /// </summary>
        public async static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if(task.IsCompleted)
                return await task; // fast path

            // Create a Task that completes when the token is canceled
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(task, tcs.Task);

                if(completed == tcs.Task)
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
            if(task.IsCompleted)
            {
                await task; // fast path
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(task, tcs.Task);

                if(completed == tcs.Task)
                    throw new OperationCanceledException(cancellationToken);

                await task; // propagate exceptions
            }
        }

        public async static ValueTask AwaitDestructionAsync(this MonoBehaviour component, CancellationToken ct = default)
        {
            if(!component)
                return;
            
            AwaitableCompletionSource destroyed = new();

            await using (ct.Register(() => destroyed.TrySetCanceled()))
            await using (component.destroyCancellationToken.Register(() => destroyed.TrySetResult()))
            {
                await destroyed.Awaitable;
            }
        }
        
        /// <summary>
        /// Runs <paramref name="action"/> when the awaitable is done, in the background (an exception is logged).
        /// With a <paramref name="ct"/>, the action is dropped if it was canceled by the time the awaitable resumed: an awaitable that
        /// completed in the same frame as the cancellation still resumes normally, so its own token is not enough to keep the action
        /// from running on things that are gone.
        /// </summary>
        public static void Then(this Awaitable awaitable, Action action, CancellationToken ct = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            ThenAsync(awaitable, action, ct).ForgetSafe();

            async static Awaitable ThenAsync(Awaitable when, Action what, CancellationToken token)
            {
                await when;
                token.ThrowIfCancellationRequested();
                what();
            }
        }

    }
}
