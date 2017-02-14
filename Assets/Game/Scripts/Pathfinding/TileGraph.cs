using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TileGraph
{
    public Dictionary<Tile, Node<Tile>> Nodes { get; private set; }

    public TileGraph()
    {
        Nodes = new Dictionary<Tile, Node<Tile>>();
        for (int x = 0; x < World.Current.Width; x++)
        {
            for (int y = 0; y < World.Current.Height; y++)
            {

                Tile tileAt = World.Current.GetTileAt(x, y);
                Node<Tile> n = new Node<Tile>
                {
                    Data = tileAt
                };

                Nodes.Add(tileAt, n);
            }
        }

        foreach (Tile tile in Nodes.Keys)
        {
            GenerateEdges(tile);
        }
    }

    private void GenerateEdges(Tile tile)
    {
        Node<Tile> node = Nodes[tile];
        Tile[] neighbours = tile.GetNeighbours(true);

        node.Edges = (from neigbour in neighbours where neigbour != null && neigbour.MovementCost > 0 && !IsClippingCorner(tile, neigbour)
            select new Edge<Tile>
            {
                Cost = neigbour.MovementCost, Node = Nodes[neigbour]
            }).ToArray();
    }

    public void Regenerate(Tile tile)
    {
        GenerateEdges(tile);
        foreach (Tile neighbour in tile.GetNeighbours(true))
        {
            GenerateEdges(neighbour);
        }
    }

    private static bool IsClippingCorner(Tile curr, Tile neigh)
    {
        int dX = curr.X - neigh.X;
        int dY = curr.Y - neigh.Y;

        if (Mathf.Abs(dX) + Mathf.Abs(dY) != 2) return false;
        if (World.Current.GetTileAt(curr.X - dX, curr.Y).MovementCost == 0) return true; 

        return World.Current.GetTileAt(curr.X, curr.Y - dY).MovementCost == 0;
    }

}
