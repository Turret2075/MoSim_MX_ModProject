using System;
using UnityEngine;

namespace RobotFramework.GamePieceSystem
{
    [Serializable]
    public class GamePieceState
    {
        /// <summary>
        /// Should be a lowercase copy of the variable name
        /// </summary>
        public string name;

        /// <summary>
        /// should not match the state of another state that handles the same game piece
        /// </summary>
        public int stateNum;
        
        /// <summary>
        /// inch/sec
        /// </summary>
        public float stateMoveSpeed;

        /// <summary>
        /// The location to target
        /// </summary>
        public Transform stateTarget;

        /// <summary>
        /// if true it will not teleport to target at the end of the execution. NOTE: not using this without immediately moving to a new state will result in detachment
        /// </summary>
        public bool smoothHandoff;
        
        /// <summary>
        /// deg/sec
        /// </summary>
        public float angularSpeed;
    } 
}