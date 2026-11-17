using UnityEngine;

namespace UncleExtraction
{
    /// Mantiene la textura de una fuente dinamica en el material de un texto 3D con sombreador propio (WorldText):
    /// Unity reconstruye esa textura al pedir caracteres nuevos y el material guardado quedaria con la anterior.
    [RequireComponent(typeof(TextMesh))]
    public class WorldTextFont : MonoBehaviour
    {
        TextMesh tm; MeshRenderer mr;
        void Awake() { tm = GetComponent<TextMesh>(); mr = GetComponent<MeshRenderer>(); Font.textureRebuilt += OnRebuilt; }
        void OnDestroy() { Font.textureRebuilt -= OnRebuilt; }
        void Start() { if (tm.font) { tm.font.RequestCharactersInTexture(tm.text, tm.fontSize, tm.fontStyle); Sync(); } }
        void OnRebuilt(Font f) { if (tm && f == tm.font) Sync(); }
        void Sync() { if (mr && tm.font && tm.font.material) { var m = mr.material; m.mainTexture = tm.font.material.mainTexture; } }
    }
}
