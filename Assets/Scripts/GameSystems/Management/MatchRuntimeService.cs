using UnityEngine;

namespace GameSystems.Management
{
    public class MatchRuntimeService : MonoBehaviour
    {
        public static MatchRuntimeService Instance { get; private set; }

        [field: SerializeField]
        public MatchData CurrentMatchData { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}