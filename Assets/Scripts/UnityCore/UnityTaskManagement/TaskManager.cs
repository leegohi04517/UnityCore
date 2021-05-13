using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCore.UnityTaskManagement
{
    public class TaskManager : MonoBehaviour
    {
        public class TaskState
        {
            public delegate void FinishedHandler(bool manual);

            private readonly IEnumerator _coroutine;

            private Coroutine _runningCoroutine;

            private bool _running;

            private bool _paused;

            private bool _forceStopped;

            private readonly TaskGroup _owner;

            public bool Running
            {
                get { return _running; }
            }

            public bool Paused
            {
                get { return _paused; }
            }

            public event FinishedHandler Finished;

            public TaskState(IEnumerator c, TaskGroup coroutineOwner)
            {
                _coroutine = c;
                _owner = coroutineOwner;
            }

            public void Pause()
            {
                _paused = true;
            }

            public void Resume()
            {
                _paused = false;
            }

            public Coroutine Start()
            {
                if (TaskManager.Paused)
                {
                    return _runningCoroutine;
                }

                _running = true;
                _forceStopped = false;
                _runningCoroutine = _owner.StartCoroutine(CallWrapper());
                return _runningCoroutine;
            }

            public void Stop()
            {
                if (_runningCoroutine != null)
                    _owner.StopCoroutine(_runningCoroutine);
                _forceStopped = true;
                _running = false;
            }

            private IEnumerator CallWrapper()
            {
                while (_running)
                {
                    if (_paused)
                    {
                        yield return null;
                    }

                    yield return _coroutine;
                    _running = false;
                }

                Finished?.Invoke(_forceStopped);
            }
        }

        private static TaskManager _instance;

        private static Dictionary<string, TaskGroup> _createdGroups = new Dictionary<string, TaskGroup>();

        public const string DEFAULT_GROUP_ID = "DEFAULT_GROUP";

        public static bool Paused { get; set; }

        private static void Prepare()
        {
            if (null == _instance)
            {
                GameObject gameObject = new GameObject("TaskManager");
                _instance = gameObject.AddComponent<TaskManager>();

                var defaultGroupObj = gameObject.AddComponent<TaskGroup>();
                defaultGroupObj.ID = DEFAULT_GROUP_ID;
                _createdGroups.Add(DEFAULT_GROUP_ID, defaultGroupObj);
                DontDestroyOnLoad(gameObject);
            }
        }


        public static void Reset()
        {
            foreach (string key in _createdGroups.Keys)
            {
                StopAllTasksInGroup(key);
            }

            _createdGroups.Clear();

            if (null != _instance)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        public static TaskState CreateTask(IEnumerator coroutine)
        {
            Prepare();
            return new TaskState(coroutine, _createdGroups[DEFAULT_GROUP_ID]);
        }

        public static TaskState CreateTask(IEnumerator coroutine, string groupID)
        {
            Prepare();
            if (!_createdGroups.TryGetValue(groupID, out var value))
            {
                value = _instance.gameObject.AddComponent<TaskGroup>();
                value.ID = groupID;
                _createdGroups.Add(groupID, value);
            }

            return new TaskState(coroutine, value);
        }

        public static void StopAllTasksInGroup(string groupID = DEFAULT_GROUP_ID)
        {
            if (_createdGroups.TryGetValue(groupID, out var value))
            {
                value.StopAllCoroutines();
            }
        }
    }
}