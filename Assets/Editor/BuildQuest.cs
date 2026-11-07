using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Compila el APK para Meta Quest en Builds/ControlCalidadXR.apk.
    /// Menú: Tools > Quality Control XR > Build Quest APK, o en batchmode con
    /// -buildTarget Android -executeMethod QualityControlXR.EditorTools.BuildQuest.Build
    /// </summary>
    public static class BuildQuest
    {
        const string Output = "Builds/ControlCalidadXR.apk";

        [MenuItem("Tools/Quality Control XR/Build Quest APK")]
        public static void Build()
        {
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                locationPathName = Output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Build] Resultado: {s.result} · {s.totalSize / (1024f * 1024f):F1} MB · {s.totalTime} · errores: {s.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
