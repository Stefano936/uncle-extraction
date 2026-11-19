using System;
using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Textos localizados EN / es_UY (kit de UI: data/localization_en_es.json, copiado sin cambios a
    /// Resources/Localization/ui_strings.json; mas los textos propios del juego en game_strings.json con el mismo formato).
    /// Los mensajes que viajan por red (avisos de interaccion, motivos de bloqueo) se envian como CLAVE con parametros
    /// ("clave|nombre=valor|..."): cada jugador los traduce a su idioma al mostrarlos.
    public static class Loc
    {
        public const string En = "en", EsUY = "es_UY";
        [Serializable] class Entry { public string key, en, es_UY; }
        [Serializable] class Catalog { public List<Entry> strings = new List<Entry>(); }

        static Dictionary<string, Entry> table;
        static string lang;
        public static event Action Changed;

        public static string Language { get { Ensure(); return lang; } }

        static void Ensure()
        {
            if (table != null) return;
            table = new Dictionary<string, Entry>();
            foreach (var res in new[] { "Localization/ui_strings", "Localization/game_strings" })
            {
                var ta = Resources.Load<TextAsset>(res);
                if (!ta) { Debug.LogWarning($"[Loc] falta {res}"); continue; }
                try { foreach (var e in JsonUtility.FromJson<Catalog>(ta.text).strings) if (!string.IsNullOrEmpty(e.key)) table[e.key] = e; }
                catch (Exception ex) { Debug.LogWarning($"[Loc] {res} no se pudo leer: {ex.Message}"); }
            }
            string saved = "";
            try { saved = PlayerPrefs.GetString("ue.lang", ""); } catch { }
            lang = saved == En || saved == EsUY ? saved : Application.systemLanguage == SystemLanguage.Spanish ? EsUY : En;
            // pruebas: -ueLang en|es_UY fuerza el idioma sin guardarlo
            var a = QaArgs.All;
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ueLang" && (a[i + 1] == En || a[i + 1] == EsUY)) lang = a[i + 1];
        }

        public static bool Has(string key) { Ensure(); return table.ContainsKey(key); }

        /// Texto de una clave en el idioma actual (si falta la traduccion, el otro idioma; si falta la clave, la clave).
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            Ensure();
            if (!table.TryGetValue(key, out var e)) return key;
            string s = lang == EsUY ? e.es_UY : e.en;
            return string.IsNullOrEmpty(s) ? (string.IsNullOrEmpty(e.en) ? key : e.en) : s;
        }

        /// Clave con parametros: reemplaza {nombre} por el valor (los valores que son claves tambien se traducen).
        public static string T(string key, params (string name, string value)[] args)
        {
            string s = T(key);
            foreach (var (n, v) in args) s = s.Replace("{" + n + "}", Has(v) ? T(v) : v);
            return s;
        }

        /// Mensaje codificado "clave|nombre=valor|..." (o texto suelto, que se devuelve tal cual).
        public static string Coded(string coded)
        {
            if (string.IsNullOrEmpty(coded)) return "";
            var parts = coded.Split('|');
            if (!Has(parts[0])) return coded;
            var args = new List<(string, string)>();
            for (int i = 1; i < parts.Length; i++) { int eq = parts[i].IndexOf('='); if (eq > 0) args.Add((parts[i].Substring(0, eq), Unescape(parts[i].Substring(eq + 1)))); }
            return T(parts[0], args.ToArray());
        }

        /// Arma un mensaje codificado para enviar por red.
        public static string Code(string key, params (string name, string value)[] args)
        {
            var sb = new System.Text.StringBuilder(key);
            foreach (var (n, v) in args) sb.Append('|').Append(n).Append('=').Append(Escape(v));
            return sb.ToString();
        }
        /// Los valores (nombres de jugador) pueden traer «|» o «=»: se escapan para que no rompan el mensaje (sesion 17).
        public static string Escape(string v) => (v ?? "").Replace("\\", "\\\\").Replace("|", "\\p").Replace("=", "\\e");
        public static string Unescape(string v)
        {
            if (string.IsNullOrEmpty(v) || v.IndexOf('\\') < 0) return v;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < v.Length; i++)
            {
                if (v[i] == '\\' && i + 1 < v.Length) { char c = v[++i]; sb.Append(c == 'p' ? '|' : c == 'e' ? '=' : c); }
                else sb.Append(v[i]);
            }
            return sb.ToString();
        }

        public static void SetLanguage(string l)
        {
            Ensure();
            if (l != En && l != EsUY) return;
            lang = l;
            try { PlayerPrefs.SetString("ue.lang", l); PlayerPrefs.Save(); } catch { }
            Changed?.Invoke();
        }

        /// Recarga los catalogos (pruebas).
        public static void Reload() { table = null; Ensure(); }
    }
}
