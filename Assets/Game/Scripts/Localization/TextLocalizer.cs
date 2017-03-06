using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Localization/Text Localizer")]
[RequireComponent(typeof(Text))]
public class TextLocalizer : MonoBehaviour
{
    public Text Text { get; set; }
    public string DefaultText { get; set; }

    [SerializeField]
    private string[] formatValues;
    public string[] FormatValues
    {
        get { return formatValues; }
        set { formatValues = value; }
    }

    [SerializeField]
    private bool localizeAtStart = true;
    private string lastLanguage;

    private void Awake()
    {
        Text = GetComponent<Text>();
        DefaultText = Text.text;
    }

    private void Start()
    {
        lastLanguage = LocalizationTable.CurrentLanguage;
        LocalizationTable.CBLocalizationFilesChanged += UpdateText;

        if (localizeAtStart)
        {
            UpdateText(FormatValues);
        }
    }

    private void Update()
    {
        if (lastLanguage == LocalizationTable.CurrentLanguage) return;

        lastLanguage = LocalizationTable.CurrentLanguage;
        UpdateText(FormatValues);

        TextScaler.Scale();
    }

    public void UpdateText()
    {
        Text.text = LocalizationTable.GetLocalization(DefaultText, FormatValues);
    }

    public void UpdateText(params string[] values)
    {
        lastLanguage = LocalizationTable.CurrentLanguage;
        FormatValues = values;
        Text.text = LocalizationTable.GetLocalization(DefaultText, values);
    }

    public void UpdateText(string text, params string[] values)
    {
        lastLanguage = LocalizationTable.CurrentLanguage;
        FormatValues = values;
        Text.text = LocalizationTable.GetLocalization(text, values);
    }
}
