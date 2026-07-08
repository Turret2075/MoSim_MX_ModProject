using System;

namespace RobotFramework.Controllers.PidSystems
{
    [Serializable]
    public class PidConstants
    {
        public float kP = 0.25f;
        public float kI = 0;
        public float kD = 0.00001f;
        public float Max = 2.25f;
        public float Isaturation = 1;

        public PidConstants(float kP, float kI, float kD, float Max, float Isaturation)
        {
            this.kP = kP;
            this.kI = kI;
            this.kD = kD;
            this.Max = Max;
            this.Isaturation = Isaturation;
        }

        public static PidConstants SpringLoaded()
        {
            var pid = new PidConstants(1,0,0,1,1);
            return pid;
        }
    }
}