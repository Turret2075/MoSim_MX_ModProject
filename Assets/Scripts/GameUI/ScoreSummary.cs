using System.Linq;
using GameSystems.Management;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class ScoreSummary : MonoBehaviour
    {
        private Button _scoreSummaryButton;
        
        [SerializeField] private RobotSpawnController robotSpawnController;

        [Header("Blue Robots")] [SerializeField]
        private GameObject[] blueRobots;

        [Header("Red Robots")] [SerializeField]
        private GameObject[] redRobots;
        
        [Header("Winner Labels")]
        [SerializeField] private GameObject blueWinnerLabel;
        [SerializeField] private GameObject redWinnerLabel;
        
        private void Start()
        {
            _scoreSummaryButton = GetComponent<Button>();
            _scoreSummaryButton.onClick.AddListener(() => gameObject.SetActive(!gameObject.activeSelf));
        }

        private void OnEnable()
        {
            foreach (var blueSpawnedRobot in robotSpawnController.BlueSpawnedRobots.Select((robot, index) => new { robot, index}))
            {
                if (blueSpawnedRobot.robot == null)
                {
                    continue;
                }
                // blueRobots[blueSpawnedRobot.index].GetComponentInChildren<TextMeshProUGUI>().text =
                //     blueSpawnedRobot.robot.GetComponent<RobotBase>().SimRobot.GetTeamNumber().ToString();
            }

            // foreach (var redSpawnedRobot in robotSpawnController.RedSpawnedRobots.Select((robot, index) => new { robot, index}))
            // {
            //     if (redSpawnedRobot.robot == null)
            //     {
            //         continue;
            //     }
            //     redRobots[redSpawnedRobot.index].GetComponentInChildren<TextMeshProUGUI>().text =
            //         redSpawnedRobot.robot.GetComponent<RobotBase>().SimRobot.GetTeamNumber().ToString();
            // }
            //
            // blueWinnerLabel.SetActive(ScoreHandler.TotalBluePoints > ScoreHandler.TotalRedPoints);
            // redWinnerLabel.SetActive(ScoreHandler.TotalRedPoints > ScoreHandler.TotalBluePoints);
        }
    }
}
