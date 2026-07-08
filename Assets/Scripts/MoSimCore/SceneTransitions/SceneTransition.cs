using System.Collections;
using UnityEngine;

namespace MoSimCore.SceneTransitions
{
    /// <summary>
    /// Abstract base class for scene transition animations.
    /// Inherit from this class to create custom transition effects.
    /// </summary>
    public abstract class SceneTransition : MonoBehaviour
    {
        /// <summary>
        /// Animates the transition entering the screen (e.g., fade to black).
        /// </summary>
        /// <returns>An enumerator for coroutine execution.</returns>
        public abstract IEnumerator AnimateTransitionIn();
        
        /// <summary>
        /// Animates the transition leaving the screen (e.g., fade from black).
        /// </summary>
        /// <returns>An enumerator for coroutine execution.</returns>
        public abstract IEnumerator AnimateTransitionOut();
    }
}