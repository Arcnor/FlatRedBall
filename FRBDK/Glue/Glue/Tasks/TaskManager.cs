﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Tasks;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Managers
{
    #region Enums

    public enum TaskExecutionPreference
    {
        AddOrMoveToEnd,
        Fifo,
        Asap
    }

    public enum TaskEvent
    {
        Created,
        Queued,
        Started,
        Removed
    }

    #endregion

    public class TaskManager : Singleton<TaskManager>
    {
        #region Fields
        int asyncTasks;

        List<GlueTaskBase> mActiveAsyncTasks = new List<GlueTaskBase>();

        const int maxTasksInHistory = 121;
        List<string> taskHistory = new List<string>();

        public int? SyncTaskThreadId { get; private set; }


        readonly BlockingCollection<KeyValuePair<int, GlueTaskBase>> taskQueue = new BlockingCollection<KeyValuePair<int, GlueTaskBase>>(new ConcurrentPriorityQueue<int, GlueTaskBase>());
        public TaskManager()
        {
            new Thread(Loop)
            {
                IsBackground = true
            }.Start();
        }

        const string RestartTaskDisplay = "Restarting due to Glue or file change";
        public bool HasRestartTask => taskQueue.Any(item => item.Value.DisplayInfo == RestartTaskDisplay);


        async void Loop()
        {
            SyncTaskThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            foreach (var item in taskQueue.GetConsumingEnumerable())
            {
                if(isTaskProcessingEnabled)
                {
                    await item.Value.DoAction();
                }
                else
                {
                    AddInternal(item.Value.DisplayInfo, item.Value);
                    System.Threading.Thread.Sleep(50);
                }
            }
        }

        int Load(string filename, int priority)
        {
            Thread.Sleep(1000);
            return priority;
        }

        public void Dispose()
        {
            taskQueue.CompleteAdding();
            taskQueue.Dispose();
        }





        #endregion

        public event Action<TaskEvent, GlueTaskBase> TaskAddedOrRemoved;

        #region Properties

        public int SyncTaskTasks
        {
            get
            {
                return taskQueue.Count;
            }
        }

        public bool AreAllAsyncTasksDone => TaskCount == 0;

        public int TaskCount
        {
            get
            {
                lock (mActiveAsyncTasks)
                {
                    return mActiveAsyncTasks.Count + asyncTasks + taskQueue.Count;
                }
            }
        }

        public string CurrentTask
        {
            get
            {
                string toReturn = "";

                if(IsTaskProcessingEnabled == false)
                {
                    toReturn += "Task processing disabled, next task when re-enabled:\n";
                }

                if (mActiveAsyncTasks.Count != 0)
                {
                    // This could update while we're looping. We don't want to throw errors, don't want to lock anything, 
                    // so just handle it with a try catch:
                    try
                    {
                        foreach(var item in mActiveAsyncTasks)
                        {
                            toReturn += item.DisplayInfo + "\n";
                        }
                    }
                    catch
                    {
                        // do nothing
                    }

                }

                if (taskQueue.Count != 0)
                {
                    try
                    {
                        toReturn += taskQueue.FirstOrDefault().Value?.DisplayInfo;
                    }
                    catch
                    {
                        // do nothing
                    }
                }


                return toReturn;
            }
        }

        bool isTaskProcessingEnabled = true;
        /// <summary>
        /// Whether to process tasks - if this is false, then tasks will not be processed.
        /// </summary>
        public bool IsTaskProcessingEnabled
        {
            get { return isTaskProcessingEnabled; }
            set
            {
                bool turnedOn = value == true && isTaskProcessingEnabled == false;
                isTaskProcessingEnabled = value;
                //if(turnedOn)
                //{
                //    ProcessNextSync();

                //}
            }
        }

        #endregion

        #region Methods


        /// <summary>
        /// Adds a task which can execute simultaneously with other tasks
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="details">The details of the task, to be displayed in the tasks window.</param>
        [Obsolete]
        public void AddParallelTask(Action action, string details)
        {

            ThreadPool.QueueUserWorkItem(
                (arg)=>ExecuteActionSync(action, details));
        }

        void ExecuteActionSync(Action action, string details)
        {
            var glueTask = new GlueTask
            {
                Action = action,
                DisplayInfo = details
            };

            lock (mActiveAsyncTasks)
            {
                mActiveAsyncTasks.Add(glueTask);
            }

            TaskAddedOrRemoved?.Invoke(TaskEvent.Queued, glueTask);

            ((Action)action)();

            lock (mActiveAsyncTasks)
            {
                mActiveAsyncTasks.Remove(glueTask);
            }
            asyncTasks--;

            // not sure why but this can go into the negative...
            asyncTasks = System.Math.Max(asyncTasks, 0);

            TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, glueTask);
        }


        public async Task<bool> WaitForAllTasksFinished()
        {
            var didWait = false;
            while (!AreAllAsyncTasksDone)
            {
                didWait = true;
                await Task.Delay(200);
            }
            return didWait;
        }


        [Obsolete("Use Add, which allows specifying the priority")]
        /// <summary>
        /// Adds an action to be executed, guaranteeing that no other actions will be executed at the same time as this.
        /// Actions added will be executed in the order they were added (fifo).
        /// </summary>
        public void AddSync(Action action, string displayInfo) => Add(action, displayInfo);

        public GlueTask Add(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = new GlueTask();
            glueTask.Action = action;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            AddInternal(displayInfo, glueTask);
            return glueTask;
        }

        public GlueTask<T> Add<T>(Func<T> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = new GlueTask<T>();
            glueTask.Func = func;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            AddInternal(displayInfo, glueTask);
            return glueTask;
        }

        public GlueAsyncTask Add(Func<Task> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = new GlueAsyncTask();
            glueTask.Func = func;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            AddInternal(displayInfo, glueTask);
            return glueTask;
        }

        /// <summary>
        /// Adds an action to the TaskManager to be executed according to the argument TaskExecutionPreference. If the 
        /// callstack is already part of a task, then the action is executed immediately. The returned task will complete
        /// when the argument Action has completed.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="displayInfo">The display info to show in the task manager tab</param>
        /// <param name="executionPreference">When to execute the action.</param>
        /// <param name="doOnUiThread">Whether the action must be performed on the UI thread.</param>
        /// <returns>A task which will complete once the action has finished executing.</returns>
        public Task AddAsync(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = AddOrRunIfTasked(action, displayInfo, executionPreference, doOnUiThread);
            return WaitForTaskToFinish(glueTask);
        }

        public async Task<T> AddAsync<T>(Func<T> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = AddOrRunIfTasked(func, displayInfo, executionPreference, doOnUiThread);
            return await WaitForTaskToFinish(glueTask);
        }

        public async Task AddAsync(Func<Task> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = await AddOrRunIfTasked(func, displayInfo, executionPreference, doOnUiThread);
            await WaitForTaskToFinish(glueTask);
        }


        public async Task WaitForTaskToFinish(GlueTaskBase glueTask)
        {
            if(glueTask == null)
            {
                return;
            }
            else
            {
                bool IsTaskDone()
                {
                    lock(mActiveAsyncTasks)
                    {
                        if(taskQueue.Any(item => item.Value == glueTask) || mActiveAsyncTasks.Contains(glueTask))
                        {
                            return false;
                        }

                        return true;
                    }
                }

                while(!IsTaskDone())
                {
                    const int waitDelay = 30;
                    await Task.Delay(waitDelay);
                }
            }
        }

        public async Task<T> WaitForTaskToFinish<T>(GlueTask<T> glueTask)
        {
            if (glueTask == null)
            {
                return default(T);
            }
            else
            {
                bool IsTaskDone()
                {
                    lock (mActiveAsyncTasks)
                    {
                        if (taskQueue.Any(item => item.Value == glueTask) || mActiveAsyncTasks.Contains(glueTask))
                        {
                            return false;
                        }

                        return true;
                    }
                }

                while (!IsTaskDone())
                {
                    await Task.Delay(150);
                }
                return (T)glueTask.Result;
            }
        }

        private void AddInternal(string displayInfo, GlueTaskBase glueTask)
        {
            glueTask.DisplayInfo = displayInfo;

            taskQueue.Add(new KeyValuePair<int, GlueTaskBase>((int)glueTask.TaskExecutionPreference, glueTask));


            //lock (mSyncLockObject)
            //{
            //    if (glueTask.TaskExecutionPreference == TaskExecutionPreference.Asap)
            //    {
            //        if (mSyncedActions.Count > 0)
            //        {
            //            // don't insert at 0, finish the current task, but insert at 1:
            //            var wasAdded = false;
            //            for(int i = 1; i < mSyncedActions.Count; i++)
            //            {
            //                if(mSyncedActions[i].TaskExecutionPreference != TaskExecutionPreference.Asap)
            //                {
            //                    mSyncedActions.Insert(i, glueTask);
            //                    wasAdded = true;
            //                    break;
            //                }
            //            }

            //            if(!wasAdded)
            //            {
            //                mSyncedActions.Add(glueTask);
            //            }
            //        }
            //        else
            //        {
            //            mSyncedActions.Add(glueTask);
            //        }
            //    }
            //    else if (glueTask.TaskExecutionPreference == TaskExecutionPreference.AddOrMoveToEnd)
            //    {
            //        // There's a few possible situations:
            //        // 1. This task is not present in the list at all. 
            //        //    - In this case, add it at the end
            //        // 2. This task is present in the list and is not currently executing.
            //        //    - In this case, remove the item from the list, and re-add it at the end
            //        // 3. This task is present in the list, but it is the first entry, which means it's probably currently running
            //        //    a. If this is the only item in the list, then do nothing since it's already executing
            //        //    b. If it's the first item in the list, but there are other actions that will execute...
            //        //       i. If there are no other entries in the list that match this, add this at the end
            //        //       ii. If there are other entries in the list (besides the first) remove those, add at the end
            //        var existingAction = mSyncedActions.FirstOrDefault(item =>
            //            item.DisplayInfo == displayInfo);

            //        GlueTaskBase actionToRemove = null;

            //        var shouldAddAtEnd = false;
            //        if (existingAction == null)
            //        {
            //            // This is #1 from above
            //            actionToRemove = null;
            //            shouldAddAtEnd = true;
            //        }
            //        else
            //        {
            //            if (existingAction != mSyncedActions[0])
            //            {
            //                // This is #2
            //                actionToRemove = existingAction;
            //                shouldAddAtEnd = true;
            //            }
            //            else if (mSyncedActions.Count == 1)
            //            {
            //                // this is #3a
            //                actionToRemove = null;
            //                shouldAddAtEnd = false;
            //            }
            //            else
            //            {
            //                // this is #3b

            //                // see if it's (a) or (b) from above
            //                var existingActionLaterInList = mSyncedActions.Skip(1).FirstOrDefault(item =>
            //                    item.DisplayInfo == displayInfo);
            //                if (existingActionLaterInList == null)
            //                {
            //                    // #3b i
            //                    shouldAddAtEnd = true;
            //                    actionToRemove = null;
            //                }
            //                else
            //                {
            //                    // #3b ii
            //                    shouldAddAtEnd = true;
            //                    actionToRemove = existingActionLaterInList;
            //                }
            //            }
            //        }

            //        if (actionToRemove != null)
            //        {
            //            mSyncedActions.Remove(actionToRemove);
            //        }
            //        if (shouldAddAtEnd)
            //        {
            //            mSyncedActions.Add(glueTask);
            //        }

            //        createdNew = (shouldAddAtEnd && actionToRemove == null);

            //    }
            //    else
            //    {
            //        mSyncedActions.Add(glueTask);
            //    }
            //    shouldProcess = createdNew && mSyncedActions.Count == 1 && IsTaskProcessingEnabled;
            //}
            //// process will take care of reporting it
            //if (createdNew)
            //{
            //    TaskAddedOrRemoved?.Invoke(TaskEvent.Created, glueTask);
            //}

            //if (shouldProcess)
            //{
            //    ProcessNextSync();
            //}
        }

        public void RecordTaskHistory(string taskDisplayInfo)
        {
            var projectName = GlueState.Self.CurrentMainProject?.FullFileName;
            
            var taskDetail = $"{DateTime.Now.ToString("hh:mm:ss tt")} {projectName} {taskDisplayInfo}";
            taskHistory.Add(taskDetail);


            while (taskHistory.Count > maxTasksInHistory)
            {
                taskHistory.RemoveAt(0);
            }
        }

        public void OnUiThread(Func<Task> action)
        {
            if(IsOnUiThread)
            {
                action.Invoke().Wait();
            }
            else
            {
                global::Glue.MainGlueWindow.Self.Invoke(() => action.Invoke().Wait());
            }
        }

        public void OnUiThread(Action action)
        {
            if (IsOnUiThread)
            {
                action();
            }
            else
            {
                global::Glue.MainGlueWindow.Self.Invoke(action);
            }
        }

        public bool IsOnUiThread => System.Threading.Thread.CurrentThread.ManagedThreadId == global::Glue.MainGlueWindow.UiThreadId;

        public bool IsInTask()
        {
            if(System.Threading.Thread.CurrentThread.ManagedThreadId == TaskManager.Self.SyncTaskThreadId)
            {
                return true;
            }

            var stackTrace = new System.Diagnostics.StackTrace();
            for(int i = stackTrace.FrameCount - 1; i > -1; i--)
            {
                var frame = stackTrace.GetFrame(i);
                var frameText = frame.ToString();
                if(frameText.StartsWith("RunOnUiThreadTasked"))
                {
                    return true;
                }
            }
            return false;
        }

        public GlueTask AddOrRunIfTasked(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            if (IsInTask())
            {
                // we're in a task:
                var task =  new GlueTask()
                {
                    DisplayInfo = displayInfo,
                    Action = action,
                    TaskExecutionPreference = executionPreference,
                    DoOnUiThread = doOnUiThread
                };
                task.DoAction();

                return task;
            }
            else
            {
                return TaskManager.Self.Add(action, displayInfo, executionPreference, doOnUiThread);
            }
        }

        public async Task<GlueAsyncTask> AddOrRunIfTasked(Func<Task> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            if (IsInTask())
            {
                // we're in a task:
                var task = new GlueAsyncTask()
                {
                    DisplayInfo = displayInfo,
                    Func = func,
                    TaskExecutionPreference = executionPreference,
                    DoOnUiThread = doOnUiThread
                };
                await task.DoAction();

                return task;
            }
            else
            {
                return TaskManager.Self.Add(func, displayInfo, executionPreference, doOnUiThread);
            }
        }

        public GlueTask<T> AddOrRunIfTasked<T>(Func<T> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            if (IsInTask())
            {
                // we're in a task:
                var task = new GlueTask<T>()
                {
                    DisplayInfo = displayInfo,
                    Func = func,
                    TaskExecutionPreference = executionPreference,
                    DoOnUiThread = doOnUiThread,
                };

                task.DoAction();

                return task;
            }
            else
            {
                return TaskManager.Self.Add(func, displayInfo, executionPreference, doOnUiThread);
            }
        }

        public void WarnIfNotInTask()
        {
            if(!IsInTask())
            {
                var stackTrace = Environment.StackTrace;

                GlueCommands.Self.DoOnUiThread(() => GlueCommands.Self.PrintOutput("Code not in task:\n" + stackTrace));
            }
        }

        #endregion
    }

}
