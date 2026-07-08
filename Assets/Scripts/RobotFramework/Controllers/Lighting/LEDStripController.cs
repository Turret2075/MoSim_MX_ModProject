using RobotFramework.Controllers.GamePieceSystem;
using UnityEngine;

namespace RobotFramework.Controllers.Lighting
{
    public class LedStripController : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject[] leds;
        private Material LEDs;
        public Shader shaderGraphShader;
        public Texture hasAlgae;
        public Texture Disabled;
        public Texture Intake;
        public Texture hasCoral;
        public Texture CoralMode;
        public Texture AlgaeMode;
        public Texture L1Mode;
        public Texture prepClimb;
        public Texture Climbed;
        public Texture AutoAligning;
        public Texture Processor;

        protected RobotGamePieceControllerBase GamePieceManager { get; private set; }
        protected RobotBase Robotbase { get; private set; }
        
        private void Start()
        {
            GamePieceManager = GetComponent<RobotGamePieceControllerBase>();
            Robotbase = GetComponent <RobotBase>();
            LEDs = new Material(shaderGraphShader);
            
            foreach (var led in leds)
            {
                led.GetComponent<Renderer>().material = LEDs;
            }
        }
        private void Update()
        {
            // if ((Robotbase is null || GamePieceManager is null) || GameManager.Instance.RobotState == RobotState.Disabled)
            // {
            //     LEDs.SetFloat("_X", 0);
            //     LEDs.SetFloat("_Y", 0.5f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", Disabled);
            // }
            // else if (Robotbase.CurrentSetpoint == Setpoints.Climb)
            // {
            //     LEDs.SetFloat("_X", 0);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", prepClimb);
            // }
            // else if (Robotbase.CurrentSetpoint == Setpoints.Climbed)
            // {
            //     LEDs.SetFloat("_X", 3f);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", Climbed);
            // }
            // else if (Robotbase.AutoAlignLeftAction.IsPressed() || Robotbase.AutoAlignRightAction.IsPressed())
            // {
            //     LEDs.SetFloat("_X", 0);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 0);
            //     LEDs.SetTexture("_Texture2D", AutoAligning);
            // }
            // else if (GamePieceManager != null && GamePieceManager.hasAlgae > 0 &&
            //          Robotbase.CurrentSetpoint == Setpoints.Processor)
            // {
            //     LEDs.SetFloat("_X", 1.5f);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", Processor);
            // }
            // else if (GamePieceManager != null && GamePieceManager.hasAlgae > 0)
            // {
            //     LEDs.SetFloat("_X", 0f);
            //     LEDs.SetFloat("_Y", 0.5f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", hasAlgae);
            // }
            // else if (GamePieceManager != null && GamePieceManager.hasCoral > 0)
            // {
            //     LEDs.SetFloat("_X", 1.5f);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", hasCoral);
            // }
            // else if (Robotbase.IntakeAction.IsPressed())
            // {
            //     if (Robotbase.CurrentIntakeMode == IntakeMode.Normal){
            //         LEDs.SetFloat("_X", 0);
            //         LEDs.SetFloat("_Y", 0.0f);
            //         LEDs.SetFloat("_intensity", 20);
            //         LEDs.SetTexture("_Texture2D", Intake);
            //     }
            // }
            // else if (Robotbase.CurrentRobotMode == RobotMode.Algae)
            // {
            //     LEDs.SetFloat("_X", 0f);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", AlgaeMode);
            // }
            // else if (Robotbase.CurrentIntakeMode == IntakeMode.L1){
            //     LEDs.SetFloat("_X", 0);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", L1Mode);
            // }
            // else if (Robotbase.CurrentRobotMode == RobotMode.Coral)
            // {
            //     LEDs.SetFloat("_X", 0);
            //     LEDs.SetFloat("_Y", 0.0f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", CoralMode);
            // }
            // else
            // {
            //     LEDs.SetFloat("_X", 0);
            //     LEDs.SetFloat("_Y", 0.5f);
            //     LEDs.SetFloat("_intensity", 20);
            //     LEDs.SetTexture("_Texture2D", Disabled);
            // }
        }
    }
}
