using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SelectionInfoInspector : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    private Text textComponent;
    private MouseController mouseController;

    // Use this for initialization
    private void Start ()
    {
        mouseController = FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("SelectionInspector::Start: No instance of class: 'MouseController' found!");
            enabled = false;
            return;
        }

        textComponent = GetComponent<Text>();
    }
	
	// Update is called once per frame
	private void Update ()
	{
	    if (mouseController.CurrentSelectionInfo == null)
	    {
	        canvasGroup.alpha = 0;
	        canvasGroup.interactable = false;
	        canvasGroup.blocksRaycasts = false;
	        return;
	    }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

	    ISelectable selection = mouseController.CurrentSelectionInfo.SelectionObjects[mouseController.CurrentSelectionInfo.SelectionIndex];

	    string additionalInfo = string.Empty;
	    if (selection.GetAdditionalInfo() != null)
	    {
	        additionalInfo = selection.GetAdditionalInfo().Aggregate(string.Empty, (current, info) => current + (info + "\n"));
        }

        textComponent.text = selection.GetName() + "\n" + selection.GetDescription() + "\n" + additionalInfo;
    }
}
