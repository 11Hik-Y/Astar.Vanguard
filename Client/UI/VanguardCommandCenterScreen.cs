using System;
using System.Collections.Generic;
using System.Linq;
using Astar.UI.Core;
using Astar.Vanguard.Client.Models;
using Astar.Vanguard.Client.UI.Render;
using Astar.Vanguard.Client.Utils;
using EFT.UI;
using HarmonyLib;

namespace Astar.Vanguard.Client.UI
{
    public static class VanguardCommandCenterScreen
    {
        private const string ScreenId = "astar.vanguard.command-center";

        private static AstarScreenHandle _handle;
        private static AstarScreenContext _context;
        private static VanguardCommandCenterRenderer _renderer;
        private static TraderScreensGroup _traderScreen;
        private static VanguardCommandCenterSnapshot _snapshot;
        private static string _selectedOperatorId;
        private static string _statusMessage = "指挥中心已连接。";
        private static bool _closeTraderOnUiClose;

        public static bool IsOpen => _handle?.IsActive == true;

        public static bool TryOpen(TraderScreensGroup traderScreen)
        {
            if (IsOpen)
            {
                return true;
            }

            try
            {
                var snapshot = McsRequestHandler.GetVanguardCommandCenter();
                NormalizeSnapshot(snapshot);

                _snapshot = snapshot;
                _traderScreen = traderScreen;
                _closeTraderOnUiClose = true;
                _statusMessage = "指挥中心已连接。";
                _selectedOperatorId =
                    snapshot.Deployment.FirstOrDefault()
                    ?? snapshot.Operators.FirstOrDefault(x => x.Recruited)?.Id
                    ?? snapshot.Operators.FirstOrDefault()?.Id;

                var request = new AstarScreenRequest(
                    ScreenId,
                    "星锋指挥中心",
                    Build,
                    "永久干员招募 · 名册管理 · 1-4 人出击编队",
                    OnUiClosed,
                    closeOnEscape: true
                );

                if (!AstarUiApi.TryOpen(request, out var handle) || handle is null)
                {
                    ResetSession();
                    AstarVanguardPlugin.Logger.LogWarning(
                        "Astar UI rejected the Vanguard command-center screen."
                    );
                    return false;
                }

                _handle = handle;
                return true;
            }
            catch (Exception exception)
            {
                ResetSession();
                AstarVanguardPlugin.Logger.LogError(
                    "Failed to open Vanguard command center: " + exception
                );
                return false;
            }
        }

        public static void CloseFromTrader()
        {
            if (!IsOpen)
            {
                ResetSession();
                return;
            }

            _closeTraderOnUiClose = false;
            _handle.Close();
        }

        private static void Build(AstarScreenContext context)
        {
            _context = context;
            _renderer?.Dispose();
            _renderer = new VanguardCommandCenterRenderer(
                context,
                SelectOperator,
                Recruit,
                ToggleDeployment,
                Refresh
            );
            Render(false);
        }

        private static void Render(bool animate = true)
        {
            if (_context is null || _snapshot is null || _renderer is null)
            {
                return;
            }

            _renderer.Render(
                VanguardCommandCenterViewState.FromSnapshot(
                    _snapshot,
                    _selectedOperatorId,
                    _statusMessage
                ),
                animate
            );
        }

        private static void SelectOperator(string operatorId)
        {
            _selectedOperatorId = operatorId;
            Render();
        }

        private static void Recruit(string operatorId)
        {
            var state = _snapshot.Operators.FirstOrDefault(x => x.Id == operatorId);
            if (state is null || state.Recruited)
            {
                return;
            }

            try
            {
                _statusMessage = $"正在永久招募 {state.CodeName} ...";
                var updatedSnapshot =
                    McsRequestHandler.RecruitVanguardOperator(operatorId);
                NormalizeSnapshot(updatedSnapshot);
                _snapshot = updatedSnapshot;
                _selectedOperatorId = operatorId;
                _statusMessage = $"{state.CodeName} 已永久加入星锋名册。";
            }
            catch (Exception exception)
            {
                _statusMessage = "招募失败：" + GetErrorMessage(exception);
                AstarVanguardPlugin.Logger.LogError(
                    "Vanguard recruit request failed: " + exception
                );
            }

            Render();
        }

