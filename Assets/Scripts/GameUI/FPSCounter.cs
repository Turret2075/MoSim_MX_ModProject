using TMPro;
using UnityEngine;

namespace GameUI
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI fpsCounter;
        
        private float _deltaTime = 0.0f;

        public static int fps;
        
        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            fps = Mathf.CeilToInt(1.0f / _deltaTime);
            fpsCounter.text = $"FPS: {fps}";
        }
    }
}