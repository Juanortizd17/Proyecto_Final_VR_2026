using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MixLab
{
    [Serializable]
    public class MixSession
    {
        public string date;
        public int score;
        public int served;
        public int perfects;
        public int knockouts;
        public bool completed;
    }

    [Serializable]
    public class MixSave
    {
        public int bestScore;
        public List<string> discovered = new List<string>();   // ids de reacciones descubiertas
        public List<MixSession> sessions = new List<MixSession>();
    }

    /// <summary>
    /// Persistencia en JSON (Application.persistentDataPath/mix_save.json):
    /// libro de reacciones descubiertas, récord e historial de turnos (últimos 50).
    /// </summary>
    public static class MixSaveRepository
    {
        const int MaxSessions = 50;
        /// <summary>Nombre del archivo (las pruebas automáticas usan otro para no tocar la partida real).</summary>
        public static string FileName =
            System.Array.IndexOf(Environment.GetCommandLineArgs(), "-autodemo") >= 0 ? "mix_save_demo.json" : "mix_save.json";
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static MixSave Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new MixSave();
                return JsonUtility.FromJson<MixSave>(File.ReadAllText(FilePath)) ?? new MixSave();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MixSave] No se pudo leer: " + e.Message);
                return new MixSave();
            }
        }

        public static void Save(MixSave save)
        {
            if (save.sessions.Count > MaxSessions) save.sessions.RemoveRange(0, save.sessions.Count - MaxSessions);
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(save, true)); }
            catch (Exception e) { Debug.LogWarning("[MixSave] No se pudo guardar: " + e.Message); }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning("[MixSave] No se pudo borrar: " + e.Message); }
        }
    }
}
