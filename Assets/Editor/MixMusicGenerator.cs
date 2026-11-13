using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MixLab.EditorTools
{
    /// <summary>
    /// Sintetiza las pistas originales de la rockola (un bucle perfecto de 16 compases por género:
    /// house, electrónica, guaracha y rock) y las guarda como WAV. Es música generada por código:
    /// sin licencias de terceros. Menú: Tools > Mix Lab > Regenerate Music.
    /// </summary>
    public static class MixMusicGenerator
    {
        public enum Style { House, Electronica, Guaracha, Rock }

        public const string Folder = "Assets/Audio/Mix";
        public const float Bpm = 124f;        // tempo de la pista house (compatibilidad)
        const int SR = 44100;
        const int Bars = 16;

        public static readonly Style[] Styles = { Style.House, Style.Electronica, Style.Guaracha, Style.Rock };

        public static string GenreName(Style s) => s == Style.Electronica ? "Electrónica" : s.ToString();
        public static string Title(Style s) =>
            s == Style.House ? "Mix Lab House" : s == Style.Electronica ? "Luces de Neón" : s == Style.Guaracha ? "Guaracha de la Barra" : "Rock del Mesón";
        public static float Tempo(Style s) => s == Style.House ? 124f : s == Style.Rock ? 120f : 128f;
        public static string PathOf(Style s) => $"{Folder}/{s.ToString().ToLowerInvariant()}_loop.wav";

        static float[] L, R;
        static int N;
        static double spb;      // muestras por negra
        static float duck;      // cuánto "bombea" el volumen con cada bombo
        static System.Random rng;

        [MenuItem("Tools/Mix Lab/Regenerate Music")]
        public static void Regenerate() { foreach (var s in Styles) Generate(s, true); }

        public static AudioClip EnsureClip() => EnsureClip(Style.House);
        public static AudioClip EnsureClip(Style style) => Generate(style, false);

        static AudioClip Generate(Style style, bool force)
        {
            string path = PathOf(style);
            if (force || !File.Exists(path))
            {
                Directory.CreateDirectory(Folder);
                Synthesize(style);
                WriteWav(path, style);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                var s = importer.defaultSampleSettings;
                if (s.loadType != AudioClipLoadType.CompressedInMemory || s.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    s.loadType = AudioClipLoadType.CompressedInMemory;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.7f;
                    importer.defaultSampleSettings = s;
                    importer.loadInBackground = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------------------------------------------------------------------

        static void Synthesize(Style style)
        {
            spb = SR * 60.0 / Tempo(style);
            N = (int)Math.Round(spb * 4 * Bars);
            L = new float[N]; R = new float[N];
            rng = new System.Random(2026 + (int)style);
            switch (style)
            {
                case Style.House: House(); break;
                case Style.Electronica: Electronica(); break;
                case Style.Guaracha: Guaracha(); break;
                case Style.Rock: Rock(); break;
            }
            // Límite suave y normalización
            float peak = 0f;
            for (int i = 0; i < N; i++)
            {
                L[i] = (float)Math.Tanh(L[i] * 1.15f); R[i] = (float)Math.Tanh(R[i] * 1.15f);
                peak = Mathf.Max(peak, Mathf.Abs(L[i]), Mathf.Abs(R[i]));
            }
            float g = peak > 0f ? 0.89f / peak : 1f;
            for (int i = 0; i < N; i++) { L[i] *= g; R[i] *= g; }
        }

        static double Step(int bar, int step) => (bar * 16 + step) * spb * 0.25;
        static readonly int[] ArpOrder = { 0, 2, 1, 3, 2, 0, 3, 1 };
        static readonly double[] ClapTaps = { 0.0, 0.011, 0.022 };

        // ---------- House (124 BPM): La menor 7 – Fa mayor 7 – Do mayor 7 – Sol 6 ----------

        static void House()
        {
            duck = 0.75f;
            double[][] chords =
            {
                new[] { 220.00, 261.63, 329.63, 392.00 }, new[] { 174.61, 220.00, 261.63, 329.63 },
                new[] { 196.00, 246.94, 261.63, 329.63 }, new[] { 196.00, 246.94, 293.66, 329.63 },
            };
            double[] roots = { 55.00, 87.31, 65.41, 98.00 };
            int[] stabsA = { 0, 3, 6, 10, 13 }, stabsB = { 2, 6, 10, 14 };
            for (int bar = 0; bar < Bars; bar++)
            {
                int chord = (bar / 2) % 4;
                FourOnTheFloor(bar, bar % 8 == 7);
                for (int step = 0; step < 16; step++)
                {
                    double t = Step(bar, step);
                    if (step % 4 == 2) Bass(t, roots[chord], 0.42f, 0.22);
                    if (step == 15 && bar % 2 == 1) Bass(t, roots[chord] * 2, 0.3f, 0.1);
                    if (step == 7 && bar % 4 == 2) Bass(t, roots[chord] * 1.5, 0.28f, 0.1);
                    if (Array.IndexOf(bar % 2 == 0 ? stabsA : stabsB, step) >= 0) Stab(t, chords[chord], 0.075f, 8.5, 0.42);
                    if (bar >= Bars / 2 && (step % 2 == 0 || step % 8 == 3))
                        Echo(t, chords[chord][ArpOrder[(step + bar) % 8]] * 2, 0.075f, false);
                }
            }
        }

        // ---------- Electrónica (128 BPM): Fa menor – Re♭ – La♭ – Mi♭, bajo rodante y arpegio ----------

        static void Electronica()
        {
            duck = 0.8f;
            double[][] chords =
            {
                new[] { 174.61, 207.65, 261.63, 311.13 }, new[] { 174.61, 207.65, 277.18, 349.23 },
                new[] { 155.56, 207.65, 261.63, 311.13 }, new[] { 155.56, 196.00, 233.08, 311.13 },
            };
            double[] roots = { 87.31, 69.30, 103.83, 77.78 };
            for (int bar = 0; bar < Bars; bar++)
            {
                int chord = (bar / 2) % 4;
                FourOnTheFloor(bar, bar % 8 == 7);
                Stab(Step(bar, 0), chords[chord], 0.05f, 1.6, 1.8);          // colchón largo
                for (int step = 0; step < 16; step++)
                {
                    double t = Step(bar, step);
                    if (step % 4 != 0) Bass(t, roots[chord] * (step % 4 == 3 ? 2 : 1), 0.34f, 0.08);   // bajo rodante
                    double f = chords[chord][ArpOrder[(step + bar * 3) % 8]] * (step % 8 < 4 ? 2 : 4);
                    Echo(t, f, bar < 4 ? 0.045f : 0.07f, true);
                }
            }
        }

        // ---------- Guaracha (128 BPM): percusión tribal, bajo de una nota y pito ----------

        static void Guaracha()
        {
            duck = 0.7f;
            double[] roots = { 55.00, 55.00, 49.00, 43.65 };
            int[] toms = { 3, 6, 7, 10, 11, 14 };
            double[] tomPitch = { 190, 150, 190, 120, 150, 230 };
            int[] riff = { 0, 3, 6, 8, 11, 14 };
            double[][] melody =
            {
                new[] { 880.00, 880.00, 1046.50, 987.77, 880.00, 783.99 },
                new[] { 880.00, 1046.50, 1318.51, 1174.66, 1046.50, 880.00 },
                new[] { 783.99, 783.99, 987.77, 880.00, 783.99, 659.25 },
                new[] { 698.46, 880.00, 1046.50, 987.77, 880.00, 659.25 },
            };
            for (int bar = 0; bar < Bars; bar++)
            {
                int part = (bar / 2) % 4;
                for (int beat = 0; beat < 4; beat++)
                {
                    double t0 = Step(bar, beat * 4);
                    Kick(t0);
                    if (beat == 1 || beat == 3) Clap(t0);
                    for (int s = 0; s < 4; s++) Hat(t0 + spb * 0.25 * s, s == 2 ? 0.16f : 0.07f, s == 2 ? 0.05 : 0.016);   // maraca
                }
                for (int k = 0; k < toms.Length; k++) Tom(Step(bar, toms[k]), tomPitch[(k + bar) % tomPitch.Length], 0.3f);
                if (bar % 4 == 3) for (int s = 12; s < 16; s++) Tom(Step(bar, s), 130 + (s - 12) * 35, 0.34f);          // redoble
                for (int step = 0; step < 16; step++)
                {
                    double t = Step(bar, step);
                    if (step % 4 == 2 || step % 8 == 7) Bass(t, roots[part] * 2, 0.4f, 0.13);
                }
                if (bar >= 2)
                    for (int k = 0; k < riff.Length; k++)
                        Lead(Step(bar, riff[k]), melody[bar % 2 == 0 ? part : (part + 1) % 4][k], 0.16, 0.085f, true);
            }
        }

        // ---------- Rock (120 BPM): batería, guitarra distorsionada en quintas, bajo y solo ----------

        static void Rock()
        {
            duck = 0f;
            double[] roots = { 82.41, 98.00, 110.00, 130.81 };      // Mi – Sol – La – Do
            double[] penta = { 329.63, 392.00, 440.00, 493.88, 587.33, 659.25, 783.99 };
            int[] solo = { 4, 4, 5, 6, 5, 4, 2, 1, 2, 4, 2, 1, 0, 1, 2, 4 };
            for (int bar = 0; bar < Bars; bar++)
            {
                int chord = (bar / 2) % 4;
                double root = roots[chord];
                Kick(Step(bar, 0)); Kick(Step(bar, 8)); Kick(Step(bar, 10));
                if (bar % 2 == 1) Kick(Step(bar, 7));
                Snare(Step(bar, 4)); Snare(Step(bar, 12));
                if (bar % 4 == 3) { Snare(Step(bar, 14)); Snare(Step(bar, 15)); }
                if (bar % 4 == 0) Hat(Step(bar, 0), 0.3f, 0.35);                                   // platillo
                for (int e = 0; e < 8; e++) Hat(Step(bar, e * 2), e % 2 == 0 ? 0.13f : 0.08f, 0.03);
                for (int e = 0; e < 8; e++)
                {
                    double t = Step(bar, e * 2);
                    bool accent = e == 0 || e == 3 || e == 6;
                    Guitar(t, root, accent ? 0.34 : 0.13, accent ? 0.2f : 0.14f);
                    Bass(t, root, 0.3f, 0.2);
                }
                if (bar >= Bars / 2)
                    for (int k = 0; k < 4; k++)
                    {
                        int idx = solo[((bar - Bars / 2) * 4 + k) % solo.Length];
                        Lead(Step(bar, k * 4), penta[idx] * (chord == 3 ? 1.0 : 1.0), k == 3 ? 0.42 : 0.3, 0.075f, false);
                    }
            }
        }

        /// <summary>Base de baile: bombo en cada negra, palmas en 2 y 4, platillo al contratiempo.</summary>
        static void FourOnTheFloor(int bar, bool fill)
        {
            for (int beat = 0; beat < 4; beat++)
            {
                double t0 = Step(bar, beat * 4);
                Kick(t0);
                if (beat == 1 || beat == 3) Clap(t0);
                Hat(t0 + spb * 0.5, 0.2f, 0.075);
                for (int s = 0; s < 4; s++)
                {
                    if (s == 2) continue;
                    Hat(t0 + spb * 0.25 * s, s == 0 ? 0.05f : 0.075f, 0.018);
                }
                if (fill && beat == 3) { Clap(t0 + spb * 0.5); Clap(t0 + spb * 0.75); }
            }
        }

        static void Echo(double t, double freq, float amp, bool bright)
        {
            Pluck(t, freq, amp, -0.4f, bright);
            Pluck(t + spb * 0.75, freq, amp * 0.55f, 0.5f, bright);
            Pluck(t + spb * 1.5, freq, amp * 0.28f, -0.6f, bright);
        }

        // ---------------------------------------------------------------------
        // Instrumentos
        // ---------------------------------------------------------------------

        static void Add(int i, float l, float r)
        {
            i %= N; if (i < 0) i += N;
            L[i] += l; R[i] += r;
        }

        /// <summary>Compresión lateral (el "bombeo" del house): baja el volumen justo después de cada bombo.</summary>
        static float Duck(double sample)
        {
            double tb = (sample % spb) / SR;
            return 1f - duck * (float)Math.Exp(-tb * 11.0);
        }

        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static void Kick(double start)
        {
            int i0 = (int)start, len = (int)(0.36 * SR);
            double phase = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                double f = 47 + 125 * Math.Exp(-t * 34);
                phase += 2 * Math.PI * f / SR;
                float env = (float)Math.Exp(-t * 8.5) * Mathf.Clamp01(i / 40f);
                float s = (float)Math.Tanh(Math.Sin(phase) * 1.6) * env * 0.9f;
                s += Noise() * (float)Math.Exp(-t * 500) * 0.18f;
                Add(i0 + i, s, s);
            }
        }

        static void Tom(double start, double freq, float amp)
        {
            int i0 = (int)start, len = (int)(0.22 * SR);
            double phase = 0;
            float pan = (float)((freq - 170) / 200.0);
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                phase += 2 * Math.PI * freq * (1 + 0.5 * Math.Exp(-t * 30)) / SR;
                float s = (float)(Math.Sin(phase) * Math.Exp(-t * 17)) * Mathf.Clamp01(i / 30f) * amp;
                s += Noise() * (float)Math.Exp(-t * 220) * 0.05f;
                Add(i0 + i, s * (1f - pan), s * (1f + pan));
            }
        }

        static void Clap(double start)
        {
            int i0 = (int)start, len = (int)(0.2 * SR);
            float lpL = 0, lpR = 0, hpL = 0, hpR = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                double env = Math.Exp(-t * 26);
                foreach (double off in ClapTaps)
                    if (t >= off && t < off + 0.008) env = Math.Max(env, 1.0 - (t - off) / 0.008 * 0.5);
                float nl = Noise(), nr = Noise();
                lpL += 0.55f * (nl - lpL); lpR += 0.55f * (nr - lpR);        // pasa bajos
                float bl = lpL - hpL, br = lpR - hpR;                        // pasa altos
                hpL += 0.12f * (lpL - hpL); hpR += 0.12f * (lpR - hpR);
                Add(i0 + i, bl * (float)env * 0.42f, br * (float)env * 0.42f);
            }
        }

        /// <summary>Caja de batería: cuerpo tonal corto más ruido.</summary>
        static void Snare(double start)
        {
            int i0 = (int)start, len = (int)(0.24 * SR);
            double phase = 0; float lp = 0, hp = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                phase += 2 * Math.PI * (185 + 60 * Math.Exp(-t * 40)) / SR;
                float n = Noise();
                lp += 0.6f * (n - lp);
                float band = lp - hp; hp += 0.08f * (lp - hp);
                float s = (float)(Math.Sin(phase) * Math.Exp(-t * 28)) * 0.4f + band * (float)Math.Exp(-t * 19) * 0.5f;
                Add(i0 + i, s, s);
            }
        }

        static void Hat(double start, float amp, double decay)
        {
            int i0 = (int)start, len = (int)(decay * 5 * SR);
            float p1 = 0, p2 = 0;
            float pan = (float)(rng.NextDouble() * 0.5 - 0.25);
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                float n = Noise();
                float h1 = n - p1; p1 = n;
                float h2 = h1 - p2; p2 = h1;
                float s = h2 * 0.5f * amp * (float)Math.Exp(-t / decay);
                Add(i0 + i, s * (1f - pan), s * (1f + pan));
            }
        }

        static void Bass(double start, double freq, float amp, double length)
        {
            int i0 = (int)start, len = (int)((length + 0.06) * SR);
            double ph = 0; float lp = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                ph += freq / SR; ph -= Math.Floor(ph);
                float saw = (float)(2 * ph - 1);
                float cutoff = 0.035f + 0.16f * (float)Math.Exp(-t * 14);
                lp += cutoff * (saw - lp);
                float sub = (float)Math.Sin(2 * Math.PI * ph);
                float env = Mathf.Clamp01(i / 90f) * (t < length ? 1f : (float)Math.Exp(-(t - length) * 60));
                float s = (lp * 0.9f + sub * 0.7f) * env * amp * Duck(start + i);
                Add(i0 + i, s, s);
            }
        }

        static void Stab(double start, double[] notes, float amp, double decay, double seconds)
        {
            int i0 = (int)start, len = (int)(seconds * SR);
            int voices = notes.Length * 2;
            var ph = new double[voices];
            for (int v = 0; v < voices; v++) ph[v] = rng.NextDouble();
            float lpL = 0, lpR = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                float l = 0, r = 0;
                for (int v = 0; v < voices; v++)
                {
                    double f = notes[v / 2] * (v % 2 == 0 ? 0.9965 : 1.0035);
                    ph[v] += f / SR; ph[v] -= Math.Floor(ph[v]);
                    float saw = (float)(2 * ph[v] - 1);
                    if (v % 2 == 0) { l += saw; r += saw * 0.35f; } else { r += saw; l += saw * 0.35f; }
                }
                float cutoff = 0.05f + 0.3f * (float)Math.Exp(-t * 11);
                lpL += cutoff * (l - lpL); lpR += cutoff * (r - lpR);
                float fade = Mathf.Clamp01((len - i) / 600f);
                float env = Mathf.Clamp01(i / 130f) * (float)Math.Exp(-t * decay) * fade;
                float g = env * amp * Duck(start + i);
                Add(i0 + i, lpL * g, lpR * g);
            }
        }

        static void Pluck(double start, double freq, float amp, float pan, bool bright = false)
        {
            int i0 = (int)start, len = (int)(0.3 * SR);
            double ph = 0; float lp = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                ph += freq / SR;
                float s;
                if (bright)
                {
                    double frac = ph - Math.Floor(ph);
                    lp += (0.12f + 0.5f * (float)Math.Exp(-t * 18)) * ((float)(2 * frac - 1) - lp);
                    s = lp * 0.8f;
                }
                else s = (float)(Math.Sin(2 * Math.PI * ph) + 0.35 * Math.Sin(4 * Math.PI * ph) * Math.Exp(-t * 20));
                s *= Mathf.Clamp01(i / 60f) * (float)Math.Exp(-t * 13) * amp;
                Add(i0 + i, s * (1f - pan), s * (1f + pan));
            }
        }

        /// <summary>Melodía sostenida: onda cuadrada suave ("pito") o diente de sierra con vibrato (solo).</summary>
        static void Lead(double start, double freq, double length, float amp, bool square)
        {
            int i0 = (int)start, len = (int)((length + 0.08) * SR);
            double ph = 0; float lp = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                double vib = 1 + 0.006 * Math.Sin(2 * Math.PI * 5.5 * t) * Math.Min(1, t * 6);
                ph += freq * vib / SR; ph -= Math.Floor(ph);
                float raw = square ? (ph < 0.5 ? 1f : -1f) : (float)(2 * ph - 1);
                lp += (square ? 0.22f : 0.3f) * (raw - lp);
                float env = Mathf.Clamp01(i / 120f) * (t < length ? 1f : (float)Math.Exp(-(t - length) * 45));
                float s = lp * env * amp * (duck > 0f ? Duck(start + i) * 0.5f + 0.5f : 1f);
                Add(i0 + i, s * 0.85f, s * 1.1f);
                Add(i0 + i + (int)(spb * 0.75), s * 0.3f, s * 0.2f);          // eco
            }
        }

        /// <summary>Guitarra distorsionada: fundamental, quinta y octava saturadas.</summary>
        static void Guitar(double start, double root, double length, float amp)
        {
            int i0 = (int)start, len = (int)((length + 0.05) * SR);
            double[] f = { root * 2, root * 3, root * 4, root * 2 * 1.004 };
            var ph = new double[f.Length];
            float lpL = 0, lpR = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                float x = 0;
                for (int v = 0; v < f.Length; v++) { ph[v] += f[v] / SR; ph[v] -= Math.Floor(ph[v]); x += (float)(2 * ph[v] - 1); }
                float driven = (float)Math.Tanh(x * 2.6f);
                float env = Mathf.Clamp01(i / 80f) * (t < length ? (float)Math.Exp(-t * 2.5) : (float)Math.Exp(-(t - length) * 70 - length * 2.5));
                lpL += 0.28f * (driven - lpL); lpR += 0.22f * (driven - lpR);
                Add(i0 + i, lpL * env * amp, lpR * env * amp);
            }
        }

        static void WriteWav(string path, Style style)
        {
            using (var bw = new BinaryWriter(File.Create(path)))
            {
                int dataBytes = N * 4;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); bw.Write(36 + dataBytes);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); bw.Write(16);
                bw.Write((short)1); bw.Write((short)2); bw.Write(SR); bw.Write(SR * 4); bw.Write((short)4); bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data")); bw.Write(dataBytes);
                for (int i = 0; i < N; i++)
                {
                    bw.Write((short)Mathf.RoundToInt(Mathf.Clamp(L[i], -1f, 1f) * 32767f));
                    bw.Write((short)Mathf.RoundToInt(Mathf.Clamp(R[i], -1f, 1f) * 32767f));
                }
            }
            Debug.Log($"[MixMusicGenerator] Pista {style} generada: {path} ({N / (float)SR:0.0} s, {Tempo(style)} BPM)");
        }
    }
}
