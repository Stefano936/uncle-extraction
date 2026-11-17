using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Informe de los modelos del kit 3D tal como los importa Unity: por cada objeto con malla, nombre, centro y tamano
    /// de sus limites en metros (marco del modelo). Sirve para adaptar el carrito y la camioneta visuales a la geometria
    /// funcional sin adivinar donde estan el asa, las puertas o el piso de carga.
    /// Batch: -executeMethod UncleExtraction.EditorTools.KitModelReport.Run
    public static class KitModelReport
    {
        public static void Run()
        {
            foreach (var name in new[] { "transport_cart", "passenger_van", "exit_door", "stool", "table", "bar_counter" })
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Kit3DImporter.Folder}/{name}.fbx");
                if (!src) { Debug.Log($"[KitReport] falta {name}"); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var rs = go.GetComponentsInChildren<Renderer>();
                var all = rs.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Debug.Log($"[KitReport] {name}: limites centro {all.center:F3} tamano {all.size:F3}; {rs.Length} mallas");
                foreach (var r in rs.OrderBy(r => r.name))
                {
                    var b = r.bounds;
                    Debug.Log($"[KitReport]   {name}/{Path(r.transform, go.transform)}: centro {b.center:F3} tamano {b.size:F3} material {(r.sharedMaterial ? r.sharedMaterial.name : "-")}");
                }
                foreach (var t in go.GetComponentsInChildren<Transform>().Where(t => !t.GetComponent<Renderer>() && t != go.transform))
                    Debug.Log($"[KitReport]   {name}/{Path(t, go.transform)} (vacio): posicion {t.position:F3}");
                Object.DestroyImmediate(go);
            }
        }

        static string Path(Transform t, Transform root) { string p = t.name; while (t.parent && t.parent != root) { t = t.parent; p = t.name + "/" + p; } return p; }
    }
}
