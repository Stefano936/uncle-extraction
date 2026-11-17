using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace UncleExtraction
{
    /// Proveedor de internet (especificacion 07: listen server + Relay; M4). Solo prepara el transporte:
    /// el anfitrion sigue siendo autoritativo, con la misma aprobacion de conexion, sala y reglas que la conexion directa.
    /// - Anfitrion: inicia sesion anonima, crea una asignacion para MaxPlayers-1 invitados y obtiene el codigo de sala.
    /// - Invitado: se une con el codigo. El transporte usa DTLS a traves del servidor Relay (sin abrir puertos).
    /// Necesita el proyecto vinculado a Unity Cloud con Relay habilitado (Application.cloudProjectId). Sin eso devuelve
    /// "error.online_unlinked" sin intentar la red: no hay credenciales ficticias.
    public static class RelayConnector
    {
        public const string Protocol = "dtls";
        const int TimeoutMs = 15000;
        static Task initTask;

        /// Perfil de autenticacion: separa la identidad anonima de varias instancias en la misma PC (-ueProfile nombre).
        public static string Profile = "";

        public static bool ProjectLinked => !string.IsNullOrEmpty(Application.cloudProjectId);

        /// Normaliza un codigo escrito por una persona (espacios, minusculas). Devuelve "" si no tiene forma de codigo.
        public static string NormalizeCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in code.Trim().ToUpperInvariant()) { if (c == ' ' || c == '-') continue; sb.Append(c); }
            string s = sb.ToString();
            if (s.Length < 6 || s.Length > 12) return "";
            foreach (char c in s) if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return "";
            return s;
        }

        /// Motivo localizable de un fallo de los servicios (pura; probada en EditMode).
        public static string MapError(Exception e)
        {
            if (e is TimeoutException) return "error.timeout";
            if (e is RelayServiceException r)
            {
                if (r.Reason == RelayExceptionReason.JoinCodeNotFound || r.Reason == RelayExceptionReason.InvalidArgument || r.Reason == RelayExceptionReason.InvalidRequest) return "error.invalid";
                if (r.Reason == RelayExceptionReason.AllocationNotFound) return "error.host_left";
                if (r.Reason == RelayExceptionReason.InactiveProject) return "error.online_unlinked";
            }
            if (e is AuthenticationException || e is RequestFailedException || e is ServicesInitializationException) return "error.service";
            return "error.service";
        }

        static async Task WithTimeout(Task t)
        {
            if (await Task.WhenAny(t, Task.Delay(TimeoutMs)) != t) throw new TimeoutException();
            await t;
        }
        static async Task<T> WithTimeout<T>(Task<T> t)
        {
            if (await Task.WhenAny(t, Task.Delay(TimeoutMs)) != t) throw new TimeoutException();
            return await t;
        }

        static async Task EnsureSignedIn()
        {
            if (initTask == null || initTask.IsFaulted || initTask.IsCanceled)
            {
                var opt = new InitializationOptions();
                if (!string.IsNullOrEmpty(Profile)) opt.SetProfile(Profile);
                initTask = UnityServices.InitializeAsync(opt);
            }
            await WithTimeout(initTask);
            if (!AuthenticationService.Instance.IsSignedIn) await WithTimeout(AuthenticationService.Instance.SignInAnonymouslyAsync());
        }

        public struct Result { public bool ok; public string code; public string error; public string region; }

        /// Anfitrion: crea la asignacion y deja el transporte listo para StartHost.
        public static async Task<Result> Host(UnityTransport utp, int maxGuests)
        {
            if (!ProjectLinked) return new Result { error = "error.online_unlinked" };
            try
            {
                await EnsureSignedIn();
                Allocation a = await WithTimeout(RelayService.Instance.CreateAllocationAsync(maxGuests));
                string code = await WithTimeout(RelayService.Instance.GetJoinCodeAsync(a.AllocationId));
                utp.SetRelayServerData(a.ToRelayServerData(Protocol));
                Debug.Log($"[Relay] asignacion creada region={a.Region} codigo={code} invitados={maxGuests}");
                return new Result { ok = true, code = code, region = a.Region };
            }
            catch (Exception e) { Debug.LogWarning($"[Relay] fallo al crear la sala: {e.GetType().Name}: {e.Message}"); return new Result { error = MapError(e) }; }
        }

        /// Invitado: se une con el codigo y deja el transporte listo para StartClient.
        public static async Task<Result> Join(UnityTransport utp, string rawCode)
        {
            string code = NormalizeCode(rawCode);
            if (code == "") return new Result { error = "error.invalid" };
            if (!ProjectLinked) return new Result { error = "error.online_unlinked" };
            try
            {
                await EnsureSignedIn();
                JoinAllocation j = await WithTimeout(RelayService.Instance.JoinAllocationAsync(code));
                utp.SetRelayServerData(j.ToRelayServerData(Protocol));
                Debug.Log($"[Relay] unido con codigo={code} region={j.Region}");
                return new Result { ok = true, code = code, region = j.Region };
            }
            catch (Exception e) { Debug.LogWarning($"[Relay] fallo al unirse: {e.GetType().Name}: {e.Message}"); return new Result { error = MapError(e) }; }
        }
    }
}
