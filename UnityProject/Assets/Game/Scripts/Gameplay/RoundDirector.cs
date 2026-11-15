using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    public enum RoundState { Running, Success, Timeout }

    /// Reloj, exito, derrota y reinicio limpio (especificacion 02). En PhysicsLab el "anfitrion" es la instancia local.
    public class RoundDirector : MonoBehaviour
    {
        public static RoundDirector Instance { get; private set; }
        public Difficulty difficulty = Difficulty.Normal;
        [Range(1, 4)] public int partySize = 1;

        public RoundState State { get; private set; } = RoundState.Running;
        public float Elapsed { get; private set; }
        public float TimeLimit => LabBalance.RoundTime(partySize, difficulty);
        public float Resistance => LabBalance.Resistance(partySize, difficulty);
        public float AttackInterval => LabBalance.AttackInterval(partySize, difficulty);
        public int Restarts { get; private set; }
        public string LastStressReport { get; private set; } = "";
        public bool StressRunning { get; private set; }

        void Awake() { Instance = this; Time.fixedDeltaTime = 1f / 60f; Physics.gravity = new Vector3(0, -9.81f, 0); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (State == RoundState.Running)
            {
                Elapsed += Time.deltaTime;
                if (TimeLimit > 0f && Elapsed >= TimeLimit) { State = RoundState.Timeout; GrabRegistry.ReleaseEverything(ReleaseReason.Reset); Debug.Log("[Round] TIMEOUT"); }
            }
            if (LabInput.Pressed(Act.Restart)) Restart();
        }

        public void OnExtracted() { if (State == RoundState.Running) { State = RoundState.Success; Debug.Log($"[Round] SUCCESS in {Elapsed:0.0}s"); } }

        public void Restart()
        {
            GrabRegistry.ReleaseEverything(ReleaseReason.Reset);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is IResettable r) r.ResetState();
            State = RoundState.Running; Elapsed = 0f; Restarts++;
            Debug.Log($"[Round] restart #{Restarts} grabs={GrabRegistry.Count} joints={CountGameplayJoints()}");
        }

        /// Uniones creadas por juego (anclaje, correa). Excluye la bisagra de la puerta.
        public static int CountGameplayJoints()
        {
            int n = 0;
            foreach (var j in FindObjectsByType<ConfigurableJoint>(FindObjectsSortMode.None)) n++;
            return n;
        }

        public void RunResetStress(int times) { if (!StressRunning) StartCoroutine(ResetStress(times)); }

        IEnumerator ResetStress(int times)
        {
            StressRunning = true;
            var rig = FindAnyObjectByType<LabForceRig>();
            int failures = 0; var notes = new List<string>();
            for (int i = 0; i < times; i++)
            {
                if (rig) rig.SetVirtualPullers(4);
                if (TransportCart.Instance) { /* sin efecto si el tio no esta en la caja */ TransportCart.Instance.Strap(); }
                yield return new WaitForSeconds(0.6f);
                int before = GrabRegistry.Count;
                if (rig) rig.SetVirtualPullers(0, false);
                Restart();
                yield return new WaitForFixedUpdate();
                int grabs = GrabRegistry.Count, joints = CountGameplayJoints();
                if (grabs != 0 || joints != 0) { failures++; notes.Add($"#{i + 1}: grabs={grabs} joints={joints}"); }
                Debug.Log($"[ResetStress] {i + 1}/{times} grabsBefore={before} after={grabs} joints={joints}");
            }
            LastStressReport = failures == 0 ? $"OK: {times} reinicios, 0 agarres/uniones huerfanos" : $"FALLO: {string.Join("; ", notes)}";
            Debug.Log($"[ResetStress] {LastStressReport}");
            StressRunning = false;
        }
    }
}
