using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextScaler : MonoBehaviour
{
    private static readonly List<TextScaler> instances = new List<TextScaler>();

    [SerializeField]
    private RectTransform rectTransform;

    private float originalWidth;
    private Text text;

    private void Start()
    {
        instances.Add(this);

        text = GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();

        StartCoroutine(LateStart()); 
    }

    private IEnumerator LateStart()
    {
        yield return null;
        originalWidth = transform.parent.GetComponent<RectTransform>().rect.width;

        DoScale();
    }

    public void DoScale()
    {
        rectTransform.localScale = new Vector3(1, 1, 1);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth);

        float stringWidth = text.preferredWidth;
        float maxWidth = originalWidth;
        float capacityPercent = (stringWidth / (maxWidth / 100));

        if (capacityPercent <= 95) return;

        float scale = 0.9f - (capacityPercent - 100) / 170;

        rectTransform.localScale = new Vector3(scale, scale, 1);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth / scale);
    }

    public static void Scale()
    {
        foreach (TextScaler textScaling in instances)
        {
            textScaling.DoScale();
        }
    }
}