        private static void ToggleDeployment(string operatorId)
        {
            var state = _snapshot.Operators.FirstOrDefault(x => x.Id == operatorId);
            if (state is null || !state.Recruited)
            {
                return;
            }

            var deployment = new List<string>(_snapshot.Deployment);
            if (deployment.Remove(operatorId))
            {
                _statusMessage = $"{state.CodeName} 已移出出击编队。";
            }
            else
            {
                if (deployment.Count >= _snapshot.MaxDeployment)
                {
                    _statusMessage = $"出击编队最多 {_snapshot.MaxDeployment} 人。";
                    Render();
                    return;
                }

                deployment.Add(operatorId);
                _statusMessage = $"{state.CodeName} 已加入出击编队。";
            }

            try
            {
                var updatedSnapshot =
                    McsRequestHandler.SetVanguardDeployment(deployment);
                NormalizeSnapshot(updatedSnapshot);
                _snapshot = updatedSnapshot;
                _selectedOperatorId = operatorId;
            }
            catch (Exception exception)
            {
                _statusMessage = "编队更新失败：" + GetErrorMessage(exception);
                AstarVanguardPlugin.Logger.LogError(
                    "Vanguard deployment request failed: " + exception
                );
            }

            Render();
        }

        private static void Refresh()
        {
            try
            {
                var updatedSnapshot =
                    McsRequestHandler.GetVanguardCommandCenter();
                NormalizeSnapshot(updatedSnapshot);
                _snapshot = updatedSnapshot;

                if (
                    string.IsNullOrWhiteSpace(_selectedOperatorId)
                    || !_snapshot.Operators.Any(x => x.Id == _selectedOperatorId)
                )
                {
                    _selectedOperatorId =
                        _snapshot.Deployment.FirstOrDefault()
                        ?? _snapshot.Operators.FirstOrDefault()?.Id;
                }

                _statusMessage = "名册与编队状态已刷新。";
            }
            catch (Exception exception)
            {
                _statusMessage = "刷新失败：" + GetErrorMessage(exception);
                AstarVanguardPlugin.Logger.LogError(
                    "Vanguard command-center refresh failed: " + exception
                );
            }

            Render();
        }

        private static void OnUiClosed()
        {
            var traderScreen = _traderScreen;
            var closeTrader = _closeTraderOnUiClose;
            ResetSession();

            if (closeTrader && traderScreen is not null)
            {
                CloseTraderScreen(traderScreen);
            }
        }

        private static void CloseTraderScreen(TraderScreensGroup traderScreen)
        {
            try
            {
                var controller = Traverse.Create(traderScreen)
                    .Field("ScreenController")
                    .GetValue();
                if (controller is null)
                {
                    AstarVanguardPlugin.Logger.LogWarning(
                        "Trader screen controller was null while closing Vanguard UI."
                    );
                    return;
                }

                var closeMethod = AccessTools.Method(
                    controller.GetType(),
                    "CloseScreen"
                );
                if (closeMethod is null)
                {
                    AstarVanguardPlugin.Logger.LogError(
                        "EFT 40087 Trader ScreenController.CloseScreen was not found."
                    );
                    return;
                }

                closeMethod.Invoke(controller, null);
            }
            catch (Exception exception)
            {
                AstarVanguardPlugin.Logger.LogError(
                    "Failed to close underlying EFT trader screen: " + exception
                );
            }
        }

        private static void NormalizeSnapshot(
            VanguardCommandCenterSnapshot snapshot
        )
        {
            if (snapshot is null)
            {
                throw new InvalidOperationException(
                    "Vanguard command-center response was null."
                );
            }

            snapshot.Operators ??= new List<VanguardOperatorState>();
            snapshot.Deployment ??= new List<string>();
            if (snapshot.MaxDeployment <= 0)
            {
                snapshot.MaxDeployment = 4;
            }

            if (snapshot.Operators.Count != 37)
            {
                throw new InvalidOperationException(
                    $"Vanguard command center expected 37 operators, received {snapshot.Operators.Count}."
                );
            }
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception.GetBaseException().Message;
        }

        private static void ResetSession()
        {
            _renderer?.Dispose();
            _renderer = null;
            _handle = null;
            _context = null;
            _traderScreen = null;
            _snapshot = null;
            _selectedOperatorId = null;
            _closeTraderOnUiClose = false;
        }
    }
}
