using System;
using System.Collections.Generic;
using System.Linq;
using Astar.UI.Controls;
using Astar.Vanguard.Client.Models;
using UnityEngine;

namespace Astar.Vanguard.Client.UI.Render
{
    internal sealed class VanguardOperatorListItemViewState
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string Info { get; init; }
        public string Status { get; init; }
        public AstarTone StatusTone { get; init; }
        public bool IsSelected { get; init; }
        public bool IsEnabled { get; init; }
    }

    internal sealed class VanguardStatViewState
    {
        public string Label { get; init; }
        public float Normalized { get; init; }
        public string Value { get; init; }
    }

    internal sealed class VanguardOperatorDetailViewState
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Role { get; init; }
        public string RosterStatus { get; init; }
        public AstarTone StatusTone { get; init; }
        public string Identity { get; init; }
        public bool IsRecruited { get; init; }
        public bool IsDeployed { get; init; }
        public bool IsDeploymentFull { get; init; }
        public IReadOnlyList<VanguardStatViewState> Stats { get; init; } = Array.Empty<VanguardStatViewState>();

        public string PrimaryActionText => !IsRecruited
            ? "永久招募"
            : IsDeployed
                ? "从出击编队移除"
                : IsDeploymentFull
                    ? "编队已满"
                    : "加入出击编队";

        public bool PrimaryActionEnabled => !IsRecruited || IsDeployed || !IsDeploymentFull;
    }

    internal sealed class VanguardDeploymentSlotViewState
    {
        public int Index { get; init; }
        public string OperatorId { get; init; }
        public string Label { get; init; }
        public bool Occupied => !string.IsNullOrWhiteSpace(OperatorId);
    }

    internal sealed class VanguardCommandCenterViewState
    {
        public IReadOnlyList<VanguardOperatorListItemViewState> Operators { get; init; } = Array.Empty<VanguardOperatorListItemViewState>();
        public VanguardOperatorDetailViewState Selected { get; init; }
        public IReadOnlyList<VanguardDeploymentSlotViewState> Deployment { get; init; } = Array.Empty<VanguardDeploymentSlotViewState>();
        public int RecruitedCount { get; init; }
        public int TotalCount { get; init; }
        public int DeployedCount { get; init; }
        public int MaxDeployment { get; init; }
        public string StatusMessage { get; init; }

        public static VanguardCommandCenterViewState FromSnapshot(
            VanguardCommandCenterSnapshot snapshot,
            string selectedOperatorId,
            string statusMessage
        )
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var operators = snapshot.Operators ?? new List<VanguardOperatorState>();
            var deployment = snapshot.Deployment ?? new List<string>();
            var selected = operators.FirstOrDefault(x => x.Id == selectedOperatorId)
                ?? operators.FirstOrDefault();
            var deploymentFull = deployment.Count >= snapshot.MaxDeployment;

            var operatorItems = operators
                .Select(state => new VanguardOperatorListItemViewState
                {
                    Id = state.Id,
                    Title = $"{state.CodeName}  /  {state.Id}",
                    Info = state.Role ?? string.Empty,
                    Status = state.Deployed
                        ? "编队"
                        : state.Recruited
                            ? "已招募"
                            : "待招募",
                    StatusTone = state.Deployed
                        ? AstarTone.Accent
                        : state.Recruited
                            ? AstarTone.Success
                            : AstarTone.Muted,
                    IsSelected = state.Id == selected?.Id,
                    IsEnabled = true,
                })
                .ToList();

            var slots = new List<VanguardDeploymentSlotViewState>();
            for (var index = 0; index < snapshot.MaxDeployment; index++)
            {
                if (index < deployment.Count)
                {
                    var operatorId = deployment[index];
                    var state = operators.FirstOrDefault(x => x.Id == operatorId);
                    slots.Add(new VanguardDeploymentSlotViewState
                    {
                        Index = index,
                        OperatorId = operatorId,
                        Label = state is null
                            ? $"{index + 1}. {operatorId}"
                            : $"{index + 1}. {state.CodeName}",
                    });
                }
                else
                {
                    slots.Add(new VanguardDeploymentSlotViewState
                    {
                        Index = index,
                        OperatorId = null,
                        Label = $"{index + 1}. 空位",
                    });
                }
            }

            return new VanguardCommandCenterViewState
            {
                Operators = operatorItems,
                Selected = selected is null ? null : CreateDetail(selected, deploymentFull),
                Deployment = slots,
                RecruitedCount = operators.Count(x => x.Recruited),
                TotalCount = operators.Count,
                DeployedCount = deployment.Count,
                MaxDeployment = snapshot.MaxDeployment,
                StatusMessage = statusMessage ?? string.Empty,
            };
        }

        private static VanguardOperatorDetailViewState CreateDetail(
            VanguardOperatorState selected,
            bool deploymentFull
        )
        {
            var identity = string.Empty;
            if (selected.Recruited)
            {
                identity = $"AID: {(selected.Aid.HasValue ? selected.Aid.Value.ToString() : "-")}";
                if (!string.IsNullOrWhiteSpace(selected.ProfileId))
                {
                    identity += $"   Profile: {selected.ProfileId}";
                }
            }

            return new VanguardOperatorDetailViewState
            {
                Id = selected.Id,
                Name = $"{selected.CodeName}  /  {selected.Id}",
                Role = $"战术定位：{selected.Role}",
                RosterStatus = selected.Recruited
                    ? selected.Deployed
                        ? "永久在册 · 当前出击编队"
                        : "永久在册"
                    : "尚未招募",
                StatusTone = selected.Recruited ? AstarTone.Success : AstarTone.Warning,
                Identity = identity,
                IsRecruited = selected.Recruited,
                IsDeployed = selected.Deployed,
                IsDeploymentFull = deploymentFull && !selected.Deployed,
                Stats = new[]
                {
                    Stat("枪法  Aim", Normalize(selected.Aim, 0.35f, 0.90f), selected.Aim.ToString("0.00")),
                    Stat("视野  Vision", Normalize(selected.Vision, 300f, 560f), selected.Vision.ToString("0")),
                    Stat("听觉  Hearing", Normalize(selected.Hearing, 70f, 125f), selected.Hearing.ToString("0")),
                    Stat("反应  Reaction", 1f - Normalize(selected.Reaction, 0.03f, 0.11f), selected.Reaction.ToString("0.000")),
                    Stat("进攻  Aggression", Normalize(selected.Aggression, 0.60f, 1.50f), selected.Aggression.ToString("0.00")),
                    Stat("耐久  DamageCoeff", 1f - Normalize(selected.DamageCoeff, 0.70f, 1.15f), selected.DamageCoeff.ToString("0.00")),
                    Stat("记忆  EnemyMemory", Normalize(selected.EnemyMemory, 30f, 90f), selected.EnemyMemory.ToString("0")),
                    Stat("掩体  Cover", Normalize(selected.Cover, 0.60f, 1.50f), selected.Cover.ToString("0.00")),
                },
            };
        }

        private static VanguardStatViewState Stat(string label, float normalized, string value)
        {
            return new VanguardStatViewState
            {
                Label = label,
                Normalized = Mathf.Clamp01(normalized),
                Value = value,
            };
        }

        private static float Normalize(float value, float min, float max)
        {
            if (max <= min)
            {
                return 0f;
            }

            return Mathf.Clamp01((value - min) / (max - min));
        }
    }
}
