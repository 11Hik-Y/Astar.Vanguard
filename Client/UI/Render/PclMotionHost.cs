using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astar.Vanguard.Client.UI.Render
{
    /// <summary>
    /// Page-scoped keyed motion host. Starting a motion with the same key interrupts
    /// the previous transition, matching the interruption semantics used by PCL.
    /// Motions requested while the host is inactive are deferred until OnEnable so
    /// Astar UI can build a screen before activating its hierarchy.
    /// </summary>
    internal sealed class PclMotionHost : MonoBehaviour
    {
        private sealed class MotionRequest
        {
            public string Key { get; init; }
            public int Version { get; init; }
            public float Duration { get; init; }
            public float Delay { get; init; }
            public Action<float> Update { get; init; }
            public PclEase Ease { get; init; }
            public Action Completed { get; init; }
        }

        private readonly Dictionary<string, Coroutine> _motions = new();
        private readonly Dictionary<string, MotionRequest> _pending = new();
        private readonly Dictionary<string, int> _versions = new();
        private int _nextVersion;

        public void Tween(
            string key,
            float duration,
            Action<float> update,
            PclEase ease = PclEase.OutFluent,
            float delay = 0f,
            Action completed = null,
            bool deferUntilActive = false
        )
        {
            if (string.IsNullOrWhiteSpace(key) || update is null)
            {
                return;
            }

            StopMotion(key);

            var request = new MotionRequest
            {
                Key = key,
                Version = ++_nextVersion,
                Duration = Mathf.Max(0.001f, duration),
                Delay = Mathf.Max(0f, delay),
                Update = update,
                Ease = ease,
                Completed = completed,
            };

            _versions[key] = request.Version;

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                if (deferUntilActive)
                {
                    _pending[key] = request;
                    return;
                }

                update(PclEasing.Evaluate(ease, 1f));
                completed?.Invoke();
                _versions.Remove(key);
                return;
            }

            StartMotion(request);
        }

        public void StopMotion(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (_motions.TryGetValue(key, out var coroutine) && coroutine is not null)
            {
                StopCoroutine(coroutine);
            }

            _motions.Remove(key);
            _pending.Remove(key);
            _versions.Remove(key);
        }

        public void StopAllMotions()
        {
            foreach (var coroutine in _motions.Values)
            {
                if (coroutine is not null)
                {
                    StopCoroutine(coroutine);
                }
            }

            _motions.Clear();
            _pending.Clear();
            _versions.Clear();
        }

        private void StartMotion(MotionRequest request)
        {
            if (request is null)
            {
                return;
            }

            if (!_versions.TryGetValue(request.Key, out var currentVersion)
                || currentVersion != request.Version)
            {
                return;
            }

            var coroutine = StartCoroutine(
                RunTween(
                    request.Key,
                    request.Version,
                    request.Duration,
                    request.Delay,
                    request.Update,
                    request.Ease,
                    request.Completed
                )
            );
            _motions[request.Key] = coroutine;
        }

        private IEnumerator RunTween(
            string key,
            int version,
            float duration,
            float delay,
            Action<float> update,
            PclEase ease,
            Action completed
        )
        {
            if (delay > 0f)
            {
                var waited = 0f;
                while (waited < delay)
                {
                    if (!IsCurrent(key, version))
                    {
                        yield break;
                    }

                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            var elapsed = 0f;
            update(PclEasing.Evaluate(ease, 0f));
            while (elapsed < duration)
            {
                if (!IsCurrent(key, version))
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                update(PclEasing.Evaluate(ease, progress));
                yield return null;
            }

            if (!IsCurrent(key, version))
            {
                yield break;
            }

            update(PclEasing.Evaluate(ease, 1f));
            completed?.Invoke();

            if (IsCurrent(key, version))
            {
                _versions.Remove(key);
                _motions.Remove(key);
            }
        }

        private bool IsCurrent(string key, int version)
        {
            return _versions.TryGetValue(key, out var currentVersion)
                && currentVersion == version;
        }

        private void OnEnable()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            var pending = new List<MotionRequest>(_pending.Values);
            _pending.Clear();
            foreach (var request in pending)
            {
                StartMotion(request);
            }
        }

        private void OnDisable()
        {
            if (_motions.Count == 0)
            {
                return;
            }

            var runningKeys = new List<string>(_motions.Keys);
            foreach (var coroutine in _motions.Values)
            {
                if (coroutine is not null)
                {
                    StopCoroutine(coroutine);
                }
            }

            _motions.Clear();
            foreach (var key in runningKeys)
            {
                _versions.Remove(key);
            }
        }
    }
}
