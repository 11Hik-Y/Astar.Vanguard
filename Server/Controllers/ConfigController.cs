
using System.Collections.Generic;
using Astar.Vanguard.Server.Models.Eft.Common.Tables;
using Astar.Vanguard.Server.Services;
using SPTarkov.DI.Annotations;

namespace Astar.Vanguard.Server.Controllers
{
    [Injectable]
    public class ConfigController(
        ConfigService configService
    )
    {
        public McsPluginClientConfig GetMcsPluginClientConfig()
        {
            return configService.GetMcsPluginClientConfig();
        }

        public McsPluginConfig GetMcsPluginConfig()
        {
            return configService.GetMcsPluginConfig();
        }

        public OrderConfig GetOrderConfig()
        {
            return configService.GetOrderConfig();
        }

        public string GetSpawnTypeDisplayName(string wildSpawnType)
        {
            return configService.GetSpawnTypeDisplayName(wildSpawnType);
        }
    }
}