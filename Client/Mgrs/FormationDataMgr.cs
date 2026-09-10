
using System.Collections.Generic;
using EFT;
using Astar.Vanguard.Client.Datas;
using Astar.Vanguard.Client.Events;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Models;
using Astar.Vanguard.Client.Utils;
using SPT.Common.Utils;

namespace Astar.Vanguard.Client.Mgrs
{
    public class FormationDataMgr : DataMgr
    {
        public override void Start()
        {
            base.Start();
            LoadFormationPreset();
        }

        public override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            OnRaidEnded();
        }

        void Update()
        {
            if (KeyInput.BetterIsDown(AstarVanguardPlugin.SaveFormationPresetHotKey.Value))
            {
                AddFormation("New Formation", AstarVanguardPlugin.FormationMatrix.Value);
                NotificationManagerClass.DisplayMessageNotification(Locales.SAVEFORMATIONPRESET.McsLocalized());
            }
        }

        public void LoadFormationPreset()
        {
            var formationDataDtos = Json.Deserialize<List<FormationDataDto>>(AstarVanguardPlugin.FormationPresets.Value);
            DataClear();
            foreach (var formationDataDto in formationDataDtos)
            {
                _datas.Add(new FormationData(formationDataDto.Id, formationDataDto.Name, formationDataDto.FormationMatrix));
            }
        }

        public void SaveFormationPresets()
        {
            List<FormationDataDto> formationDataDtos = new();
            foreach (FormationData formationData in _datas)
            {
                var formationDataDto = new FormationDataDto
                {
                    Id = formationData.Id,
                    Name = formationData.Name,
                    FormationMatrix = formationData.FormationMatrix
                };
                formationDataDtos.Add(formationDataDto);
            }
            AstarVanguardPlugin.FormationPresets.Value = Json.Serialize(formationDataDtos);
            LoadFormationPreset();
        }

        public void SaveFormationPreset(MongoID id, string rename, string formationMatrix)
        {
            foreach (FormationData formationData in _datas)
            {
                if (formationData.Id == id)
                {
                    formationData.Name = rename;
                    formationData.FormationMatrix = formationMatrix;
                    break;
                }
            }
            SaveFormationPresets();
        }

        public void AddFormation(string name, string formationMatrix)
        {
            _datas.Add(new FormationData(name, formationMatrix));
            SaveFormationPresets();
        }

        public void DeleteFormation(FormationData formationData)
        {
            _datas.Remove(formationData);
            SaveFormationPresets();
        }

        public FormationData GetFormationData(MongoID id)
        {
            foreach (FormationData formationData in _datas)
            {
                if (formationData.Id == id)
                {
                    return formationData;
                }
            }
            return null;
        }

        public void ApplyFormationData(MongoID id)
        {
            var formationData = GetFormationData(id);
            if (formationData == null)
            {
                return;
            }
            AstarVanguardPlugin.FormationMatrix.Value = formationData.FormationMatrix;
        }
    }
}