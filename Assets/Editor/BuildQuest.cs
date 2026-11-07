using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Compilación del proyecto:
    ///  · Windows (plataforma por defecto): Builds/Windows/ControlCalidadXR.exe
    ///    Funciona con el Quest por Link o, sin visor, con el simulador (mouse y teclado).
    ///  · Meta Quest (Android): Builds/ControlCalidadXR.apk
    /// En batchmode: -buildTarget Win64 -executeMethod QualityControlXR.EditorTools.BuildQuest.BuildWindows
    ///               -buildTarget Android -executeMethod QualityControlXR.EditorTools.BuildQuest.Build
    /// </summary>
    public static class BuildQuest
    {
        const string ApkOutput = "Builds/ControlCalidadXR.apk";
        const string WindowsOutput = "Builds/Windows/ControlCalidadXR.exe";

        [MenuItem("Tools/Quality Control XR/Switch Platform to Windows")]
        public static void SwitchToWindows() =>
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        [MenuItem("Tools/Quality Control XR/Switch Platform to Meta Quest (Android)")]
        public static void SwitchToQuest()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ProjectSetup.ConfigureQuest();
        }

        [MenuItem("Tools/Quality Control XR/Build Windows App")]
        public static void BuildWindows() => Run(WindowsOutput, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);

        [MenuItem("Tools/Quality Control XR/Build Quest APK")]
        public static void Build() => Run(ApkOutput, BuildTarget.Android, BuildTargetGroup.Android);

        static void Run(string output, BuildTarget target, BuildTargetGroup group)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            });
            var s = report.summary;
            Debug.Log($"[Build] {target}: {s.result} · {s.totalSize / (1024f * 1024f):F1} MB · {s.totalTime} · errores: {s.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
