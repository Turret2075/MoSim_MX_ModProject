using System;
using System.Linq;
using GameSystems.Management;
using MoSimCore.BaseClasses;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Reefscape.Scoring
{
    public class ReefscapeScoreUI : BaseScoreUI<ReefscapeScoreData>
    {
        [Header("Total Scores")] [SerializeField]
        private TextMeshProUGUI blueScoreText;

        [SerializeField] private TextMeshProUGUI redScoreText;

        private int _totalBlueScore;
        private int _totalRedScore;

        [Header("Game Pieces Scored")] [SerializeField]
        private TextMeshProUGUI blueCoralScoredText;

        [SerializeField] private TextMeshProUGUI redCoralScoredText;
        [SerializeField] private TextMeshProUGUI blueAlgaeScoredText;
        [SerializeField] private TextMeshProUGUI redAlgaeScoredText;

        [Header("Detailed Score Overlay")] [SerializeField]
        private GameObject detailedScoreOverlay;

        [SerializeField] private Button detailedScoreOverlayButton;
        private Button _detailedScoreButton;

        [Header("Blue Detailed Scores")] [SerializeField]
        private TextMeshProUGUI detailedBlueScoreText;

        [SerializeField] private TextMeshProUGUI blueLeaveText;
        [SerializeField] private TextMeshProUGUI blueCoralText;
        [SerializeField] private TextMeshProUGUI blueAlgaeText;
        [SerializeField] private TextMeshProUGUI blueBargeText;

        [Header("Red Detailed Scores")] [SerializeField]
        private TextMeshProUGUI detailedRedScoreText;

        [SerializeField] private TextMeshProUGUI redLeaveText;
        [SerializeField] private TextMeshProUGUI redCoralText;
        [SerializeField] private TextMeshProUGUI redAlgaeText;
        [SerializeField] private TextMeshProUGUI redBargeText;

        private RobotSpawnController _robotSpawnController;

        [Header("Robot Labels")] [SerializeField]
        private GameObject[] blueRobots;

        [SerializeField] private GameObject[] redRobots;

        [Header("Modding Disclaimer Images")] [SerializeField]
        private GameObject[] blueDisclaimerImages;

        [SerializeField] private GameObject[] redDisclaimerImages;

        [Header("Winner Labels")] [SerializeField]
        private GameObject blueWinnerLabel;

        [SerializeField] private GameObject redWinnerLabel;

        private void Start()
        {
            _robotSpawnController = FindFirstObjectByType<RobotSpawnController>();

            _detailedScoreButton = GetComponent<Button>();
            _detailedScoreButton.onClick.AddListener(() =>
                {
                    if (_robotSpawnController.BlueSpawnedRobots != null)
                    {
                        for (var i = 0; i < _robotSpawnController.BlueSpawnedRobots.Length; i++)
                        {
                            var robot = _robotSpawnController.BlueSpawnedRobots[i];
                            if (robot == null) continue;
                            blueRobots[i].GetComponentInChildren<TextMeshProUGUI>().text =
                                robot.GetComponent<RobotBase>().TeamNumber.ToString();
                            blueDisclaimerImages[i].SetActive(_robotSpawnController.BlueRobotsModded[i]);
                        }
                    }

                    if (_robotSpawnController.RedSpawnedRobots != null)
                    {
                        for (var i = 0; i < _robotSpawnController.RedSpawnedRobots.Length; i++)
                        {
                            var robot = _robotSpawnController.RedSpawnedRobots[i];
                            if (robot == null) continue;
                            redRobots[i].GetComponentInChildren<TextMeshProUGUI>().text =
                                robot.GetComponent<RobotBase>().TeamNumber.ToString();
                            redDisclaimerImages[i].SetActive(_robotSpawnController.RedRobotsModded[i]);
                        }
                    }

                    blueWinnerLabel.SetActive(_totalBlueScore > _totalRedScore);
                    redWinnerLabel.SetActive(_totalRedScore > _totalBlueScore);

                    detailedScoreOverlay.SetActive(BaseGameManager.Instance.GameState == GameState.End &&
                                                   !detailedScoreOverlay.activeSelf);
                }
            );

            detailedScoreOverlayButton.onClick.AddListener(() => detailedScoreOverlay.SetActive(false));
        }

        protected override void UpdateTotalScores(ReefscapeScoreData blueScore, ReefscapeScoreData redScore)
        {
            _totalBlueScore = blueScore.TotalPoints;
            _totalRedScore = redScore.TotalPoints;

            blueScoreText.text = _totalBlueScore.ToString();
            redScoreText.text = _totalRedScore.ToString();
        }

        protected override void UpdateDetailedScores(ReefscapeScoreData blueScore, ReefscapeScoreData redScore)
        {
            detailedBlueScoreText.text = blueScore.TotalPoints.ToString();
            
            detailedRedScoreText.text = redScore.TotalPoints.ToString();

            if (BaseGameManager.Instance.wasCheated)
            {
                blueLeaveText.text = "0";
                blueCoralText.text = "0";
                blueAlgaeText.text = "0";
                blueBargeText.text = "0";
                
                redLeaveText.text = "0";
                redCoralText.text = "0";
                redAlgaeText.text = "0";
                redBargeText.text = "0";

                return;
            }
            
            blueLeaveText.text = blueScore.LeavePoints.ToString();
            blueCoralText.text = blueScore.CoralPoints.ToString();
            blueAlgaeText.text = (blueScore.NetPoints + blueScore.ProcessorPoints).ToString();
            blueBargeText.text = (blueScore.ClimbPoints + blueScore.ParkPoints).ToString();

            redLeaveText.text = redScore.LeavePoints.ToString();
            redCoralText.text = redScore.CoralPoints.ToString();
            redAlgaeText.text = (redScore.NetPoints + redScore.ProcessorPoints).ToString();
            redBargeText.text = (redScore.ClimbPoints + redScore.ParkPoints).ToString();
        }

        protected override void UpdateGamePieceCounters(ReefscapeScoreData blueScore, ReefscapeScoreData redScore)
        {
            if (BaseGameManager.Instance.wasCheated)
            {
                blueCoralScoredText.text = "0";
                redCoralScoredText.text = "0";
                blueAlgaeScoredText.text = "0";
                redAlgaeScoredText.text = "0";

                return;
            }
            
            blueCoralScoredText.text = blueScore.CoralScored.ToString();
            redCoralScoredText.text = redScore.CoralScored.ToString();
            blueAlgaeScoredText.text = blueScore.AlgaeScored.ToString();
            redAlgaeScoredText.text = redScore.AlgaeScored.ToString();
        }
    }
}