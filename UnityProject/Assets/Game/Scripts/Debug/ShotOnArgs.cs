using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace UncleExtraction
{
    /// Verificacion visual en la build (solo pruebas; sin argumentos no hace nada):
    /// -ueShot archivo.png@s (repetible): captura a los s segundos de cargada la escena de juego;
    /// -ueShotMenu archivo.png@s: captura del menu (sin escena de juego);
    /// -ueForceAttack slap|push|kick|shake@s (repetible, anfitrion): fuerza ese ataque a los s segundos, para capturar
    ///   la preparacion. No cambia reglas: es lo mismo que el boton «Forzar ataque» del panel de depuracion.
    public class ShotOnArgs : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-ueShot" || a == "-ueShotMenu" || a == "-ueForceAttack")
                {
                    var go = new GameObject("ShotOnArgs"); DontDestroyOnLoad(go); go.AddComponent<ShotOnArgs>();
                    return;
                }
        }

        static (string what, float at) Parse(string s, float def)
        {
            var parts = s.Split('@');
            float at = parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
            return (parts[0], at);
        }

        IEnumerator Start()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ueShotMenu") StartCoroutine(Capture(Parse(a[i + 1], 2f), false));
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-ueShot") StartCoroutine(Capture(Parse(a[i + 1], 3f), true));
                if (a[i] == "-ueForceAttack") StartCoroutine(Force(Parse(a[i + 1], 5f)));
            }
            yield break;
        }

        static IEnumerator WaitGame(float at)
        {
            while (LevelLayout.Current == null) yield return null; // el menu carga primero la escena de juego
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < at) yield return null;
        }

        IEnumerator Capture((string file, float at) p, bool game)
        {
            if (game) yield return WaitGame(p.at);
            else { float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < p.at) yield return null; }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(p.file);
            Debug.Log($"[Shot] {p.file} a los {p.at:0.0} s");
        }

        IEnumerator Force((string id, float at) p)
        {
            yield return WaitGame(p.at);
            var npc = NpcBrain.Instance;
            if (!NetAuth.IsServer || !npc || !Enum.TryParse(p.id, true, out AttackId id)) { Debug.Log($"[Shot] no se pudo forzar '{p.id}'"); yield break; }
            Debug.Log($"[Shot] fuerza {id} a los {p.at:0.0} s: {npc.ForceAttack(id)}");
        }
    }
}
