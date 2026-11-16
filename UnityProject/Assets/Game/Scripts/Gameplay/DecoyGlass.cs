using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Vaso señuelo (GDD 09; balance.distraction): un punto de interés, no un efecto de «dormir».
    /// Se lleva con una mano (socket "glass") y atrae al tío si está APOYADO (quieto, sin que nadie lo sostenga) y a la vista.
    [RequireComponent(typeof(Rigidbody))]
    public class DecoyGlass : MonoBehaviour, IResettable
    {
        public static readonly List<DecoyGlass> All = new List<DecoyGlass>();
        public float maxSeeDistance = 7f;

        Rigidbody rb; Vector3 spawnPos; Quaternion spawnRot; GrabSocket socket;
        float stillSince;

        public Vector3 Position => transform.position;
        /// Colocado por un jugador (GDD 06: «colocar una distraccion valida»): el vaso en su lugar de origen no atrae.
        public bool Placed { get; private set; }
        /// Apoyado: colocado, no lo sostiene nadie y lleva 0,4 s casi quieto.
        public bool Resting => Placed && (socket == null || GrabRegistry.OccupancyOf(socket) == 0) && Time.time - stillSince > 0.4f;
        /// Para pruebas y depuracion: marcarlo como colocado (equivale a que un jugador lo haya llevado y soltado).
        public void MarkPlaced() { Placed = true; stillSince = Time.time; }

        void Awake() { rb = GetComponent<Rigidbody>(); socket = GetComponentInChildren<GrabSocket>(); spawnPos = transform.position; spawnRot = transform.rotation; }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void FixedUpdate()
        {
            bool held = socket != null && GrabRegistry.OccupancyOf(socket) > 0;
            if (held) Placed = true;
            if (held || rb.linearVelocity.sqrMagnitude > 0.04f) stillSince = Time.time;
        }

        static readonly RaycastHit[] hits = new RaycastHit[12];
        /// Visible desde los ojos del tío: ninguna geometría estática en el medio (personas y objetos no tapan).
        public bool VisibleFrom(Vector3 eye)
        {
            Vector3 to = Position - eye; float d = to.magnitude;
            if (d > maxSeeDistance || d < 0.01f) return false;
            int n = Physics.RaycastNonAlloc(eye, to / d, hits, d - 0.05f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (hits[i].collider.attachedRigidbody == null) return false;
            return true;
        }

        public void ResetState()
        {
            if (!NetAuth.IsServer) return;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos; rb.rotation = spawnRot; transform.SetPositionAndRotation(spawnPos, spawnRot);
            stillSince = Time.time; Placed = false;
        }
    }
}
