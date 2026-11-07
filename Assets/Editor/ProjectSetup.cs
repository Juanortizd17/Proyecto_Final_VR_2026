using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Configura el proyecto para Meta Quest 2/3 (Android + OpenXR) y para PC (OpenXR / Quest Link).
    /// Uso: Tools > Quality Control XR > Configure for Meta Quest, o en batchmode con
    /// -buildTarget Android -executeMethod QualityControlXR.EditorTools.ProjectSetup.ConfigureQuest
    /// </summary>
    public static class ProjectSetup
    {
        const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";
        const string SettingsFolder = "Assets/XR";

        [MenuItem("Tools/Quality Control XR/Configure for Meta Quest")]
        public static void ConfigureQuest()
        {
            ConfigurePlayer();
            ConfigureXRManagement(BuildTargetGroup.Android);
            ConfigureXRManagement(BuildTargetGroup.Standalone);
            ConfigureOpenXRFeatures();
            FixSampleMaterials();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Proyecto configurado para Meta Quest (Android/OpenXR) y PC (OpenXR).");
        }

        /// <summary>
        /// Modo PC puro: las gafas no se inicializan al abrir la app (OpenXR queda configurado
        /// pero apagado en Windows). Para volver a VR: Configure for Meta Quest.
        /// </summary>
        [MenuItem("Tools/Quality Control XR/Configure for PC (sin gafas)")]
        public static void ConfigureDesktop()
        {
            PlayerSettings.companyName = "UAO Immersive Solutions";
            PlayerSettings.productName = "Control de Calidad XR";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            ConfigureXRManagement(BuildTargetGroup.Standalone);
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
            {
                var s = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (s != null) { s.InitManagerOnStart = false; EditorUtility.SetDirty(s); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Modo PC: XR desactivado al iniciar en Windows.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "UAO Immersive Solutions";
            PlayerSettings.productName = "Control de Calidad XR";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.uao.controlcalidadxr");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // Active Input Handling = Both (XRI usa el Input System; deja el legacy por compatibilidad)
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (ps != null)
            {
                var so = new SerializedObject(ps);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null) { prop.intValue = 2; so.ApplyModifiedProperties(); }
            }

            // Calidad pensada para un visor móvil: 4x MSAA, sombras suaves de una sola cascada.
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowDistance = 15f;
            QualitySettings.vSyncCount = 0;
        }

        static void ConfigureXRManagement(BuildTargetGroup group)
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                    out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                if (!AssetDatabase.IsValidFolder(SettingsFolder)) AssetDatabase.CreateFolder("Assets", "XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, SettingsFolder + "/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            var general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;
            bool ok = XRPackageMetadataStore.AssignLoader(general.AssignedSettings, OpenXRLoader, group);
            EditorUtility.SetDirty(perTarget);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.AssignedSettings);
            Debug.Log($"[Setup] OpenXR loader en {group}: {ok}");
        }

        /// <summary>
        /// Los XRI Starter Assets traen materiales de URP / Shader Graph. Este proyecto usa el
        /// pipeline integrado, así que esos materiales se verían magenta (controles, teleport, resaltado).
        /// Se pasan a Standard conservando el color guardado.
        /// </summary>
        [MenuItem("Tools/Quality Control XR/Fix Sample Materials")]
        public static void FixSampleMaterials()
        {
            var standard = Shader.Find("Standard");
            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Samples" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) continue;
                bool broken = m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported ||
                              ShaderUtil.ShaderHasError(m.shader);
                if (!broken) continue;

                Color c = Color.white;
                var so = new SerializedObject(m);
                var colors = so.FindProperty("m_SavedProperties.m_Colors");
                for (int i = 0; colors != null && i < colors.arraySize; i++)
                {
                    var e = colors.GetArrayElementAtIndex(i);
                    string n = e.FindPropertyRelative("first").stringValue;
                    if (n == "_BaseColor" || n == "_Color") { c = e.FindPropertyRelative("second").colorValue; if (n == "_BaseColor") break; }
                }

                m.shader = standard;
                string lower = m.name.ToLowerInvariant();
                bool see = lower.Contains("telport") || lower.Contains("teleport") || lower.Contains("fresnel") || lower.Contains("interactable");
                if (see)
                {
                    c.a = Mathf.Min(c.a, 0.4f);
                    m.SetFloat("_Mode", 3f);
                    m.SetInt("_SrcBlend", (int)BlendMode.One);
                    m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    m.renderQueue = 3000;
                }
                m.color = c;
                EditorUtility.SetDirty(m);
                fixedCount++;
                Debug.Log($"[Setup] Material corregido a Standard: {path}");
            }
            Debug.Log($"[Setup] Materiales de samples corregidos: {fixedCount}");
        }

        static void ConfigureOpenXRFeatures()
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null) { Debug.LogWarning($"[Setup] Sin OpenXRSettings para {group}"); continue; }

                settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                foreach (var f in settings.GetFeatures())
                {
                    string n = f.GetType().Name;
                    bool enable =
                        n == "OculusTouchControllerProfile" ||
                        n == "MetaQuestTouchPlusControllerProfile" ||
                        n == "MetaQuestTouchProControllerProfile" ||
                        (group == BuildTargetGroup.Android && n == "MetaQuestFeature");
                    if (enable) { f.enabled = true; Debug.Log($"[Setup] {group}: {n} habilitado"); }
                }
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
