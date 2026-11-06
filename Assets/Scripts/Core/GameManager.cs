using System;
using UnityEngine;

namespace QualityControlXR
{
    public enum GameState { Onboarding, Playing, Finished }

    /// <summary>
    /// Estado global del juego: temporizador, puntuación, fallos y cierre.
    /// Centraliza los eventos para que UI, banda y audio reaccionen sin acoplarse.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Reglas")]
        [SerializeField] float roundDuration = 180f;
        [SerializeField] int pointsCorrect = 100;
        [SerializeField] int penaltyWrong = 50;
        [SerializeField] int maxMistakes = 5;

        public GameState State { get; private set; } = GameState.Onboarding;
        public float TimeLeft { get; private set; }
        public int Score { get; private set; }
        public int Mistakes { get; private set; }
        public int Correct { get; private set; }
        public bool Won { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnMistake;
        public event Action<string> OnReport; // reporte de daño/problema

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            TimeLeft = roundDuration;
        }

        void Update()
        {
            if (State != GameState.Playing) return;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) { TimeLeft = 0f; Finish(false); }
        }

        public void StartRound()
        {
            if (State == GameState.Playing) return;
            Score = 0; Mistakes = 0; Correct = 0; Won = false;
            TimeLeft = roundDuration;
            SetState(GameState.Playing);
            OnScoreChanged?.Invoke(Score);
        }

        public void RegisterCorrect(string detail)
        {
            if (State != GameState.Playing) return;
            Correct++;
            Score += pointsCorrect;
            OnScoreChanged?.Invoke(Score);
            OnReport?.Invoke(detail);
        }

        public void RegisterMistake(string detail)
        {
            if (State != GameState.Playing) return;
            Mistakes++;
            Score = Mathf.Max(0, Score - penaltyWrong);
            OnScoreChanged?.Invoke(Score);
            OnMistake?.Invoke(Mistakes);
            OnReport?.Invoke(detail);
            if (Mistakes >= maxMistakes) Finish(false);
        }

        /// <summary>Evento inesperado: se suscribe desde ConveyorBelt / EventDirector.</summary>
        public void AddTime(float seconds) => TimeLeft = Mathf.Max(0f, TimeLeft + seconds);

        public void Finish(bool won)
        {
            if (State == GameState.Finished) return;
            Won = won;
            SetState(GameState.Finished);
        }

        void SetState(GameState s)
        {
            State = s;
            OnStateChanged?.Invoke(s);
        }
    }
}
