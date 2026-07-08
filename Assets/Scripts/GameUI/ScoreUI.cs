using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class ScoreUI : MonoBehaviour
    {
        // Total scores
        public static SecureFloat RedScore { get; set; }
        public static SecureFloat BlueScore { get; set; }

        // Individual scores
        public static SecureFloat RedLeave, RedCoral, RedAlgae, RedBarge, RedCorals, RedAlgaes;
        public static SecureFloat BlueLeave, BlueCoral, BlueAlgae, BlueBarge, BlueCorals, BlueAlgaes;

        [Header("Overlay")] [SerializeField] private TextMeshProUGUI BlueScoreText;
        [SerializeField] private TextMeshProUGUI RedScoreText;
        [SerializeField] private TextMeshProUGUI BlueCoralsText;
        [SerializeField] private TextMeshProUGUI RedCoralsText;
        [SerializeField] private TextMeshProUGUI BlueAlgaesText;
        [SerializeField] private TextMeshProUGUI RedAlgaesText;

        [Header("Total")] [SerializeField] private TextMeshProUGUI BlueTotalText;
        [SerializeField] private TextMeshProUGUI RedTotalText;

        [Header("Blue Scores")] [SerializeField]
        private TextMeshProUGUI BlueLeaveText;

        [SerializeField] private TextMeshProUGUI BlueCoralText;
        [SerializeField] private TextMeshProUGUI BlueAlgaeText;
        [SerializeField] private TextMeshProUGUI BlueBargeText;

        [Header("Red Scores")] [SerializeField]
        private TextMeshProUGUI RedLeaveText;

        [SerializeField] private TextMeshProUGUI RedCoralText;
        [SerializeField] private TextMeshProUGUI RedAlgaeText;
        [SerializeField] private TextMeshProUGUI RedBargeText;

        [Header("Score Summary")] [SerializeField]
        private GameObject scoreSummaryPage;

        private Button _scoreSummaryButton;

        private void Start()
        {
            _scoreSummaryButton = GetComponent<Button>();
            _scoreSummaryButton.onClick.AddListener(() =>
                scoreSummaryPage.SetActive(BaseGameManager.Instance.GameState == GameState.End && !scoreSummaryPage.activeSelf));
        }

        private void LateUpdate()
        {
            BlueCoralsText.text = BlueCorals.ToString();
            RedCoralsText.text = RedCorals.ToString();
            BlueAlgaesText.text = BlueAlgaes.ToString();
            RedAlgaesText.text = RedAlgaes.ToString();

            BlueScoreText.text = BlueScore.ToString();
            RedScoreText.text = RedScore.ToString();

            BlueTotalText.text = BlueScore.ToString();
            RedTotalText.text = RedScore.ToString();

            BlueLeaveText.text = BlueLeave.ToString();
            BlueCoralText.text = BlueCoral.ToString();
            BlueAlgaeText.text = BlueAlgae.ToString();
            BlueBargeText.text = BlueBarge.ToString();

            RedLeaveText.text = RedLeave.ToString();
            RedCoralText.text = RedCoral.ToString();
            RedAlgaeText.text = RedAlgae.ToString();
            RedBargeText.text = RedBarge.ToString();
        }
    }
}