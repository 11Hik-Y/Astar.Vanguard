using System;
using System.Linq;
using EFT.Interactive;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Utils;
using UnityEngine;

namespace Astar.Vanguard.Client.Datas
{
    public class ExfilData : TriggerData
    {
        private WeakReference<ExfiltrationPoint> _exfilRef;
        public ExfiltrationPoint ExfiltrationPoint => _exfilRef.TryGetTarget(out var exfil) ? exfil : null;

        public ExfilData(ExfiltrationPoint exfiltrationPoint) : base()
        {
            _exfilRef = new WeakReference<ExfiltrationPoint>(exfiltrationPoint);
            _colliders = exfiltrationPoint.transform.GetComponentsInChildren<Collider>().ToList();
        }

        public override string GetActionName() => ExfiltrationPoint.Settings.Name.McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, ExfiltrationPoint.gameObject.transform.position)));
        
        public override bool IsDisabled() => ExfiltrationPoint.Status switch
        {
            EExfiltrationStatus.NotPresent => true,
            _ => false
        };

        public override void Dispose()
        {
            base.Dispose();
            _exfilRef = null;
        }
    }
}