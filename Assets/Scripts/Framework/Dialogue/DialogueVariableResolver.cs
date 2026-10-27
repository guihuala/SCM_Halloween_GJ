using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class DialogueVariableResolver
{
    private static readonly Regex VariablePattern = new Regex(@"\{([A-Za-z0-9_.-]+)\}", RegexOptions.Compiled);
    private static readonly Dictionary<string, Func<string>> Providers = new Dictionary<string, Func<string>>();

    public static void Set(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        Providers[key] = () => value ?? string.Empty;
    }

    public static void SetProvider(string key, Func<string> provider)
    {
        if (string.IsNullOrEmpty(key) || provider == null) return;
        Providers[key] = provider;
    }

    public static void Remove(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        Providers.Remove(key);
    }

    public static string Resolve(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return VariablePattern.Replace(text, match =>
        {
            string key = match.Groups[1].Value;
            return Providers.TryGetValue(key, out Func<string> provider) ? provider() : match.Value;
        });
    }
}
