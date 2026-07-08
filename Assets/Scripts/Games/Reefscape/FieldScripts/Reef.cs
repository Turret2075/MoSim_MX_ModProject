using MoSimCore.Enums;
using UnityEngine;

namespace Games.Reefscape.FieldScripts
{
    public class Reef : MonoBehaviour
    {
        [field: SerializeField] public Alliance Alliance { get; private set; } = Alliance.Blue;
    }
}