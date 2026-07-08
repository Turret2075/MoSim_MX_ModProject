using MoSimCore.BaseClasses.GameManagement.AudioManagement;
using UnityEngine;

namespace Games.Reefscape.GameManagement
{
    public class ReefscapeAudioManager : BaseAudioManager
    {
        [SerializeField] private AudioClip matchStartClip;
        [SerializeField] private AudioClip teleopStartClip;
        [SerializeField] private AudioClip endgameStartClip;
        [SerializeField] private AudioClip matchEndClip;

        protected override AudioClip MatchStartClip => matchStartClip;
        protected override AudioClip TeleopStartClip => teleopStartClip;
        protected override AudioClip EndgameStartClip => endgameStartClip;
        protected override AudioClip MatchEndClip => matchEndClip;
    }
}