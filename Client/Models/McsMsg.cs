
using UnityEngine;

namespace Astar.Vanguard.Client.Models
{
    public class McsMsg
    {
        public EPhraseTrigger PhraseTrigger = EPhraseTrigger.None;
        public Vector3? Position = null;
        public string[] Keys = null;
    }
}