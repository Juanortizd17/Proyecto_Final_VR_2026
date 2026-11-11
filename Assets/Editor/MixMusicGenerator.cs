using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Sintetiza la pista house del bar (bucle perfecto de 16 compases a 124 BPM) y la guarda
    /// como WAV. Es música original generada por código: sin licencias de terceros.
    /// Menú: Tools > Mix Lab > Regenerate Music.
    /// </summary>
    public static class MixMusicGenerator
    {
        public const string ClipPath = "Assets/Audio/Mix/house_loop.wav";
        public const float Bpm = 124f;
        const int SR = 44100;
        const int Bars = 16;

        static float[] L, R;
        static int N;
        static double spb;      // muestras por negra
        static System.Random rng;

        [MenuItem("Tools/Mix Lab/Regenerate Music")]
        public static void Regenerate() => Generate(true);

        public static AudioClip EnsureClip() => Generate(false);

        static AudioClip Generate(bool force)
        {
            if (force || !File.Exists(ClipPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ClipPath));
                Synthesize();
                WriteWav(ClipPath);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceUpdate);
            }
            var importer = AssetImporter.GetAtPath(ClipPath) as AudioImporter;
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
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        }

        // ---------------------------------------------------------------------

        // Progresión: La menor 7 – Fa mayor 7 – Do mayor 7 – Sol 6 (dos compases cada uno)
        static readonly double[][] Chords =
        {
            new[] { 220.00, 261.63, 329.63, 392.00 },
            new[] { 174.61, 220.00, 261.63, 329.63 },
            new[] { 196.00, 246.94, 261.63, 329.63 },
            new[] { 196.00, 246.94, 293.66, 329.63 },
        };
        static readonly double[] Roots = { 55.00, 87.31, 65.41, 98.00 };
        static readonly int[] StabsA = { 0, 3, 6, 10, 13 };
        static readonly int[] StabsB = { 2, 6, 10, 14 };
        static readonly int[] ArpOrder = { 0, 2, 1, 3, 2, 0, 3, 1 };
        static readonly double[] ClapTaps = { 0.0, 0.011, 0.022 };

        static void Synthesize()
        {
            spb = SR * 60.0 / Bpm;
            N = (int)Math.Round(spb * 4 * Bars);
            L = new float[N]; R = new float[N];
            rng = new System.Random(2026);

            for (int bar = 0; bar < Bars; bar++)
            {
                int chord = (bar / 2) % 4;
                bool second = bar >= Bars / 2;
                bool lastOfPhrase = bar % 8 == 7;
                for (int beat = 0; beat < 4; beat++)
                {
                    double t0 = (bar * 4 + beat) * spb;
                    Kick(t0);
                    if (beat == 1 || beat == 3) Clap(t0);
                    Hat(t0 + spb * 0.5, 0.2f, 0.075);                         // contratiempo abierto
                    for (int s = 0; s < 4; s++)
                    {
                        if (s == 2) continue;
                        Hat(t0 + spb * 0.25 * s, s == 0 ? 0.05f : 0.075f, 0.018);
                    }
                    if (lastOfPhrase && beat == 3) { Clap(t0 + spb * 0.5); Clap(t0 + spb * 0.75); }
                }
                for (int step = 0; step < 16; step++)
                {
                    double t = (bar * 16 + step) * spb * 0.25;
                    if (step % 4 == 2) Bass(t, Roots[chord], 0.42f, 0.22);
                    if (step == 15 && bar % 2 == 1) Bass(t, Roots[chord] * 2, 0.3f, 0.1);
                    if (step == 7 && bar % 4 == 2) Bass(t, Roots[chord] * 1.5, 0.28f, 0.1);
                    if (Array.IndexOf(bar % 2 == 0 ? StabsA : StabsB, step) >= 0) Stab(t, Chords[chord]);
                    if (second && (step % 2 == 0 || step % 8 == 3))
                    {
                        // Arpegio con eco en la segunda mitad
                        double f = Chords[chord][ArpOrder[(step + bar) % 8]] * 2;
                        Pluck(t, f, 0.075f, -0.4f);
                        Pluck(t + spb * 0.75, f, 0.04f, 0.5f);
                        Pluck(t + spb * 1.5, f, 0.02f, -0.6f);
                    }
                }
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

        static void Add(int i, float l, float r)
        {
            i %= N; if (i < 0) i += N;
            L[i] += l; R[i] += r;
        }

        /// <summary>Compresión lateral (el "bombeo" del house): baja el volumen justo después de cada bombo.</summary>
        static float Duck(double sample)
        {
            double tb = (sample % spb) / SR;
            return 1f - 0.75f * (float)Math.Exp(-tb * 11.0);
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

        static void Stab(double start, double[] notes)
        {
            int i0 = (int)start, len = (int)(0.42 * SR);
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
                float env = Mathf.Clamp01(i / 130f) * (float)Math.Exp(-t * 8.5);
                float g = env * 0.075f * Duck(start + i);
                Add(i0 + i, lpL * g, lpR * g);
            }
        }

        static void Pluck(double start, double freq, float amp, float pan)
        {
            int i0 = (int)start, len = (int)(0.3 * SR);
            double ph = 0;
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)SR;
                ph += freq / SR;
                float s = (float)(Math.Sin(2 * Math.PI * ph) + 0.35 * Math.Sin(4 * Math.PI * ph) * Math.Exp(-t * 20));
                s *= Mathf.Clamp01(i / 60f) * (float)Math.Exp(-t * 13) * amp;
                Add(i0 + i, s * (1f - pan), s * (1f + pan));
            }
        }

        static void WriteWav(string path)
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
            Debug.Log($"[MixMusicGenerator] Pista house generada: {path} ({N / (float)SR:0.0} s, {Bpm} BPM)");
        }
    }
}
