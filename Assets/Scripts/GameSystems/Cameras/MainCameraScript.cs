using MoSimCore.Enums;
using UnityEngine;

namespace GameSystems.Cameras
{
    public class MainCameraScript : MonoBehaviour
    {
        [SerializeField] protected Camera Camera;
        
        [SerializeField] private AudioListener listener;

        private void Start()
        {
            Camera = GetComponent<Camera>();
        }
        
        public void SetSplitScreen(Alliance alliance)
        {
            if (Camera == null)
            {
                Debug.LogError("Camera component not found on this GameObject.");
                return;
            }
            
            if (Camera != null)
            {
                Camera.rect = new Rect(alliance == Alliance.Blue ? -0.5f : 0.5f, 0f, 1f, 1f);
            }
            
            if (listener == null)
            {
                listener = GetComponent<AudioListener>();
            }
            
            if (listener != null)
            {
                listener.enabled = alliance == Alliance.Blue;
            }
            else
            {
                Debug.LogError("AudioListener component not found on this GameObject.");
            }
        }
    }
}