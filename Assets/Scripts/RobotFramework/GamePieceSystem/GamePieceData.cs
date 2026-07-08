using System;
using UnityEngine;

namespace RobotFramework.GamePieceSystem
{
    [Serializable]
    public struct GamePieceData
    {
        public int stateNum;
        public Transform stateTarget;
        public float stateMoveSpeed;
        public float stateAngularSpeed;
        public bool smoothHandoff;

        public GamePieceData(int num, Transform target, float moveSpeed, float angularSpeed, bool handoff)
        {
            stateNum = num;
            stateTarget = target;
            stateMoveSpeed = moveSpeed;
            stateAngularSpeed = angularSpeed;
            smoothHandoff = handoff;
        }
    }
}