using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Poses de ataque del tio articulado (UncleAttackPose). Se llama con el tio todavia mirando a +z (antes del giro final)
    /// y con la malla del kit ligada (RagdollVisual): empujon y patada se muestrean de Push_Prototype y Kick_Prototype
    /// (cuadro de preparacion = la mano/el pie mas atras antes del golpe; cuadro de golpe = lo mas adelante); manotazo y
    /// sacudida se autorizan por direcciones de segmento. Si un clip falta o no se mueve, se usa la pose autorizada.
    public static class AttackPoseBuilder
    {
        // Sesion 14: las direcciones se escriben en el marco de la CARA (+z adelante, +x a la derecha del tio). La malla del
        // kit mira hacia -z de la raiz (ver NpcBody.FaceSign), asi que con el kit el marco de la cara es la raiz girada 180°.
        // Antes se escribian sobre +z de la raiz: el golpe salia hacia la nuca. Lado anatomico: con la cara en -z, la derecha
        // del tio son las partes ".R" (en -x); sin kit (cara en +z), las ".L".

        public static UncleAttackPose Build(GameObject go, NpcBody body, NpcBrain brain, RagdollVisual vis, GameObject kit, Transform t)
        {
            var pose = go.AddComponent<UncleAttackPose>(); pose.brain = brain; pose.body = body;
            float fs = kit ? -1f : 1f;
            string R = fs < 0f ? ".R" : ".L", Lf = fs < 0f ? ".L" : ".R";
            Vector3 Face(Vector3 d) => new Vector3(d.x * fs, d.y, d.z * fs);       // marco de la cara -> marco de la raiz
            Vector3 Mirror(Vector3 d) => new Vector3(-d.x, d.y, d.z);              // lado derecho -> izquierdo (marco de la cara)
            Vector3 faceFwd = t.forward * fs;
            var parts = body.parts; int n = parts.Length;
            var restRot = parts.Select(p => p.transform.rotation).ToArray();
            int ParentIndex(int i) { var cb = parts[i].joint.connectedBody; return Array.FindIndex(parts, p => p.Body == cb || p.GetComponent<Rigidbody>() == cb); }
            int Idx(string role) => Array.FindIndex(parts, p => p.role == role);

            // Objetivos de articulacion para rotaciones de mundo deseadas de cada parte (con ejes de junta identidad:
            // target = inversa(local deseada) * local de reposo, ambas relativas a la parte padre).
            Quaternion[] Targets(Quaternion[] world, Quaternion rootWorld)
            {
                var res = new Quaternion[n];
                for (int i = 0; i < n; i++)
                {
                    int pi = ParentIndex(i);
                    Quaternion parRest = pi >= 0 ? restRot[pi] : t.rotation, parPose = pi >= 0 ? world[pi] : rootWorld;
                    Quaternion startLocal = Quaternion.Inverse(parRest) * restRot[i], targetLocal = Quaternion.Inverse(parPose) * world[i];
                    res[i] = Quaternion.Inverse(targetLocal) * startLocal;
                }
                return res;
            }
            // Pose autorizada: direcciones nuevas (en el marco del tio, +z adelante) para algunos segmentos
            UncleAttackPose.Pose Authored(string name, params (string role, Vector3 dir)[] dirs)
            {
                var world = (Quaternion[])restRot.Clone();
                foreach (var (role, dir) in dirs)
                {
                    int i = Idx(role); if (i < 0) continue;
                    Vector3 rest = restRot[i] * Vector3.up; // el eje del segmento es +y local de la parte
                    world[i] = Quaternion.FromToRotation(rest, t.TransformDirection(Face(dir).normalized)) * restRot[i];
                    // los hijos acompanan la rotacion del padre si no tienen direccion propia
                    foreach (var c in Enumerable.Range(0, n).Where(c => ParentIndex(c) == i && !dirs.Any(d => d.role == parts[c].role)))
                        world[c] = world[i] * Quaternion.Inverse(restRot[i]) * restRot[c];
                }
                return new UncleAttackPose.Pose { name = name, targets = Targets(world, t.rotation) };
            }

            // Manotazo (mano derecha): preparacion con el brazo arriba y atras; golpe cruzando por delante
            pose.slapWindup = Authored("slap_windup", ("upper_arm" + R, new Vector3(0.75f, 0.55f, -0.35f)), ("forearm" + R, new Vector3(0.25f, 0.95f, 0.1f)));
            pose.slapStrike = Authored("slap_strike", ("upper_arm" + R, new Vector3(0.2f, 0.15f, 1f)), ("forearm" + R, new Vector3(-0.8f, 0.1f, 0.6f)));
            pose.slapWindupL = Authored("slap_windup_l", ("upper_arm" + Lf, Mirror(new Vector3(0.75f, 0.55f, -0.35f))), ("forearm" + Lf, Mirror(new Vector3(0.25f, 0.95f, 0.1f))));
            pose.slapStrikeL = Authored("slap_strike_l", ("upper_arm" + Lf, Mirror(new Vector3(0.2f, 0.15f, 1f))), ("forearm" + Lf, Mirror(new Vector3(-0.8f, 0.1f, 0.6f))));
            // Sacudida (a quien lo agarra): brazos abiertos e inhala; en el golpe, el torso gira de lado a lado (UncleAttackPose)
            pose.shakeWindup = Authored("shake_windup", ("upper_arm" + R, new Vector3(1f, 0.25f, 0f)), ("upper_arm" + Lf, new Vector3(-1f, 0.25f, 0f)), ("chest", new Vector3(0f, 1f, -0.2f)));
            pose.shakeStrike = Authored("shake_strike", ("chest", new Vector3(0.35f, 1f, 0f)), ("upper_arm" + R, new Vector3(1f, -0.2f, 0.3f)), ("upper_arm" + Lf, new Vector3(-1f, -0.2f, -0.3f)));
            // Respaldo autorizado de empujon y patada (si los clips no sirven)
            var pushW = Authored("push_windup", ("upper_arm" + R, new Vector3(0.3f, -0.4f, -0.85f)), ("forearm" + R, new Vector3(0.05f, 0.35f, 0.95f)), ("upper_arm" + Lf, new Vector3(-0.3f, -0.4f, -0.85f)), ("forearm" + Lf, new Vector3(-0.05f, 0.35f, 0.95f)));
            var pushS = Authored("push_strike", ("upper_arm" + R, new Vector3(0.2f, 0.05f, 1f)), ("forearm" + R, new Vector3(0.05f, 0.1f, 1f)), ("upper_arm" + Lf, new Vector3(-0.2f, 0.05f, 1f)), ("forearm" + Lf, new Vector3(-0.05f, 0.1f, 1f)));
            var kickW = Authored("kick_windup", ("thigh" + R, new Vector3(0.05f, -0.45f, 0.9f)), ("calf" + R, new Vector3(0f, -1f, -0.35f)));
            var kickS = Authored("kick_strike", ("thigh" + R, new Vector3(0.05f, -0.55f, 0.85f)), ("calf" + R, new Vector3(0f, -0.25f, 1f)));
            pose.kickWindupL = Authored("kick_windup_l", ("thigh" + Lf, Mirror(new Vector3(0.05f, -0.45f, 0.9f))), ("calf" + Lf, Mirror(new Vector3(0f, -1f, -0.35f))));
            pose.kickStrikeL = Authored("kick_strike_l", ("thigh" + Lf, Mirror(new Vector3(0.05f, -0.55f, 0.85f))), ("calf" + Lf, Mirror(new Vector3(0f, -0.25f, 1f))));

            // Clips del kit
            pose.pushSource = pose.kickSource = "autorizada";
            pose.pushWindup = pushW; pose.pushStrike = pushS; pose.kickWindup = kickW; pose.kickStrike = kickS;
            var clips = kit ? AssetDatabase.LoadAllAssetsAtPath($"{Kit3DImporter.Folder}/uncle_prototype.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList() : new List<AnimationClip>();
            if (kit && vis && clips.Count > 0)
            {
                var all = kit.GetComponentsInChildren<Transform>();
                var saved = all.Select(x => (x, x.localPosition, x.localRotation)).ToArray();
                var links = vis.links.ToList();
                var boneRest = links.Select(l => l.bone.rotation).ToArray();
                Transform pelvisBone = links.FirstOrDefault(l => l.part == t).bone;
                Quaternion pelvisRest = pelvisBone ? pelvisBone.rotation : Quaternion.identity;

                // Rotaciones de mundo de las partes para la pose muestreada (parte = hueso * inversa(hueso en reposo) * parte en reposo)
                Quaternion[] PartWorld(out Quaternion root)
                {
                    var w = (Quaternion[])restRot.Clone();
                    for (int k = 0; k < links.Count; k++)
                    {
                        int i = Array.FindIndex(parts, p => p.transform == links[k].part); if (i < 0) continue;
                        w[i] = links[k].bone.rotation * Quaternion.Inverse(boneRest[k]) * restRot[i];
                    }
                    root = pelvisBone ? pelvisBone.rotation * Quaternion.Inverse(pelvisRest) * t.rotation : t.rotation;
                    return w;
                }
                bool FromClip(string clipName, string effectorRole, out UncleAttackPose.Pose w, out UncleAttackPose.Pose s, out string info)
                {
                    w = s = null; info = "";
                    var clip = clips.FirstOrDefault(c => c.name.Contains(clipName));
                    if (!clip) { info = $"falta {clipName}"; return false; }
                    // efector: el hueso ligado a la parte de ese rol con mayor recorrido hacia adelante
                    var effLinks = links.Where(l => l.part && l.part.GetComponent<RagdollPart>() is RagdollPart rp0 && rp0 != null && rp0.role.StartsWith(effectorRole)).ToList();
                    const int N = 48; var fwd = new float[effLinks.Count, N]; var pos = new Vector3[effLinks.Count, N];
                    for (int f = 0; f < N; f++)
                    {
                        clip.SampleAnimation(kit, clip.length * f / (N - 1));
                        for (int e = 0; e < effLinks.Count; e++)
                        {   // punta del segmento (hijo del hueso o el propio hueso), relativa a la pelvis, sobre el frente del tio
                            var b = effLinks[e].bone; var tip = b.childCount > 0 ? b.GetChild(0) : b;
                            pos[e, f] = tip.position - (pelvisBone ? pelvisBone.position : t.position);
                            fwd[e, f] = Vector3.Dot(pos[e, f], faceFwd);
                        }
                    }
                    int bestE = 0; float bestRange = -1f;
                    for (int e = 0; e < effLinks.Count; e++) { float mn = float.MaxValue, mx = float.MinValue; for (int f = 0; f < N; f++) { mn = Mathf.Min(mn, fwd[e, f]); mx = Mathf.Max(mx, fwd[e, f]); } if (mx - mn > bestRange) { bestRange = mx - mn; bestE = e; } }
                    int strike = 0; for (int f = 1; f < N; f++) if (fwd[bestE, f] > fwd[bestE, strike]) strike = f;
                    int windup = 0; for (int f = 1; f <= strike; f++) if (fwd[bestE, f] < fwd[bestE, windup]) windup = f;
                    string how = "mas adelante";
                    if (strike == 0)
                    {   // el efector nunca pasa por delante de su posicion inicial (patada del kit): golpe = mayor desplazamiento
                        for (int f = 1; f < N; f++) if ((pos[bestE, f] - pos[bestE, 0]).sqrMagnitude > (pos[bestE, strike] - pos[bestE, 0]).sqrMagnitude) strike = f;
                        windup = strike / 2; how = "mayor desplazamiento";
                    }
                    if (bestRange < 0.12f || strike == 0) { info = $"{clip.name}: recorrido {bestRange:0.00} m, sin golpe claro"; RestoreAll(); return false; }
                    clip.SampleAnimation(kit, clip.length * windup / (N - 1)); var ww = PartWorld(out var rw);
                    w = new UncleAttackPose.Pose { name = clipName + "_windup", targets = Targets(ww, rw) };
                    clip.SampleAnimation(kit, clip.length * strike / (N - 1)); var sw = PartWorld(out var rs);
                    s = new UncleAttackPose.Pose { name = clipName + "_strike", targets = Targets(sw, rs) };
                    info = $"{clip.name} ({clip.length:0.00} s): efector {effLinks[bestE].bone.name}, recorrido {bestRange:0.00} m, preparacion t={clip.length * windup / (N - 1):0.00} s, golpe t={clip.length * strike / (N - 1):0.00} s ({how})";
                    RestoreAll(); return true;
                }
                void RestoreAll() { foreach (var (x, p, r) in saved) if (x) { x.localPosition = p; x.localRotation = r; } }

                // Preparacion del empujon: la del clip apenas mueve el antebrazo (0,09 m medidos en la prueba): se usa la autorizada
                // (brazos atras, mas legible) y el golpe del clip.
                if (FromClip("Push", "forearm", out var pw, out var ps, out var pinfo)) { pose.pushStrike = ps; pose.pushSource = pinfo + "; preparacion autorizada (la del clip mueve 0,09 m)"; }
                else pose.pushSource = "autorizada (" + pinfo + ")";
                // patada: autorizada para los dos lados (el clip patea con una sola pierna y no se puede espejar con seguridad)
                pose.kickSource = "autorizada, derecha e izquierda (el clip del kit usa una sola pierna)";
                RestoreAll();
            }
            Debug.Log($"[AttackPoses] empujon: {pose.pushSource}; patada: {pose.kickSource}; manotazo y sacudida: autorizadas");
            return pose;
        }

        /// Material URP/Unlit transparente de doble cara para los avisos en el piso.
        public static Material TelegraphMaterial()
        {
            const string path = "Assets/Game/Art/Materials/Telegraph_Unlit.mat";
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(1f, 0.6f, 0.1f, 0.5f)); m.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            EditorUtility.SetDirty(m); return m;
        }
    }
}
