using System;
using MoSimCore.Enums;
using RobotFramework;
using UnityEngine;

namespace GameSystems.Management
{
    [Serializable]
    public class MatchData
    {
        [field: SerializeField]
        public RobotMetadataSO BlueRobot { get; set; }
        [field: SerializeField]
        public RobotMetadataSO RedRobot { get; set; }

        public void SetSelectedRobot(Alliance alliance, RobotMetadataSO robotMetadata)
        {
            switch (alliance)
            {
                case Alliance.Blue:
                    BlueRobot = robotMetadata;
                    break;
                case Alliance.Red:
                    RedRobot = robotMetadata;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(alliance), alliance, null);
            }
        }
        
        public RobotMetadataSO GetSelectedRobot(Alliance alliance)
        {
            return alliance switch
            {
                Alliance.Blue => BlueRobot,
                Alliance.Red => RedRobot,
                _ => throw new ArgumentOutOfRangeException(nameof(alliance), alliance, null)
            };
        }
    }
}