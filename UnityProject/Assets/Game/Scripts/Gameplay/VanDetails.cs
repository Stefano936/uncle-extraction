using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction
{
    /// Detalle de la camioneta (sesion 15), sin tocar colisiones ni la zona de cierre:
    /// - hojas traseras con burlete de goma, ventanilla, manija y dos bisagras (grosor y lectura de «puerta»);
    /// - piso de carga con nervaduras de goma;
    /// - LUZ que indica donde acomodar al tio (cono calido al fondo, contra la mampara) y DESDE DONDE CERRAR (pastilla de luz
    ///   y huellas pintadas en la vereda, en LevelLayout.CloseStand);
    /// - carroceria con menos brillo (los reflejos saturados competian con las señales).
    public class VanDetails : MonoBehaviour
    {
        const float RearZ = -9.25f, FloorY = 0.30f, RoofY = 2.3f, HalfW = 1.25f, CabZ = -12.4f;

        void Start()
        {
            var doors = GetComponent<VanDoors>(); if (!doors) { enabled = false; return; }
            var rubber = Solid("Van_Rubber", new Color(0.05f, 0.05f, 0.055f), 0.25f, 0f);
            var chrome = Solid("Van_Handle", new Color(0.7f, 0.72f, 0.75f), 0.8f, 0.9f);
            var glass = Solid("Van_Window", new Color(0.08f, 0.1f, 0.13f), 0.92f, 0.2f);
            var paint = Solid("Floor_Paint", new Color(0.95f, 0.78f, 0.15f), 0.3f, 0f);
            if (!RouteDressing.Enabled) // sesion 21: las hojas modeladas (RouteDressing) ya traen ventanilla, manija y bisagras
            {
                Leaf(doors.leftLeaf, 1f, rubber, chrome, glass);
                Leaf(doors.rightLeaf, -1f, rubber, chrome, glass);
            }
            // nervaduras del piso de carga
            for (int i = -3; i <= 3; i++) Box(null, "Floor_Rib", new Vector3(i * 0.3f, FloorY + 0.012f, (RearZ + CabZ) * 0.5f), new Vector3(0.05f, 0.024f, (RearZ - CabZ) - 0.2f), rubber);
            // donde acomodarlo: cono de luz al fondo de la caja
            var place = new GameObject("Van_PlaceLight").AddComponent<Light>(); place.transform.SetParent(transform, false);
            place.type = LightType.Spot; place.color = new Color(1f, 0.82f, 0.55f); place.intensity = 2.2f; place.range = 3.2f; place.spotAngle = 70f; place.shadows = LightShadows.None;
            place.transform.position = new Vector3(0f, RoofY - 0.15f, CabZ + 1.0f); place.transform.rotation = Quaternion.LookRotation(Vector3.down + Vector3.forward * 0.25f);
            // desde donde cerrar: huellas pintadas y pastilla de luz en la vereda
            var L = LevelLayout.Current; Vector3 stand = L ? L.CloseStand(0f) : new Vector3(0f, 0f, RearZ + 2f);
            for (int s = -1; s <= 1; s += 2)
            {
                var f = Box(null, "Close_Foot", stand + new Vector3(0.14f * s, 0.006f, 0.05f * s), new Vector3(0.11f, 0.004f, 0.27f), paint);
                f.transform.rotation = Quaternion.Euler(0, 6f * s, 0);
            }
            var pool = new GameObject("Van_CloseLight").AddComponent<Light>(); pool.transform.SetParent(transform, false);
            pool.type = LightType.Spot; pool.color = new Color(1f, 0.9f, 0.6f); pool.intensity = 3f; pool.range = 4f; pool.spotAngle = 42f; pool.shadows = LightShadows.None;
            pool.transform.position = new Vector3(0f, RoofY + 0.1f, RearZ + 0.2f); pool.transform.rotation = Quaternion.LookRotation(stand - pool.transform.position);
            // carroceria: menos brillo (instancias, no el asset)
            foreach (var r in transform.root.GetComponentsInChildren<Renderer>())
                if (r && r.sharedMaterial && r.sharedMaterial.name.StartsWith("Van_Body") && r.material.HasProperty("_Smoothness")) r.material.SetFloat("_Smoothness", 0.38f);
        }

        void Leaf(Transform hinge, float dir, Material rubber, Material chrome, Material glass)
        {
            if (!hinge) return;
            float w = 1.18f, h = 1.75f, cy = FloorY + 0.9f, cx = dir * 0.6f, z = 0.06f; // cara de afuera (+z)
            Box(hinge, "Gasket_Top", new Vector3(cx, cy + h * 0.5f - 0.02f, z), new Vector3(w, 0.04f, 0.02f), rubber);
            Box(hinge, "Gasket_Bottom", new Vector3(cx, cy - h * 0.5f + 0.02f, z), new Vector3(w, 0.04f, 0.02f), rubber);
            Box(hinge, "Gasket_Free", new Vector3(dir * (w - 0.01f), cy, z), new Vector3(0.04f, h, 0.02f), rubber);
            Box(hinge, "Window", new Vector3(cx, cy + 0.45f, z), new Vector3(w - 0.24f, 0.55f, 0.012f), glass);
            Box(hinge, "Handle", new Vector3(dir * (w - 0.16f), cy - 0.05f, z + 0.03f), new Vector3(0.05f, 0.24f, 0.04f), chrome);
            foreach (float y in new[] { cy - 0.55f, cy + 0.55f })
            {
                var k = Box(hinge, "Hinge", new Vector3(dir * 0.015f, y, z - 0.02f), new Vector3(0.05f, 0.16f, 0.05f), chrome, PrimitiveType.Cylinder);
                k.transform.localScale = new Vector3(0.05f, 0.08f, 0.05f);
            }
        }

        GameObject Box(Transform parent, string n, Vector3 pos, Vector3 size, Material m, PrimitiveType t = PrimitiveType.Cube)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; var c = g.GetComponent<Collider>(); if (c) { c.enabled = false; Destroy(c); }
            if (parent) { g.transform.SetParent(parent, false); g.transform.localPosition = pos; } else { g.transform.SetParent(transform, false); g.transform.position = pos; }
            g.transform.localScale = size; var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; return g;
        }
        static Material Solid(string n, Color c, float smooth, float metal)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit"); var m = new Material(sh ? sh : Shader.Find("Standard")) { name = n, color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal); return m;
        }
    }
}
