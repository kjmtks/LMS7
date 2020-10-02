using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections;


namespace ALMS.App.Models
{
    public class QueuedHostedBackgroundJobService : BackgroundService
    {
        private IBackgroundTaskQueueSet TaskQueue;
        public QueuedHostedBackgroundJobService(IBackgroundTaskQueueSet taskQueue)
        {
            TaskQueue = taskQueue;
            TaskQueue.SetQueues(5, 3);
        }
        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await TaskQueue.ExecAsync(cancellationToken);
        }
    }

    public interface IBackgroundTaskQueueSet
    {
        public IEnumerable<BackgroundTaskQueue> GetQueues();
        public IEnumerable<BackgroundTaskQueue> GetPrioritiedQueues();

        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, bool prioritied = false);
        Task ExecAsync(CancellationToken cancellationToken);
        void SetQueues(int number_of_queue, int number_of_prioritied_queues);
    }

    public class BackgroundTaskQueueSet : IBackgroundTaskQueueSet
    {
        private BackgroundTaskQueue[] queues = new BackgroundTaskQueue[0];
        private BackgroundTaskQueue[] prioritied_queues = new BackgroundTaskQueue[0];


        public IEnumerable<BackgroundTaskQueue> GetQueues()
        {
            return queues;
        }

        public IEnumerable<BackgroundTaskQueue> GetPrioritiedQueues()
        {
            return prioritied_queues;
        }


        public void SetQueues(int number_of_queue, int number_of_prioritied_queues)
        {
            queues = new BackgroundTaskQueue[number_of_queue];
            prioritied_queues = new BackgroundTaskQueue[number_of_prioritied_queues];
            for (var i = 0; i < number_of_queue; i++)
            {
                queues[i] = new BackgroundTaskQueue();
            }
            for (var i = 0; i < number_of_prioritied_queues; i++)
            {
                prioritied_queues[i] = new BackgroundTaskQueue();
            }
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, bool prioritied = false)
        {
            if (prioritied)
            {
                var queue = prioritied_queues.OrderBy(q => q.Count + (q.IsRunning ? 1 : 0)).First();
                queue.QueueBackgroundWorkItem(workItem);
            }
            else
            {
                var queue = queues.OrderBy(q => q.Count + (q.IsRunning ? 1 : 0)).First();
                queue.QueueBackgroundWorkItem(workItem);
            }
        }

        public async Task ExecAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                foreach (var queue in queues.Concat(prioritied_queues))
                {
                    Task.Run(async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var workItem = await queue.DequeueAsync(cancellationToken);
                            if (workItem != null)
                            {
                                queue.IsRunning = true;
                                await workItem(cancellationToken);
                                queue.IsRunning = false;
                            }
                        }
                    });
                }
            });
        }
    }






    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);

        int Count { get; }
        bool IsRunning { get; }
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<Func<CancellationToken, Task>> _workItems =
            new ConcurrentQueue<Func<CancellationToken, Task>>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public int Count { get { return _signal.CurrentCount; } }
        public bool IsRunning { get; set; } = false;
        public void QueueBackgroundWorkItem(
            Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}
