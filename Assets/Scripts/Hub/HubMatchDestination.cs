using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Production scene routing only; contains no profile or economy state.</summary>
    public sealed class HubMatchDestination : ScriptableObject
    {
        public string sceneName = "PrototypePool";
    }
}
