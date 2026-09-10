using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astar.Vanguard.Client.UI.Render
{
    /// <summary>
    /// Lightweight uGUI opacity group that avoids a hard dependency on UnityEngine.UIModule's CanvasGroup.
    /// It snapshots child Graphic alpha values and restores them when opacity returns to 1.
    /// </summary>
    internal sealed class PclOpacityGroup : MonoBehaviour
    {
        private readonly List<Graphic> _graphics = new();
        private readonly List<float> _baseAlpha = new();
        private float _alpha = 1f;

        public float Alpha
        {
            get => _alpha;
            set
            {
                _alpha = Mathf.Clamp01(value);
                Apply();
            }
        }

        public void Refresh()
        {
            // When a page is fully faded out, its current Graphic alpha values are zero.
            // Keep the previous snapshot so a later interrupted/re-enter transition can restore it.
            if (_graphics.Count > 0 && _alpha <= 0.001f)
            {
                Apply();
                return;
            }

            _graphics.Clear();
            _baseAlpha.Clear();
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is null)
                {
                    continue;
                }

                _graphics.Add(graphic);
                _baseAlpha.Add(graphic.color.a / Mathf.Max(_alpha, 0.0001f));
            }

            Apply();
        }

        private void Awake()
        {
            Refresh();
        }

        private void Apply()
        {
            if (_graphics.Count == 0)
            {
                RefreshIfPossible();
            }

            for (var index = 0; index < _graphics.Count; index++)
            {
                var graphic = _graphics[index];
                if (graphic is null)
                {
                    continue;
                }

                var color = graphic.color;
                color.a = Mathf.Clamp01(_baseAlpha[index] * _alpha);
                graphic.color = color;
            }
        }

        private void RefreshIfPossible()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is null)
                {
                    continue;
                }

                _graphics.Add(graphic);
                _baseAlpha.Add(graphic.color.a);
            }
        }
    }
}
