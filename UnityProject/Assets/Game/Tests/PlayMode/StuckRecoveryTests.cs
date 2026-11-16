using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Reproducciones controladas de los dos atascos vistos en partidas automaticas (sesion 7) y comprobacion de que un
    /// jugador sale de ellos con acciones normales: mirar, moverse, agarrar/soltar, Q, E y agacharse (LabInput.Simulated,
    /// la misma via que el teclado). No hay teletransportes despues de armar la situacion inicial, ni Strap()/Secure()
    /// durante la recuperacion. El cerebro del tio esta apagado: se prueba la geometria y la interaccion, no la resistencia.
    [Timeout(120000)]
    public class StuckRecoveryTests
    {
        protected virtual string Scene => "Bar_LastRound";
        NpcBrain npc; NpcBody body; PlayerMotor player; PlayerHands hands; RoundDirector round;
        ExtractionZone zone; VanDoors van; TransportCart cart; LevelLayout L;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            LabInput.ClearSimulated();
            SceneManager.LoadScene(Scene);
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local, "el anfitrion local deberia generar al jugador");
            yield return new WaitForFixedUpdate();
            npc = Object.FindAnyObjectByType<NpcBrain>(); body = npc.GetComponent<NpcBody>();
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            round = Object.FindAnyObjectByType<RoundDirector>(); zone = Object.FindAnyObjectByType<ExtractionZone>();
            van = Object.FindAnyObjectByType<VanDoors>(); cart = Object.FindAnyObjectByType<TransportCart>();
            L = LevelLayout.Current;
            npc.brainEnabled = false;
        }

        [TearDown]
        public void Clear() => LabInput.ClearSimulated();

        // ---------- entradas de jugador ----------
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        Vector3 Eye => hands.cam.transform.position;
        Vector3 Me => Flat(player.transform.position);

        void LookAt(Vector3 p)
        {
            Vector3 to = p - Eye; Vector3 f = Flat(to);
            player.SetYawPitch(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, Mathf.Clamp(-Mathf.Atan2(to.y, Mathf.Max(0.05f, f.magnitude)) * Mathf.Rad2Deg, -80f, 80f));
        }
        void MoveWorld(Vector3 dir)
        {
            dir = Flat(dir); if (dir.sqrMagnitude < 1e-4f) { LabInput.SimulatedMove = Vector2.zero; return; }
            Vector3 local = Quaternion.Euler(0, -player.transform.eulerAngles.y, 0) * dir.normalized;
            LabInput.SimulatedMove = new Vector2(local.x, local.z);
        }
        static void Hold(Act a, bool on) => LabInput.Simulated[(int)a] = on;
        IEnumerator Tap(Act a) { Hold(a, true); yield return null; Hold(a, false); yield return null; }
        void Place(Vector3 pos, float yaw)
        {
            player.Body.position = pos; player.transform.position = pos; player.Body.linearVelocity = Vector3.zero; player.SetYawPitch(yaw, 15f);
        }

        /// Caminar hasta un punto (mirando 'look' si se da). Devuelve por 'ok' si llego.
        IEnumerator WalkTo(Vector3 dest, float timeout, Vector3? look = null, float tol = 0.2f)
        {
            float t0 = Time.time;
            while (Time.time - t0 < timeout && Vector3.Distance(Me, Flat(dest)) > tol)
            {
                if (look.HasValue) LookAt(look.Value); else LookAt(Flat(dest) + Vector3.up * 1.5f);
                MoveWorld(dest - Me);
                yield return null;
            }
            LabInput.SimulatedMove = Vector2.zero;
        }

        /// Apuntar a un punto y mantener el clic de una mano hasta tomarlo (como un jugador: el candidato debe ser ese).
        IEnumerator GrabWith(int hand, GrabSocket s, float timeout)
        {
            var act = hand == 0 ? Act.GrabLeft : Act.GrabRight;
            for (float t = 0; t < timeout && hands.HeldSocketId(hand) != s.socketId; t += Time.deltaTime)
            {
                LookAt(s.Position); Hold(act, hands.Candidate == s); yield return null;
            }
        }

        /// Que tapa la linea de vision del ojo a un punto (diagnostico de los asserts).
        string Blocker(GrabSocket s)
        {
            Vector3 to = s.Position - Eye;
            foreach (var h in Physics.RaycastAll(Eye, to.normalized, to.magnitude - 0.05f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                var r = h.collider.attachedRigidbody;
                if (r == player.Body || r == s.body) continue;
                return $"{h.collider.name} a {h.distance:0.00} m";
            }
            return Vector3.Angle(hands.cam.transform.forward, to) > hands.aimConeDeg ? "fuera del cono de mira" : "nada";
        }

        /// Con que solidos se toca el carrito o el tio (diagnostico): nombres de colliders ajenos a 3 cm, sin piso ni jugadores.
        string Contacts()
        {
            var seen = new System.Collections.Generic.List<string>();
            void Check(Collider own, string who)
            {
                if (!own || own.isTrigger) return;
                var bb = own.bounds; bb.Expand(0.06f);
                foreach (var c in Physics.OverlapBox(bb.center, bb.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    var r = c.attachedRigidbody;
                    if (c.name == "Floor" || c.name.Contains("Sidewalk") || body.Owns(r) || (r && (r.GetComponent<TransportCart>() || r.GetComponent<PlayerMotor>()))) continue;
                    if (!Physics.ComputePenetration(own, own.transform.position, own.transform.rotation, c, c.transform.position, c.transform.rotation, out _, out _)
                        && Physics.ClosestPoint(own.bounds.center, c, c.transform.position, c.transform.rotation) is Vector3 cp && Vector3.Distance(cp, own.ClosestPoint(cp)) > 0.04f) continue;
                    string k = $"{who}~{c.name}"; if (!seen.Contains(k)) seen.Add(k);
                }
            }
            foreach (var c in cart.GetComponentsInChildren<Collider>()) Check(c, c.name);
            foreach (var c in body.Colliders) Check(c, c.attachedRigidbody ? c.attachedRigidbody.name : c.name);
            return seen.Count == 0 ? "sin contactos" : string.Join(", ", seen);
        }

        /// Acuesta al tio dentro de la camioneta a lo largo del eje, cabeza hacia la cabina (armado de la situacion).
        void LayUncleInVan(float pelvisZ, float pelvisX)
        {
            Quaternion lying = Quaternion.LookRotation(Vector3.up, Vector3.back);
            Vector3 pelvisLocal = body.transform.InverseTransformPoint(body.pelvis.position);
            Vector3 pel = new Vector3(pelvisX, zone.transform.position.y - 0.85f + 0.36f, pelvisZ);
            body.TeleportTo(pel - lying * pelvisLocal, lying);
            body.SetFallen("prueba");
        }

        IEnumerator CloseFromOutside(float timeout)
        {
            float t0 = Time.time;
            while (Time.time - t0 < timeout && !zone.Secured)
            {
                LookAt(van.closePoint); Hold(Act.Interact, true); yield return null;
            }
            Hold(Act.Interact, false);
        }

        // ---------- 1) una extremidad en la abertura de la camioneta ----------

        /// Reproduce «le queda algo afuera»: torso, pelvis y hombros adentro y un pie en la franja que barren las hojas.
        /// El motivo tiene que verse en el HUD y nombrar la parte; un jugador lo resuelve agarrando el tobillo, empujandolo
        /// hacia adentro caminando, soltando (Q), saliendo de la franja y manteniendo E.
        [UnityTest]
        public IEnumerator Van_FootInDoorway_IsExplained_AndPlayerTucksItIn()
        {
            float found = float.NaN; string reason = "";
            foreach (var dz in new[] { 0.55f, 0.65f, 0.75f, 0.85f, 0.95f })
            {
                LayUncleInVan(L.vanRearZ - dz, L.vanAxisX + 0.2f);
                yield return new WaitForSeconds(1.0f);
                reason = van.BlockReason();
                if (zone.AllPointsInside && reason.StartsWith("van.part_outside")) { found = dz; break; }
            }
            Assert.IsFalse(float.IsNaN(found), $"no se pudo reproducir un pie en la abertura con el resto adentro (ultimo motivo '{reason}')");
            Debug.Log($"[Stuck] pie en la abertura con la pelvis a {found:0.00} m de la abertura: '{reason}'");
            StringAssert.Contains("part=part.foot", reason, "el motivo debe nombrar la parte que sobresale");

            // Desde el lugar normal de cierre, el HUD muestra el motivo en lugar de «Mantener E»
            Place(L.CloseStand(1f), 180f);
            for (int i = 0; i < 5; i++) { LookAt(van.closePoint); yield return null; }
            StringAssert.StartsWith("van.part_outside", hands.InteractLabel, "el jugador ve por que no se puede cerrar");
            yield return CloseFromOutside(1.5f);
            Assert.IsFalse(zone.Secured, "con un pie afuera no se cierra");

            // Recuperacion: tomar el tobillo que sobresale y empujarlo caminando hacia adentro
            var ankle = GrabSocket.All.Where(s => body.Owns(s.body) && s.socketId.StartsWith("ankle")).OrderByDescending(s => s.Position.z).First();
            float t0 = Time.time;
            Hold(Act.Crouch, true);
            yield return WalkTo(new Vector3(ankle.Position.x, 0f, L.vanRearZ + 1.1f), 3f, ankle.Position);
            yield return GrabWith(0, ankle, 2f);
            Assert.AreEqual(ankle.socketId, hands.HeldSocketId(0),
                $"deberia poder tomar el tobillo desde afuera (a {Vector3.Distance(Eye, ankle.Position):0.00} m, apunta a {(hands.Candidate ? hands.Candidate.socketId : "nada")}, jugador {Me}, tobillo {ankle.Position})");
            for (float t = 0; t < 4f && ankle.Position.z > L.vanRearZ - 0.25f; t += Time.deltaTime)
            {
                LookAt(ankle.Position + Vector3.back * 0.6f); MoveWorld(Vector3.back); yield return null;
            }
            LabInput.SimulatedMove = Vector2.zero; Hold(Act.GrabLeft, false); Hold(Act.Crouch, false);
            yield return Tap(Act.ReleaseAll);
            Debug.Log($"[Stuck] tobillo empujado hasta z={ankle.Position.z:0.00} (abertura {L.vanRearZ:0.00}) en {Time.time - t0:0.0} s");
            yield return WalkTo(L.CloseStand(1f), 3f, van.closePoint);
            yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.2f);
            string afterFoot = van.BlockReason();
            Debug.Log($"[Stuck] tras empujar el pie: motivo '{afterFoot}', HUD '{hands.InteractLabel}'");
            if (afterFoot.StartsWith("van.part_outside"))
            {   // La pierna vuelve a estirarse: el cuerpo entero esta demasiado cerca de la abertura. Entrar por el costado
                // hasta la mampara, tomar un hombro y tirar del torso hacia la cabina; salir por el costado y cerrar.
                float side = body.pelvis.position.x > L.vanAxisX ? -0.8f : 0.8f, lane = L.vanAxisX + side;
                yield return WalkTo(new Vector3(lane, 0f, L.vanRearZ + 1.0f), 3f);
                var sh = GrabSocket.All.Where(s => body.Owns(s.body) && s.socketId.StartsWith("shoulder")).OrderBy(s => Mathf.Abs(s.Position.x - lane)).First();
                yield return WalkTo(new Vector3(lane, 0f, Mathf.Max(L.vanCabZ + 0.45f, sh.Position.z - 0.35f)), 5f);
                Hold(Act.Crouch, true);
                yield return GrabWith(0, sh, 2f);
                var second = GrabSocket.All.First(s => body.Owns(s.body) && s.socketId.StartsWith("shoulder") && s != sh); // el otro hombro
                yield return GrabWith(1, second, 1.5f);
                Debug.Log($"[Stuck] adentro en {Me}: manos {hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}; contactos del tio: {Contacts()}");
                float pz0 = body.pelvis.position.z;
                for (float t = 0; t < 5f && van.BlockReason().StartsWith("van.part_outside"); t += Time.deltaTime)
                { LookAt(sh.Position); MoveWorld(Vector3.back); Hold(Act.Effort, true); yield return null; }
                LabInput.SimulatedMove = Vector2.zero; Hold(Act.Effort, false); Hold(Act.GrabLeft, false); Hold(Act.GrabRight, false); Hold(Act.Crouch, false);
                Debug.Log($"[Stuck] tiro del torso: pelvis {pz0:0.00} -> {body.pelvis.position.z:0.00}, jugador {Me}, manos {hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}");
                yield return Tap(Act.ReleaseAll);
                Debug.Log($"[Stuck] tras tirar del hombro: motivo '{van.BlockReason()}', pelvis z={body.pelvis.position.z:0.00}");
                yield return WalkTo(new Vector3(lane, 0f, Me.z), 1.5f);
                yield return WalkTo(new Vector3(lane, 0f, L.vanRearZ + 1.0f), 5f);
                yield return WalkTo(L.CloseStand(Mathf.Sign(side)), 3f, van.closePoint);
                yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.2f);
            }
            Debug.Log($"[Stuck] antes de cerrar: motivo '{van.BlockReason()}', HUD '{hands.InteractLabel}', jugador {Me}");
            yield return CloseFromOutside(4f);
            Debug.Log($"[Stuck] pie en la abertura: recuperado={zone.Secured} en {Time.time - t0:0.0} s");
            Assert.IsTrue(zone.Secured, $"con el pie acomodado deberia cerrarse (motivo '{van.BlockReason()}')");
        }

        /// El cliente de la corrida coop quedo dentro de la camioneta entre el tio y la pared. Con el tio acostado en el
        /// centro, un jugador que termino junto a la mampara tiene que poder salir caminando por un costado o, si el paso no
        /// alcanza, apartando al tio con las manos.
        [UnityTest]
        public IEnumerator Van_PlayerBehindUncle_CanGetOut()
        {
            LayUncleInVan(L.vanRearZ - 1.4f, L.vanAxisX);
            yield return new WaitForSeconds(1.0f);
            Place(new Vector3(L.vanAxisX, 0.35f, L.vanCabZ + 0.45f), 0f);
            yield return new WaitForSeconds(0.3f);
            float t0 = Time.time; string how = "";
            foreach (var side in new[] { 0.8f, -0.8f })
            {
                yield return WalkTo(new Vector3(L.vanAxisX + side, 0f, Me.z), 1.5f);
                yield return WalkTo(new Vector3(L.vanAxisX + side, 0f, L.vanRearZ + 1.0f), 5f);
                if (Me.z > L.vanRearZ + 0.6f) { how = $"caminando por el costado {side:+0.0;-0.0}"; break; }
            }
            if (how == "")
            {   // el paso no alcanza: tomar al tio del hombro y correrlo hacia el otro lado antes de volver a intentar
                var sh = GrabSocket.All.Where(s => body.Owns(s.body) && s.socketId.StartsWith("shoulder")).OrderBy(s => Vector3.Distance(s.Position, Eye)).First();
                yield return GrabWith(0, sh, 2f);
                for (float t = 0; t < 2f; t += Time.deltaTime) { LookAt(sh.Position); MoveWorld(new Vector3(-Mathf.Sign(Me.x - L.vanAxisX), 0, 0)); yield return null; }
                Hold(Act.GrabLeft, false); yield return Tap(Act.ReleaseAll);
                yield return WalkTo(new Vector3(Me.x, 0f, L.vanRearZ + 1.0f), 5f);
                if (Me.z > L.vanRearZ + 0.6f) how = "apartando al tio con las manos";
            }
            Debug.Log($"[Stuck] jugador detras del tio: {(how == "" ? "NO SALE" : how)} en {Time.time - t0:0.0} s; jugador en {Me}");
            Assert.AreNotEqual("", how, $"el jugador quedo encerrado en la camioneta (en {Me})");
        }

        // ---------- 2) carrito atravesado en la puerta del bar ----------

        /// Carga al tio en el carrito y lleva el conjunto (carrito + tio atado) a una pose dada (armado de la situacion).
        IEnumerator StrappedCartAt(Vector3 pos, float yaw)
        {
            Vector3 c = cart.transform.position + cart.transform.right * 0.9f; c.y = 0f;
            body.TeleportTo(c, cart.transform.rotation);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(cart.Strap(), $"deberia poder cargarlo: {cart.LastFailure}");
            for (float t = 0; t < 1.5f && !cart.IsStrapped; t += Time.deltaTime) yield return null;
            Assert.IsTrue(cart.IsStrapped);
            yield return new WaitForSeconds(0.3f);
            var crb = cart.GetComponent<Rigidbody>();
            Quaternion inv = Quaternion.Inverse(crb.rotation);
            Vector3 relP = inv * (body.Body.position - crb.position); Quaternion relR = inv * body.Body.rotation;
            Quaternion rot = Quaternion.Euler(0, yaw, 0);
            // la hoja de la puerta del bar, abierta hacia la vereda (70 grados, sin llegar al reten) en el mismo instante:
            // despues se cierra sola y se apoya en el carrito, como en las corridas que fallaron
            var door = Object.FindAnyObjectByType<HingeJoint>(); var drb = door.GetComponent<Rigidbody>();
            drb.rotation = Quaternion.Euler(0, 70f, 0); door.transform.rotation = drb.rotation; drb.angularVelocity = Vector3.zero;
            crb.linearVelocity = Vector3.zero; crb.angularVelocity = Vector3.zero;
            crb.position = pos; crb.rotation = rot; cart.transform.SetPositionAndRotation(pos, rot);
            body.TeleportTo(pos + rot * relP, rot * relR);
            yield return new WaitForSeconds(0.4f);
            Debug.Log($"[Stuck] armado: puerta a {door.angle:0} grados; contactos: {Contacts()}");
        }

        /// Carrito cargado atravesado en la puerta (poses vistas cuando el bot fallaba): el jugador, ya en la vereda, tira del
        /// asa. Resultado medido (2026-11-16): en las dos poses sale tirando recto, la hoja y el marco lo enderezan como a un
        /// remolque; si alguna vez queda trabado, se intenta empujarlo hacia adentro, alinearse y volver a tirar, y como
        /// ultimo recurso soltar la correa (E). Falla si no sale o si solo sale soltando al tio.
        /// Poses del carrito cargado en la puerta: "diag45" montado sobre el marco en diagonal; "cross90" girado 90 grados
        /// dentro del bar contra la cara interior del muro, con el asa en la abertura.
        public static readonly string[] CartPoses = { "diag45", "cross90", "bar_fail_m38", "lab_fail_m28" };

        [UnityTest]
        public IEnumerator Cart_CrookedInDoorway_StraightPullBringsItThrough([ValueSource(nameof(CartPoses))] string pose)
        {
            if (pose == "diag45") yield return StrappedCartAt(new Vector3(L.doorX + 0.1f, 0f, L.doorZ + 0.2f), 45f);
            else if (pose == "cross90") yield return StrappedCartAt(new Vector3(L.doorX + 0.3f, 0f, L.doorZ + 0.57f), 90f);
            else if (pose == "bar_fail_m38") yield return StrappedCartAt(new Vector3(L.doorX - 0.07f, 0f, L.doorZ + 0.01f), -38f); // pose exacta del FAIL de pt_ba4 (bot 150 s tirando)
            else yield return StrappedCartAt(new Vector3(L.doorX + 0.12f, 0f, L.doorZ + 0.63f), -28f); // pose del FAIL de solo_gap (laboratorio, tio central)
            var handle = GrabSocket.ById("cart_handle");
            Place(Flat(handle.Position) + new Vector3(0.5f, 0f, -0.75f), 0f); // del lado opuesto a la bisagra: la hoja no tapa el asa
            yield return new WaitForSeconds(0.2f);
            yield return GrabWith(0, handle, 2f); yield return GrabWith(1, handle, 2f);
            Assert.IsTrue(hands.HeldSocketId(0) == "cart_handle" && hands.HeldSocketId(1) == "cart_handle",
                $"dos manos en el asa (manos {hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}, asa a {Vector3.Distance(Eye, handle.Position):0.00} m, apunta a {(hands.Candidate ? hands.Candidate.socketId : "nada")}, tapa: {Blocker(handle)}, jugador {Me}, asa {handle.Position})");
            Hold(Act.GrabLeft, true); Hold(Act.GrabRight, true);

            // Atasco: tirar recto hacia la vereda con el carrito girado (sin alejarse mas que el agarre)
            for (float t = 0; t < 3f; t += Time.deltaTime)
            {
                LookAt(handle.Position); float g = Vector3.Distance(Me, Flat(handle.Position));
                Hold(Act.Effort, g > 1.1f); if (g > 1.2f) LabInput.SimulatedMove = Vector2.zero; else MoveWorld(Vector3.back); yield return null;
            }
            Vector3 jam = Flat(cart.transform.position);
            Debug.Log($"[Stuck] carrito tras tirar 3 s: {jam} yaw={cart.transform.eulerAngles.y:0}; manos {hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}; jugador {Me}; contactos: {Contacts()}");
            bool jammed = jam.z > L.doorZ - 0.6f;
            if (!jammed) Debug.Log($"[Stuck] {pose}: tirando recto del asa el carrito cargado sale (no queda trabado)");

            float t0 = Time.time; int attempts = 0; string how = "";
            IEnumerator Regrab()
            {
                if (hands.HeldSocketId(0) == "cart_handle" && hands.HeldSocketId(1) == "cart_handle") yield break;
                Hold(Act.GrabLeft, false); Hold(Act.GrabRight, false); yield return null;
                yield return WalkTo(Flat(handle.Position) + Flat(handle.Position - cart.transform.position).normalized * 0.9f, 3f, handle.Position);
                yield return GrabWith(0, handle, 2f); yield return GrabWith(1, handle, 2f);
                Hold(Act.GrabLeft, true); Hold(Act.GrabRight, true);
            }
            IEnumerator Attempt(string label)
            {
                attempts++;
                yield return Regrab();
                // a) empujar hacia adentro (mirando el asa, caminando hacia el bar)
                Vector3 c0 = Flat(cart.transform.position), p0 = Me;
                bool HoldingCart() => hands.HeldSocketId(0) == "cart_handle" || hands.HeldSocketId(1) == "cart_handle";
                for (float t = 0; t < 2f && HoldingCart() && cart.transform.position.z < L.doorZ + 1.6f && Vector3.Distance(Flat(cart.transform.position), c0) < 1.0f; t += Time.deltaTime)
                { LookAt(handle.Position); MoveWorld(Vector3.forward); Hold(Act.Effort, true); yield return null; }
                Hold(Act.Effort, false);
                Debug.Log($"[Stuck] {label} {attempts} empujar: carrito {c0}->{Flat(cart.transform.position)} yaw={cart.transform.eulerAngles.y:0}, jugador {p0}->{Me}, manos {hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}; contactos: {Contacts()}");
                yield return Regrab();
                // b) ponerse sobre el eje de la puerta
                for (float t = 0; t < 1.5f && Mathf.Abs(Me.x - L.doorX) > 0.1f; t += Time.deltaTime)
                { LookAt(handle.Position); MoveWorld(new Vector3(L.doorX - Me.x, 0, 0)); yield return null; }
                // c) tirar derecho por el centro (sin alejarse mas que el agarre)
                c0 = Flat(cart.transform.position);
                for (float t = 0; t < 8f && HoldingCart() && Flat(cart.transform.position).z > L.doorZ - 1.6f; t += Time.deltaTime)
                {
                    LookAt(handle.Position);
                    float gap = Vector3.Distance(Me, Flat(handle.Position));
                    Hold(Act.Effort, gap > 1.0f);
                    if (gap > 1.15f) LabInput.SimulatedMove = Vector2.zero; else MoveWorld(new Vector3(L.doorX, 0, Me.z - 1f) - Me);
                    yield return null;
                }
                LabInput.SimulatedMove = Vector2.zero;
                Debug.Log($"[Stuck] {label} {attempts} tirar: carrito {c0}->{Flat(cart.transform.position)} yaw={cart.transform.eulerAngles.y:0}, jugador {Me}, manos {hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}; contactos: {Contacts()}");
            }
            bool Through() => Flat(cart.transform.position).z <= L.doorZ - 1.6f;

            if (!Through()) yield return Attempt("con el tio atado");
            if (Through() && !jammed) how = "tirando recto del asa";
            else if (Through()) how = $"empujar, alinear y tirar ({attempts} intento(s))";
            bool strappedThrough = Through() && cart.IsStrapped;
            if (!Through())
            {   // otra accion normal: soltar la correa (E), sacar el carrito vacio y despues pasar al tio a mano
                Hold(Act.GrabLeft, false); Hold(Act.GrabRight, false); yield return Tap(Act.ReleaseAll);
                yield return WalkTo(Flat(cart.transform.position) + Vector3.back * 1.4f, 3f, cart.transform.position);
                for (int i = 0; i < 10 && cart.IsStrapped; i++) { LookAt(cart.transform.position + Vector3.up * 0.6f); yield return Tap(Act.Interact); }
                Debug.Log($"[Stuck] suelta la correa (E): correa={cart.IsStrapped}, HUD '{hands.InteractLabel}'");
                yield return new WaitForSeconds(0.5f);
                for (int i = 0; i < 2 && !Through(); i++) yield return Attempt("carrito vacio");
                if (Through()) how = $"soltar la correa y sacar el carrito vacio ({attempts} intento(s))";
            }
            LabInput.SimulatedMove = Vector2.zero;
            bool through = Through();
            Debug.Log($"[Stuck] {pose} carrito atravesado (trabado={jammed}): {(through ? "SALE: " + how : "NO SALE")} en {Time.time - t0:0.0} s; correa={cart.IsStrapped}");
            Assert.IsTrue(through, $"el carrito no paso la puerta tras {attempts} intentos (en {Flat(cart.transform.position)})");
            Assert.IsTrue(strappedThrough, $"solo salio soltando la correa ({how}): un jugador solo deberia poder sacarlo cargado");
        }
    
        /// Medicion (no criterio): par y fuerza en el borde con que la puerta del bar se cierra sola desde 45 grados.
        [UnityTest]
        public IEnumerator Measure_BarDoorClosingForce()
        {
            var door = Object.FindAnyObjectByType<HingeJoint>(); var drb = door.GetComponent<Rigidbody>();
            foreach (var ang in new[] { 20f, 45f, 90f })
            {
                drb.rotation = Quaternion.Euler(0, ang, 0); door.transform.rotation = drb.rotation; drb.angularVelocity = Vector3.zero;
                yield return new WaitForFixedUpdate(); drb.angularVelocity = Vector3.zero;
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                float w = drb.angularVelocity.y, alpha = w / (2f * Time.fixedDeltaTime);
                float I = drb.inertiaTensor.y; // eje vertical (rotacion del tensor ~identidad para el panel)
                float torque = Mathf.Abs(I * alpha);
                Debug.Log($"[DoorMeasure] {ang:0} grados (angulo {door.angle:0.0}): par {torque:0.0} N*m, en el borde (1,46 m) {torque / 1.46f:0} N, en la manija (1,3 m) {torque / 1.3f:0} N; spring {door.spring.spring} masa {drb.mass}");
            }
        }
    }

    /// Mismas reproducciones en el laboratorio con el tio de cuerpo central (respaldo).
    public class StuckRecoveryTests_Lab : StuckRecoveryTests { protected override string Scene => "PhysicsLab"; }
}
