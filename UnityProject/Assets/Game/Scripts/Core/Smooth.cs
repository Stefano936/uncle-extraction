using UnityEngine;

namespace UncleExtraction
{
    /// Suavizado visual independiente de los cuadros por segundo (sesion 13). Un Lerp con factor fijo por cuadro converge
    /// el doble de rapido a 120 fps que a 60; Factor(f60) da el factor equivalente para el dt actual, calibrado para que a
    /// 60 fps sea exactamente f60 (el aspecto que ya estaba aprobado).
    public static class Smooth
    {
        public static float Factor(float perFrameAt60, float dt) => 1f - Mathf.Pow(1f - Mathf.Clamp01(perFrameAt60), Mathf.Max(0f, dt) * 60f);
        public static float Factor(float perFrameAt60) => Factor(perFrameAt60, Time.deltaTime);
    }
}
