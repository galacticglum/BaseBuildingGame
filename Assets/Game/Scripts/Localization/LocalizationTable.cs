using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class LocalizationTable
{
    public static string CurrentLanguage = defaultLanguage;
    public static bool Initialized { get; private set; }

    private const string defaultLanguage = "en_US";

    private static readonly Dictionary<string, Dictionary<string, string>> localizationTable = new Dictionary<string, Dictionary<string, string>>();
    private static readonly HashSet<string> missingKeysLogged = new HashSet<string>();

    public static event Action CBLocalizationFilesChanged;

    public static void LoadingLanguagesFinished()
    {
        Initialized = true;
        if (CBLocalizationFilesChanged != null)
        {
            CBLocalizationFilesChanged();
        }
    }

    public static string[] GetLanguages()
    {
        return localizationTable.Keys.ToArray();
    }

    public static string GetLocalization(string key, params object[] additionalValues)
    {
        return GetLocalization(key, LocalizationFallbackMode.ReturnDefaultLanguage, CurrentLanguage, additionalValues);
    }

    private static string GetLocalization(string key, LocalizationFallbackMode localizationFallbackMode, string language, params object[] additionalValues)
    {
        string value;
        if (localizationTable.ContainsKey(language) && localizationTable[language].TryGetValue(key, out value))
        {
            return string.Format(value, additionalValues);
        }

        if (!missingKeysLogged.Contains(key) && language != "en_US")
        {
            missingKeysLogged.Add(key);
            Debug.LogError(string.Format("LocalizationTable::GetLocalization: Translation for {0} in {1} language failed! Key not in dictionary", key, language));
        }

        switch (localizationFallbackMode)
        {
            case LocalizationFallbackMode.ReturnKey:
                return additionalValues != null && additionalValues.Length >= 1 ? key + " " + additionalValues[0] : key;
            case LocalizationFallbackMode.ReturnDefaultLanguage:
                return GetLocalization(key, LocalizationFallbackMode.ReturnKey, defaultLanguage, additionalValues);
            default:
                return string.Empty;
        }
    }

    public static void Load(string path)
    {
        string localizationCode = Path.GetFileNameWithoutExtension(path);
        Load(path, localizationCode);
    }

    private static void Load(string path, string localizationCode)
    {
        try
        {
            if (localizationTable.ContainsKey(localizationCode) == false)
            {
                localizationTable[localizationCode] = new Dictionary<string, string>();
            }

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] keyValuePair = line.Split(new[] { '=' }, 2);
                if (keyValuePair.Length != 2)
                {
                    Debug.LogError(string.Format("LocalizationTable::Load(string, string): Invalid format of localization string. {0}", line));
                    continue;
                }
                localizationTable[localizationCode][keyValuePair[0]] = keyValuePair[1];
            }
        }
        catch (FileNotFoundException)
        {
            Debug.LogError(string.Format("LocalizationTable::Load(string, string): There is no localization file for {0}", localizationCode));
        }
    }
}

