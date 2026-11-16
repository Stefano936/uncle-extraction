using System.Linq;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Viste el carrito y la camioneta FUNCIONALES con las piezas de los modelos del kit (transport_cart.fbx,
    /// passenger_van.fbx). La geometria de juego no cambia: colliders, masas, sockets, piso de carga (0,30 m), rampa,
    /// volumen de extraccion y hojas de las puertas quedan como estaban. Cada pieza del kit se ajusta a una caja funcional
    /// (centro y tamano) y los primitivos que quedan visibles toman los materiales del kit.
    /// Medidas: carrito del kit 1,20 x 0,80 m con el manubrio en el lado largo (plataforma de empuje lateral) frente al
    /// funcional 0,90 x 1,40 m con el asa en el extremo corto (necesario para pasar al tio acostado por puertas de 1,5 m);
    /// camioneta del kit 2,52 x 5,38 x 2,33 m con piso de carga a 0,64 m y 2,65 m de largo frente a la funcional
    /// 2,50 x 5,40 x 2,30 m, piso a 0,30 m y 3,15 m de carga. Se conservan las medidas funcionales (ver CATALOGO_RECURSOS).
    public static class KitDressing
    {
        /// Mueve y escala una pieza para que sus limites de mundo coincidan con la caja (en el marco de 'frame', que debe
        /// estar alineado con los ejes del mundo salvo un giro en y multiplo de 90 grados).
        static void Fit(Renderer r, Transform frame, Vector3 localCenter, Vector3 localSize)
        {
            var b = r.bounds; if (b.size.x < 1e-4f || b.size.y < 1e-4f || b.size.z < 1e-4f) return;
            Vector3 ws = frame.rotation * localSize; ws = new Vector3(Mathf.Abs(ws.x), Mathf.Abs(ws.y), Mathf.Abs(ws.z));
            var pivot = new GameObject(r.name + "_Fit").transform; pivot.SetPositionAndRotation(b.center, Quaternion.identity);
            r.transform.SetParent(pivot, true);                 // pivote sin padre ni giro: escala en ejes de mundo
            pivot.localScale = new Vector3(ws.x / b.size.x, ws.y / b.size.y, ws.z / b.size.z);
            pivot.position = frame.TransformPoint(localCenter);
            pivot.SetParent(frame, true);
        }

        static Renderer Piece(GameObject kit, string name) => kit.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x => x.name == name);
        static void Hide(Transform root, params string[] names) { foreach (var n in names) { var t = root.Find(n); if (t) { var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; } } }
        static void Retint(Transform root, Material m, params string[] names) { if (!m) return; foreach (var n in names) { var t = root.Find(n); if (t) { var r = t.GetComponent<Renderer>(); if (r) r.sharedMaterial = m; } } }

        public static bool Cart(Transform ct)
        {
            var kit = PhysicsLabBuilder.KitVisual("transport_cart", ct, Vector3.zero, Quaternion.identity);
            if (!kit) return false;
            var deck = Piece(kit, "CartDeck"); var bar = Piece(kit, "HandleBar");
            if (!deck || !bar) { Object.DestroyImmediate(kit); return false; }
            Fit(deck, ct, new Vector3(0, 0.3f, 0), new Vector3(0.9f, 0.12f, 1.4f));
            Fit(bar, ct, new Vector3(0, 1.0f, -0.74f), new Vector3(0.75f, 0.06f, 0.06f));
            var ups = kit.GetComponentsInChildren<Renderer>(true).Where(x => x.name.StartsWith("HandleUpright")).ToArray();
            for (int i = 0; i < ups.Length && i < 2; i++) Fit(ups[i], ct, new Vector3(i == 0 ? 0.35f : -0.35f, 0.65f, -0.72f), new Vector3(0.05f, 0.7f, 0.05f));
            var wheels = kit.GetComponentsInChildren<Renderer>(true).Where(x => x.name.StartsWith("Wheel")).ToArray();
            for (int i = 0; i < wheels.Length && i < 4; i++)
                Fit(wheels[i], ct, new Vector3(i % 2 == 0 ? 0.38f : -0.38f, 0.12f, i < 2 ? 0.55f : -0.55f), new Vector3(0.08f, 0.24f, 0.24f));
            Hide(ct, "Cart_Base", "Cart_Post_L", "Cart_Post_R", "Cart_Handle");
            Retint(ct, deck.sharedMaterial, "Cart_Side_L", "Cart_Side_R", "Cart_Front");
            return true;
        }

        public static bool Van(Transform van, float rearZ, float frontZ, float cabZ, float floorY, float roofY, float halfW)
        {
            var kit = PhysicsLabBuilder.KitVisual("passenger_van", van, Vector3.zero, Quaternion.identity);
            if (!kit) return false;
            var frame = new GameObject("Van_Frame").transform; frame.SetParent(van, false); // marco de mundo
            float mid = (rearZ + frontZ) * 0.5f, len = rearZ - frontZ, cargoMid = (rearZ + cabZ) * 0.5f, cargoLen = rearZ - cabZ;
            var teal = Piece(kit, "Roof"); var floorR = Piece(kit, "RearFloor");
            void F(string n, Vector3 c, Vector3 s) { var r = Piece(kit, n); if (r) Fit(r, frame, c, s); }
            F("Roof", new Vector3(0, roofY + 0.03f, mid), new Vector3(halfW * 2f + 0.04f, 0.14f, len));
            F("RearSide", new Vector3(halfW - 0.05f, (floorY + roofY) * 0.5f, cargoMid), new Vector3(0.12f, roofY - floorY, cargoLen));
            F("RearSide.001", new Vector3(-halfW + 0.05f, (floorY + roofY) * 0.5f, cargoMid), new Vector3(0.12f, roofY - floorY, cargoLen));
            F("Cabin", new Vector3(0, (floorY + roofY) * 0.5f, (cabZ + frontZ) * 0.5f), new Vector3(halfW * 2f, roofY - floorY, cabZ - frontZ));
            F("Chassis", new Vector3(0, floorY * 0.5f - 0.05f, mid), new Vector3(halfW * 2f - 0.3f, 0.2f, len - 0.4f));
            F("Windshield", new Vector3(0, 1.7f, frontZ - 0.02f), new Vector3(1.85f, 0.6f, 0.04f));
            var wheels = kit.GetComponentsInChildren<Renderer>(true).Where(x => x.name.StartsWith("Wheel")).ToArray();
            for (int i = 0; i < wheels.Length && i < 4; i++)
                Fit(wheels[i], frame, new Vector3(i % 2 == 0 ? halfW + 0.1f : -halfW - 0.1f, 0.36f, i < 2 ? rearZ - 1.0f : frontZ + 1.1f), new Vector3(0.2f, 0.72f, 0.72f));
            foreach (var n in new[] { "RearFloor", "PassengerBench", "RearDoorPanel", "RearDoorPanel.001" })
                foreach (var r in kit.GetComponentsInChildren<Renderer>(true).Where(x => x.name == n)) r.enabled = false; // piso y hojas funcionales
            Hide(van, "Van_Side_L", "Van_Side_R", "Van_Roof", "Van_Front");
            if (floorR) Retint(van, floorR.sharedMaterial, "Van_Floor");
            if (teal) Retint(van, teal.sharedMaterial, "Van_Cab_Partition", "Van_Rear_Lintel");
            return true;
        }

        /// Hojas funcionales de las puertas traseras con el material del kit.
        public static void VanLeaves(Transform doorsRoot, Transform van)
        {
            var kit = van.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x => x.name == "Roof");
            if (!kit) return;
            foreach (var r in doorsRoot.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = kit.sharedMaterial;
        }
    }
}
