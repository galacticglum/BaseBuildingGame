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
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }
	// Update is called once per frame
	private void Update ()
	{
	    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, transform.childCount * childHeight);
	}
}
