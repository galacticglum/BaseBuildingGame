public class ContextMenuAction
{
    public bool RequiresCharacterSelection { get; set; }
    public string Text { get; set; }

    public event ContextMenuEventHandler Action;
    public void OnClick(MouseController mouseController)
    {
        if (Action == null) return;
        if (RequiresCharacterSelection)
        {
            if (!mouseController.IsCharacterSelected) return;

            ISelectable actualSelection = mouseController.CurrentSelectionInfo.Selection;
            Action(this, new ContextMenuEventArgs(this, (Character)actualSelection));
        }
        else
        {
            Action(this, new ContextMenuEventArgs(this, null));
        }
    }
}