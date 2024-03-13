using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HttpListener
{
    public sealed class SafeThreadingComponent : MonoBehaviour
    {
        [SerializeField] private Phase phase;
        private readonly Queue<Action> _actions = new(8);
        private Thread _mainThread;

        private void Awake() => _mainThread = Thread.CurrentThread;

        public void ExecuteInMainThread(Action callback)
        {
            if (callback == null)
            {
                return;
            }

            if (Thread.CurrentThread == _mainThread)
            {
                // this IS already main thread, so no point waiting
                callback();
                return;
            }
            var tcs = new TaskCompletionSource<bool>();
            _actions.Enqueue(() =>
            {
                try
                {
                    callback();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    tcs.SetResult(false);
                }
            });

            tcs.Task.Wait();
        }

        private void Update()
        {
            if (phase == Phase.Update)
            {

                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    action();
                }
            }

        }

        private void LateUpdate()
        {
            if (phase == Phase.LateUpdate)
            {

                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    action();
                }
            }
        }

        private void FixedUpdate()
        {
            if (phase == Phase.FixedUpdate)
            {
                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    action();
                }
            }

        }

        public enum Phase
        {
            Update, LateUpdate, FixedUpdate
        }
    }
}