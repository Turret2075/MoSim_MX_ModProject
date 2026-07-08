using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameUI
{
    [ExecuteInEditMode]
    public class ProgressBar : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("GameObject/UI/Linear Progress Bar")]
        public static void AddLinearProgressBar()
        {
            var obj = Instantiate(Resources.Load<GameObject>("UI/LinearProgressBar"), Selection.activeGameObject.transform, false);
        }
        
        [MenuItem("GameObject/UI/Radial Progress Bar")]
        public static void AddRadialProgressBar()
        {
            var obj = Instantiate(Resources.Load<GameObject>("UI/RadialProgressBar"), Selection.activeGameObject.transform, false);
        }
#endif
        
        [SerializeField] private int minimum;
        [SerializeField] private int maximum;
        [SerializeField] private int current;

        [SerializeField] private Image mask;
        [SerializeField] private Image fill;
        [SerializeField] private Color color;

        private void OnValidate()
        {
            UpdateVisuals();
        }

        public void SetRange(int minimum, int maximum)
        {
            if (this.minimum == minimum && this.maximum == maximum)
            {
                return;
            }
            this.minimum = minimum;
            this.maximum = maximum;
            UpdateVisuals();
        }
        
        public void SetCurrent(int current)
        {
            if (this.current == current)
            {
                return;
            }
            this.current = Mathf.Clamp(current, minimum, maximum);
            UpdateVisuals();
        }
        
        public void SetColor(Color color)
        {
            if (this.color == color)
            {
                return;
            }
            this.color = color;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            var min = minimum;
            var max = Mathf.Max(min + 1, maximum);
            float currentOffset = current - min;
            float maxOffset = max - min;
            var fillAmount = Mathf.Clamp01(currentOffset / maxOffset);

            if (mask != null)
            {
                mask.fillAmount = fillAmount;
            }
            
            if (fill != null)
            {
                fill.color = color;
            }
        }
    }
}
