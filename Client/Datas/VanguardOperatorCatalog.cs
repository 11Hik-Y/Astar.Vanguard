using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Astar.Vanguard.Client.Models;
using Newtonsoft.Json;

namespace Astar.Vanguard.Client.Datas
{
    public static class VanguardOperatorCatalog
    {
        private const string ResourceName = "Astar.Vanguard.Client.Assets.vanguard_operators.json";
        private static readonly Lazy<IReadOnlyDictionary<string, VanguardOperatorProfile>> Profiles = new(Load);

        public static VanguardOperatorProfile Get(string codeName)
        {
            if (string.IsNullOrWhiteSpace(codeName))
            {
                return null;
            }

            return Profiles.Value.TryGetValue(codeName, out var profile) ? profile : null;
        }

        private static IReadOnlyDictionary<string, VanguardOperatorProfile> Load()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Missing embedded Astar Vanguard operator catalog: {ResourceName}");
            using var reader = new StreamReader(stream);
            var profiles = JsonConvert.DeserializeObject<List<VanguardOperatorProfile>>(reader.ReadToEnd())
                ?? throw new InvalidOperationException("Astar Vanguard operator catalog could not be deserialized.");

            if (profiles.Count != 37)
            {
                throw new InvalidOperationException($"Astar Vanguard requires exactly 37 operators, but {profiles.Count} were embedded.");
            }

            var duplicate = profiles.GroupBy(x => x.CodeName, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"Duplicate Astar Vanguard operator codename: {duplicate.Key}");
            }

            return profiles.ToDictionary(x => x.CodeName, StringComparer.Ordinal);
        }
    }
}
