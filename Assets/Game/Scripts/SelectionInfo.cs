// This is a temporary struct, it will be replaced with an interface later.
public class SelectionInfo
{
    public Tile Tile { get; set; }

    public ISelectable[] SelectionObjects { get; set; }
    public int SelectionIndex { get; set; }
}

