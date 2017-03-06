using UnityEngine;

[ExecuteInEditMode]
public class AutomaticVerticalSize : MonoBehaviour
{
    public float childHeight = 35f;

    private void Start()
    {
        CalculateSize();
    }

    private void Update()
    {
        CalculateSize();
    }

    public void CalculateSize()
    {
        Vector2 size = GetComponent<RectTransform>().sizeDelta;
        size.y = transform.childCount * childHeight;

        GetComponent<RectTransform>().sizeDelta = size;
    }
}
