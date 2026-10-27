using UnityEngine;

public static class AppConfigProvider
{
    private const string AppConfigPath = "Data/AppConfig";

    private static AppConfig _config;

    public static AppConfig Config
    {
        get
        {
            if (_config == null)
            {
                _config = Resources.Load<AppConfig>(AppConfigPath);
            }

            return _config;
        }
    }

    public static string DefaultLanguage
    {
        get
        {
            AppConfig config = Config;
            return config != null && !string.IsNullOrEmpty(config.defaultLanguage)
                ? config.defaultLanguage
                : "zh-CN";
        }
    }
}
