using NUnit.Framework;
using UnityEngine;

namespace UncleExtraction.Tests
{
    /// Validacion de lo que manda un invitado (sesion 13): intenciones y marcadores.
    public class NetIntentTests
    {
        [Test] public void Intent_NonFinite_IsRejected()
        {
            var i = new PlayerIntent { move = new Vector2(float.NaN, 0) }; Assert.IsFalse(PlayerIntent.Sanitize(ref i));
            i = new PlayerIntent { yaw = float.PositiveInfinity }; Assert.IsFalse(PlayerIntent.Sanitize(ref i));
            i = new PlayerIntent { pitch = float.NaN }; Assert.IsFalse(PlayerIntent.Sanitize(ref i));
        }
        [Test] public void Intent_IsClamped_AndOnlyGameplayButtonsSurvive()
        {
            var i = new PlayerIntent { move = new Vector2(5, 5), yaw = 725f, pitch = 400f, buttons = 0xFFFF };
            Assert.IsTrue(PlayerIntent.Sanitize(ref i));
            Assert.LessOrEqual(i.move.magnitude, 1f + 1e-4f, "sin velocidad extra");
            Assert.AreEqual(5f, i.yaw, 1e-3); Assert.AreEqual(89f, i.pitch, 1e-3);
            Assert.IsTrue(i.Has(Act.GrabLeft) && i.Has(Act.Dodge));
            Assert.IsFalse(i.Has(Act.Restart) || i.Has(Act.ToggleCursor) || i.Has(Act.Pause) || i.Has(Act.Mark), "R, Tab, Esc y Marcar no viajan en la intencion");
        }
        [Test] public void Mark_IsAccepted_OnlyFiniteInRangeAndNotTooOften()
        {
            Vector3 eye = Vector3.zero;
            Assert.IsTrue(CoopMarkers.Accept(eye, new Vector3(0, 0, 10), -99f, 0f));
            Assert.IsFalse(CoopMarkers.Accept(eye, new Vector3(0, 0, 200), -99f, 0f), "fuera de alcance");
            Assert.IsFalse(CoopMarkers.Accept(eye, new Vector3(float.NaN, 0, 0), -99f, 0f), "no finito");
            Assert.IsFalse(CoopMarkers.Accept(eye, new Vector3(0, 0, 5), 0f, CoopMarkers.MinInterval * 0.5f), "demasiado seguido");
            Assert.IsTrue(CoopMarkers.Accept(eye, new Vector3(0, 0, 5), 0f, CoopMarkers.MinInterval + 0.01f));
        }
    }
}
