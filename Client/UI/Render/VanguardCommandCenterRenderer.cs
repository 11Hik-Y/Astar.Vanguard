using System;
using System.Collections.Generic;
using System.Linq;
using Astar.UI.Controls;
using Astar.UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Astar.Vanguard.Client.UI.Render
{
    /// <summary>
    /// Presentation-only renderer for the Vanguard command center. Business state is
    /// supplied as a view state; this class owns the retained uGUI tree and transitions.
    /// </summary>
    internal sealed class VanguardCommandCenterRenderer : IDisposable
    {
        private readonly AstarScreenContext _context;
        private readonly Action<string> _selectOperator;
        private readonly Action<string> _recruitOperator;
        private readonly Action<string> _toggleDeployment;
        private readonly Action _refresh;
        private readonly Dictionary<string, AstarListItemView> _operatorViews = new();
        private readonly List<AstarProgressBarView> _statViews = new();
        private readonly List<AstarButtonView> _deploymentButtons = new();

        private RectTransform _root;
        private AstarMotionHost _motion;
        private AstarComponentFactory _components;
        private AstarCardView _rosterCard;
        private AstarCardView _detailCard;
        private AstarCardView _deploymentCard;
        private AstarScrollView _rosterScroll;
        private Text _detailRole;
        private Text _detailStatus;
        private Text _identity;
        private Text _statusMessage;
        private AstarButtonView _primaryAction;
        private AstarButtonView _refreshButton;
        private string _selectedOperatorId;
        private bool _selectedIsRecruited;
        private string[] _deploymentOperatorIds = Array.Empty<string>();
        private bool _built;
        private bool _disposed;

        public VanguardCommandCenterRenderer(
            AstarScreenContext context,
            Action<string> selectOperator,
            Action<string> recruitOperator,
            Action<string> toggleDeployment,
            Action refresh
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _selectOperator = selectOperator ?? throw new ArgumentNullException(nameof(selectOperator));
            _recruitOperator = recruitOperator ?? throw new ArgumentNullException(nameof(recruitOperator));
            _toggleDeployment = toggleDeployment ?? throw new ArgumentNullException(nameof(toggleDeployment));
            _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
        }

        public void Render(VanguardCommandCenterViewState state, bool animate = true)
        {
            if (_disposed || state is null)
            {
                return;
            }

            var firstRender = !_built;
            if (firstRender)
            {
                Build(state);
                animate = false;
            }

            _rosterCard.SetTitle($"干员名册  {state.RecruitedCount} / {state.TotalCount}");
            _deploymentCard.SetTitle($"出击编队  {state.DeployedCount} / {state.MaxDeployment}");
            SetTextIfChanged(_statusMessage, state.StatusMessage);

            foreach (var item in state.Operators)
            {
                if (!_operatorViews.TryGetValue(item.Id, out var view))
                {
                    continue;
                }

                view.SetContent(item.Title, item.Info, item.Status, item.StatusTone);
                view.SetEnabled(item.IsEnabled);
                view.SetSelected(item.IsSelected);
            }

            RenderDetail(state.Selected, animate);
            RenderDeployment(state.Deployment);
}

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_rosterCard?.Root is not null) _context.UnregisterTransitionTarget(_rosterCard.Root);
            if (_detailCard?.Root is not null) _context.UnregisterTransitionTarget(_detailCard.Root);
            if (_deploymentCard?.Root is not null) _context.UnregisterTransitionTarget(_deploymentCard.Root);
            _motion?.StopAllMotions();
            if (_root is not null)
            {
                _root.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            _root = null;
            _operatorViews.Clear();
            _statViews.Clear();
            _deploymentButtons.Clear();
        }

        private void Build(VanguardCommandCenterViewState state)
        {
            _root = _context.Factory.CreateRect(
                _context.ContentRoot,
                "VanguardCommandCenter.RenderRoot",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero
            );
            _motion = _context.Motion;
            _components = _context.Components;

            var backdropRect = _context.Factory.CreateRect(
                _root,
                "Backdrop",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero
            );
            var backdrop = backdropRect.gameObject.AddComponent<AstarRoundedRectGraphic>();
            backdrop.Radius = AstarDesignTokens.RadiusCard;
            backdrop.color = AstarDesignTokens.Canvas;
            backdrop.raycastTarget = false;

            _rosterCard = _components.CreateCard(
                _root,
                "RosterCard",
                "干员名册",
                new Vector2(0f, 0.14f),
                new Vector2(0.36f, 1f),
                Vector2.zero,
                new Vector2(-8f, 0f)
            );
            _detailCard = _components.CreateCard(
                _root,
                "DetailCard",
                "干员档案",
                new Vector2(0.36f, 0.14f),
                Vector2.one,
                new Vector2(8f, 0f),
                Vector2.zero
            );
            _deploymentCard = _components.CreateCard(
                _root,
                "DeploymentCard",
                "出击编队",
                Vector2.zero,
                new Vector2(1f, 0.125f),
                Vector2.zero,
                new Vector2(0f, -6f)
            );

            _context.RegisterTransitionTarget(_rosterCard.Root);
            _context.RegisterTransitionTarget(_detailCard.Root);
            _context.RegisterTransitionTarget(_deploymentCard.Root);

            BuildRoster(state);
            BuildDetail();
            BuildDeployment(Math.Max(1, state.MaxDeployment));
            _built = true;
        }

        private void BuildRoster(VanguardCommandCenterViewState state)
        {
            _rosterScroll = _context.Factory.CreateScrollView(
                _rosterCard.ContentRoot,
                "RosterScroll",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero
            );
            _components.StyleScrollView(_rosterScroll);

            const float rowHeight = 64f;
            const float rowGap = 5f;
            const float verticalPadding = 6f;
            _rosterScroll.SetContentHeight(
                state.Operators.Count * (rowHeight + rowGap) + verticalPadding * 2f
            );
            for (var index = 0; index < state.Operators.Count; index++)
            {
                var item = state.Operators[index];
                var operatorId = item.Id;
                var top = -verticalPadding - index * (rowHeight + rowGap);
                var bottom = top - rowHeight;
                var view = _components.CreateListItem(
                    _rosterScroll.Content,
                    "Operator_" + operatorId,
                    () => _selectOperator(operatorId),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(3f, bottom),
                    new Vector2(-6f, top)
                );
                _operatorViews[operatorId] = view;
            }
        }

        private void BuildDetail()
        {
            var content = _detailCard.ContentRoot;
            _detailRole = _context.Factory.CreateText(
                content,
                "Role",
                string.Empty,
                AstarDesignTokens.TypographySubtitle,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0.88f),
                new Vector2(0.68f, 1f),
                new Vector2(4f, 0f),
                Vector2.zero,
                AstarDesignTokens.ForegroundMuted
            );
            _detailRole.raycastTarget = false;

            _detailStatus = _context.Factory.CreateText(
                content,
                "RosterStatus",
                string.Empty,
                AstarDesignTokens.TypographyBody,
                TextAnchor.MiddleRight,
                new Vector2(0.64f, 0.88f),
                Vector2.one,
                Vector2.zero,
                new Vector2(-4f, 0f),
                AstarDesignTokens.Success,
                FontStyle.Bold
            );
            _detailStatus.raycastTarget = false;

            const float firstTop = 0.84f;
            const float step = 0.087f;
            const float height = 0.068f;
            for (var index = 0; index < 8; index++)
            {
                var top = firstTop - index * step;
                var bottom = top - height;
                _statViews.Add(
                    _components.CreateProgressBar(
                        content,
                        "Stat_" + index,
                        string.Empty,
                        0f,
                        string.Empty,
                        new Vector2(0.015f, bottom),
                        new Vector2(0.985f, top),
                        Vector2.zero,
                        Vector2.zero
                    )
                );
            }

            _identity = _context.Factory.CreateText(
                content,
                "Identity",
                string.Empty,
                AstarDesignTokens.TypographyCaption,
                TextAnchor.MiddleLeft,
                new Vector2(0.015f, 0.005f),
                new Vector2(0.62f, 0.09f),
                Vector2.zero,
                Vector2.zero,
                AstarDesignTokens.ForegroundMuted
            );
            _identity.raycastTarget = false;

            _refreshButton = _components.CreateButton(
                content,
                "Refresh",
                "刷新",
                _refresh,
                new Vector2(0.62f, 0.005f),
                new Vector2(0.76f, 0.09f),
                Vector2.zero,
                Vector2.zero
            );
            _primaryAction = _components.CreateButton(
                content,
                "PrimaryAction",
                "永久招募",
                OnPrimaryAction,
                new Vector2(0.775f, 0.005f),
                new Vector2(0.985f, 0.09f),
                Vector2.zero,
                Vector2.zero,
                true
            );
        }

        private void BuildDeployment(int maxDeployment)
        {
            _statusMessage = _context.Factory.CreateText(
                _deploymentCard.Root,
                "StatusMessage",
                string.Empty,
                AstarDesignTokens.TypographyCaption,
                TextAnchor.MiddleRight,
                new Vector2(0.54f, 1f),
                Vector2.one,
                new Vector2(0f, -40f),
                new Vector2(-AstarDesignTokens.SpacingLg, 0f),
                AstarDesignTokens.ForegroundMuted
            );
            _statusMessage.raycastTarget = false;

            _deploymentOperatorIds = new string[maxDeployment];
            const float gap = 0.012f;
            var slotWidth = (1f - gap * (maxDeployment - 1)) / maxDeployment;
            for (var index = 0; index < maxDeployment; index++)
            {
                var slot = index;
                var left = index * (slotWidth + gap);
                var right = left + slotWidth;
                var button = _components.CreateButton(
                    _deploymentCard.ContentRoot,
                    "DeploymentSlot_" + index,
                    $"{index + 1}. 空位",
                    () => OnDeploymentSlot(slot),
                    new Vector2(left, 0f),
                    new Vector2(right, 1f),
                    Vector2.zero,
                    Vector2.zero
                );
                _deploymentButtons.Add(button);
            }
        }

        private void RenderDetail(VanguardOperatorDetailViewState detail, bool animate)
        {
            _selectedOperatorId = detail?.Id;
            _selectedIsRecruited = detail?.IsRecruited == true;
            if (detail is null)
            {
                _detailCard.SetTitle("未选择干员");
                SetTextIfChanged(_detailRole, "从左侧名册选择一名干员。");
                SetTextIfChanged(_detailStatus, string.Empty);
                SetTextIfChanged(_identity, string.Empty);
                _primaryAction.SetText("不可用");
                _primaryAction.SetEnabled(false);
                for (var index = 0; index < _statViews.Count; index++)
                {
                    SetTextIfChanged(_statViews[index].Label, string.Empty);
                    _statViews[index].SetValue(0f, string.Empty, animate);
                }
                return;
            }

            _detailCard.SetTitle(detail.Name);
            SetTextIfChanged(_detailRole, detail.Role);
            SetTextIfChanged(_detailStatus, detail.RosterStatus);
            _detailStatus.color = AstarComponentFactory.GetToneColor(detail.StatusTone);
            SetTextIfChanged(_identity, detail.Identity);
            _primaryAction.SetText(detail.PrimaryActionText);
            _primaryAction.SetEnabled(detail.PrimaryActionEnabled);
            _primaryAction.SetSelected(detail.IsRecruited && !detail.IsDeployed && detail.PrimaryActionEnabled);

            for (var index = 0; index < _statViews.Count; index++)
            {
                if (index >= detail.Stats.Count)
                {
                    SetTextIfChanged(_statViews[index].Label, string.Empty);
                    _statViews[index].SetValue(0f, string.Empty, animate);
                    continue;
                }

                var stat = detail.Stats[index];
                SetTextIfChanged(_statViews[index].Label, stat.Label);
                _statViews[index].SetValue(stat.Normalized, stat.Value, animate);
            }
        }

        private void RenderDeployment(IReadOnlyList<VanguardDeploymentSlotViewState> deployment)
        {
            for (var index = 0; index < _deploymentButtons.Count; index++)
            {
                var state = index < deployment.Count ? deployment[index] : null;
                _deploymentOperatorIds[index] = state?.OperatorId;
                var occupied = state?.Occupied == true;
                _deploymentButtons[index].SetText(state?.Label ?? $"{index + 1}. 空位");
                _deploymentButtons[index].SetEnabled(occupied);
                _deploymentButtons[index].SetSelected(occupied);
            }
        }

        private static void SetTextIfChanged(Text target, string value)
        {
            if (target is null)
            {
                return;
            }

            var next = value ?? string.Empty;
            if (!string.Equals(target.text, next, StringComparison.Ordinal))
            {
                target.text = next;
            }
        }
        private void OnPrimaryAction()
        {
            if (string.IsNullOrWhiteSpace(_selectedOperatorId))
            {
                return;
            }

            if (!_operatorViews.ContainsKey(_selectedOperatorId))
            {
                return;
            }

            if (!_selectedIsRecruited)
            {
                _recruitOperator(_selectedOperatorId);
            }
            else
            {
                _toggleDeployment(_selectedOperatorId);
            }
        }

        private void OnDeploymentSlot(int slot)
        {
            if (slot < 0 || slot >= _deploymentOperatorIds.Length)
            {
                return;
            }

            var operatorId = _deploymentOperatorIds[slot];
            if (!string.IsNullOrWhiteSpace(operatorId))
            {
                _toggleDeployment(operatorId);
            }
        }
    }
}