using RobotFramework.Controllers.GamePieceSystem;

namespace Games.Reefscape.GamePieceSystem
{
    public class ReefscapeRobotGamePieceController : RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>
    {
        private new void OnEnable()
        {
            gamePieceNodes.Clear();

            var coralNode = new RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode();
            coralNode.pieceName = "Coral";
            
            var algaeNode = new RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode();
            algaeNode.pieceName = "Algae";
            
            gamePieceNodes.Add(coralNode);
            gamePieceNodes.Add(algaeNode);
            
            base.OnEnable();
        }
    }
}