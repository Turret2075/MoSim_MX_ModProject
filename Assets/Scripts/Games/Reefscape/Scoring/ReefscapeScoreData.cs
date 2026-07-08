using MoSimCore.Interfaces;

namespace Games.Reefscape.Scoring
{
    public class ReefscapeScoreData : IScoreData
    {
        public int CoralPoints { get; set; }
        public int TroughPoints { get; set; }
        public int NetPoints { get; set; }
        public int ProcessorPoints { get; set; }
        public int ClimbPoints { get; set; }
        public int ParkPoints { get; set; }
        public int LeavePoints { get; set; }
        
        public int CoralScored { get; set; }
        public int AlgaeScored { get; set; }
        
        public int TotalPoints => CoralPoints + TroughPoints + NetPoints + ProcessorPoints + ClimbPoints + ParkPoints + LeavePoints;
        
        public void Reset()
        {
            CoralPoints = 0;
            TroughPoints = 0;
            NetPoints = 0;
            ProcessorPoints = 0;
            ClimbPoints = 0;
            ParkPoints = 0;
            LeavePoints = 0;
            CoralScored = 0;
            AlgaeScored = 0;
        }
    }
}