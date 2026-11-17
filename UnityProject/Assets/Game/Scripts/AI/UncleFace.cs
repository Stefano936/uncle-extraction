using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction
{
    /// Expresión del tío (sesión 13). La malla del kit trae cejas y boca fundidas en una sola pieza, así que se superponen
    /// cejas y boca propias ancladas a la cabeza (siguen al hueso y a la física): enojado al preparar o lanzar un golpe,
    /// cejas arriba y boca abierta al recuperarse o caer, gesto caído de borracho el resto del tiempo. Solo presentación,
    /// en todos los pares (lee el estado replicado). Posiciones del generador del kit, en coordenadas del tío (frente +z).
    public class UncleFace : MonoBehaviour
    {
        NpcBrain brain; NpcBody body; Transform anchor;
        Transform browL, browR, mouth;
        Vector3 browLPos, browRPos, mouthPos; Quaternion faceRot;
        float angry, surprised;
        public float Angry => angry;
        public float Surprised => surprised;

        void Start()
        {
            brain = GetComponent<NpcBrain>(); body = GetComponent<NpcBody>();
            anchor = body && body.head ? body.head.parent : null; // parte de la cabeza (o el cuerpo si es el tio central)
            if (!anchor) { enabled = false; return; }
            var dark = FaceMaterial();
            // offsets calculados en reposo respecto de la cabeza: despues siguen al hueso
            float fs = body.FaceSign; // la cara mira hacia -z de la raiz en el tio articulado (ver NpcBody.Forward)
            Vector3 L(Vector3 localUncle) => anchor.InverseTransformPoint(transform.TransformPoint(new Vector3(localUncle.x * fs, localUncle.y, localUncle.z * fs)));
            faceRot = Quaternion.Inverse(anchor.rotation) * transform.rotation * (fs < 0f ? Quaternion.Euler(0, 180f, 0) : Quaternion.identity);
            browLPos = L(new Vector3(-0.09f, 1.785f, 0.252f)); browRPos = L(new Vector3(0.09f, 1.785f, 0.252f)); mouthPos = L(new Vector3(0f, 1.552f, 0.232f));
            browL = Part(PrimitiveType.Cube, "Brow_L", new Vector3(0.15f, 0.042f, 0.03f), dark);
            browR = Part(PrimitiveType.Cube, "Brow_R", new Vector3(0.15f, 0.042f, 0.03f), dark);
            mouth = Part(PrimitiveType.Sphere, "Mouth_Open", new Vector3(0.1f, 0.06f, 0.03f), dark);
            if (RoundDirector.DiagnosticKeys || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueFaceUncle") >= 0)
            {
                Transform nose = null; foreach (var t in GetComponentsInChildren<Transform>()) if (t.name.ToLowerInvariant().Contains("nose")) nose = t;
                Debug.Log($"[Face] raiz={transform.position} adelante={transform.forward} ancla={anchor.name}@{anchor.position} ceja_der={browR.parent.TransformPoint(browRPos)} hijos_cabeza={anchor.childCount}");
            }
        }

        Material FaceMaterial()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials) if (m && m.name.StartsWith("Uncle_Hair")) return m;
            var sh = Shader.Find("Universal Render Pipeline/Lit"); var mat = new Material(sh ? sh : Shader.Find("Standard")); mat.color = new Color(0.1f, 0.07f, 0.05f); return mat;
        }

        Transform Part(PrimitiveType type, string name, Vector3 size, Material m)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name;
            var c = g.GetComponent<Collider>(); if (c) { c.enabled = false; Destroy(c); }
            g.transform.SetParent(anchor, false); g.transform.localScale = new Vector3(size.x / anchor.lossyScale.x, size.y / anchor.lossyScale.y, size.z / anchor.lossyScale.z);
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
            return g.transform;
        }

        void LateUpdate()
        {
            if (!anchor || !brain) return;
            var st = brain.State;
            float a = st == NpcState.Windup || st == NpcState.Attack || st == NpcState.Struggle || st == NpcState.Protest ? 1f : 0f;
            float s = st == NpcState.Recover || st == NpcState.ForcedRecovery || st == NpcState.Fallen || st == NpcState.Distracted ? 1f : 0f;
            angry = Mathf.Lerp(angry, a, Smooth.Factor(0.2f)); surprised = Mathf.Lerp(surprised, s, Smooth.Factor(0.2f));
            // ceja: borracho = puntas externas caidas (-8°); enojado = puntas internas abajo (+24°); sorprendido = arriba y recta
            float tilt = Mathf.Lerp(-8f, 24f, angry) * (1f - surprised);
            float lift = 0.022f * surprised - 0.008f * angry;
            Vector3 up = faceRot * Vector3.up * lift;
            Place(browR, browRPos + up, tilt);
            Place(browL, browLPos + up, -tilt);
            float open = Mathf.Max(angry * 0.8f, surprised);
            mouth.gameObject.SetActive(open > 0.05f);
            if (open > 0.05f) { mouth.localPosition = mouthPos; mouth.localRotation = faceRot; mouth.localScale = Vector3.Scale(new Vector3(0.09f + 0.02f * angry, 0.025f + 0.05f * open, 0.03f), Inv(anchor.lossyScale)); }
        }

        void Place(Transform t, Vector3 localPos, float rollDeg)
        {
            t.localPosition = localPos;
            t.localRotation = faceRot * Quaternion.Euler(0f, 0f, rollDeg);
        }
        static Vector3 Inv(Vector3 v) => new Vector3(1f / Mathf.Max(1e-4f, v.x), 1f / Mathf.Max(1e-4f, v.y), 1f / Mathf.Max(1e-4f, v.z));
    }
}
