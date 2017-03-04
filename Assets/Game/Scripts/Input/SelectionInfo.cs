using System.Collections.Generic;

public class SelectionInfo
{
    public Tile Tile { get; private set; }

    public ISelectable Selection { get { return objectsInTile[selectedIndex]; } }
    public bool IsCharacterSelected
    {
        get
        {
            ISelectable selection = objectsInTile[selectedIndex];
            return selection is Character;
        }
    }

    private List<ISelectable> objectsInTile;
    private int selectedIndex;

    public SelectionInfo(Tile tile)
    {
        Tile = tile;

        Build();
        Select();
    }


    public void Build()
    {
        objectsInTile = new List<ISelectable>();
        foreach (Character character in Tile.Characters)
        {
            objectsInTile.Add(character);
        }

        objectsInTile.Add(Tile.Furniture);
        objectsInTile.Add(Tile.Inventory);
        objectsInTile.Add(Tile);
    }

    public void Select()
    {
        if (objectsInTile[selectedIndex] == null)
        {
            SelectNextStuff();
        }
    }

    public void SelectNextStuff()
    {
        do
        {
            selectedIndex = (selectedIndex + 1) % objectsInTile.Count;
        }
        while (objectsInTile[selectedIndex] == null);
    }
}
