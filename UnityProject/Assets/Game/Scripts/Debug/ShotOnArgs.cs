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
    /// -ueFaceUncle s: a los s segundos pone al jugador local de frente al tio, a 1,3 m, mirando su cara (capturas de la
    ///   expresion y de las manos; solo pruebas).
    public class ShotOnArgs : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-ueShot" || a == "-ueShotMenu" || a == "-ueForceAttack" || a == "-ueFaceUncle")
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
                if (a[i] == "-ueFaceUncle") StartCoroutine(FaceUncle(Parse("x@" + a[i + 1], 3f).at));
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

        IEnumerator FaceUncle(float at)
        {
            yield return WaitGame(at);
            var npc = NpcBrain.Instance; var pc = PlayerControls.Local; var body = npc ? npc.GetComponent<NpcBody>() : null;
            if (!npc || !pc || !body) { Debug.Log("[Shot] no se pudo poner al jugador frente al tio"); yield break; }
            var motor = pc.GetComponent<PlayerMotor>(); var rb = pc.GetComponent<Rigidbody>();
            for (int k = 0; k < 20; k++) // unos cuadros: el tio puede moverse un poco
            {
                Vector3 head = body.head ? body.head.position : npc.transform.position + Vector3.up * 1.7f;
                // el lugar libre mas de frente posible (el tio suele mirar la barra): circulo de 1,2 m, de 0° a ±100°
                Vector3 at3 = npc.transform.position - body.Forward * 1.3f; Vector3 c0 = npc.transform.position; c0.y = 0f;
                for (int d = 0; d <= 100; d += 10)
                {
                    bool found = false;
                    foreach (int sgn in new[] { 1, -1 })
                    {
                        Vector3 dir = Quaternion.Euler(0, d * sgn, 0) * body.Forward, p = c0 + dir * 1.2f;
                        if (!Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.7f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) { at3 = p; found = true; break; }
                    }
                    if (found) break;
                }
                if (rb) { rb.position = at3; rb.linearVelocity = Vector3.zero; } pc.transform.position = at3;
                Vector3 to = head - (at3 + Vector3.up * 1.62f), f = new Vector3(to.x, 0, to.z);
                motor.SetYawPitch(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, f.magnitude) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate();
            }
            Debug.Log("[Shot] jugador frente al tio");
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
