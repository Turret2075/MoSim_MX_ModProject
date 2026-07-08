using RobotFramework;

namespace Games.Reefscape.Robots
{
    public interface IReefscapeRobotMetadata : IRobotMetadata
    {
        bool AutoClimbs { get; }
    }
}