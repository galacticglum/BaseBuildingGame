using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Move me to UnityUtilities assembly.

[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class AutomaticVerticalSize : MonoBehaviour
{
    [SerializeField]
    private float childHeight = 35f;
    [SerializeField]
    private bool ignoreInactiveObjects = false;

    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

	private void Update ()
	{
        Recalculate();
    }

    public void Recalculate()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        int childCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (ignoreInactiveObjects && transform.GetChild(i).gameObject.activeInHierarchy)
            {
                childCount++;
            }
        }

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, (childCount == 0 ? transform.childCount : childCount) * childHeight);
    }
}
