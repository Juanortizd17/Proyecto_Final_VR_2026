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
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(save, true)); ExportCsv(save); }
            catch (Exception e) { Debug.LogWarning("[MixSave] No se pudo guardar: " + e.Message); }
        }


        /// <summary>Ruta del historial en CSV (se abre con Excel o Google Sheets).</summary>
        public static string CsvPath => Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(FileName) + "_historial.csv");

        /// <summary>
        /// Exporta el historial de turnos a CSV, separado por punto y coma (Excel en español lo abre
        /// directo en columnas). Se llama cada vez que se guarda.
        /// </summary>
        public static void ExportCsv(MixSave save)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("turno;fecha;puntaje;bebidas_servidas;pedidos_perfectos;desmayos;completado");
                for (int i = 0; i < save.sessions.Count; i++)
                {
                    var s = save.sessions[i];
                    sb.AppendLine($"{i + 1};{s.date};{s.score};{s.served};{s.perfects};{s.knockouts};{(s.completed ? "si" : "no")}");
                }
                File.WriteAllText(CsvPath, sb.ToString(), new System.Text.UTF8Encoding(true));
            }
            catch (Exception e) { Debug.LogWarning("[MixSave] No se pudo exportar el CSV: " + e.Message); }
        }
        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning("[MixSave] No se pudo borrar: " + e.Message); }
        }
    }
}
