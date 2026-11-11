using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Pantalla de la rockola: muestra género, canción y volumen, y sus botones manejan el
    /// reproductor de música (con la mira en PC o con el rayo del control en VR).
    /// </summary>
    public class JukeboxBoard : MonoBehaviour
    {
        [SerializeField] Text genreText;
        [SerializeField] Text trackText;
        [SerializeField] Text volumeText;
        [SerializeField] Text infoText;

        MusicPlayer player;
        string lastMessage = "";

        public void Setup(Text genre, Text track, Text volume, Text info)
        {
            genreText = genre; trackText = track; volumeText = volume; infoText = info;
        }

        void Start()
        {
            player = MusicPlayer.Instance;
            if (player == null) { enabled = false; return; }
            player.OnChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (player != null) player.OnChanged -= Refresh;
        }

        void Update()
        {
            // El aviso temporal se borra solo
            if (player != null && player.Message != lastMessage) Refresh();
        }

        void Refresh()
        {
            lastMessage = player.Message;
            genreText.text = player.GenreName.ToUpper();
            trackText.text = player.TrackTitle;
            volumeText.text = player.Muted ? "<color=#FF8073>SILENCIO</color>" : $"VOL {Mathf.RoundToInt(player.Volume * 100f)}%";
            infoText.text = lastMessage.Length > 0
                ? $"<color=#FFC21A>{lastMessage}</color>"
                : $"Canción {player.TrackNumber} de {player.TrackCount}   ·   Tus canciones: carpeta {MusicPlayer.FolderName}/{player.GenreName}";
        }

        public void PrevGenre() { if (player) player.NextGenre(-1); }
        public void NextGenre() { if (player) player.NextGenre(1); }
        public void PrevTrack() { if (player) player.NextTrack(-1); }
        public void NextTrack() { if (player) player.NextTrack(1); }
        public void VolumeDown() { if (player) player.ChangeVolume(-0.05f); }
        public void VolumeUp() { if (player) player.ChangeVolume(0.05f); }
        public void Mute() { if (player) player.ToggleMute(); }
    }
}
