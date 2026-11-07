using System;
using System.Collections.Generic;
using UnityEngine;

namespace QualityControlXR
{
    public enum GameState { Onboarding, Playing, Finished }

    /// <summary>
    /// Estado global del juego: temporizador, puntuación, fallos, reportes y cierre.
    /// Centraliza los eventos para que UI, banda y audio reaccionen sin acoplarse.
    /// Al terminar guarda la sesión en JSON (ScoreRepository).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Reglas")]
        [SerializeField] float roundDuration = 180f;
        [SerializeField] int pointsCorrect = 100;
        [SerializeField] int penaltyWrong = 50;
        [SerializeField] int maxMistakes = 5;
        [SerializeField] float lowTimeWarning = 30f;

        public GameState State { get; private set; } = GameState.Onboarding;
        public float TimeLeft { get; private set; }
        public float RoundDuration => roundDuration;
        public int MaxMistakes => maxMistakes;
        public int Score { get; private set; }
        public int Mistakes { get; private set; }
        public int Correct { get; private set; }
        public bool Won { get; private set; }
        public bool NewRecord { get; private set; }
        public int BestScore { get; private set; }
        public IReadOnlyList<string> Reports => reports;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnMistake;
        public event Action<string, bool> OnReport;   // texto, ¿fue acierto?
        public event Action OnLowTime;

        readonly List<string> reports = new List<string>();
        bool lowTimeFired;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            TimeLeft = roundDuration;
            BestScore = ScoreRepository.BestScore();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (State != GameState.Playing) return;
            TimeLeft -= Time.deltaTime;
            if (!lowTimeFired && TimeLeft <= lowTimeWarning) { lowTimeFired = true; OnLowTime?.Invoke(); }
            if (TimeLeft <= 0f) { TimeLeft = 0f; Finish(true); }   // sobrevivió al turno
        }

        public void StartRound()
        {
            if (State == GameState.Playing) return;
            Score = 0; Mistakes = 0; Correct = 0; Won = false; NewRecord = false; lowTimeFired = false;
            reports.Clear();
            TimeLeft = roundDuration;
            SetState(GameState.Playing);
            OnScoreChanged?.Invoke(Score);
            OnMistake?.Invoke(Mistakes);
        }

        public void RegisterCorrect(string detail)
        {
            if (State != GameState.Playing) return;
            Correct++;
            Score += pointsCorrect;
            reports.Add("[OK] " + detail);
            OnScoreChanged?.Invoke(Score);
            OnReport?.Invoke(detail, true);
        }

        public void RegisterMistake(string detail)
        {
            if (State != GameState.Playing) return;
            Mistakes++;
            Score = Mathf.Max(0, Score - penaltyWrong);
            reports.Add("[X] " + detail);
            OnScoreChanged?.Invoke(Score);
            OnMistake?.Invoke(Mistakes);
            OnReport?.Invoke(detail, false);
            if (Mistakes >= maxMistakes) Finish(false);
        }

        /// <summary>Usado por eventos inesperados (p. ej. avería de la banda compensa tiempo).</summary>
        public void AddTime(float seconds) => TimeLeft = Mathf.Max(0f, TimeLeft + seconds);

        public void Finish(bool survived)
        {
            if (State != GameState.Playing) return;
            Won = survived && Correct > 0;
            NewRecord = Score > BestScore;
            if (NewRecord) BestScore = Score;
            ScoreRepository.Save(new SessionRecord
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                score = Score, correct = Correct, mistakes = Mistakes,
                secondsPlayed = roundDuration - TimeLeft, completed = Won,
                reports = new List<string>(reports)
            });
            SetState(GameState.Finished);
        }

        void SetState(GameState s)
        {
            State = s;
            OnStateChanged?.Invoke(s);
        }
    }
}
