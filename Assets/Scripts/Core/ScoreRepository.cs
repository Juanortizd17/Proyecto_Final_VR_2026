using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QualityControlXR
{
    [Serializable]
    public class SessionRecord
    {
        public string date;
        public int score;
        public int correct;
        public int mistakes;
        public float secondsPlayed;
        public bool completed;
        public List<string> reports = new List<string>();
    }

    [Serializable]
    class SessionHistory { public List<SessionRecord> sessions = new List<SessionRecord>(); }

    /// <summary>
    /// Persistencia de resultados en JSON (Application.persistentDataPath/qc_history.json).
    /// Guarda cada turno con sus reportes de daño, y expone el récord histórico.
    /// </summary>
    public static class ScoreRepository
    {
        const int MaxSessions = 50;
        static string FilePath => Path.Combine(Application.persistentDataPath, "qc_history.json");

        public static List<SessionRecord> Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new List<SessionRecord>();
                var h = JsonUtility.FromJson<SessionHistory>(File.ReadAllText(FilePath));
                return h?.sessions ?? new List<SessionRecord>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ScoreRepository] No se pudo leer el historial: {e.Message}");
                return new List<SessionRecord>();
            }
        }

        public static void Save(SessionRecord record)
        {
            var list = Load();
            list.Add(record);
            if (list.Count > MaxSessions) list.RemoveRange(0, list.Count - MaxSessions);
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(new SessionHistory { sessions = list }, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ScoreRepository] No se pudo guardar: {e.Message}");
            }
        }

        public static int BestScore() => Load().Select(s => s.score).DefaultIfEmpty(0).Max();
        public static int SessionCount() => Load().Count;
    }
}
