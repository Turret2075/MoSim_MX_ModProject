using System.Collections;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using UnityEngine;

namespace RobotFramework.Controllers.Lighting
{
    public class RSLController : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        [SerializeField] private Material rslMaterial;
        [SerializeField] private float maxIntensity;
        [SerializeField] private float minIntensity;
        
        [SerializeField] private float flashDelay;
        [SerializeField] private Color rslColor;
        
        private void Start() 
        {
            rslMaterial.SetColor(EmissionColor, rslColor * maxIntensity);
            StartCoroutine(RSLFlash());
        }
        
        private IEnumerator RSLFlash() 
        {
            while (true) 
            {
                while (BaseGameManager.Instance.RobotState == RobotState.Enabled)
                {
                    rslMaterial.SetColor(EmissionColor, rslColor * maxIntensity);
                    yield return new WaitForSeconds(flashDelay);
                    rslMaterial.SetColor(EmissionColor, rslColor * minIntensity);
                    yield return new WaitForSeconds(flashDelay);
                }
                rslMaterial.SetColor(EmissionColor, rslColor * maxIntensity);
                yield return null;
                
            }
        }

        private void OnApplicationQuit()
        {
            rslMaterial.SetColor(EmissionColor, rslColor * maxIntensity);
        }
    }
}