using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Astar.Vanguard.Server.Models.Mcs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace Astar.Vanguard.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class DeploymentService(
    ConfigService configService,
    JsonUtil jsonUtil,
    FileUtil fileUtil,
    ProfileService profileService
)
{
    public const int MaxDeployment = 4;

    private readonly string _path = Path.Combine(
        configService.GetModPath(),
        "Assets",
        "database",
        "deployments.json"
    );

    private readonly ConcurrentDictionary<string, List<string>> _deployments =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public List<string> Get(MongoId leadPlayerId)
    {
        if (!_deployments.TryGetValue(leadPlayerId.ToString(), out var operatorIds))
        {
            return [];
        }

        return [.. operatorIds];
    }

    public List<int> GetAids(MongoId leadPlayerId)
    {
        var output = new List<int>();
        foreach (var operatorId in Get(leadPlayerId))
        {
            var profile = profileService.GetProfileByOperatorId(
                leadPlayerId,
                operatorId
            );
            if (profile?.ProfileInfo.Aid is int aid)
            {
                output.Add(aid);
            }
        }

        return output;
    }

    public bool ContainsAid(MongoId leadPlayerId, int aid)
    {
        return GetAids(leadPlayerId).Contains(aid);
    }

    public async Task Set(MongoId leadPlayerId, IEnumerable<string> operatorIds)
    {
        var normalized = operatorIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Take(MaxDeployment + 1)
            .ToList();

        if (normalized.Count > MaxDeployment)
        {
            throw new ArgumentException(
                $"Astar Vanguard deployment is limited to {MaxDeployment} operators."
            );
        }

        foreach (var operatorId in normalized)
        {
            if (profileService.GetProfileByOperatorId(leadPlayerId, operatorId) is null)
            {
                throw new InvalidOperationException(
                    $"Operator {operatorId} has not been recruited."
                );
            }
        }

        _deployments[leadPlayerId.ToString()] = normalized;
        await Save();
    }

    public async Task OnPostLoadAsync()
    {
        if (!fileUtil.FileExists(_path))
        {
            await fileUtil.WriteFileAsync(_path, "{}");
            return;
        }

        var saved = await jsonUtil.DeserializeFromFileAsync<
            Dictionary<string, List<string>>
        >(_path) ?? [];

        foreach (var (leadPlayerId, operatorIds) in saved)
        {
            _deployments[leadPlayerId] = operatorIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Take(MaxDeployment)
                .ToList();
        }
    }

    private async Task Save()
    {
        await _saveLock.WaitAsync();
        try
        {
            var snapshot = _deployments.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );
            await fileUtil.WriteFileAsync(
                _path,
                jsonUtil.Serialize(snapshot, true)
            );
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
