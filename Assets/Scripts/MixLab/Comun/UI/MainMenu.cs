using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Pantalla inicial: un panel grande frente al jugador con el bar vivo detrás.
    /// Jugar (con entrenamiento), ir directo al turno, cómo jugar, opciones y salir.
    /// Se pulsa con el mouse en PC o con el rayo de los controles en VR.
    /// Mientras está abierta, el jugador de PC queda quieto y con el cursor libre.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public const string MasterVolumeKey = "mix_master_volume";

        [SerializeField] GameObject mainPanel, howToPanel, optionsPanel;
        [SerializeField] Text statsText, howToText, musicValue, masterValue, sensValue, resetLabel;
        [SerializeField] GameObject sensRow;

        /// <summary>Al reiniciar desde el juego no se vuelve a mostrar el menú.</summary>
        public static bool SkipNextLoad;
        public static bool IsOpen { get; private set; }

        MixInteractor hand;
        GameObject hud;
        float resetArmedUntil;
        bool placedForVr;

        public void Setup(GameObject main, GameObject howTo, GameObject options, Text stats, Text howToBody,
                          Text music, Text master, Text sens, GameObject sensRowGo, Text reset)
        {
            mainPanel = main; howToPanel = howTo; optionsPanel = options; statsText = stats; howToText = howToBody;
            musicValue = music; masterValue = master; sensValue = sens; sensRow = sensRowGo; resetLabel = reset;
        }

        void Awake()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        }

        void Start()
        {
            bool skip = SkipNextLoad || Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-autodemo");
            SkipNextLoad = false;
            if (skip) { gameObject.SetActive(false); return; }

            hand = FindFirstObjectByType<MixInteractor>();
            var hudComp = FindFirstObjectByType<DesktopHud>();
            hud = hudComp ? hudComp.gameObject : null;
            Open();
        }

        void Open()
        {
            IsOpen = true;
            gameObject.SetActive(true);
            ShowPanel(mainPanel);
            RefreshStats();
            SetGameInput(false);
        }

        void Close()
        {
            IsOpen = false;
            gameObject.SetActive(false);
            SetGameInput(true);
        }

        /// <summary>Congela o libera al jugador de PC (en VR no hace falta: el menú es un panel más).</summary>
        void SetGameInput(bool on)
        {
            var player = DesktopPlayer.Instance;
            if (player) player.Paused = !on;
            if (hand) hand.enabled = on;
            if (hud) hud.SetActive(on && !MixVrRig.Active);
            if (!MixVrRig.Active) DesktopPlayer.SetCursorLocked(on);
        }

        void Update()
        {
            // El jugador de PC bloquea el cursor al arrancar: mientras el menú está abierto, se mantiene libre.
            if (IsOpen && !MixVrRig.Active && Cursor.lockState != CursorLockMode.None) DesktopPlayer.SetCursorLocked(false);
            if (IsOpen && MixVrRig.Active && MixVrRig.Placed && !placedForVr) PlaceInFrontOfHead();
            if (resetLabel && resetArmedUntil > 0f && Time.unscaledTime > resetArmedUntil)
            {
                resetArmedUntil = 0f; resetLabel.text = "<b>BORRAR PROGRESO</b>";
            }
        }

        void OnDestroy() { if (IsOpen) IsOpen = false; }

        /// <summary>
        /// En VR el panel de PC quedaría pegado a la cara: se pone a 85 cm frente a la cabeza,
        /// a la altura de los ojos y un poco más grande, para leerlo y apuntarle cómodo con el rayo.
        /// </summary>
        void PlaceInFrontOfHead()
        {
            placedForVr = true;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            transform.position = cam.transform.position + fwd * 0.85f + Vector3.down * 0.08f;   // antes de las botellas del estante (z 0.92) para que no lo tapen
            transform.rotation = Quaternion.LookRotation(fwd);
            transform.localScale = Vector3.one * 0.0008f;
            var canvas = GetComponent<Canvas>();
            if (canvas) canvas.worldCamera = cam;
        }

        void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            howToPanel.SetActive(panel == howToPanel);
            optionsPanel.SetActive(panel == optionsPanel);
        }

        void RefreshStats()
        {
            var gm = MixGameManager.Instance;
            if (gm == null || statsText == null) return;
            statsText.text = $"Récord: <b>{gm.BestScore}</b>     ·     Turnos jugados: <b>{gm.Sessions.Count}</b>     ·     " +
                             $"Reacciones descubiertas: <b>{gm.DiscoveredCount}/{gm.Reactions.Count}</b>";
        }

        // =====================================================================
        // Botones
        // =====================================================================

        public void Play() => Close();

        public void PlayDirect()
        {
            Close();
            FindFirstObjectByType<MixTutorial>()?.Skip();
            MixGameManager.Instance?.StartShift();
        }

        public void ShowHowTo()
        {
            howToText.text = MixVrRig.Active ? HowToVr : HowToPc;
            ShowPanel(howToPanel);
        }

        public void ShowOptions()
        {
            if (sensRow) sensRow.SetActive(!MixVrRig.Active);
            RefreshOptions();
            ShowPanel(optionsPanel);
        }

        public void Back() => ShowPanel(mainPanel);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void MusicDown() { MusicPlayer.Instance?.ChangeVolume(-0.1f); RefreshOptions(); }
        public void MusicUp() { MusicPlayer.Instance?.ChangeVolume(0.1f); RefreshOptions(); }

        public void MasterDown() => SetMaster(AudioListener.volume - 0.1f);
        public void MasterUp() => SetMaster(AudioListener.volume + 0.1f);

        void SetMaster(float v)
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Round(v * 10f) / 10f);
            PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
            RefreshOptions();
        }

        public void SensDown() { var p = DesktopPlayer.Instance; if (p) p.Sensitivity -= 0.02f; RefreshOptions(); }
        public void SensUp() { var p = DesktopPlayer.Instance; if (p) p.Sensitivity += 0.02f; RefreshOptions(); }

        /// <summary>Hay que pulsarlo dos veces seguidas: borra récord, libro e historial.</summary>
        public void ResetProgress()
        {
            if (resetArmedUntil <= 0f)
            {
                resetArmedUntil = Time.unscaledTime + 3f;
                resetLabel.text = "<b>¿SEGURO? PULSA OTRA VEZ</b>";
                return;
            }
            resetArmedUntil = 0f;
            MixSaveRepository.Delete();
            try { if (System.IO.File.Exists(MixSaveRepository.CsvPath)) System.IO.File.Delete(MixSaveRepository.CsvPath); } catch { }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // vuelve al menú con todo en cero
        }

        void RefreshOptions()
        {
            var music = MusicPlayer.Instance;
            if (musicValue) musicValue.text = music ? $"{Mathf.RoundToInt(music.Volume * 100f)}%" : "-";
            if (masterValue) masterValue.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
            var p = DesktopPlayer.Instance;
            if (sensValue) sensValue.text = p ? (p.Sensitivity * 10f).ToString("0.0") : "-";
        }

        const string HowToPc =
            "<b>OBJETIVO</b>\nPrepara lo que pide cada cliente antes de que pierda la paciencia. " +
            "Si te equivocas, la mezcla reacciona en él. Tres desmayos y se acaba el turno.\n\n" +
            "<b>CONTROLES</b>\nWASD caminar · Mouse mirar\nClic en un ingrediente: verter · Clic en una palanca: usarla\n" +
            "Clic en el vaso: agarrarlo, luego clic en el cliente: servir\nLavaplatos: vaciar · Esc: pausa · H: ayuda\n\n" +
            "<b>ANALIZADOR</b>\nVerde: se puede servir · Ámbar: inestable, enfríala con hielo o palanca en FRÍO · Rojo: tóxica";

        const string HowToVr =
            "<b>OBJETIVO</b>\nPrepara lo que pide cada cliente antes de que pierda la paciencia. " +
            "Si te equivocas, la mezcla reacciona en él. Tres desmayos y se acaba el turno.\n\n" +
            "<b>CONTROLES</b>\nJoystick izquierdo: caminar · derecho: girar\nBotón lateral (GRIP): agarrar botellas, vaso y palancas\n" +
            "Inclina la botella sobre el vaso para verter · Sacude el vaso para agitar\nVoltea el vaso para vaciarlo · Suéltalo en el posavasos para servir\n\n" +
            "<b>ANALIZADOR</b>\nVerde: se puede servir · Ámbar: inestable, enfríala con hielo o palanca en FRÍO · Rojo: tóxica";
    }
}
