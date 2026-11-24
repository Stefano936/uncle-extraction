using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace UncleExtraction
{
    /// Recorrido fijo de camara para comparar el aspecto antes y despues (solo con -ueCamTour carpeta; sin el argumento no
    /// hace nada). Entra solo a Bar_LastRound, oculta las interfaces y captura las mismas vistas cada vez:
    /// salon, barra, tio, karaoke, carrito, puerta por dentro y por fuera, vereda, camioneta, esquinas y vista general.
    /// Registra "[CamTour] vista archivo" y sale.
    public class CamTour : MonoBehaviour
    {
        string dir;
        (string name, Vector3 pos, Vector3 look, float fov)[] custom;
        static readonly (string name, Vector3 pos, Vector3 look, float fov)[] Views =
        {
            ("salon",            new Vector3(0.5f, 1.7f, 1.2f),  new Vector3(4.5f, 1.0f, 8.5f), 0f),
            ("barra",            new Vector3(2.4f, 1.5f, 5.6f),  new Vector3(5.2f, 0.9f, 8.9f), 0f),
            ("tio",              new Vector3(3.7f, 1.55f, 5.6f), new Vector3(4.5f, 1.0f, 7.4f), 0f),
            ("karaoke",          new Vector3(-3.5f, 1.7f, 4.0f), new Vector3(-7.4f, 0.6f, 8.8f), 0f),
            ("carrito",          new Vector3(4.0f, 1.4f, 0.6f),  new Vector3(5.6f, 0.5f, 2.4f), 0f),
            ("puerta_dentro",    new Vector3(0.6f, 1.6f, 3.4f),  new Vector3(0f, 1.1f, -2f), 0f),
            ("puerta_fuera",     new Vector3(1.8f, 1.6f, -5.2f), new Vector3(0f, 1.2f, -2f), 0f),
            ("vereda",           new Vector3(5.5f, 2.6f, -4.0f), new Vector3(0f, 0.8f, -10f), 0f),
            ("camioneta",        new Vector3(1.6f, 1.6f, -6.4f), new Vector3(0f, 1.0f, -10.5f), 0f),
            ("camioneta_dentro", new Vector3(0.3f, 1.5f, -8.9f), new Vector3(0f, 0.6f, -12.2f), 0f),
            ("esquina",          new Vector3(-6.6f, 1.6f, 0.4f), new Vector3(-9f, 0.4f, -2f), 0f),
            ("techo",            new Vector3(0f, 1.6f, 4.0f),    new Vector3(1f, 4.5f, 9.5f), 0f),
            ("general",          new Vector3(9f, 7f, -10f),      new Vector3(1f, 0f, 1f), 0f),
            ("menu_fondo",       new Vector3(0.6f, 1.45f, 4.4f), new Vector3(3.2f, 1.35f, 8.4f), 0f), // fondo del menu (Art/UI/Backgrounds/bar_night.png)
            ("menu_tio",         new Vector3(5.9f, 1.95f, 9.45f), new Vector3(4.2f, 1.45f, 7.0f), 0f), // sesion 20: el tio de frente desde atras de la barra, el salon detras (fondo del menu)
            ("menu_s21",         new Vector3(6.0f, 1.75f, 9.3f), new Vector3(5.4f, 1.35f, 6.4f), 58f), // sesion 21: tio a la derecha bajo SALIDA, carrito entre el y los botones (Art/UI/Backgrounds/bar_night.png)
        };

        /// -ueCamViews "nombre:px,py,pz:lx,ly,lz[:fov];..." reemplaza el recorrido por esas vistas (para buscar encuadres
        /// sin recompilar, p. ej. el fondo del menu).
        static (string, Vector3, Vector3, float)[] ParseViews(string s)
        {
            var list = new System.Collections.Generic.List<(string, Vector3, Vector3, float)>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            Vector3 V(string t) { var p = t.Split(','); return new Vector3(float.Parse(p[0], inv), float.Parse(p[1], inv), float.Parse(p[2], inv)); }
            foreach (var item in s.Split(';'))
            {
                var f = item.Trim().Split(':'); if (f.Length < 3) continue;
                list.Add((f[0], V(f[1]), V(f[2]), f.Length > 3 ? float.Parse(f[3], inv) : 0f));
            }
            return list.ToArray();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var a = QaArgs.All;
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == "-ueCamTour")
                {
                    var go = new GameObject("CamTour"); DontDestroyOnLoad(go);
                    var tour = go.AddComponent<CamTour>(); tour.dir = Path.GetFullPath(a[i + 1]);
                    for (int j = 0; j < a.Length - 1; j++) if (a[j] == "-ueCamViews") tour.custom = ParseViews(a[j + 1]);
                    if (FindAnyObjectByType<FrontendMenu>()) UnityEngine.SceneManagement.SceneManager.LoadScene(FrontendMenu.GameScene);
                    return;
                }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            while (LevelLayout.Current == null || PlayerControls.Local == null) yield return null;
            float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 2.5f) yield return null;
            // se usa la camara del jugador (con su posproceso), separada del cuerpo
            var cam = Camera.main; if (!cam) yield break;
            var camGo = cam.gameObject; camGo.transform.SetParent(null, true);
            var motor = FindAnyObjectByType<PlayerMotor>(); if (motor) motor.enabled = false;
            cam.fieldOfView = 70f;
            foreach (var r in camGo.GetComponentsInChildren<Renderer>()) r.enabled = false; // manos del jugador
            foreach (var pm in FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None)) foreach (var r in pm.GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled = false;
            var hud = FindAnyObjectByType<LabDebugHud>(); if (hud) hud.enabled = false;
            foreach (var v in custom ?? Views)
            {
                cam.fieldOfView = v.fov > 0f ? v.fov : 70f;
                camGo.transform.position = v.pos; camGo.transform.rotation = Quaternion.LookRotation(v.look - v.pos);
                for (int f = 0; f < 6; f++) yield return null;
                yield return new WaitForEndOfFrame();
                string file = Path.Combine(dir, v.name + ".png");
                ScreenCapture.CaptureScreenshot(file);
                Debug.Log($"[CamTour] {v.name} {file}");
                yield return null; yield return null;
            }
            yield return null;
            Application.Quit();
        }
    }
}
