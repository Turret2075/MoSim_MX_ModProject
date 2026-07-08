using System.Collections;
using UnityEngine;

namespace RobotFramework.Controllers.GamePieceSystem
{
    public abstract class RobotGamePieceControllerBase : MonoBehaviour
    {
        public abstract void RunCoroutine(IEnumerator routine);
    }
}