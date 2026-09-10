using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Astar.Vanguard.Server.Generators.CustomGeneration;
using Astar.Vanguard.Server.Helper;
using Astar.Vanguard.Server.Models.Eft.Common.Tables;
using Astar.Vanguard.Server.Models.Enums;
using Astar.Vanguard.Server.Models.Mcs;
using Astar.Vanguard.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Enums.RaidSettings;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Logger;

namespace Astar.Vanguard.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public class ProfileService(
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        NotificationSendHelper notificationSendHelper,
        SptLogger<ProfileService> logger,
        ConfigService configService,
        RandomUtil randomUtil,
        HashUtil hashUtil,
        ICloner cloner,
        ConfigServer configServer,
        BotHelper botHelper,
        ProfileHelper profileHelper,
        BuildsService buildsService,
        InventoryHelper inventoryHelper,
        ServerLocalisationService serverLocalisationService,
        NotificationHelper notificationHelper,
        BotInventoryContainerService botInventoryContainerService,
        BotLootCacheService botLootCacheService,
        McsBotGenerator mcsBotGenerator,
        BotGenerator botGenerator,
        PlayerScavGenerator playerScavGenerator,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        BotNameService botNameService,
        MailSendService mailSendService,
        InfoService infoService,
        DatabaseService databaseService,
        CompatibilityService compatibilityService,
        ProfileValidatorService profileValidatorService
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "profiles");
        private readonly string _operatorCatalogPath = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "operators", "vanguard.json");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, SptProfile>> _profiles = new();
        private readonly ConcurrentDictionary<MongoId, int> _mcsInventoryModeIds = new();
        private readonly ConcurrentDictionary<MongoId, SemaphoreSlim> _saveLocks = new();
        private List<VanguardOperatorProfile> _operatorProfiles = [];

        public bool RemoveMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var file = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerId, $"{mcsBotPlayerId}.json");
            logger.Error(string.Format(serverLocalisationService.GetText(Locales.CLEANINGUPOUTDATEDMCSPLAYERPROFILE), mcsBotPlayerId));
            if (_profiles[mcsLeadPlayerId].ContainsKey(mcsBotPlayerId))
            {
                _profiles[mcsLeadPlayerId].TryRemove(mcsBotPlayerId, out _);
                if (!fileUtil.DeleteFile(file))
                {
                    logger.Error(string.Format(serverLocalisationService.GetText(Locales.CANNOTDELETEFILENOTFOUND), file));
                }
            }

            return !fileUtil.FileExists(file);
        }

        public void ProcessExpiredMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var mcsBotPlayerProfile = GetMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            if (mcsBotPlayerProfile is null)
            {
                var errorInfo = serverLocalisationService.GetText(Locales.FAILEDLOADMCSPLAYERPROFILE);
                logger.Error(errorInfo);
            }

            try
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    try
                    {
                        if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                        {
                            var notification = notificationHelper.GenerateWsGroupMatchUserLeave(mcsBotPlayerProfile);
                            var notification2 = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.youAreRemovedFromFriendList);
                            notificationSendHelper.SendMessage(mcsLeadPlayerId, notification);
                            notificationSendHelper.SendMessage(mcsLeadPlayerId, notification2);
                        }
                    }
                    finally
                    {

                    }
                });
            }
            finally
            {
                RemoveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public void ProcessExpiredMcsBotPlayerProfiles(MongoId mcsLeadPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public void ProcessExpiredMcsBotPlayerNotify(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var mcsBotPlayerProfile = GetMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            if (mcsBotPlayerProfile is null)
            {
                var errorInfo = serverLocalisationService.GetText(Locales.FAILEDLOADMCSPLAYERPROFILE);
                logger.Error(errorInfo);
                return;
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                try
                {
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.friendListRequestAccept, true);
                        notificationSendHelper.SendMessage(mcsLeadPlayerId, notification);
                    }
                }
                finally
                {

                }
            });
        }

        public void ProcessExpiredMcsBotPlayerNotifies(MongoId mcsLeadPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerNotify(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public void TeamKillPunish(MongoId mcsLeadPlayerId)
        {
            infoService.SetAllOrderInfosToExpire(mcsLeadPlayerId, ProcessExpiredMcsBotPlayerNotify);
        }

        public async Task SaveMcsBotPlayerProfile(MongoId mcsLeadPlayerId, SptProfile mcsBotPlayerProfile)
        {
            var mcsBotPlayerId = mcsBotPlayerProfile.ProfileInfo.ProfileId.Value;
            var saveLock = _saveLocks.GetOrAdd(mcsBotPlayerId, _ => new(1, 1));
            await saveLock.WaitAsync();
            try
            {
                try
                {
                    var profilePath = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerId, $"{mcsBotPlayerId}.json");
                    _profiles.GetOrAdd(mcsLeadPlayerId, _ => new ConcurrentDictionary<MongoId, SptProfile>()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
                    var jsonProfile = jsonUtil.Serialize(_profiles[mcsLeadPlayerId][mcsBotPlayerId], true);
                    await fileUtil.WriteFileAsync(profilePath, jsonProfile);
                }
                catch (Exception e)
                {
                    logger.Error(serverLocalisationService.GetText(Locales.SAVEMCSPLAYERPROFILEEXCEPTION), e);
                }
            }
            finally
            {
                saveLock.Release();
            }
        }

        public async Task<long> SaveAllMcsBotPlayerProfile(MongoId mcsLeadPlayerId)
        {
            var mcsBotPlayerProfiles = _profiles[mcsLeadPlayerId].Values;
            var start = Stopwatch.StartNew();
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                await SaveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerProfile);
            }
            start.Stop();
            return start.ElapsedMilliseconds;
        }

        private async Task LoadMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var filePath = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerId, $"{mcsBotPlayerId}.json");
            if (!fileUtil.FileExists(filePath))
            {
                return;
            }

            var profile = await jsonUtil.DeserializeFromFileAsync<JsonObject>(filePath);
            if (profile is null)
            {
                return;
            }

            SptProfile mcsBotPlayerProfile;
            try
            {
                mcsBotPlayerProfile = profileValidatorService.MigrateAndValidateProfile(profile);
            }
            catch (InvalidOperationException)
            {
                mcsBotPlayerProfile = await jsonUtil.DeserializeFromFileAsync<SptProfile>(filePath);
            }

            if (mcsBotPlayerProfile is not null)
            {
                _profiles.GetOrAdd(mcsLeadPlayerId, _ => new()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
            }
        }

        private async Task LoadAllMcsBotPlayerProfile()
        {
            if (!fileUtil.DirectoryExists(_profileFolderDir))
            {
                fileUtil.CreateDirectory(_profileFolderDir);
            }

            var mcsLeadPlayerIdsFolderPath = fileUtil.GetDirectories(_profileFolderDir);
            foreach (var mcsLeadPlayerIdFolderPath in mcsLeadPlayerIdsFolderPath)
            {
                var mcsLeadPlayerId = System.IO.Path.GetFileNameWithoutExtension(mcsLeadPlayerIdFolderPath);
                if (MongoId.IsValidMongoId(mcsLeadPlayerId))
                {
                    var mcsLeadPlayerIdProfileFolderPath = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerIdFolderPath);
                    var files = fileUtil.GetFiles(mcsLeadPlayerIdProfileFolderPath).Where(item => fileUtil.GetFileExtension(item) == "json");
                    foreach (var file in files)
                    {
                        var mcsBotPlayerId = System.IO.Path.GetFileNameWithoutExtension(file);
                        if (MongoId.IsValidMongoId(mcsBotPlayerId))
                        {
                            // Permanent Vanguard roster: a persisted operator profile is
                            // authoritative and no longer requires a rental OrderInfo record.
                            await LoadMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
                        }
                    }
                }
            }

            await MigrateLoadedProfilesToOperatorCatalog();
        }

        private async Task MigrateLoadedProfilesToOperatorCatalog()
        {
            var validCodeNames = _operatorProfiles
                .Select(x => x.CodeName)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var (leadPlayerId, profileMap) in _profiles)
            {
                var orderedProfiles = profileMap
                    .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                    .ToList();

                if (orderedProfiles.Count > _operatorProfiles.Count)
                {
                    logger.Warning(
                        $"Astar Vanguard found {orderedProfiles.Count} persisted profiles for {leadPlayerId}. "
                        + $"Only {_operatorProfiles.Count} fixed operators will be loaded; excess profile files are left untouched."
                    );

                    foreach (var pair in orderedProfiles.Skip(_operatorProfiles.Count))
                    {
                        profileMap.TryRemove(pair.Key, out _);
                    }

                    orderedProfiles = orderedProfiles
                        .Take(_operatorProfiles.Count)
                        .ToList();
                }

                var usedCodeNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var (botPlayerId, profile) in orderedProfiles)
                {
                    var currentCodeName = profile.CharacterData?.PmcData?.Info?.Nickname;
                    if (
                        !string.IsNullOrWhiteSpace(currentCodeName)
                        && validCodeNames.Contains(currentCodeName)
                        && usedCodeNames.Add(currentCodeName)
                    )
                    {
                        continue;
                    }

                    var operatorProfile = SelectOperatorForMigration(
                        botPlayerId,
                        usedCodeNames
                    );
                    ApplyOperatorIdentity(profile, operatorProfile);
                    usedCodeNames.Add(operatorProfile.CodeName);
                    await SaveMcsBotPlayerProfile(leadPlayerId, profile);
                }
            }
        }

        private VanguardOperatorProfile SelectOperatorForMigration(
            MongoId botPlayerId,
            HashSet<string> usedCodeNames
        )
        {
            uint seed = 2166136261;
            foreach (var ch in botPlayerId.ToString())
            {
                seed ^= ch;
                seed *= 16777619;
            }

            var start = (int)(seed % (uint)_operatorProfiles.Count);
            for (var offset = 0; offset < _operatorProfiles.Count; offset++)
            {
                var candidate = _operatorProfiles[
                    (start + offset) % _operatorProfiles.Count
                ];
                if (!usedCodeNames.Contains(candidate.CodeName))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Astar Vanguard could not allocate a unique operator identity during profile migration."
            );
        }

        private static void ApplyOperatorIdentity(
            SptProfile profile,
            VanguardOperatorProfile operatorProfile
        )
        {
            var pmcInfo = profile.CharacterData?.PmcData?.Info;
            if (pmcInfo is not null)
            {
                pmcInfo.Nickname = operatorProfile.CodeName;
                pmcInfo.LowerNickname = operatorProfile.CodeName.ToLowerInvariant();
            }

            var scavInfo = profile.CharacterData?.ScavData?.Info;
            if (scavInfo is not null)
            {
                scavInfo.MainProfileNickname = operatorProfile.CodeName;
            }

            if (profile.ProfileInfo is not null)
            {
                profile.ProfileInfo.Username = operatorProfile.CodeName;
            }
        }

        private async Task LoadOperatorCatalog()
        {
            if (!fileUtil.FileExists(_operatorCatalogPath))
            {
                throw new InvalidOperationException($"Astar Vanguard operator catalog is missing: {_operatorCatalogPath}");
            }

            _operatorProfiles = await jsonUtil.DeserializeFromFileAsync<List<VanguardOperatorProfile>>(_operatorCatalogPath) ?? [];
            if (_operatorProfiles.Count != 37)
            {
                throw new InvalidOperationException($"Astar Vanguard requires exactly 37 local operators, but {_operatorProfiles.Count} were loaded.");
            }

            var duplicateCodeName = _operatorProfiles
                .GroupBy(x => x.CodeName, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateCodeName is not null)
            {
                throw new InvalidOperationException($"Duplicate Astar Vanguard operator codename: {duplicateCodeName.Key}");
            }
        }

        private VanguardOperatorProfile SelectOperator(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            if (_operatorProfiles.Count == 0)
            {
                throw new InvalidOperationException("Astar Vanguard operator catalog has not been loaded.");
            }

            var usedCodeNames = new HashSet<string>(StringComparer.Ordinal);
            if (_profiles.TryGetValue(mcsLeadPlayerId, out var existingProfiles))
            {
                foreach (var profile in existingProfiles.Values)
                {
                    var nickname = profile.CharacterData?.PmcData?.Info?.Nickname;
                    if (!string.IsNullOrWhiteSpace(nickname))
                    {
                        usedCodeNames.Add(nickname);
                    }
                }
            }

            uint seed = 2166136261;
            foreach (var ch in mcsBotPlayerId.ToString())
            {
                seed ^= ch;
                seed *= 16777619;
            }
            var start = (int)(seed % (uint)_operatorProfiles.Count);
            for (var offset = 0; offset < _operatorProfiles.Count; offset++)
            {
                var candidate = _operatorProfiles[(start + offset) % _operatorProfiles.Count];
                if (!usedCodeNames.Contains(candidate.CodeName))
                {
                    return candidate;
                }
            }

            return _operatorProfiles[start];
        }

        public IReadOnlyList<VanguardOperatorProfile> GetOperatorCatalog()
        {
            return _operatorProfiles;
        }

        public VanguardOperatorProfile? GetOperatorById(string operatorId)
        {
            return _operatorProfiles.FirstOrDefault(
                x => string.Equals(x.Id, operatorId, StringComparison.Ordinal)
            );
        }

        public SptProfile? GetProfileByOperatorId(
            MongoId mcsLeadPlayerId,
            string operatorId
        )
        {
            var operatorProfile = GetOperatorById(operatorId);
            if (operatorProfile is null)
            {
                return null;
            }

            return GetAllMcsBotPlayerProfileByBossId(mcsLeadPlayerId)
                .FirstOrDefault(
                    profile => string.Equals(
                        profile.CharacterData?.PmcData?.Info?.Nickname,
                        operatorProfile.CodeName,
                        StringComparison.Ordinal
                    )
                );
        }

        public SptProfile GeneratePermanentOperator(
            MongoId mcsLeadPlayerId,
            string operatorId
        )
        {
            var operatorProfile = GetOperatorById(operatorId)
                ?? throw new ArgumentException(
                    $"Unknown Astar Vanguard operator id: {operatorId}",
                    nameof(operatorId)
                );

            var existing = GetProfileByOperatorId(mcsLeadPlayerId, operatorId);
            if (existing is not null)
            {
                return existing;
            }

            var fullProfile = profileHelper.GetFullProfile(mcsLeadPlayerId)
                ?? throw new InvalidOperationException(
                    $"Unable to find lead profile {mcsLeadPlayerId}."
                );
            var leadPmcData = fullProfile.CharacterData?.PmcData
                ?? throw new InvalidOperationException(
                    $"Lead profile {mcsLeadPlayerId} has no PMC data."
                );

            var mcsBotPlayerId = new MongoId();
            var compatibilityOrder = new OrderInfo
            {
                McsLeadPlayerId = mcsLeadPlayerId,
                QuestId = new MongoId(),
                PlayerIds = [mcsBotPlayerId],
                SpawnType = configService.TryGetSpawnType(0),
                CarryServiceLevel = 5,
                Duration = 0,
                Status = EInfoStatus.Started,
                ExpirationTime = long.MaxValue
            };

            return Generate(
                mcsLeadPlayerId,
                mcsBotPlayerId,
                leadPmcData,
                compatibilityOrder,
                operatorProfile
            );
        }

        public SptProfile? GetMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            if (_profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles))
            {
                return mcsBotPlayerProfiles.FirstOrDefault(p => p.Key == mcsBotPlayerId).Value;
            }

            return null;
        }

        public SptProfile? GetMcsBotPlayerProfileByBotId(MongoId mcsBotPlayerId)
        {
            foreach (var profiles in _profiles.Values)
            {
                foreach (var profile in profiles.Values)
                {
                    if (profile.CharacterData.PmcData.SessionId.Value == mcsBotPlayerId)
                    {
                        return profile;
                    }
                }
            }

            return null;
        }

        public List<PmcData> GetMcsBotPlayerProfileForInventoryMode(MongoId mcsLeadPlayerId)
        {
            if (_mcsInventoryModeIds.TryGetValue(mcsLeadPlayerId, out var intMcsAid))
            {
                var mcsBotPlayerFullProfile = GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, intMcsAid);
                var mcsBotPlayerFullProfileClone = cloner.Clone(mcsBotPlayerFullProfile)!;

                var output = new List<PmcData>
                {
                    mcsBotPlayerFullProfileClone.CharacterData!.PmcData!,
                    mcsBotPlayerFullProfileClone.CharacterData!.ScavData!
                };

                return output;
            }

            return new();
        }

        public SptProfile? GetMcsBotPlayerFullProfileForInventoryMode(MongoId mcsLeadPlayerId)
        {
            if (_mcsInventoryModeIds.TryGetValue(mcsLeadPlayerId, out var intMcsAid))
            {
                return GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, intMcsAid);
            }

            return null;
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsLeadPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error(string.Format(serverLocalisationService.GetText(Locales.ACCOUNTIDISINVAILD), mcsAid));
            }

            return GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, intMcsAid);
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsLeadPlayerId, int mcsAid)
        {
            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return null;
                }
                return mcsBotPlayerProfiles.FirstOrDefault(p => p.Value.ProfileInfo.Aid == mcsAid).Value;
            }
            return null;
        }

        public List<SptProfile> GetAllMcsBotPlayerProfileByBossId(MongoId mcsLeadPlayerId)
        {
            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var bossCSPlayerFullProfiles);
                if (bossCSPlayerFullProfiles is null)
                {
                    return new();
                }
                List<SptProfile> mcsBotPlayerFullProfles = [.. bossCSPlayerFullProfiles.Values];
                return mcsBotPlayerFullProfles;
            }
            return new();
        }

        public async Task<bool> VerifyMcsBotPlayerAid(MongoId mcsLeadPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error(string.Format(serverLocalisationService.GetText(Locales.ACCOUNTIDISINVAILD), mcsAid));
                return false;
            }

            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return false;
                }

                var verify = mcsBotPlayerProfiles.Any(p => p.Value.ProfileInfo.Aid == intMcsAid);
                if (verify)
                {
                    if (!_mcsInventoryModeIds.TryAdd(mcsLeadPlayerId, intMcsAid))
                    {
                        _mcsInventoryModeIds.TryRemove(mcsLeadPlayerId, out _);
                        _mcsInventoryModeIds.TryAdd(mcsLeadPlayerId, intMcsAid);
                    }
                }
                return verify;
            }
            return false;
        }

        public async Task<bool> RemoveMcsBotPlayerAid(MongoId mcsLeadPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error(string.Format(serverLocalisationService.GetText(Locales.ACCOUNTIDISINVAILD), mcsAid));
                return false;
            }

            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return false;
                }

                var verify = _mcsInventoryModeIds.Any(p => p.Value == intMcsAid);
                if (verify)
                {
                    verify = _mcsInventoryModeIds.TryRemove(mcsLeadPlayerId, out _);
                }
                return verify;
            }
            return false;
        }

        public void RemoveMcsBotPlayerAid(MongoId mcsLeadPlayerId)
        {
            _mcsInventoryModeIds.TryRemove(mcsLeadPlayerId, out _);
        }

        public bool IsMcsBotPlayerInventoryMode(MongoId mcsLeadPlayerId)
        {
            return _mcsInventoryModeIds.ContainsKey(mcsLeadPlayerId);
        }

        private int GetRandomLevelByCarryServiceLevel(int carryServiceLevel)
        {
            return carryServiceLevel switch
            {
                1 => randomUtil.RandInt(1, 15),
                2 => randomUtil.RandInt(15, 30),
                3 => randomUtil.RandInt(30, 50),
                4 => randomUtil.RandInt(50, 70),
                >= 5 => randomUtil.RandInt(70, 79),
                _ => randomUtil.RandInt(1, 15)
            };
        }

        public SptProfile Generate(
            MongoId mcsLeadPlayerId,
            MongoId mcsBotPlayerId,
            PmcData completeQuestPmcData,
            OrderInfo orderInfo
        )
        {
            return Generate(
                mcsLeadPlayerId,
                mcsBotPlayerId,
                completeQuestPmcData,
                orderInfo,
                null
            );
        }

        private SptProfile Generate(
            MongoId mcsLeadPlayerId,
            MongoId mcsBotPlayerId,
            PmcData completeQuestPmcData,
            OrderInfo orderInfo,
            VanguardOperatorProfile? forcedOperator
        )
        {
            var isPmc = orderInfo.SpawnType.WildSpawnType is "common" or "pmcUSEC" or "pmcBEAR";
            var botDifficulty = (BotDifficulty)orderInfo.CarryServiceLevel;
            var role = isPmc ? (completeQuestPmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR") : orderInfo.SpawnType.WildSpawnType;
            var level = GetRandomLevelByCarryServiceLevel(orderInfo.CarryServiceLevel + (orderInfo.SpawnType.IsBoss ? 1 : 0));
            var botGenerationDetails = new BotGenerationDetails()
            {
                IsPmc = isPmc,
                Side = isPmc ? completeQuestPmcData.Info.Side : "Savage",
                Role = role,
                BotLevel = level,
                PlayerLevel = level,
                BotRelativeLevelDeltaMin = 0,
                BotRelativeLevelDeltaMax = 0,
                BotCountToGenerate = 1,
                BotDifficulty = botDifficulty <= BotDifficulty.Impossible && botDifficulty > BotDifficulty.AsOnline ? botDifficulty is BotDifficulty.Medium ? "normal" : botDifficulty.ToString().ToLower() : "impossible",
                IsPlayerScav = false,
                AllPmcsHaveSameNameAsPlayer = false
            };

            // 适配APBS重复添加Tier
            var clonedBotGenerationDetails = cloner.Clone(botGenerationDetails);

            PmcData pmcData;
            try
            {
                pmcData = GeneratePmcData(
                    mcsLeadPlayerId,
                    mcsBotPlayerId,
                    botGenerationDetails,
                    orderInfo,
                    forcedOperator
                );

            }
            catch (Exception e)
            {
                var msg = string.Format(serverLocalisationService.GetText(Locales.GENERATEPROFILEERROR), botGenerationDetails.Role);
                logger.Error(msg, e);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    mcsLeadPlayerId,
                    TraderService.VanguardTraderId,
                    MessageType.NpcTraderMessage,
                    msg + $"\n{e.Message}",
                    null
                );
                botGenerationDetails.Side = completeQuestPmcData.Info.Side;
                botGenerationDetails.Role = completeQuestPmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR";
                pmcData = GeneratePmcData(
                    mcsLeadPlayerId,
                    mcsBotPlayerId,
                    botGenerationDetails,
                    orderInfo,
                    forcedOperator
                );
            }
            pmcData.Info.Level = botGenerationDetails.PlayerLevel;

            PmcData scavData;
            try
            {
                scavData = isPmc ? GenerateScavData(mcsLeadPlayerId, clonedBotGenerationDetails, pmcData, orderInfo) : GenerateScavData(pmcData, clonedBotGenerationDetails);
            }
            catch (Exception e)
            {
                var msg = string.Format(serverLocalisationService.GetText(Locales.GENERATEPROFILEERROR), clonedBotGenerationDetails.Role);
                logger.Error(msg, e);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    mcsLeadPlayerId,
                    TraderService.VanguardTraderId,
                    MessageType.NpcTraderMessage,
                    msg + $"\n{e.Message}",
                    null
                );

                clonedBotGenerationDetails.Role = "assault";
                scavData = isPmc ? GenerateScavData(mcsLeadPlayerId, clonedBotGenerationDetails, pmcData, orderInfo) : GenerateScavData(pmcData, clonedBotGenerationDetails);
            }

            scavData.Info.Bans = [];
            scavData.Info.RegistrationDate = pmcData.Info.RegistrationDate;
            scavData.Info.GameVersion = pmcData.Info.GameVersion;
            scavData.Info.MemberCategory = pmcData.Info.MemberCategory;
            scavData.Info.SelectedMemberCategory = pmcData.Info.SelectedMemberCategory;
            scavData.Info.LockedMoveCommands = true;
            scavData.Info.MainProfileNickname = pmcData.Info.Nickname;
            scavData.Info.Level = pmcData.Info.Level;
            scavData.Info.Experience = pmcData.Info.Experience;

            var fullProfile = GenerateFullProfile(pmcData, scavData);
            var userBuilds = buildsService.GetUserBuilds(mcsLeadPlayerId);
            if (userBuilds != null)
            {
                fullProfile.UserBuildData = userBuilds;
            }

            if (fullProfile.CharacterData.PmcData.Inventory.Items != null)
            {
                foreach (var item in fullProfile.CharacterData.PmcData.Inventory.Items)
                {
                    fullProfile.CharacterData.PmcData.Encyclopedia.TryAdd(item.Template, true);
                }
            }

            buildsService.ExaminedUserBuildsItem(fullProfile, fullProfile.UserBuildData);
            _ = SaveMcsBotPlayerProfile(mcsLeadPlayerId, fullProfile);
            return fullProfile;
        }

        private PmcData GeneratePmcData(
            MongoId mcsLeadPlayerId,
            MongoId mcsBotPlayerId,
            BotGenerationDetails botGenerationDetails,
            OrderInfo orderInfo,
            VanguardOperatorProfile? forcedOperator = null
        )
        {
            var botBase = compatibilityService.HasAPBS ? botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails) : mcsBotGenerator.CustomPrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails, orderInfo);

            var operatorProfile = forcedOperator ?? SelectOperator(mcsLeadPlayerId, mcsBotPlayerId);
            botBase.Info.Nickname = operatorProfile.CodeName;
            botBase.Info.LowerNickname = operatorProfile.CodeName.ToLowerInvariant();

            botBase.Info.Level = botGenerationDetails.PlayerLevel;
            botBase.Id = mcsBotPlayerId;
            botBase.SessionId = mcsBotPlayerId;
            botBase.Aid = hashUtil.GenerateAccountId();

            var tradersInfo = new Dictionary<MongoId, TraderInfo>();
            var traders = databaseService.GetTraders();

            foreach (var (traderId, trader) in traders)
            {
                tradersInfo[traderId] = new TraderInfo
                {
                    LoyaltyLevel = 4,
                    SalesSum = 999999999.0,
                    Standing = 10.0,
                    NextResupply = trader.Base?.NextResupply ?? 0,
                    Unlocked = true,
                    Disabled = false,
                };
            }

            var areas = new List<BotHideoutArea>();

            var dbHideout = databaseService.GetHideout();
            var hideoutAreas = dbHideout.Areas;

            foreach (HideoutAreas areaType in Enum.GetValues(typeof(HideoutAreas)))
            {
                if (areaType == HideoutAreas.NotSet)
                {
                    continue;
                }

                var dbArea = hideoutAreas.FirstOrDefault(area => area.Type == areaType);
                int maxLevel = 0;
                MongoId? containerId = null;

                if (dbArea != null && dbArea.Stages != null)
                {
                    maxLevel = dbArea.Stages.Count - 1; // 等级从0开始，所以减1  

                    if (maxLevel >= 0 && dbArea.Stages.TryGetValue(maxLevel.ToString(), out var maxStage))
                    {
                        if (maxStage.Container.HasValue && !maxStage.Container.Value.IsEmpty)
                        {
                            containerId = maxStage.Container.Value;
                        }
                    }
                }

                areas.Add(new BotHideoutArea
                {
                    Type = areaType,
                    Level = maxLevel,
                    Active = true,
                    PassiveBonusesEnabled = areaType != HideoutAreas.ChristmasIllumination,
                    CompleteTime = 0,
                    Constructing = false,
                    Slots = new(),
                    LastRecipe = "",
                });
            }

            var random = new Random();
            var bytes = new byte[16];
            random.NextBytes(bytes);
            var hideoutSeed = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            var hideout = new Hideout
            {
                Areas = areas,
                Production = new(),
                Improvements = new(),
                Seed = hideoutSeed,
                Customization = new()
                {
                    { "Wall", "675844bdf94a97cbbe096f1a" },
                    { "Floor", "6758443ff94a97cbbe096f18" },
                    { "Light", "675fe8abbc3deae49a0b947f" },
                    { "Ceiling", "673b3f977038192ee006aa09" },
                    { "ShootingRangeMark", "67585d416c72998cf60ed85a" },
                },
                MannequinPoses = new(),
            };

            var moneyTransferLimitData = new MoneyTransferLimits
            {
                NextResetTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 86400,
                RemainingLimit = 1000000,
                TotalLimit = 1000000,
                ResetInterval = 86400,
            };

            var expTable = databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable;
            botBase.Info.Experience = expTable.Take(botGenerationDetails.PlayerLevel.Value).Sum(entry => entry.Experience);

            var pmcData = new PmcData
            {
                Id = botBase.Id,
                Aid = botBase.Aid,
                SessionId = botBase.SessionId,
                KarmaValue = botBase.KarmaValue,
                Info = botBase.Info,
                Customization = botBase.Customization,
                Health = botBase.Health,
                Inventory = botBase.Inventory,
                Skills = botBase.Skills,
                Stats = botBase.Stats,
                Encyclopedia = new(),
                TaskConditionCounters = botBase.TaskConditionCounters,
                InsuredItems = botBase.InsuredItems,
                Hideout = hideout,
                Quests = new(),
                TradersInfo = tradersInfo,
                UnlockedInfo = botBase.UnlockedInfo,
                RagfairInfo = botBase.RagfairInfo,
                Achievements = new(),
                RepeatableQuests = new(),
                Bonuses = botBase.Bonuses,
                Notes = new(),
                CarExtractCounts = botBase.CarExtractCounts,
                CoopExtractCounts = botBase.CoopExtractCounts,
                SurvivorClass = botBase.SurvivorClass,
                WishList = botBase.WishList,
                MoneyTransferLimitData = moneyTransferLimitData,
                IsPmc = botBase.IsPmc,
                Prestige = new(),
            };

            var currencies = new Dictionary<MongoId, int>
            {
                { Money.EUROS, 114514 },
                { Money.DOLLARS, 1919810 },
                { Money.GP, 1314 },
                { Money.ROUBLES, 151247016 },
            };

            var stashId = pmcData.Inventory.Stash.Value;
            var inventoryHelperTraverse = Traverse.Create(inventoryHelper);

            foreach (var (moneyId, amount) in currencies)
            {
                var item = new Item
                {
                    Id = new(),
                    Template = moneyId,
                    Upd = new Upd
                    {
                        StackObjectsCount = amount
                    }
                };

                var stashFS2D = inventoryHelperTraverse.Method("GetStashSlotMap", [pmcData]).GetValue<int[,]>();
                var placeResult = inventoryHelper.PlaceItemInContainer(
                    stashFS2D,
                    [item],
                    stashId,
                    "hideout"
                );

                if (!placeResult.Success.GetValueOrDefault(false))
                {
                    continue;
                }

                pmcData.Inventory.Items.Add(item);
            }

            if (pmcData.Inventory.HideoutAreaStashes == null)
            {
                pmcData.Inventory.HideoutAreaStashes = new Dictionary<string, MongoId>();
            }

            foreach (var dbArea in hideoutAreas)
            {
                if (dbArea.Type == null || dbArea.Stages == null)
                {
                    continue;
                }

                var profileArea = areas.FirstOrDefault(area => area.Type == dbArea.Type);
                if (profileArea == null || profileArea.Level < 0)
                {
                    continue;
                }

                if (!dbArea.Stages.TryGetValue(profileArea.Level.ToString(), out var stage))
                {
                    continue;
                }

                if (!stage.Container.HasValue || stage.Container.Value.IsEmpty)
                {
                    continue;
                }

                var keyForHideoutAreaStash = ((int)dbArea.Type).ToString();
                if (!pmcData.Inventory.HideoutAreaStashes.ContainsKey(keyForHideoutAreaStash))
                {
                    pmcData.Inventory.HideoutAreaStashes[keyForHideoutAreaStash] = dbArea.Id;
                }

                var existingInventoryItem = pmcData.Inventory.Items.FirstOrDefault(item => item.Id == dbArea.Id);
                if (existingInventoryItem == null)
                {
                    var newContainerItem = new Item
                    {
                        Id = dbArea.Id,
                        Template = stage.Container.Value
                    };
                    pmcData.Inventory.Items.Add(newContainerItem);
                }
                else
                {
                    existingInventoryItem.Template = stage.Container.Value;
                }
            }

            return pmcData;
        }

        private PmcData GenerateScavData(PmcData pmcData, BotGenerationDetails botGenerationDetails)
        {
            var scavData = cloner.Clone(pmcData);
            scavData.Id = new();

            botGenerationDetails.IsPmc = false;
            botGenerationDetails.Side = "Savage";

            scavData.Info.Nickname = botNameService.GenerateUniqueBotNickname(
                cloner.Clone(botHelper.GetBotTemplate("assault")),
                botGenerationDetails,
                configServer.GetConfig<BotConfig>().BotRolesThatMustHaveUniqueName
            );

            return scavData;
        }

        private PmcData GenerateScavData(MongoId mcsLeadPlayerId, BotGenerationDetails botGenerationDetails, PmcData pmcData, OrderInfo orderInfo)
        {
            var scavKarmaLevel = Math.Clamp(orderInfo.CarryServiceLevel + 2, -7, 6);
            var playerScavConfig = configServer.GetConfig<PlayerScavConfig>();

            if (!playerScavConfig.KarmaLevel.TryGetValue(scavKarmaLevel.ToString(CultureInfo.InvariantCulture), out var playerScavKarmaSettings))
            {
                logger.Error(serverLocalisationService.GetText("scav-missing_karma_settings", scavKarmaLevel));
            }

            botGenerationDetails.IsPmc = false;
            botGenerationDetails.Side = "Savage";
            botGenerationDetails.Role = playerScavKarmaSettings.BotTypeForLoot.ToLowerInvariant();

            var baseBotNode = cloner.Clone(botHelper.GetBotTemplate("assault"));

            var playerScavGeneratorTraverse = Traverse.Create(playerScavGenerator);
            playerScavGeneratorTraverse.Method("AdjustBotTemplateWithKarmaSpecificSettings", [playerScavKarmaSettings, baseBotNode]).GetValue();

            var botBase = compatibilityService.HasAPBS ? botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails) : mcsBotGenerator.CustomPrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails, orderInfo);

            var expTable = databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable;
            botBase.Info.Experience = expTable.Take(botGenerationDetails.PlayerLevel.Value).Sum(entry => entry.Experience);

            var scavData = new PmcData
            {
                Id = new(),
                Aid = pmcData.Aid,
                SessionId = pmcData.SessionId,
                Savage = null,
                KarmaValue = botBase.KarmaValue,
                Info = botBase.Info,
                Customization = botBase.Customization,
                Health = botBase.Health,
                Inventory = botBase.Inventory,
                Skills = botBase.Skills,
                Stats = botBase.Stats,
                Encyclopedia = pmcData.Encyclopedia,
                TaskConditionCounters = botBase.TaskConditionCounters,
                InsuredItems = botBase.InsuredItems,
                Hideout = botBase.Hideout,
                Quests = botBase.Quests,
                TradersInfo = pmcData.TradersInfo,
                UnlockedInfo = pmcData.UnlockedInfo,
                RagfairInfo = pmcData.RagfairInfo,
                Achievements = botBase.Achievements,
                RepeatableQuests = botBase.RepeatableQuests,
                Bonuses = botBase.Bonuses,
                Notes = botBase.Notes,
                CarExtractCounts = botBase.CarExtractCounts,
                CoopExtractCounts = botBase.CoopExtractCounts,
                SurvivorClass = botBase.SurvivorClass,
                WishList = botBase.WishList,
                MoneyTransferLimitData = botBase.MoneyTransferLimitData,
                IsPmc = botBase.IsPmc,
                Variables = new(),
                Prestige = new()
            };

            playerScavGeneratorTraverse.Method("AddAdditionalLootToPlayerScavContainers", [
                scavData.Id.Value,
                playerScavKarmaSettings.LootItemsToAddChancePercent,
                scavData,
                new HashSet<EquipmentSlots>{
                    EquipmentSlots.TacticalVest, EquipmentSlots.Pockets, EquipmentSlots.Backpack
                }
            ]).GetValue();

            botInventoryContainerService.ClearCache(scavData.Id.Value);
            botLootCacheService.ClearCache();

            return scavData;
        }

        private SptProfile GenerateFullProfile(PmcData pmcData, PmcData scavData)
        {
            pmcData.Savage = scavData.Id;
            scavData.SessionId = pmcData.SessionId;
            return new SptProfile
            {
                ProfileInfo = new SPTarkov.Server.Core.Models.Eft.Profile.Info
                {
                    ProfileId = pmcData.SessionId,
                    Username = pmcData.Info.Nickname,
                    Aid = pmcData.Aid,
                    ScavengerId = scavData.Id
                },
                CharacterData = new Characters
                {
                    PmcData = pmcData,
                    ScavData = scavData
                },
                UserBuildData = new UserBuilds
                {
                    EquipmentBuilds = [],
                    WeaponBuilds = [],
                    MagazineBuilds = [],
                },
                DialogueRecords = new(),
                SptData = profileHelper.GetDefaultSptDataObject(),
                InraidData = new(),
                InsuranceList = [],
                BtrDeliveryList = [],
                TraderPurchases = [],
                FriendProfileIds = [],
                CustomisationUnlocks = [],
            };
        }

        public bool SettleOrder(MongoId mcsLeadPlayerId, string aid)
        {
            if (IsMcsBotPlayerInventoryMode(mcsLeadPlayerId))
            {
                return false;
            }
            var profile = GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, aid);
            if (profile is null)
            {
                return false;
            }
            var botProfileId = profile.ProfileInfo.ProfileId.Value;
            var playerIds = infoService.SettleOrderByBotPlayerProfileId(botProfileId);
            if (playerIds is null)
            {
                return false;
            }
            ProcessExpiredMcsBotPlayerProfiles(mcsLeadPlayerId, playerIds);
            return true;
        }

        public async Task OnPostLoadAsync()
        {
            await LoadOperatorCatalog();
            await LoadAllMcsBotPlayerProfile();
        }
    }
}