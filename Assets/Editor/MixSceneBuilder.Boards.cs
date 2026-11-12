using MixLab;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SB = QualityControlXR.EditorTools.SceneBuilder;

namespace QualityControlXR.EditorTools
{
    /// <summary>Tableros del juego (letra grande, pensada para leerse en VR) y pantalla de la rockola.</summary>
    public static partial class MixSceneBuilder
    {
        const float BookX = -2.6f;
        static readonly Vector2 BookSize = new Vector2(1340, 1250);
        const float BookScale = 0.0014f;

        static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rt = SB.Child(parent, name);
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
            rt.anchoredPosition = min; rt.sizeDelta = max - min;
            return rt;
        }

        static Text Fit(Text t, int min, int max)   // min se conserva solo como referencia de diseño
        {
            // Tamaño fijo: el ajuste automático de Unity rasteriza cada tamaño que prueba y desborda la textura de la fuente
            t.fontSize = max;
            return t;
        }

        static MixBoard BuildMainBoard()
        {
            var canvas = SB.MakeCanvas("MainBoard", new Vector3(0f, 2.52f, WallZ - 0.06f), Quaternion.identity, new Vector2(1700, 820), 0.0016f);
            canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;   // letras grandes: menos resolución por glifo para no llenar la textura de la fuente
            SB.Panel(canvas.transform, "Bg", PanelBg);
            SB.Label(canvas.transform, "Header", "<b>LABORATORIO DE MEZCLAS</b>", 30, TextAnchor.MiddleCenter, Muted,
                new Vector2(40, 770), new Vector2(1660, 812), absolute: true);

            var waiting = SB.Child(canvas.transform, "WaitingPanel");
            SB.Label(waiting, "Text", "Completa el <b>entrenamiento</b>\ndel tablero de la derecha\npara iniciar el turno", 96, TextAnchor.MiddleCenter, Ink,
                new Vector2(60, 60), new Vector2(1640, 760), absolute: true);

            var game = SB.Child(canvas.transform, "GamePanel");
            SB.Label(game, "OrderCaption", "PEDIDO DEL CLIENTE", 38, TextAnchor.MiddleLeft, Muted, new Vector2(50, 715), new Vector2(1000, 765), absolute: true);
            var title = SB.Label(game, "OrderTitle", "LIMONADA HELADA", 124, TextAnchor.MiddleLeft, Copper, new Vector2(50, 575), new Vector2(1110, 715), absolute: true);
            title.fontStyle = FontStyle.Bold; Fit(title, 56, 104);
            var detail = Fit(SB.Label(game, "OrderDetail", "agua · limón · hielo", 60, TextAnchor.UpperLeft, Ink, new Vector2(50, 385), new Vector2(1110, 565), absolute: true), 44, 60);
            SB.Label(game, "PatienceCaption", "PACIENCIA", 38, TextAnchor.MiddleLeft, Muted, new Vector2(50, 325), new Vector2(400, 375), absolute: true);
            var barBg = Box(game, "PatienceBar", new Vector2(50, 255), new Vector2(1110, 320));
            barBg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var fill = SB.Child(barBg, "Fill");
            fill.pivot = new Vector2(0f, 0.5f);
            var fillImg = fill.gameObject.AddComponent<Image>(); fillImg.color = new Color(0.45f, 0.9f, 0.5f); fillImg.raycastTarget = false;
            var timer = SB.Label(game, "Timer", "TIEMPO\n<b>4:00</b>", 98, TextAnchor.MiddleCenter, Color.white, new Vector2(1140, 505), new Vector2(1660, 765), absolute: true);
            timer.lineSpacing = 0.85f;
            var score = SB.Label(game, "Score", "PUNTOS\n<b>0</b>", 44, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 0.55f), new Vector2(1140, 255), new Vector2(1400, 490), absolute: true);
            var kos = SB.Label(game, "Knockouts", "DESMAYOS\n<b>0/3</b>", 44, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.45f), new Vector2(1400, 255), new Vector2(1660, 490), absolute: true);
            score.lineSpacing = kos.lineSpacing = 0.85f;
            var alert = Fit(SB.Label(game, "Alert", "", 64, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.2f), new Vector2(40, 85), new Vector2(1660, 240), absolute: true), 48, 64);
            SB.Label(game, "Footer", "Vierte · Agita · Sirve     ·     El lavaplatos vacía el vaso     ·     3 desmayos suspenden el turno", 36, TextAnchor.MiddleCenter, Muted,
                new Vector2(40, 20), new Vector2(1660, 75), absolute: true);

            var results = SB.Child(canvas.transform, "ResultsPanel");
            var resultsText = Fit(SB.Label(results, "Text", "", 52, TextAnchor.MiddleCenter, Ink, new Vector2(50, 200), new Vector2(1650, 765), absolute: true), 40, 52);
            resultsText.lineSpacing = 1.1f;

            var board = canvas.gameObject.AddComponent<MixBoard>();
            SB.MakeButton(results, "RestartButton", "REINICIAR", new Vector2(390, 40), new Vector2(830, 180), new Color(0.12f, 0.55f, 0.3f), board.OnRestartPressed, 60);
            SB.MakeButton(results, "QuitButton", "SALIR", new Vector2(870, 40), new Vector2(1310, 180), new Color(0.55f, 0.15f, 0.15f), board.OnQuitPressed, 60);
            board.Setup(waiting.gameObject, game.gameObject, results.gameObject, title, detail, timer, score, kos, alert, resultsText, fill, fillImg);
            game.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            return board;
        }

        /// <summary>Libro de reacciones: empieza vacío y se va llenando con lo que el jugador descubre.</summary>
        static void BuildBook(int count)
        {
            var canvas = SB.MakeCanvas("ReactionBook", new Vector3(BookX, 2.32f, WallZ - 0.06f), Quaternion.identity, BookSize, BookScale);
            canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;   // letras grandes: menos resolución por glifo para no llenar la textura de la fuente
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            SB.Panel(canvas.transform, "Bg", PanelBg);
            float w = BookSize.x;
            var title = SB.Label(canvas.transform, "Title", "<b>LIBRO DE REACCIONES</b>", 66, TextAnchor.MiddleCenter, Copper,
                new Vector2(20, 1150), new Vector2(w - 20, 1240), absolute: true);
            var rows = new Text[count];
            float step = Mathf.Min(84f, 1060f / count);
            int font = Mathf.Clamp(Mathf.FloorToInt(step * 0.86f), 40, 60);
            for (int i = 0; i < count; i++)
            {
                float y = 1140f - (i + 1) * step;
                rows[i] = SB.Label(canvas.transform, "Row" + i, "", font, TextAnchor.MiddleLeft, Ink, new Vector2(40, y), new Vector2(w - 20, y + step), absolute: true);
                rows[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var footer = SB.Label(canvas.transform, "Footer", "", 40, TextAnchor.LowerCenter, Muted, new Vector2(20, 14), new Vector2(w - 20, 120), absolute: true);
            canvas.gameObject.AddComponent<ReactionBookBoard>().Setup(title, rows, footer);
        }

        static void BuildTutorial(MixGlass glass, AudioSource uiAudio)
        {
            var canvas = SB.MakeCanvas("TutorialBoard", new Vector3(2.4f, 2.4f, WallZ - 0.06f), Quaternion.identity, new Vector2(1150, 1000), 0.0014f);
            canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;   // letras grandes: menos resolución por glifo para no llenar la textura de la fuente
            SB.Panel(canvas.transform, "Bg", PanelBg);
            SB.Label(canvas.transform, "Title", "<b>ENTRENAMIENTO</b>", 62, TextAnchor.MiddleCenter, Copper, new Vector2(30, 895), new Vector2(1120, 985), absolute: true);
            var instr = Fit(SB.Label(canvas.transform, "Instruction", "", 54, TextAnchor.MiddleCenter, Ink, new Vector2(40, 280), new Vector2(1110, 885), absolute: true), 44, 54);
            instr.lineSpacing = 1.1f;
            var progress = SB.Label(canvas.transform, "Progress", "0/4", 44, TextAnchor.LowerRight, Muted, new Vector2(880, 25), new Vector2(1110, 95), absolute: true);

            var tutorial = canvas.gameObject.AddComponent<MixTutorial>();
            var start = SB.MakeButton(canvas.transform, "StartButton", "INICIAR TURNO", new Vector2(225, 115), new Vector2(925, 265), new Color(0.1f, 0.6f, 0.3f), tutorial.StartShift, 62);
            var skip = SB.MakeButton(canvas.transform, "SkipButton", "Saltar tutorial", new Vector2(40, 25), new Vector2(420, 100), new Color(0.25f, 0.22f, 0.36f), tutorial.Skip, 36);
            tutorial.Setup(instr, progress, start.gameObject, skip.gameObject, glass, uiAudio, SB.Clip("select"), SceneBuilder.DesktopMode);
            EditorUtility.SetDirty(tutorial);
        }

        /// <summary>
        /// Rockola: mueble a la izquierda del puesto del jugador con una pantalla para elegir
        /// género, canción y volumen. Sus botones se pulsan con la mira (PC) o el rayo (VR).
        /// </summary>
        static void BuildJukebox()
        {
            var basePos = new Vector3(-2.85f, 0f, -0.75f);
            Vector3 toPlayer = PlayerStart - basePos; toPlayer.y = 0f;
            var root = new GameObject("--- ROCKOLA ---").transform;
            root.SetPositionAndRotation(basePos, Quaternion.LookRotation(-toPlayer));     // la cara -Z queda hacia el jugador

            var body = SB.Mat("JukeboxBody", new Color(0.32f, 0.1f, 0.22f));
            var dark = SB.Mat("StationDark", new Color(0.12f, 0.1f, 0.18f));
            var pink = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NeonPink.mat");
            var amber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NeonAmber.mat");
            SB.Prim(PrimitiveType.Cube, "Body", root, new Vector3(0f, 0.6f, 0.2f), new Vector3(1.1f, 1.2f, 0.45f), body, true, local: true);
            SB.Prim(PrimitiveType.Cube, "Grill", root, new Vector3(0f, 0.55f, -0.03f), new Vector3(0.9f, 0.7f, 0.02f), dark, false, local: true);
            for (int i = 0; i < 5; i++)
                pulseNeons.Add(L(PrimitiveType.Cube, "GrillBar" + i, root, new Vector3(-0.32f + i * 0.16f, 0.55f, -0.045f), new Vector3(0.03f, 0.6f, 0.012f), i % 2 == 0 ? pink : amber).GetComponent<Renderer>());
            SB.Prim(PrimitiveType.Cube, "ScreenBack", root, new Vector3(0f, 1.68f, 0.12f), new Vector3(1.4f, 0.94f, 0.06f), dark, false, local: true)
                .transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            foreach (float s in new[] { -1f, 1f })
                pulseNeons.Add(L(PrimitiveType.Cube, "Tube", root, new Vector3(s * 0.57f, 0.6f, -0.01f), new Vector3(0.035f, 1.2f, 0.035f), pink).GetComponent<Renderer>());

            var size = new Vector2(1100, 720);
            var canvas = SB.MakeCanvas("JukeboxScreen", root.TransformPoint(new Vector3(0f, 1.68f, 0.08f)), root.rotation * Quaternion.Euler(10f, 0f, 0f), size, 0.0012f);
            canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;   // letras grandes: menos resolución por glifo para no llenar la textura de la fuente
            canvas.transform.SetParent(root, true);
            SB.Panel(canvas.transform, "Bg", PanelBg);
            SB.Label(canvas.transform, "Title", "<b>ROCKOLA</b>  <size=34>elige la música del bar</size>", 54, TextAnchor.MiddleCenter, Copper,
                new Vector2(20, 630), new Vector2(1080, 710), absolute: true);
            var genre = Fit(SB.Label(canvas.transform, "Genre", "HOUSE", 88, TextAnchor.MiddleCenter, Ink, new Vector2(190, 480), new Vector2(910, 620), absolute: true), 50, 88);
            genre.fontStyle = FontStyle.Bold;
            var track = Fit(SB.Label(canvas.transform, "Track", "", 44, TextAnchor.MiddleCenter, new Color(0.6f, 0.95f, 0.88f), new Vector2(190, 320), new Vector2(910, 465), absolute: true), 34, 44);
            var volume = SB.Label(canvas.transform, "Volume", "VOL 30%", 52, TextAnchor.MiddleCenter, Ink, new Vector2(330, 165), new Vector2(620, 300), absolute: true);
            var info = Fit(SB.Label(canvas.transform, "Info", "", 30, TextAnchor.MiddleCenter, Muted, new Vector2(20, 20), new Vector2(1080, 150), absolute: true), 26, 30);

            var board = canvas.gameObject.AddComponent<JukeboxBoard>();
            board.Setup(genre, track, volume, info);
            var blue = new Color(0.22f, 0.3f, 0.5f); var violet = new Color(0.36f, 0.24f, 0.55f);
            SB.MakeButton(canvas.transform, "PrevGenre", "«", new Vector2(30, 485), new Vector2(180, 615), violet, board.PrevGenre, 90);
            SB.MakeButton(canvas.transform, "NextGenre", "»", new Vector2(920, 485), new Vector2(1070, 615), violet, board.NextGenre, 90);
            SB.MakeButton(canvas.transform, "PrevTrack", "«", new Vector2(30, 330), new Vector2(180, 460), blue, board.PrevTrack, 90);
            SB.MakeButton(canvas.transform, "NextTrack", "»", new Vector2(920, 330), new Vector2(1070, 460), blue, board.NextTrack, 90);
            SB.MakeButton(canvas.transform, "VolumeDown", "VOL −", new Vector2(30, 170), new Vector2(310, 295), blue, board.VolumeDown, 50);
            SB.MakeButton(canvas.transform, "VolumeUp", "VOL +", new Vector2(640, 170), new Vector2(820, 295), blue, board.VolumeUp, 50);
            SB.MakeButton(canvas.transform, "Mute", "SILENCIO", new Vector2(840, 170), new Vector2(1070, 295), new Color(0.55f, 0.15f, 0.15f), board.Mute, 40);
            EditorUtility.SetDirty(board);
        }
    }
}
