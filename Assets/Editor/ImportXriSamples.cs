using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Importa las muestras del XR Interaction Toolkit (Starter Assets y XR Device Simulator)
    /// sin abrir el Package Manager. Uso: Unity -batchmode -executeMethod
    /// QualityControlXR.EditorTools.ImportXriSamples.Run
    /// </summary>
    public static class ImportXriSamples
    {
        const string Package = "com.unity.xr.interaction.toolkit";
        static readonly string[] Wanted = { "Starter Assets", "XR Device Simulator" };

        [MenuItem("Tools/Quality Control XR/Import XRI Samples")]
        public static void Run()
        {
            var samples = Sample.FindByPackage(Package, null);
            foreach (var s in samples.Where(x => Wanted.Contains(x.displayName)))
            {
                if (s.isImported) { Debug.Log($"[XRI] Already imported: {s.displayName}"); continue; }
                bool ok = s.Import(Sample.ImportOptions.OverridePreviousImports);
                Debug.Log($"[XRI] Import {s.displayName}: {ok}");
            }
            AssetDatabase.Refresh();
        }
    }
}
