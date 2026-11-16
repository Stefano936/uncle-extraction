using System;
using System.Collections;
using UnityEngine;

namespace UncleExtraction
{
    /// Capturas de pantalla para verificar la presentacion en la build: -ueShot archivo.png@segundos (se puede repetir).
    /// Los segundos cuentan desde la carga de la escena de juego. Solo pruebas; no hace nada sin el argumento.
    public class ShotOnArgs : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == "-ueShot")
                {
                    var go = new GameObject("ShotOnArgs"); DontDestroyOnLoad(go); go.AddComponent<ShotOnArgs>();
                    return;
                }
        }

        IEnumerator Start()
        {
            var a = Environment.GetCommandLineArgs();
            // esperar a la escena de juego (el menu la carga primero)
            while (LevelLayout.Current == null) yield return null;
            float t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] != "-ueShot") continue;
                var parts = a[i + 1].Split('@');
                float at = parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 3f;
                while (Time.realtimeSinceStartup - t0 < at) yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(parts[0]);
                Debug.Log($"[Shot] {parts[0]} a los {at:0.0} s");
            }
        }
    }
}
