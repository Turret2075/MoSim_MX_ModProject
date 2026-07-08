// using System;
// using MoSimCore;
// using MoSimCore.Enums;
// using MoSimCore.Interfaces;
// using MoSimLib;
// using MoSimLib.Robots;
// using UnityEngine;
//
// namespace Games.Reefscape.Scoring.Scorers
// {
//     public class OldClimbScorer : MonoBehaviour, IScorer
//     {
//         private RobotBase _robotBase;
//         
//         [field: SerializeField]
//         public bool IsTouchingGround { get; private set; }
//
//         // Use a simple reference count so multiple systems (drive raycasts, trigger checkers, etc.)
//         // can report contacts without clobbering each other's state.
//         private int _groundContactCount = 0;
//
//         public void AddTouchingGround()
//         {
//             _groundContactCount = Math.Max(0, _groundContactCount + 1);
//             IsTouchingGround = _groundContactCount > 0;
//         }
//
//         public void RemoveTouchingGround()
//         {
//             _groundContactCount = Math.Max(0, _groundContactCount - 1);
//             IsTouchingGround = _groundContactCount > 0;
//         }
//
//         /// <summary>
//         /// Force-clear any tracked ground contacts (useful on disable/reset).
//         /// </summary>
//         public void ResetGroundContacts()
//         {
//             _groundContactCount = 0;
//             IsTouchingGround = false;
//         }
//
//         [SerializeField] private CageAndGate robotCageDetector;
//
//         private void Start()
//         {
//             _robotBase = GetComponentInParent<RobotBase>();
//             if (_robotBase == null)
//             {
//                 Debug.LogError("ClimbScorer: RobotBase component not found in parent.");
//             }
//         }
//
//         private void Update()
//         {
//             if (_robotBase is null || GameManager.Instance.GameState != GameState.Endgame) return;
//
//             switch (_robotBase.SimRobot.GetAlliance())
//             {
//                 case Alliance.Blue:
//                     if (!IsTouchingGround && _robotBase.InBlueBargeZone && _robotBase.CurrentSetpoint == Setpoints.Climbed &&
//                         robotCageDetector.HookedInCage)
//                     {
//                         if (!ScoreHandler.BlueClimbScorers.Contains(this))
//                         {
//                             ScoreHandler.BlueClimbScorers.Add(this);
//                         }
//                     }
//                     else
//                     {
//                         ScoreHandler.BlueClimbScorers.Remove(this);
//                     }
//
//                     if (IsTouchingGround && _robotBase.InBlueBargeZone && !ScoreHandler.BlueClimbScorers.Contains(this))
//                     {
//                         ScoreHandler.BlueParkScorers.Add(this);
//                     }
//                     else if ((!IsTouchingGround || !_robotBase.InBlueBargeZone) && ScoreHandler.BlueParkScorers.Contains(this))
//                     {
//                         ScoreHandler.BlueParkScorers.Remove(this);
//                     }
//                     break;
//                 case Alliance.Red:
//                     if (!IsTouchingGround && _robotBase.InRedBargeZone && _robotBase.CurrentSetpoint == Setpoints.Climbed 
//                         && robotCageDetector.HookedInCage)
//                     {
//                         if (!ScoreHandler.RedClimbScorers.Contains(this))
//                         {
//                             ScoreHandler.RedClimbScorers.Add(this);
//                         }
//                     }
//                     else
//                     {
//                         ScoreHandler.RedClimbScorers.Remove(this);
//                     }
//                     
//                     if (IsTouchingGround && _robotBase.InRedBargeZone && !ScoreHandler.RedClimbScorers.Contains(this))
//                     {
//                         ScoreHandler.RedParkScorers.Add(this);
//                     }
//                     else if ((!IsTouchingGround || !_robotBase.InRedBargeZone) && ScoreHandler.RedParkScorers.Contains(this))
//                     {
//                         ScoreHandler.RedParkScorers.Remove(this);
//                     }
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//         }
//
//         public Alliance Alliance { get; }
//         public void AddScore(IScoreData scoreData, GameState gameState)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }
