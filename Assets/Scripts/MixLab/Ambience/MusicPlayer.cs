using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace MixLab
{
    /// <summary>
    /// Rockola del bar: géneros con su lista de canciones. Cada género trae una pista original
    /// incluida en el juego y, además, se suman las canciones que el jugador copie en
    ///   StreamingAssets/Musica/&lt;Género&gt;/   (.mp3, .ogg o .wav)
    /// Cada subcarpeta es un género; no hace falta recompilar. La elección se recuerda.
    /// Teclas: M silencio · N siguiente canción · G siguiente género.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [Serializable]
        public class BuiltIn
        {
            public string genre;
            public string title;
            public float bpm = 124f;
            public AudioClip clip;
        }

        class Track
        {
            public string title;
            public string path;          // null = pista incluida
            public AudioClip clip;
        }

        class Genre
        {
            public string name;
            public float bpm = 124f;
            public readonly List<Track> tracks = new List<Track>();
        }

        public const string FolderName = "Musica";
        const string GenreKey = "mix_music_genre", VolumeKey = "mix_music_volume";
        static readonly string[] Extensions = { ".mp3", ".ogg", ".wav" };

        [SerializeField] AudioSource source;
        [SerializeField] BuiltIn[] builtIns;
        [SerializeField] float volume = 0.3f;

        readonly List<Genre> genres = new List<Genre>();
        int genreIndex, trackIndex, repeatsLeft, loadId;
        bool loading;
        float graceUntil, messageUntil;
        string message = "";
        AudioClip streamed;

        public static MusicPlayer Instance { get; private set; }
        public event Action OnChanged;

        public AudioSource Source => source;
        public string GenreName => genres.Count > 0 ? genres[genreIndex].name : "-";
        public string TrackTitle => loading ? "Cargando…" : CurrentTrack != null ? CurrentTrack.title : "(sin canciones)";
        public int TrackNumber => trackIndex + 1;
        public int TrackCount => genres.Count > 0 ? genres[genreIndex].tracks.Count : 0;
        public int GenreCount => genres.Count;
        public float Volume => volume;
        public bool Muted => source && source.mute;
        public float CurrentBpm => genres.Count > 0 ? genres[genreIndex].bpm : 124f;
        /// <summary>Pista incluida: su tempo es exacto y las luces pueden seguir su reloj.</summary>
        public bool SyncedToClip => !loading && CurrentTrack != null && CurrentTrack.path == null && source && source.isPlaying;
        public string Message => Time.unscaledTime < messageUntil ? message : "";
        public static string FolderPath => Path.Combine(Application.streamingAssetsPath, FolderName);

        Track CurrentTrack => genres.Count > 0 && trackIndex < genres[genreIndex].tracks.Count ? genres[genreIndex].tracks[trackIndex] : null;

        public void Setup(AudioSource audioSource, BuiltIn[] included, float startVolume)
        {
            source = audioSource; builtIns = included; volume = startVolume;
        }

        void Awake()
        {
            Instance = this;
            BuildLibrary();
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, volume));
            if (source) { source.volume = volume; source.playOnAwake = false; }
            string saved = PlayerPrefs.GetString(GenreKey, "");
            int i = genres.FindIndex(g => g.name == saved && g.tracks.Count > 0);
            genreIndex = i >= 0 ? i : Mathf.Max(0, genres.FindIndex(g => g.tracks.Count > 0));
            trackIndex = 0;
        }

        void Start() => PlayCurrent();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (streamed) Destroy(streamed);
        }

        // ---------------------------------------------------------------------
        // Biblioteca
        // ---------------------------------------------------------------------

        void BuildLibrary()
        {
            genres.Clear();
            if (builtIns != null)
                foreach (var b in builtIns)
                {
                    if (b == null || string.IsNullOrEmpty(b.genre)) continue;
                    var g = FindGenre(b.genre) ?? AddGenre(b.genre);
                    g.bpm = b.bpm;
                    if (b.clip) g.tracks.Add(new Track { title = b.title, clip = b.clip });
                }
            try
            {
                string root = FolderPath;
                if (!Directory.Exists(root)) return;
                foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d))
                {
                    var files = Directory.GetFiles(dir).Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderBy(f => f).ToList();
                    string name = Path.GetFileName(dir);
                    var g = FindGenre(name);
                    if (g == null) { if (files.Count == 0) continue; g = AddGenre(name); }
                    foreach (string f in files) g.tracks.Add(new Track { title = Path.GetFileNameWithoutExtension(f), path = f });
                }
            }
            catch (Exception e) { Debug.LogWarning("[MusicPlayer] No se pudo leer la carpeta de música: " + e.Message); }
        }

        Genre AddGenre(string name) { var g = new Genre { name = name }; genres.Add(g); return g; }
        Genre FindGenre(string name) => genres.FirstOrDefault(g => Plain(g.name) == Plain(name));

        /// <summary>Minúsculas y sin tildes, para que "Electrónica" y "electronica" sean el mismo género.</summary>
        static string Plain(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString().Trim();
        }

        // ---------------------------------------------------------------------
        // Controles
        // ---------------------------------------------------------------------

        public void NextGenre(int dir)
        {
            if (genres.Count == 0) return;
            int i = (genreIndex + dir + genres.Count) % genres.Count;
            if (genres[i].tracks.Count == 0)
            {
                // Género sin canciones: se avisa y se sigue buscando uno que sí tenga
                Say($"{genres[i].name}: sin canciones. Copia archivos .mp3 en {FolderName}/{genres[i].name}");
                int guard = 0;
                while (genres[i].tracks.Count == 0 && guard++ < genres.Count) i = (i + dir + genres.Count) % genres.Count;
                if (genres[i].tracks.Count == 0) return;
            }
            if (i == genreIndex) { OnChanged?.Invoke(); return; }
            genreIndex = i; trackIndex = 0;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-autodemo") < 0) PlayerPrefs.SetString(GenreKey, genres[i].name);   // la demo no cambia la preferencia
            PlayCurrent();
        }

        public void NextTrack(int dir)
        {
            int n = TrackCount;
            if (n == 0) return;
            trackIndex = (trackIndex + dir + n) % n;
            PlayCurrent();
        }

        public void ChangeVolume(float delta)
        {
            volume = Mathf.Clamp01(Mathf.Round((volume + delta) * 20f) / 20f);
            if (source) { source.volume = volume; if (volume > 0f) source.mute = false; }
            PlayerPrefs.SetFloat(VolumeKey, volume);
            OnChanged?.Invoke();
        }

        public void ToggleMute()
        {
            if (source) source.mute = !source.mute;
            OnChanged?.Invoke();
        }

        void Say(string text) { message = text; messageUntil = Time.unscaledTime + 5f; OnChanged?.Invoke(); }

        // ---------------------------------------------------------------------
        // Reproducción
        // ---------------------------------------------------------------------

        void PlayCurrent(int attempts = 0)
        {
            var t = CurrentTrack;
            if (t == null || source == null) { OnChanged?.Invoke(); return; }
            loadId++;
            if (t.path == null)
            {
                loading = false;
                bool alone = TrackCount == 1;
                repeatsLeft = alone ? 0 : 3;          // las pistas incluidas son bucles cortos: se repiten antes de avanzar
                StartClip(t.clip, alone);
            }
            else StartCoroutine(LoadAndPlay(t, loadId, attempts));
            OnChanged?.Invoke();
        }

        void StartClip(AudioClip clip, bool loop)
        {
            source.Stop();
            source.clip = clip; source.loop = loop; source.time = 0f;
            source.Play();
            graceUntil = Time.unscaledTime + 1f;
            Debug.Log($"[MusicPlayer] {GenreName}: {clip.name} ({clip.length:0} s)");
        }

        IEnumerator LoadAndPlay(Track t, int id, int attempts)
        {
            loading = true;
            string ext = Path.GetExtension(t.path).ToLowerInvariant();
            var type = ext == ".mp3" ? AudioType.MPEG : ext == ".ogg" ? AudioType.OGGVORBIS : AudioType.WAV;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(t.path).AbsoluteUri, type))
            {
                yield return req.SendWebRequest();
                if (id != loadId) yield break;                      // el jugador ya cambió de canción
                AudioClip clip = req.result == UnityWebRequest.Result.Success ? DownloadHandlerAudioClip.GetContent(req) : null;
                loading = false;
                if (clip == null || clip.length < 0.5f)
                {
                    Say("No se pudo abrir: " + t.title);
                    if (attempts < TrackCount) { trackIndex = (trackIndex + 1) % TrackCount; PlayCurrent(attempts + 1); }
                    yield break;
                }
                clip.name = t.title;
                var old = streamed;
                streamed = clip;
                repeatsLeft = 0;
                StartClip(clip, TrackCount == 1);
                if (old) Destroy(old);
                OnChanged?.Invoke();
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.mKey.wasPressedThisFrame) ToggleMute();
                if (kb.nKey.wasPressedThisFrame) NextTrack(1);
                if (kb.gKey.wasPressedThisFrame) NextGenre(1);
            }
            // Fin de la canción: repetir (pistas incluidas) o pasar a la siguiente de la lista
            if (source == null || loading || source.isPlaying || source.clip == null || Time.unscaledTime < graceUntil) return;
            if (!Application.isFocused && !Application.runInBackground) return;
            if (repeatsLeft > 0) { repeatsLeft--; source.Play(); graceUntil = Time.unscaledTime + 1f; }
            else NextTrack(1);
        }
    }
}
