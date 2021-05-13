using System;
using System.Collections.Generic;
using UnityCore.UnitySingleton;
using UnityEngine;

namespace UnityCore.UnityService
{
    public class TimerService : MonoSingleton<TimerService>
    {
        private readonly SortedDictionary<DateTime, List<TimerTask>> _registeredEvents =
            new SortedDictionary<DateTime, List<TimerTask>>();

        private DateTime _currentLocalTime;
        private TimeSpan _serverOffset;
        private DateTime _lastSyncedServerTime = DateTime.MinValue;

        /// <summary>
        /// Usage: detect valid offline time to avoid time cheat 
        /// </summary>
        private DateTime _lastPauseTime = DateTime.MinValue;

        private static readonly TimeSpan MAX_PAUSE_TIME = new TimeSpan(24, 0, 0);
        private static readonly TimeSpan MIN_PAUSE_TIME = new TimeSpan(0, -5, 0);


        private readonly List<TimerTask> _finishedEvents = new List<TimerTask>();

        private static readonly int VALID_OFFSET_TIME_IN_SECONDS = 10 * 60;
        private static readonly int SYNC_SERVER_TIME_INTERVAL_IN_SECONDS = 5 * 60;
        public const int HourToSecond = 3600;

        public TimerService()
        {
            _currentLocalTime = DateTime.UtcNow;
        }

        public DateTime UtcNow
        {
            get { return _currentLocalTime - _serverOffset; }
        }

        public class TimerTask
        {
            private DateTime _initialTime;
            private DateTime _scheduledTime;
            private TimeSpan _curRemainingTime;
            private TimeSpan _initialRemainingTime;
            private bool _isFinished;
            private float _progress;
            private Action _timerScheduledCallback;
            private bool _repetitive;

            public DateTime ScheduledTime
            {
                get { return _scheduledTime; }
            }

            public DateTime InitialTime => _initialTime;

            public bool IsFinished
            {
                get { return _isFinished; }
            }

            public TimeSpan InitialRemainingTime
            {
                get { return _initialRemainingTime; }
            }

            public TimeSpan RemainingTime
            {
                get { return _curRemainingTime; }
            }

            public float ProgressPercent
            {
                get { return _progress; }
            }

            public bool IsRepetitive
            {
                get { return _repetitive; }
            }

            public Action TimerScheduledCallback
            {
                get { return _timerScheduledCallback; }
            }

            public TimerTask(DateTime initialTime, DateTime scheduledTime, Action timerScheduledCallback,
                bool repetitive)
            {
                Init(initialTime, scheduledTime, timerScheduledCallback, repetitive);
            }

            private void Init(DateTime initialTime, DateTime scheduledTime, Action timerScheduledCallback,
                bool repetitive)
            {
                _initialTime = initialTime;
                _initialRemainingTime = scheduledTime - initialTime;
                _scheduledTime = scheduledTime;
                _curRemainingTime = _initialRemainingTime;
                _timerScheduledCallback = timerScheduledCallback;
                _repetitive = repetitive;
            }

            public void ChangeTimer(DateTime initialTime, DateTime scheduledTime, Action timerScheduledCallback,
                bool repetitive)
            {
                Init(initialTime, scheduledTime, timerScheduledCallback, repetitive);
            }

            private DateTime _pauseTime = DateTime.MinValue;

            public void Pause(DateTime pauseTime)
            {
                _pauseTime = pauseTime;
            }

            public void Resume(DateTime resumeTime, out TimeSpan awayIntervalTime)
            {
                if (_pauseTime != DateTime.MinValue)
                {
                    awayIntervalTime = resumeTime - _pauseTime;
                }
                else
                {
                    awayIntervalTime = TimeSpan.Zero;
                }

                _pauseTime = DateTime.MinValue;
            }


            public void Update(DateTime nowTime)
            {
                if (_pauseTime != DateTime.MinValue)
                {
                    return;
                }

                _curRemainingTime = _scheduledTime - nowTime;
                if (_curRemainingTime.TotalSeconds <= 0.0)
                {
                    _isFinished = true;
                    _curRemainingTime = TimeSpan.Zero;
                    _progress = 1f;
                }
                else
                {
                    _isFinished = false;
                    _progress = Mathf.InverseLerp((float) _initialRemainingTime.TotalSeconds, 0f,
                        (float) _curRemainingTime.TotalSeconds);
                }
            }
        }

