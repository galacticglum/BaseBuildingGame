using UnityEngine;
using UnityEngine.UI;

public struct CursorTextBox
{
    public GameObject GameObject { get; private set; }
    public Text Text { get; private set; }
    public RectTransform Transform { get; private set; }

    public CursorTextBox(GameObject parentObject, TextAnchor textAlignment, GUIStyle style, Vector3 localPosition, Vector2 size) : this()
    {
        GameObject = new GameObject("Cursor-Text");
        GameObject.transform.SetParent(parentObject.transform);
        GameObject.transform.localPosition = localPosition;

        Text = GameObject.AddComponent<Text>();
        Text.alignment = textAlignment;
        Text.font = style.font;
        Text.fontSize = style.fontSize;

        Outline outline = GameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        Transform = GameObject.GetComponentInChildren<RectTransform>();
        Transform.sizeDelta = size;
    }
}