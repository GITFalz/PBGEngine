using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PBG.Threads
{
    public static class TaskPool
    {
        public static readonly int MAX_THREAD_COUNT = Environment.ProcessorCount;
        public static readonly int MIN_THREAD_COUNT = 2;
        public static int ThreadCount { get; private set; } = 6;
        public static int CurrentProcessingThreads = 0;
        public static int AvailableProcesses => ThreadCount - CurrentProcessingThreads;
        public static int QueueCount => _actions.Count;

        public static int MaxGlobalThreads = 1;

        private static PriorityQueue _actions = new PriorityQueue();

        private static List<Task?> Pool = [null, null, null, null, null, null];
        private static ConcurrentQueue<Action> OnCompleteActions = [];

        static TaskPool()
        {
            SetThreadCount(ThreadCount);
        }

        public static void SetThreadCount(int count)
        {
            ThreadCount = Math.Clamp(count, MIN_THREAD_COUNT, MAX_THREAD_COUNT);
            Pool.Clear();
            for (int i = 0; i < ThreadCount; i++)
            {
                Pool.Add(null);
            }
            MaxGlobalThreads = ThreadCount >> 1;
        }

        public static void QueueAction(Func<bool> action, TaskPriority priority = TaskPriority.Normal)
        {
            ActionProcess actionProcess = new ActionProcess(action);
            actionProcess.Status = ThreadProcessStatus.Pending;
            actionProcess.SetPriority(priority);
            _actions.Enqueue(actionProcess);
        }

        public static void QueueAction(Func<bool> action, out ThreadProcess threadProcess, TaskPriority priority = TaskPriority.Normal)
        {
            ActionProcess actionProcess = new ActionProcess(action);
            actionProcess.Status = ThreadProcessStatus.Pending;
            threadProcess = actionProcess;
            actionProcess.SetPriority(priority);
            _actions.Enqueue(actionProcess);
        }

        public static void QueueAction(ThreadProcess process, TaskPriority priority = TaskPriority.Normal)
        {
            process.Status = ThreadProcessStatus.Pending;
            process.SetPriority(priority);
            _actions.Enqueue(process);
        }

        public static bool TryRemoveProcess(ThreadProcess process)
        {
            if (!_actions.TryRemoveProcess(process))
                return false;

            process.Status = ThreadProcessStatus.Canceled;
            return true;
        }

        public static void Update()
        {
            try
            {
                ProcessTasks();
            }
            catch (NullReferenceException e)
            {
                throw new InvalidOperationException("ThreadPool encountered a null reference during update.", e);
            }
        }

        public static void Clear()
        {
            Console.WriteLine("Clearing ThreadPool with " + _actions.Count + " queued actions.");
            _actions.Clear();
        }

        private static void ProcessTasks()
        {
            for (int i = 0; i < ThreadCount; i++)
            {
                var poolTask = Pool[i];
                if (poolTask == null || (poolTask != null && poolTask.IsCompleted))
                {
                    if (_actions.TryDequeue(out var process))
                    {
                        process.SetThreadIndex(i);
                        var task = process.ExecuteAsync().ContinueWith(t =>
                        {
                            OnCompleteActions.Enqueue(() =>
                            {
                                process.OnComplete();
                            });

                            Pool[process.ThreadIndex] = null;
                            Interlocked.Decrement(ref CurrentProcessingThreads);
                        });

                        Pool[i] = task;
                        Interlocked.Increment(ref CurrentProcessingThreads);
                    }
                }
            }

            while (OnCompleteActions.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }

        private class ActionProcess : ThreadProcess
        {
            private readonly Func<bool> _action;
            public ActionProcess(Func<bool> action)
            {
                _action = action;
            }
            public override bool Function()
            {
                return _action.Invoke();
            }
        }

        private class PriorityQueue 
        {
            public int Count => _urgentPriorityActions.Count + _highPriorityActions.Count + _normalPriorityActions.Count + _lowPriorityActions.Count + _backgroundPriorityActions.Count;
            
            public List<ThreadProcess> _urgentPriorityActions = [];
            public List<ThreadProcess> _highPriorityActions = [];
            public List<ThreadProcess> _normalPriorityActions = [];
            public List<ThreadProcess> _lowPriorityActions = [];
            public List<ThreadProcess> _backgroundPriorityActions = [];

            public void Enqueue(ThreadProcess process)
            {
                switch (process.Priority)
                {
                    case TaskPriority.Background:
                        _backgroundPriorityActions.Add(process);
                        break;
                    case TaskPriority.Low:
                        _lowPriorityActions.Add(process);
                        break;
                    case TaskPriority.Normal:
                        _normalPriorityActions.Add(process);
                        break;
                    case TaskPriority.High:
                        _highPriorityActions.Add(process);
                        break;
                    case TaskPriority.Urgent:
                        _urgentPriorityActions.Add(process);
                        break;
                }
            }

            public bool TryDequeue([NotNullWhen(true)] out ThreadProcess? process)
            {
                process = null;
                if (_urgentPriorityActions.Count > 0)
                {
                    process = _urgentPriorityActions[0];
                    _urgentPriorityActions.RemoveAt(0);
                    return true;
                }
                else if (_highPriorityActions.Count > 0)
                {
                    process = _highPriorityActions[0];
                    _highPriorityActions.RemoveAt(0);
                    return true;
                }
                else if (_normalPriorityActions.Count > 0)
                {
                    process = _normalPriorityActions[0];
                    _normalPriorityActions.RemoveAt(0);
                    return true;
                }
                else if (_lowPriorityActions.Count > 0)
                {
                    process = _lowPriorityActions[0];
                    _lowPriorityActions.RemoveAt(0);
                    return true;
                }
                else if (_backgroundPriorityActions.Count > 0)
                {
                    process = _backgroundPriorityActions[0];
                    _backgroundPriorityActions.RemoveAt(0);
                    return true;
                }

                return false;
            }

            public bool TryRemoveProcess(ThreadProcess process)
            {
                switch (process.Priority)
                {
                    case TaskPriority.Background:
                        return _backgroundPriorityActions.Remove(process);
                    case TaskPriority.Low:
                        return _lowPriorityActions.Remove(process);
                    case TaskPriority.Normal:
                        return _normalPriorityActions.Remove(process);
                    case TaskPriority.High:
                        return _highPriorityActions.Remove(process);
                    case TaskPriority.Urgent:
                        return _urgentPriorityActions.Remove(process);
                }
                return false;
            }

            public void Clear()
            {
                Console.WriteLine("Clearing PriorityQueue with " + Count + " queued actions.");
                _urgentPriorityActions = [];
                _highPriorityActions = [];
                _normalPriorityActions = [];
                _lowPriorityActions = [];
                _backgroundPriorityActions = [];
            }
        }

        public static void Print()
        {
            Console.WriteLine($"ThreadPool: {ThreadCount} threads, {_actions.Count} actions queued.");
            Console.WriteLine($"Urgent: {_actions._urgentPriorityActions.Count}, High: {_actions._highPriorityActions.Count}, Normal: {_actions._normalPriorityActions.Count}, Low: {_actions._lowPriorityActions.Count}, Background: {_actions._backgroundPriorityActions.Count}");
        }
    }

    public class CustomProcess
    {
        protected Action OnCompleteAction;

        public CustomProcess()
        {
            OnCompleteAction = OnCompleteBase;
        }

        public virtual bool Function() { return true; }
        public virtual void OnCompleteBase() { }

        public void OnComplete() { OnCompleteAction.Invoke(); }
        public void SetOnCompleteAction(Action action) { OnCompleteAction = action; }
    }

    public class ThreadProcess : CustomProcess
    {
        public int ThreadIndex { get; private set; } = 0;
        public TaskPriority Priority { get; private set; } = TaskPriority.Normal;
        public ThreadProcessStatus Status { get; set; } = ThreadProcessStatus.NotStarted;
        public bool Succeded => Status == ThreadProcessStatus.Completed;
        public bool Failed => Status == ThreadProcessStatus.Failed || Status == ThreadProcessStatus.Canceled;
        public bool IsPending => Status == ThreadProcessStatus.Pending;
        public bool IsRunning => Status == ThreadProcessStatus.Running;

        public ThreadProcess() : base() { }

        public void SetThreadIndex(int index) => ThreadIndex = index;
        public void SetPriority(TaskPriority priority) => Priority = priority;

        public Task ExecuteAsync()
        {
            return Task.Run(() =>
            {
                Status = ThreadProcessStatus.Running;
                bool succes = Function();
                if (succes)
                {
                    Status = ThreadProcessStatus.Completed;
                }
                else
                {
                    Status = ThreadProcessStatus.Failed;
                }
            });
        }

        public bool TryRemoveProcess()
        {
            return TaskPool.TryRemoveProcess(this);
        }

        public virtual void Break()
        {
            Status = ThreadProcessStatus.Canceled;
        }
    }

    public enum ThreadProcessStatus
    {
        NotStarted,
        Pending,
        Running,
        Completed,
        Failed,
        Canceled,
    }

    public enum TaskPriority
    {
        Background,
        Low,
        Normal,
        High,
        Urgent,
    }
}