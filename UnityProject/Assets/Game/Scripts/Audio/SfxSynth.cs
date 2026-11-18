using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Efectos de sonido sintetizados por el juego (procedencia propia, sin archivos de terceros; CREDITS_ASSETS.md).
    /// Son provisionales pero funcionales: cada ataque tiene una senal distinta en ritmo y altura (no solo volumen),
    /// para reconocerla sin mirar (biblia 03, seccion 08: «las senales de peligro tambien usan sonido»).
    /// Deterministas (semilla fija): el mismo id da siempre la misma forma de onda.
    public static class SfxSynth
    {
        public const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        public static readonly string[] Ids = { "grab", "release", "impact", "whoosh", "cue_slap", "cue_push", "cue_kick", "cue_shake", "van_close", "door_creak", "success", "timeout", "ui", "ping", "grab_try", "slip", "strain", "cart_roll", "cart_bump", "latch", "ambience", "win_sting", "clink", "glass_crash", "bell" };

        public static AudioClip Get(string id)
        {
            if (cache.TryGetValue(id, out var c) && c) return c;
            float[] d = Make(id);
            float peak = 0f; foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0.9f) for (int i = 0; i < d.Length; i++) d[i] *= 0.9f / peak; // sin saturar (el impacto sumaba 1,13)
            c = AudioClip.Create("sfx_" + id, d.Length, 1, Rate, false); c.SetData(d, 0);
            cache[id] = c; return c;
        }

        static float[] Buf(float seconds) => new float[Mathf.Max(1, (int)(seconds * Rate))];
        static float Env(float t, float a, float dur) => t < a ? t / a : Mathf.Exp(-5f * (t - a) / Mathf.Max(0.01f, dur - a));

        static float[] Make(string id)
        {
            var rng = new System.Random(id.GetHashCode());
            float N() => (float)(rng.NextDouble() * 2.0 - 1.0);
            float[] b; float lp = 0f;
            switch (id)
            {
                case "grab":   // tela que se tensa: ruido filtrado corto
                    b = Buf(0.12f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; lp += 0.25f * (N() - lp); b[i] = lp * Env(t, 0.01f, 0.12f) * 0.8f; } break;
                case "release": // soltar: chasquido mas agudo
                    b = Buf(0.08f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; lp += 0.6f * (N() - lp); b[i] = lp * Env(t, 0.003f, 0.08f) * 0.5f; } break;
                case "impact": // golpe seco exagerado (sin sonido de lesion): seno grave + ruido
                    b = Buf(0.35f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float f = Mathf.Lerp(120f, 55f, t / 0.35f); b[i] = (Mathf.Sin(2 * Mathf.PI * f * t) * 0.9f + N() * 0.35f * Mathf.Exp(-30f * t)) * Env(t, 0.004f, 0.35f); } break;
                case "whoosh": // golpe lanzado: barrido de ruido
                    b = Buf(0.25f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float k = Mathf.Lerp(0.05f, 0.5f, t / 0.25f); lp += k * (N() - lp); b[i] = lp * Mathf.Sin(Mathf.PI * t / 0.25f) * 0.7f; } break;
                case "cue_slap": b = Tones(new[] { 880f, 1320f }, 0.09f, 0.04f); break;              // dos notas agudas rapidas
                case "cue_push": b = Sweep(180f, 360f, 0.45f); break;                                  // subida grave larga
                case "cue_kick": b = Tones(new[] { 440f, 330f, 440f }, 0.1f, 0.03f); break;            // tres notas medias
                case "cue_shake": b = Tremolo(520f, 14f, 0.5f); break;                                 // vibrato rapido
                case "van_close": b = Buf(0.5f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; b[i] = (Mathf.Sin(2 * Mathf.PI * 70f * t) + 0.4f * N() * Mathf.Exp(-40f * t)) * Env(t, 0.003f, 0.5f) * 0.9f; } break;
                case "door_creak": b = Buf(0.4f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float f = 300f + 60f * Mathf.Sin(t * 30f); b[i] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * 0.12f * Mathf.Sin(Mathf.PI * t / 0.4f); } break;
                case "ping": b = Tones(new[] { 988f, 1318f }, 0.07f, 0.02f); break;                       // marcador de un compañero
                // ---- sesion 15: sensacion del agarre, del carrito y del cierre; ambiente de bar ----
                case "grab_try": b = Buf(0.09f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; lp += 0.25f * (N() - lp); b[i] = lp * Mathf.Sin(Mathf.PI * t / 0.09f) * 0.5f; } break; // roce de tela
                case "slip": { b = Buf(0.22f); float ph = 0f; for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; ph += 2 * Mathf.PI * Mathf.Lerp(1100f, 420f, t / 0.22f) / Rate; lp += 0.4f * (N() - lp); b[i] = (Mathf.Sin(ph) * 0.35f + lp * 0.25f) * Env(t, 0.004f, 0.22f); } break; } // se resbala
                case "strain": { b = Buf(0.55f); float ph = 0f; for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float f = 105f + 14f * Mathf.Sin(t * 23f); ph += 2 * Mathf.PI * f / Rate; float saw = (ph / (2 * Mathf.PI)) % 1f * 2f - 1f; b[i] = saw * 0.22f * Mathf.Sin(Mathf.PI * t / 0.55f) * (0.7f + 0.3f * Mathf.Sin(t * 61f)); } break; } // tela que cruje
                case "cart_roll": { b = Buf(1.0f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float tick = Mathf.Exp(-60f * ((t * 17f) % 1f) / 17f); lp += 0.08f * (N() - lp); b[i] = lp * 0.5f + N() * 0.12f * tick + Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.08f; } break; } // ruedas y traqueteo (bucle)
                case "cart_bump": b = Buf(0.28f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; b[i] = (Mathf.Sin(2 * Mathf.PI * 85f * t) * 0.7f + N() * 0.35f * Mathf.Exp(-70f * t) + Mathf.Sin(2 * Mathf.PI * 1900f * t) * 0.08f * Mathf.Exp(-40f * t)) * Env(t, 0.002f, 0.28f); } break; // golpe contra el marco
                case "latch": b = Buf(0.42f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float c1 = Mathf.Exp(-90f * t), c2 = t > 0.07f ? Mathf.Exp(-110f * (t - 0.07f)) : 0f; b[i] = (Mathf.Sin(2 * Mathf.PI * 2400f * t) * 0.4f + N() * 0.3f) * (c1 + c2) + Mathf.Sin(2 * Mathf.PI * 70f * t) * 0.6f * Env(t, 0.003f, 0.42f); } break; // pestillo: clac-clac y golpe
                case "ambience": { b = Buf(8f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; lp += 0.02f * (N() - lp); float mur = lp * (0.6f + 0.4f * Mathf.Sin(t * 1.3f) * Mathf.Sin(t * 0.7f + 1f)); b[i] = mur * 1.6f; }
                    int[] clinks = { 21000, 97000, 151000, 230000, 301000 }; foreach (int c0 in clinks) for (int k = 0; k < 9000 && c0 + k < b.Length; k++) { float t = k / (float)Rate; b[c0 + k] += Mathf.Sin(2 * Mathf.PI * (2600f + c0 % 700) * t) * 0.12f * Mathf.Exp(-30f * t); }
                    for (int k = 0; k < 2000; k++) { float w = k / 2000f; b[k] *= w; b[b.Length - 1 - k] *= w; } break; } // murmullo y vasos (bucle, extremos suavizados)
                case "win_sting": { b = Buf(1.1f); float[] f = { 392f, 494f, 587f, 784f }; for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float v = 0f; for (int k = 0; k < f.Length; k++) { float on = t - k * 0.06f; if (on > 0f) { float ph = 2 * Mathf.PI * f[k] * on; v += (Mathf.Sin(ph) + 0.35f * Mathf.Sin(2 * ph) + 0.15f * Mathf.Sin(3 * ph)) * Env(on, 0.01f, 1.0f) * 0.22f; } } b[i] = v; } break; } // remate breve
                case "clink": b = Buf(0.3f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; b[i] = (Mathf.Sin(2 * Mathf.PI * 3100f * t) * 0.4f + Mathf.Sin(2 * Mathf.PI * 4650f * t) * 0.2f) * Mathf.Exp(-22f * t); } break;
                case "glass_crash": b = Buf(0.9f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float grit = N() * Mathf.Exp(-6f * t) * 0.5f; float ping = Mathf.Sin(2 * Mathf.PI * (2800f + 900f * Mathf.Sin(t * 40f)) * t) * 0.2f * Mathf.Exp(-9f * t); b[i] = grit + ping; } break;
                case "bell": b = Buf(1.6f); for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; float e = Mathf.Exp(-2.6f * t); b[i] = (Mathf.Sin(2 * Mathf.PI * 1760f * t) * 0.32f + Mathf.Sin(2 * Mathf.PI * 2640f * t) * 0.18f * Mathf.Exp(-4f * t) + Mathf.Sin(2 * Mathf.PI * 4230f * t) * 0.1f * Mathf.Exp(-7f * t)) * e + N() * 0.2f * Mathf.Exp(-160f * t); } break; // campana de mostrador (sesion 17)
                case "success": b = Tones(new[] { 523f, 659f, 784f, 1046f }, 0.14f, 0.02f); break;
                case "timeout": b = Tones(new[] { 392f, 311f, 233f }, 0.22f, 0.04f); break;
                default: b = Tones(new[] { 1000f }, 0.03f, 0f); break;                               // ui
            }
            return b;
        }

        static float[] Tones(float[] f, float each, float gap)
        {
            var b = Buf(f.Length * (each + gap));
            for (int n = 0; n < f.Length; n++)
            {
                int s = (int)(n * (each + gap) * Rate);
                for (int i = 0; i < (int)(each * Rate) && s + i < b.Length; i++) { float t = i / (float)Rate; b[s + i] = (Mathf.Sin(2 * Mathf.PI * f[n] * t) * 0.6f + Mathf.Sin(4 * Mathf.PI * f[n] * t) * 0.15f) * Env(t, 0.005f, each); }
            }
            return b;
        }
        static float[] Sweep(float f0, float f1, float dur)
        {
            var b = Buf(dur); float ph = 0f;
            for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; ph += 2 * Mathf.PI * Mathf.Lerp(f0, f1, t / dur) / Rate; b[i] = Mathf.Sin(ph) * 0.6f * Mathf.Min(1f, t / 0.05f) * Mathf.Min(1f, (dur - t) / 0.05f); }
            return b;
        }
        static float[] Tremolo(float f, float rate, float dur)
        {
            var b = Buf(dur);
            for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; b[i] = Mathf.Sin(2 * Mathf.PI * f * t) * 0.55f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * rate * t)) * Mathf.Min(1f, (dur - t) / 0.05f); }
            return b;
        }
    }
}
