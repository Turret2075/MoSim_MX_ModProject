using System;
using System.Collections.Generic;
using System.Linq;
using MoSimCore.BaseClasses;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimCore.Interfaces;
using UnityEngine;

namespace GameSystems.Scoring
{
    public abstract class ScoreHandler<T, TUI> : MonoBehaviour 
        where T : IScoreData, new()
        where TUI : BaseScoreUI<T>
    {
        protected T BlueScoreData = new();
        protected T RedScoreData = new();
        protected TUI ScoreUI;
        
        public readonly List<IScorer> BlueScorers = new();
        public readonly List<IScorer> RedScorers = new();

        protected virtual void Awake()
        {
            ScoreUI = FindFirstObjectByType<TUI>();
        }
        
        protected virtual void Start()
        {
            var allScorers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IScorer>();

            foreach (var scorer in allScorers)
            {
                RegisterScorer(scorer);
            }
            
            UpdateUI();
        }

        protected virtual void LateUpdate()
        {
            CalculateScore(Alliance.Blue);
            CalculateScore(Alliance.Red);
            UpdateUI();
        }

        protected virtual void UpdateUI()
        {
            ScoreUI?.UpdateUI(BlueScoreData, RedScoreData);
        }
        
        protected virtual void CalculateScore(Alliance alliance)
        {
            var scoreData = alliance == Alliance.Blue ? BlueScoreData : RedScoreData;
            var scorers = alliance == Alliance.Blue ? BlueScorers : RedScorers;
            
            scoreData.Reset();
            
            foreach (var scorer in scorers)
            {
                if (scorer is not MonoBehaviour mb || !mb || !mb.isActiveAndEnabled)
                {
                    Debug.LogError("ScoreHandler: Scorer is not a valid active MonoBehaviour.");
                    continue;
                }
                scorer.AddScore(scoreData, BaseGameManager.Instance.GameState);
            }
        }
        
        protected virtual int GetTotalScore(Alliance alliance)
        {
            return alliance == Alliance.Blue ? BlueScoreData.TotalPoints : RedScoreData.TotalPoints;
        }

        public void RegisterScorer(IScorer scorer)
        {
            switch (scorer.Alliance)
            {
                case Alliance.Blue:
                    if (BlueScorers.Contains(scorer))
                    {
                        Debug.LogError("ScoreHandler: Attempted to register already registered Blue scorer.");
                        break;
                    }
                    BlueScorers.Add(scorer);
                    break;
                case Alliance.Red:
                    if (RedScorers.Contains(scorer))
                    {
                        Debug.LogError("ScoreHandler: Attempted to register already registered Red scorer.");
                        break;
                    }
                    RedScorers.Add(scorer);
                    break;
                default:
                    Debug.LogError($"ScoreHandler: Attempted to register scorer with invalid alliance {scorer.Alliance}.");
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UnregisterScorer(IScorer scorer)
        {
            switch (scorer.Alliance)
            {
                case Alliance.Blue when BlueScorers.Contains(scorer):
                    BlueScorers.Remove(scorer);
                    break;
                case Alliance.Red when RedScorers.Contains(scorer):
                    RedScorers.Remove(scorer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ReRegisterScorers()
        {
            UnregisterAllScorers();
            
            var allScorers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IScorer>();

            foreach (var scorer in allScorers)
            {
                RegisterScorer(scorer);
            }
        }
        
        private void UnregisterAllScorers()
        {
            BlueScorers.Clear();
            RedScorers.Clear();
        }

        public virtual void Reset()
        {
            UnregisterAllScorers();
            BlueScoreData = new T();
            RedScoreData = new T();
            UpdateUI();
        }
    }
}