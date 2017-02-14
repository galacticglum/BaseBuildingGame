using System;

public class ContextMenuAction
{
    public bool RequiresCharacterSelection { get; set; }
    public string Text { get; set; }
    
    public Action<ContextMenuAction, Character> Action;

    public void OnClick(MouseController mouseController)
    {
        if (Action == null) return;
        if (RequiresCharacterSelection)
        {
            if (!mouseController.IsCharacterSelected) return;

            ISelectable actualSelection = mouseController.CurrentSelectionInfo.Selection;
            Action(this, actualSelection as Character);
        }
        else
        {
            Action(this, null);
        }
    }
}