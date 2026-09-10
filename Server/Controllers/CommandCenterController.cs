using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astar.Vanguard.Server.Models.Mcs;
using Astar.Vanguard.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace Astar.Vanguard.Server.Controllers;

[Injectable]
public sealed class CommandCenterController(
    ProfileService profileService,
    DeploymentService deploymentService,
    InfoService infoService,
    RaidService raidService
)
{
    public VanguardCommandCenterSnapshot GetSnapshot(MongoId leadPlayerId)
    {
        var deployment = deploymentService.Get(leadPlayerId);
        var deployed = deployment.ToHashSet();
        var operators = new List<VanguardOperatorState>();

        foreach (var operatorProfile in profileService.GetOperatorCatalog())
        {
            var profile = profileService.GetProfileByOperatorId(
                leadPlayerId,
                operatorProfile.Id
            );

            operators.Add(new VanguardOperatorState
            {
                Id = operatorProfile.Id,
                CodeName = operatorProfile.CodeName,
                Role = operatorProfile.Role,
                Aim = operatorProfile.Aim,
                Vision = operatorProfile.Vision,
                Hearing = operatorProfile.Hearing,
                Reaction = operatorProfile.Reaction,
                Aggression = operatorProfile.Aggression,
                DamageCoeff = operatorProfile.DamageCoeff,
                EnemyMemory = operatorProfile.EnemyMemory,
                Cover = operatorProfile.Cover,
                Recruited = profile is not null,
                Deployed = deployed.Contains(operatorProfile.Id),
                ProfileId = profile?.ProfileInfo.ProfileId?.ToString(),
                Aid = profile?.ProfileInfo.Aid
            });
        }

        return new VanguardCommandCenterSnapshot
        {
            Operators = operators,
            Deployment = deployment,
            MaxDeployment = DeploymentService.MaxDeployment
        };
    }

    public VanguardCommandCenterSnapshot Recruit(
        MongoId leadPlayerId,
        string operatorId
    )
    {
        var existing = profileService.GetProfileByOperatorId(
            leadPlayerId,
            operatorId
        );

        if (existing is null)
        {
            var profile = profileService.GeneratePermanentOperator(
                leadPlayerId,
                operatorId
            );
            infoService.CompleteOrderQuestSendFriendRequest(
                profile,
                leadPlayerId
            );
        }

        return GetSnapshot(leadPlayerId);
    }

    public async Task<VanguardCommandCenterSnapshot> SetDeployment(
        MongoId leadPlayerId,
        IReadOnlyCollection<string> operatorIds
    )
    {
        await deploymentService.Set(leadPlayerId, operatorIds);
        raidService.ApplyConfiguredDeployment(leadPlayerId);
        return GetSnapshot(leadPlayerId);
    }
}
