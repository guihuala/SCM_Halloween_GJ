using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCourier
{
    public enum CourierLanguage { English, SimplifiedChinese, TraditionalChinese }

    // Translate only at the presentation boundary: names and speaker IDs in the
    // simulation remain stable, even when switching in the middle of a shift.
    public static class CourierLocalization
    {
        public const string PreferenceKey = "Courier.Language";
        [Serializable] private sealed class Entry { public string en, zhHans, zhHant; public bool fragment; }
        [Serializable] private sealed class Catalog { public Entry[] entries; }
        private static Dictionary<string, Entry> entries;
        private static Regex fragments;
        private static KeyValuePair<Regex, Entry>[] templates;
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private static readonly Dictionary<Text, string> sources = new Dictionary<Text, string>();
        private static readonly Dictionary<Text, Func<string, string>> formatters = new Dictionary<Text, Func<string, string>>();
        private static CourierLanguage? language;
        public static event Action Changed;
        public static CourierLanguage Language
        {
            get
            {
                if (!language.HasValue)
                {
                    int saved = PlayerPrefs.GetInt(PreferenceKey, -1);
                    language = saved >= 0 && saved <= 2 ? (CourierLanguage)saved :
                        Application.systemLanguage == SystemLanguage.ChineseTraditional ? CourierLanguage.TraditionalChinese :
                        Application.systemLanguage == SystemLanguage.ChineseSimplified || Application.systemLanguage == SystemLanguage.Chinese ? CourierLanguage.SimplifiedChinese : CourierLanguage.English;
                }
                return language.Value;
            }
        }
        public static bool IsChinese => Language != CourierLanguage.English;
        public static string LanguageName => Language == CourierLanguage.English ? "English" : Language == CourierLanguage.SimplifiedChinese ? "简体中文" : "繁體中文";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            language = null; sources.Clear(); formatters.Clear(); cache.Clear(); Changed = null;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var dead in sources.Keys.Where(label => label == null).ToArray()) { sources.Remove(dead); formatters.Remove(dead); }
            foreach (var root in scene.GetRootGameObjects()) Apply(root);
        }
        private static void Load()
        {
            if (entries != null) return;
            var asset = Resources.Load<TextAsset>("NightVoyage/Localization/Chinese");
            if (asset == null) throw new InvalidOperationException("Chinese localization catalog is missing.");
            entries = JsonUtility.FromJson<Catalog>(asset.text).entries.ToDictionary(e => e.en, StringComparer.Ordinal);
            templates = entries.Values.Where(e => e.en.Contains("{0}"))
                .Select(e => new KeyValuePair<Regex, Entry>(new Regex("^" + Regex.Escape(e.en)
                    .Replace(Regex.Escape("{0}"), @"(?<value>\d+)") + "$", RegexOptions.CultureInvariant), e)).ToArray();
            // One pass, longest phrase first. Replacements never translate each other.
            fragments = new Regex(string.Join("|", entries.Values.Where(e => e.fragment)
                .OrderByDescending(e => e.en.Length).Select(e => Regex.Escape(e.en))), RegexOptions.CultureInvariant);
        }
        // Separators may also come from composed model strings, outside the catalog.
        private static string PresentChinese(string value) => IsChinese && !string.IsNullOrEmpty(value)
            ? Regex.Replace(value, @"[ \t]*·[ \t]*", " ") : value;
        private static string Value(Entry e) => Language == CourierLanguage.TraditionalChinese ? e.zhHant : e.zhHans;
        public static string T(string source)
        {
            if (string.IsNullOrEmpty(source) || !IsChinese) return source ?? "";
            Load();
            if (cache.TryGetValue(source, out var result)) return result;
            if (entries.TryGetValue(source, out var entry)) result = Value(entry);
            else
            {
                var template = templates.FirstOrDefault(pair => pair.Key.IsMatch(source));
                result = template.Key != null ? Value(template.Value).Replace("{0}", template.Key.Match(source).Groups["value"].Value)
                    : fragments.Replace(source, m => Value(entries[m.Value]));
            }
            result = PresentChinese(result);
            if (cache.Count > 2048) cache.Clear();
            cache[source] = result;
            return result;
        }
        public static void Set(Text label, string source)
        {
            if (label == null) return;
            sources[label] = source ?? "";
            formatters.Remove(label);
            label.font = CourierTypography.For(label);
            label.fontStyle = FontStyle.Normal;
            label.text = T(source);
        }
        // Keep raw text so open rich-text tooltips can switch back to English too.
        public static void SetFormatted(Text label, string source, Func<string, string> formatter)
        {
            if (label == null) return;
            sources[label] = source ?? "";
            formatters[label] = formatter;
            label.font = CourierTypography.For(label);
            label.fontStyle = FontStyle.Normal;
            label.text = PresentChinese(formatter(source ?? ""));
        }
        public static string Source(Text label) => label != null && sources.TryGetValue(label, out var source) ? source : (label != null ? label.text : "");
        public static void Apply(GameObject root)
        {
            foreach (var label in root.GetComponentsInChildren<Text>(true))
            {
                string original = Source(label);
                if (formatters.TryGetValue(label, out var formatter)) SetFormatted(label, original, formatter);
                else Set(label, original);
            }
        }
        public static void Cycle(int direction) => SetLanguage((CourierLanguage)(((int)Language + (direction < 0 ? 2 : 1)) % 3));
        public static void SetLanguage(CourierLanguage selected)
        {
            if (!Enum.IsDefined(typeof(CourierLanguage), selected)) throw new ArgumentOutOfRangeException(nameof(selected));
            language = selected; cache.Clear();
            PlayerPrefs.SetInt(PreferenceKey, (int)selected); PlayerPrefs.Save();
            foreach (var pair in sources.ToArray())
            {
                if (pair.Key == null) { sources.Remove(pair.Key); formatters.Remove(pair.Key); continue; }
                if (formatters.TryGetValue(pair.Key, out var formatter)) SetFormatted(pair.Key, pair.Value, formatter);
                else Set(pair.Key, pair.Value);
            }
            Changed?.Invoke();
            Canvas.ForceUpdateCanvases();
        }
    }
}
