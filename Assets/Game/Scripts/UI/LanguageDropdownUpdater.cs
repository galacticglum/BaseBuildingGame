using UnityEngine;
using UnityEngine.UI;

public class LanguageDropdownUpdater : MonoBehaviour
{
    private void Start()
    {
        UpdateLanguageDropdown();
        LocalizationTable.CBLocalizationFilesChanged += UpdateLanguageDropdown;
    }

    public void SelectLanguage(int language)
    {
        string[] languages = LocalizationTable.GetLanguages();

        LocalizationTable.CurrentLanguage = languages[language];
        GameSettings.Set("localization", languages[language]);
    }

    private void UpdateLanguageDropdown()
    {
        Dropdown dropdown = GetComponent<Dropdown>();
        string[] languages = LocalizationTable.GetLanguages();

        dropdown.options.RemoveRange(0, dropdown.options.Count);
        foreach (string language in languages)
        {
            dropdown.options.Add(new Dropdown.OptionData(language));
        }

        for (int i = 0; i < languages.Length; i++)
        {
            if (languages[i] != LocalizationTable.CurrentLanguage) continue;

            dropdown.value = i + 1;
            dropdown.value = i;
        }

        dropdown.template.GetComponent<ScrollRect>().scrollSensitivity = dropdown.options.Count / 3.0f;
    }
}
