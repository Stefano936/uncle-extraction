using UnityEngine;

namespace UncleExtraction
{
    /// Luz de relleno del lado de la camara local sobre el tio (solo en el bar): en la penumbra del salon la cara, las manos
    /// y los puntos de agarre se leen sin subir la exposicion de toda la escena. No proyecta sombras y no cambia nada de juego.
    [RequireComponent(typeof(Light))]
    public class CharacterFill : MonoBehaviour
    {
        NpcBody body;
        void LateUpdate()
        {
            if (!body) body = FindAnyObjectByType<NpcBody>();
            var cam = Camera.main;
            if (!body || !cam) return;
            Vector3 chest = body.ChestPosition;
            Vector3 toCam = cam.transform.position - chest; toCam.y = 0f;
            Vector3 dir = toCam.sqrMagnitude > 0.01f ? toCam.normalized : Vector3.back;
            transform.position = chest + dir * 1.1f + Vector3.up * 0.45f;
        }
    }
}
