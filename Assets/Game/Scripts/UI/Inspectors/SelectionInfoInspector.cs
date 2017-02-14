using UnityEngine;
using UnityEngine.UI;

public class SelectionInfoInspector : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    private MouseController mouseController;
    private Text text;

    // Use this for initialization
    private void Start()
    {
        mouseController = WorldController.Instance.MouseController;
        text = GetComponent<Text>();
    }
	
    // Update is called once per frame
    private void Update()
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

        ISelectable selection = mouseController.CurrentSelectionInfo.Selection;
        if(selection.GetType() == typeof(Character))
        {
            text.text = selection.GetName() + "\n" + selection.GetDescription() + "\n" + selection.GetHitPointString() + "\n" + 
                LocalizationTable.GetLocalization(selection.GetJobDescription()); 
        }
        else
        {
            text.text = LocalizationTable.GetLocalization(selection.GetName()) + "\n" + 
                LocalizationTable.GetLocalization(selection.GetDescription()) + "\n" + selection.GetHitPointString(); 
        }
    }
}
