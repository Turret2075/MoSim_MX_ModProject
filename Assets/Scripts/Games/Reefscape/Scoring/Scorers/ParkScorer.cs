// using System;
// using MoSimCore;
// using MoSimLib;
// using Robot.Robots;
// using UnityEngine;
//
// namespace Robot.Climbing
// {
//     public class ParkScorer : MonoBehaviour
//     {
//         private IGameState _gameState;
//         
//         private RobotBase _robotBase;
//         private ClimbScorer _scorer;
//
//         private void Awake()
//         {
//             _scorer = GetComponent<ClimbScorer>();
//             _robotBase = GetComponentInParent<RobotBase>();
//             if (_robotBase == null)
//             {
//                 Debug.LogError("ParkScorer: RobotBase component not found in parent.");
//             }
//             
//         }
//
//         private void Update()
//         {
//             if (_robotBase is null || _gameState.GameState != GameState.Endgame) return;
//
//             switch (_robotBase.SimRobot.GetAlliance())
//             {
//                 case Alliance.Blue:
//                     if (_scorer.IsTouchingGround && _robotBase.InBlueBargeZone)
//                     {
//                         if (!ScoreHandler.BlueParkScorers.Contains(this))
//                         {
//                             ScoreHandler.BlueParkScorers.Add(this);
//                         }
//                     }
//                     else
//                     {
//                         ScoreHandler.BlueParkScorers.Remove(this);
//                     }
//                     break;
//                 case Alliance.Red:
//                     if (_scorer.IsTouchingGround && _robotBase.InRedBargeZone)
//                     {
//                         if (!ScoreHandler.RedParkScorers.Contains(this))
//                         {
//                             ScoreHandler.RedParkScorers.Add(this);
//                         }
//                     }
//                     else
//                     {
//                         ScoreHandler.RedParkScorers.Remove(this);
//                     }
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//         }
//     }
// }
