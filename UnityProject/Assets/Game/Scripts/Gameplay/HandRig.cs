using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction
{
    /// Mano visible del jugador (sesion 13; reemplaza el cubo). Se arma en tiempo de ejecucion sobre el objeto Hand_L/Hand_R
    /// que ya existe en el prefab: palma, cuatro dedos y pulgar con piel, y el puño de la manga que lleva el color del
    /// estado (libre, agarrando, tensión). Los dedos se cierran al agarrar, quedan a medias mientras el agarre está pendiente
    /// y se abren al soltar; el suavizado no depende de los FPS. Solo presentación: no tiene colisión ni afecta la física.
    public class HandRig : MonoBehaviour
    {
        public static readonly Color Skin = new Color(0.86f, 0.66f, 0.52f);
        Transform[] fingers; Transform thumb; Renderer cuff; Transform root;
        float curl, curlTarget, pulseAt = -9f;
        /// Respuesta inmediata al clic (sesion 15): apreton y empujon cortos, locales, antes de la confirmacion del anfitrion.
        public void Pulse() { pulseAt = Time.time; }
        public const float PulseTime = 0.14f;
        public Renderer Cuff => cuff;
        public float Curl => curl;

        /// Construye la mano dentro de 'hand' (el cubo original queda oculto). hand: 0 = izquierda, 1 = derecha.
        public static HandRig Build(Transform hand, int side)
        {
            var old = hand.GetComponent<Renderer>();
            Material baseMat = old ? old.sharedMaterial : null;
            if (old) old.enabled = false;
            var go = new GameObject("HandRig"); go.transform.SetParent(hand, false);
            var ps = hand.localScale;
            // compensa la escala no uniforme del cubo para trabajar en metros
            go.transform.localScale = new Vector3(1f / Mathf.Max(1e-4f, ps.x), 1f / Mathf.Max(1e-4f, ps.y), 1f / Mathf.Max(1e-4f, ps.z));
            var rig = go.AddComponent<HandRig>();
            var skin = baseMat ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit")); skin.color = Skin; skin.name = "HandSkin";
            var sleeve = baseMat ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit")); sleeve.name = "HandCuff";
            var fabric = baseMat ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit")); fabric.name = "Sleeve"; fabric.color = new Color(0.16f, 0.2f, 0.27f);
            if (fabric.HasProperty("_Smoothness")) fabric.SetFloat("_Smoothness", 0.12f);
            if (skin.HasProperty("_Smoothness")) skin.SetFloat("_Smoothness", 0.35f);
            float inner = side == 0 ? 1f : -1f; // el pulgar va hacia el centro de la pantalla

            rig.root = go.transform;
            // manga: capsula larga que sale por abajo del cuadro (sin tapas planas a la vista) y puño fino con el color de estado
            Part(PrimitiveType.Capsule, "Sleeve", go.transform, new Vector3(0, -0.004f, -0.29f), Quaternion.Euler(90, 0, 0), new Vector3(0.1f, 0.22f, 0.092f), fabric);
            rig.cuff = Part(PrimitiveType.Capsule, "Cuff", go.transform, new Vector3(0, -0.002f, -0.088f), Quaternion.Euler(90, 0, 0), new Vector3(0.104f, 0.03f, 0.096f), sleeve);
            Part(PrimitiveType.Capsule, "Forearm", go.transform, new Vector3(0, 0f, -0.06f), Quaternion.Euler(90, 0, 0), new Vector3(0.066f, 0.03f, 0.056f), skin);
            Part(PrimitiveType.Sphere, "Palm", go.transform, new Vector3(0, 0, -0.005f), Quaternion.identity, new Vector3(0.088f, 0.034f, 0.1f), skin);
            Part(PrimitiveType.Sphere, "Wrist", go.transform, new Vector3(0, 0.002f, -0.045f), Quaternion.identity, new Vector3(0.07f, 0.04f, 0.05f), skin);
            rig.fingers = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float x = (-0.03f + i * 0.02f) * inner;
                float len = i == 1 || i == 2 ? 0.068f : 0.056f; // medio y anular mas largos
                var pivot = new GameObject("Finger" + i).transform; pivot.SetParent(go.transform, false); pivot.localPosition = new Vector3(x, 0.004f, 0.04f);
                Part(PrimitiveType.Capsule, "Bone", pivot, new Vector3(0, 0, len * 0.5f), Quaternion.Euler(90, 0, 0), new Vector3(0.019f, len * 0.5f, 0.019f), skin);
                rig.fingers[i] = pivot;
            }
            var tp = new GameObject("Thumb").transform; tp.SetParent(go.transform, false); tp.localPosition = new Vector3(0.042f * inner, -0.004f, -0.012f);
            tp.localRotation = Quaternion.Euler(0, 38f * inner, 0);
            Part(PrimitiveType.Capsule, "Bone", tp, new Vector3(0, 0, 0.026f), Quaternion.Euler(90, 0, 0), new Vector3(0.021f, 0.026f, 0.021f), skin);
            rig.thumb = tp;
            rig.curl = rig.curlTarget = 0.15f; rig.ApplyCurl();
            return rig;
        }

        static Renderer Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name;
            var c = g.GetComponent<Collider>(); if (c) { c.enabled = false; Destroy(c); }
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localRotation = rot; g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
            return r;
        }

        /// 0 = abierta, 1 = cerrada. Se llama cada cuadro desde PlayerHands.
        public void Set(float targetCurl, Color cuffColor)
        {
            curlTarget = Mathf.Clamp01(targetCurl);
            float pulse = Mathf.Clamp01(1f - (Time.time - pulseAt) / PulseTime);
            curl = Mathf.Lerp(curl, Mathf.Max(curlTarget, 0.75f * pulse), Smooth.Factor(pulse > 0f ? 0.6f : 0.3f));
            if (root && root.parent) root.localPosition = new Vector3(0f, 0f, 0.025f * Mathf.Sin(pulse * Mathf.PI) / Mathf.Max(1e-4f, root.parent.localScale.z)); // empujon hacia adelante
            if (cuff) cuff.material.color = cuffColor;
            ApplyCurl();
        }

        void ApplyCurl()
        {
            if (fingers != null) for (int i = 0; i < fingers.Length; i++) if (fingers[i]) fingers[i].localRotation = Quaternion.Euler(Mathf.Lerp(8f, 95f, curl) + i * 3f * curl, 0, 0);
            if (thumb) { var e = thumb.localEulerAngles; thumb.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 35f, curl), e.y, 0); }
        }
    }
}
