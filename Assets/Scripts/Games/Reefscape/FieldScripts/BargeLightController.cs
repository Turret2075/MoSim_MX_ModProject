using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Games.Reefscape.FieldScripts
{
    public class BargeLightController : MonoBehaviour
    {
        [Header("Barge Lights")] [SerializeField]
        private List<MeshRenderer> blueBargeLights;
        [SerializeField] private List<MeshRenderer> redBargeLights;
        [SerializeField] private Material blueBargeLightMaterial;
        [SerializeField] private Material redBargeLightMaterial;
        [SerializeField] private Material disabledBargeLightMaterial;
        
        public IEnumerator StartEndgameSequence()
        {
            const float flashWaitTime = 10f / 60f;

            foreach (var blueBargeLight in blueBargeLights)
            {
                blueBargeLight.material = disabledBargeLightMaterial;
            }

            foreach (var redBargeLight in redBargeLights)
            {
                redBargeLight.material = disabledBargeLightMaterial;
            }

            yield return null;

            blueBargeLights[0].material = blueBargeLightMaterial;
            redBargeLights[0].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[0].material = disabledBargeLightMaterial;
            redBargeLights[0].material = disabledBargeLightMaterial;
            blueBargeLights[1].material = blueBargeLightMaterial;
            redBargeLights[1].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[1].material = disabledBargeLightMaterial;
            redBargeLights[1].material = disabledBargeLightMaterial;
            blueBargeLights[2].material = blueBargeLightMaterial;
            redBargeLights[2].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[2].material = disabledBargeLightMaterial;
            redBargeLights[2].material = disabledBargeLightMaterial;
            blueBargeLights[1].material = blueBargeLightMaterial;
            redBargeLights[1].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[1].material = disabledBargeLightMaterial;
            redBargeLights[1].material = disabledBargeLightMaterial;
            blueBargeLights[0].material = blueBargeLightMaterial;
            redBargeLights[0].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[0].material = disabledBargeLightMaterial;
            redBargeLights[0].material = disabledBargeLightMaterial;
            blueBargeLights[1].material = blueBargeLightMaterial;
            redBargeLights[1].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[1].material = disabledBargeLightMaterial;
            redBargeLights[1].material = disabledBargeLightMaterial;
            blueBargeLights[2].material = blueBargeLightMaterial;
            redBargeLights[2].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            foreach (var blueBargeLight in blueBargeLights)
            {
                blueBargeLight.material = disabledBargeLightMaterial;
            }

            foreach (var redBargeLight in redBargeLights)
            {
                redBargeLight.material = disabledBargeLightMaterial;
            }

            yield return new WaitForSecondsRealtime(40f / 60f);

            blueBargeLights[0].material = blueBargeLightMaterial;
            redBargeLights[0].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[0].material = disabledBargeLightMaterial;
            redBargeLights[0].material = disabledBargeLightMaterial;
            blueBargeLights[1].material = blueBargeLightMaterial;
            redBargeLights[1].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[1].material = disabledBargeLightMaterial;
            redBargeLights[1].material = disabledBargeLightMaterial;
            blueBargeLights[2].material = blueBargeLightMaterial;
            redBargeLights[2].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[2].material = disabledBargeLightMaterial;
            redBargeLights[2].material = disabledBargeLightMaterial;
            blueBargeLights[1].material = blueBargeLightMaterial;
            redBargeLights[1].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[1].material = disabledBargeLightMaterial;
            redBargeLights[1].material = disabledBargeLightMaterial;
            blueBargeLights[0].material = blueBargeLightMaterial;
            redBargeLights[0].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[0].material = disabledBargeLightMaterial;
            redBargeLights[0].material = disabledBargeLightMaterial;
            blueBargeLights[1].material = blueBargeLightMaterial;
            redBargeLights[1].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            blueBargeLights[1].material = disabledBargeLightMaterial;
            redBargeLights[1].material = disabledBargeLightMaterial;
            blueBargeLights[2].material = blueBargeLightMaterial;
            redBargeLights[2].material = redBargeLightMaterial;

            yield return new WaitForSecondsRealtime(flashWaitTime);

            foreach (var blueBargeLight in blueBargeLights)
            {
                blueBargeLight.material = disabledBargeLightMaterial;
            }

            foreach (var redBargeLight in redBargeLights)
            {
                redBargeLight.material = disabledBargeLightMaterial;
            }

            yield return new WaitForSecondsRealtime(40f / 60f);

            foreach (var blueBargeLight in blueBargeLights)
            {
                blueBargeLight.material = blueBargeLightMaterial;
            }

            foreach (var redBargeLight in redBargeLights)
            {
                redBargeLight.material = redBargeLightMaterial;
            }
        }
    }
}