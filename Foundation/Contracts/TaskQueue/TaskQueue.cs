namespace SecondMonitor.Contracts.TaskQueue
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    using SecondMonitor.Contracts.Extensions;

    public class TaskQueue
    {
        private readonly bool _allowExecutionOnDifferentThread;
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
        private readonly object _lockObject;
        private long _taskCount;

        private TaskCompletionSource<object> _queueCompletionSource;

        private bool _acceptingTasks = true;

        public TaskQueue(bool allowExecutionOnDifferentThread)
        {
            _allowExecutionOnDifferentThread = allowExecutionOnDifferentThread;
            _queueCompletionSource = new TaskCompletionSource<object>();
            _queueCompletionSource.SetResult(new object());
            _lockObject = new object();
        }

        public void EnqueueTask(Func<Task> taskFunc)
        {
            _log.Trace("Enqueue task Start");
            if (_acceptingTasks)
            {
                lock (_lockObject)
                {
                    bool queueWasEmpty = _queue.Count == 0;

                    _queue.Enqueue(taskFunc);

                    if (queueWasEmpty)
                    {
                        _queueCompletionSource = new TaskCompletionSource<object>();
                        AwaitCurrentAndExecuteAsync().FireAndForget();
                    }
                }
            }

            _log.Trace("Enqueue task End");
        }

        private async Task AwaitCurrentAndExecuteAsync()
        {
            while (_queue.Count > 0)
            {
                try
                {
                    Func<Task> nextTaskFunc = _queue.Peek();
                    _log.Trace($"Starting Task Execution, {_taskCount}, {Thread.CurrentThread.ManagedThreadId}");
                    await nextTaskFunc().ConfigureAwait(!_allowExecutionOnDifferentThread);
                    _log.Trace($"Ending Task Execution, {_taskCount}, {Thread.CurrentThread.ManagedThreadId}");
                    unchecked
                    {
                        _taskCount++;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _log.Debug(ex, "Task canceled in a queue");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Task in a queue threw an exception");
                }
                finally
                {
                    _queue.Dequeue();
                }
            }

            _queueCompletionSource.SetResult(new object());
        }

        public async Task FinishAsync()
        {
            _acceptingTasks = false;
            await _queueCompletionSource.Task;
        }
    }
}