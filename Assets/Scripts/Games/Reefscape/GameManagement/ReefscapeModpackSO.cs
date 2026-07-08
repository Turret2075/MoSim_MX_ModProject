using GameSystems.Management;
using UnityEngine;

namespace Games.Reefscape.GameManagement
{
    /// <summary>
    /// Reefscape modpack metadata. Extend BaseModpackSO for Reefscape-specific packs.
    /// Create via Assets > Create > Games > Reefscape > ReefscapeModpackMetadata.
    /// </summary>
    [CreateAssetMenu(fileName = "ReefscapeModpack", menuName = "Games/Reefscape/ReefscapeModpackMetadata", order = 0)]
    public class ReefscapeBaseModpackSo : BaseModpackSO
    {
    }
}