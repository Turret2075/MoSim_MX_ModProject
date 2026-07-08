using System;
using RobotFramework.Interfaces;
using UnityEngine;

namespace RobotFramework.GamePieceSystem
{
    [Serializable]
    public class GamePiece<TData> where TData : IGamePieceData
    {
        public Rigidbody rigidbody;
        public GameObject gameObject;
        public Transform transform;
        public GameObject owner;
        
        public TData GamePieceData { get; private set; }
        
        public bool isScored;
        
        public void Initialize(TData data, GameObject gameObject)
        {
            GamePieceData = data;
            this.gameObject = gameObject;
        }
    }
}