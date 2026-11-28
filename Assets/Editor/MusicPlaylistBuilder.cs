using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MixLab.EditorTools
{
    /// <summary>
    /// Antes de cada compilación escribe Assets/Resources/MusicaPlaylist.txt con las canciones de
    /// StreamingAssets/Musica ("Género/archivo.mp3" por línea). En el Quest esas canciones van dentro
    /// del APK y no se pueden listar: MusicPlayer usa esta lista para encontrarlas.
    /// </summary>
    public class MusicPlaylistBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Write();

        [MenuItem("Tools/Mix Lab/Update Music Playlist")]
        public static void Write()
        {
            string root = Path.Combine(Application.dataPath, "StreamingAssets", MusicPlayer.FolderName);
            string[] ext = { ".mp3", ".ogg", ".wav" };
            var lines = Directory.Exists(root)
                ? Directory.GetDirectories(root).OrderBy(d => d).SelectMany(dir =>
                    Directory.GetFiles(dir).Where(f => ext.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderBy(f => f)
                        .Select(f => Path.GetFileName(dir) + "/" + Path.GetFileName(f))).ToList()
                : new System.Collections.Generic.List<string>();
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));
            string path = "Assets/Resources/" + MusicPlayer.PlaylistResource + ".txt";
            File.WriteAllText(path, string.Join("\n", lines));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[MusicPlaylist] {lines.Count} canciones propias incluidas en la compilación");
        }
    }
}
