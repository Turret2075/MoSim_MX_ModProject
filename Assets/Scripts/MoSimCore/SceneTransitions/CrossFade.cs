using System.Collections;
using UnityEngine;

namespace MoSimCore.SceneTransitions
{
    /// <summary>
    /// Scene transition that fades the screen to and from a solid color using a CanvasGroup.
    /// </summary>
    public class CrossFade : SceneTransition
    {
        [SerializeField]
        [Tooltip("The CanvasGroup used for the fade effect (typically a full-screen UI panel).")]
        private CanvasGroup crossFade;
        
        [SerializeField]
        [Tooltip("Duration of the fade animation in seconds.")]
        private float fadeDuration = 0.3f;
        
        /// <summary>
        /// Fades the screen from transparent to opaque.
        /// </summary>
        /// <returns>An enumerator for coroutine execution.</returns>
        public override IEnumerator AnimateTransitionIn()
        {
            yield return Fade(0f, 1f, fadeDuration);
        }

        /// <summary>
        /// Fades the screen from opaque to transparent.
        /// </summary>
        /// <returns>An enumerator for coroutine execution.</returns>
        public override IEnumerator AnimateTransitionOut()
        {
            yield return Fade(1f, 0f, fadeDuration);
        }
        
        /// <summary>
        /// Performs a smooth fade between two alpha values over the specified duration.
        /// </summary>
        /// <param name="startAlpha">The starting alpha value (0 = transparent, 1 = opaque).</param>
        /// <param name="endAlpha">The ending alpha value (0 = transparent, 1 = opaque).</param>
        /// <param name="duration">The duration of the fade in seconds.</param>
        /// <returns>An enumerator for coroutine execution.</returns>
        private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
        {
            var elapsed = 0f;
            crossFade.alpha = startAlpha;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                crossFade.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            crossFade.alpha = endAlpha;
        }
        
        /// <summary>
        /// Manually sets the fade alpha value without animation.
        /// Useful for initializing the fade state.
        /// </summary>
        /// <param name="alpha">The alpha value to set (0 = transparent, 1 = opaque).</param>
        public void SetInitialAlpha(float alpha)
        {
            crossFade.alpha = alpha;
        }
    }
}