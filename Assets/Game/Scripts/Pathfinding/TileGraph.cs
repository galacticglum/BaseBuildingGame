using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TileGraph
{
    public Dictionary<Tile, Node<Tile>> Nodes { get; protected set; }

    public TileGraph(World world)
    {
        Nodes = new Dictionary<Tile, Node<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile = world.GetTileAt(x, y);
                Node<Tile> node = new Node<Tile> { Tile = tile };
                Nodes.Add(tile, node);
            }
        }

        foreach (Tile node in Nodes.Keys)
        {
            GenerateEdges(node);
        }
    }

    private void GenerateEdges(Tile tile)
    {
        Node<Tile> node = Nodes[tile];
        Tile[] neighbours = tile.GetNeighbours(true);

        node.Edges = (from neighbour in neighbours where neighbour != null && neighbour.MovementCost > 0 && !ClippingCorner(tile, neighbour)
            select new Edge<Tile>
            {
                Cost = neighbour.MovementCost, Node = Nodes[neighbour]
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

    private static bool ClippingCorner(Tile current, Tile neighbour)
    {
        int differenceX = current.X - neighbour.X;
        int differentY = current.Y - neighbour.Y;

        if ((Mathf.Abs(differenceX) + Mathf.Abs(differentY)) != 2) return false;

        if (World.Current.GetTileAt(current.X - differenceX, current.Y).MovementCost == 0)
        {
            return true;
        }

        return World.Current.GetTileAt(current.X, current.Y - differentY).MovementCost == 0;
    }
}