        public TimerTask AddTimerTask(DateTime initialTime, DateTime scheduledTime, Action timerScheduledCallback,
            bool repetitive)
        {
            var timerEvent = new TimerTask(initialTime, scheduledTime, timerScheduledCallback, repetitive);
            AddTimerEvent(timerEvent);
            return timerEvent;
        }

        public bool CancelTimerTask(TimerTask timerTask)
        {
            if (null != timerTask)
            {
                return RemoveTimerTask(timerTask);
            }

            return false;
        }

        public void PauseTimerTask(TimerTask timerTask)
        {
            if (null != timerTask && !timerTask.IsFinished)
            {
                timerTask.Pause(UtcNow);
            }
        }

        public void ResumeTimerTask(TimerTask timerTask)
        {
            if (null != timerTask && !timerTask.IsFinished)
            {
                timerTask.Resume(UtcNow, out var awayIntervalTime);
                if (awayIntervalTime > TimeSpan.Zero)
                {
                    ChangeTimerTask(timerTask, timerTask.InitialTime.Add(awayIntervalTime),
                        timerTask.ScheduledTime.Add(awayIntervalTime),
                        timerTask.TimerScheduledCallback, timerTask.IsRepetitive);
                }
            }
        }

        public void CancelAllTimerTask()
        {
            _registeredEvents.Clear();
        }

        private bool RemoveTimerTask(TimerTask timerTask)
        {
            bool result = false;
            List<TimerTask> value;
            if (_registeredEvents.TryGetValue(timerTask.ScheduledTime, out value))
            {
                result = value.Remove(timerTask);
                if (value.Count == 0)
                {
                    _registeredEvents.Remove(timerTask.ScheduledTime);
                }
            }

            return result;
        }

        public TimerTask ChangeTimerTask(TimerTask trigger, DateTime initialTime, DateTime scheduledTime,
            Action timerScheduledCallback,
            bool repetitive)
        {
            if (trigger != null)
            {
                RemoveTimerTask(trigger);
                trigger.ChangeTimer(initialTime, scheduledTime, timerScheduledCallback, repetitive);
                AddTimerEvent(trigger);
            }

            return trigger;
        }

        private void AddTimerEvent(TimerTask taskRequest)
        {
            List<TimerTask> value;
            if (_registeredEvents.TryGetValue(taskRequest.ScheduledTime, out value))
            {
                value.Add(taskRequest);
            }
            else
            {
                value = new List<TimerTask> {taskRequest};
                _registeredEvents.Add(taskRequest.ScheduledTime, value);
            }
        }

        private void Update()
        {
            _currentLocalTime = DateTime.UtcNow;
            ProcessPendingTimer();
        }


        private void Awake()
        {
            if (null != instance)
            {
                Destroy(gameObject);
            }
        }

        private void ProcessPendingTimer()
        {
            if (_registeredEvents.Count > 0)
            {
                foreach (var registeredEvent in _registeredEvents)
                {
                    foreach (var timerEvent in registeredEvent.Value)
                    {
                        timerEvent.Update(UtcNow);
                        if (timerEvent.IsFinished)
                        {
                            _finishedEvents.Add(timerEvent);
                        }
                    }
                }

                foreach (var finishedEvent in _finishedEvents)
                {
                    if (finishedEvent.IsRepetitive)
                    {
                        ChangeTimerTask(finishedEvent, finishedEvent.ScheduledTime,
                            finishedEvent.ScheduledTime + finishedEvent.InitialRemainingTime,
                            finishedEvent.TimerScheduledCallback, true);
                    }
                    else
                    {
                        RemoveTimerTask(finishedEvent);
                    }

                    if (finishedEvent.TimerScheduledCallback != null)
                    {
                        finishedEvent.TimerScheduledCallback();
                    }
                }

                _finishedEvents.Clear();
            }
        }
    }
}