using UnityEngine;
using System.Collections.Generic;

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

        foreach (Tile tile in Nodes.Keys)
        {
            Node<Tile> node = Nodes[tile];
            List<Edge<Tile>> edges = new List<Edge<Tile>>();

            // Get a list of neighbours for the tile
            Tile[] neighbours = tile.GetNeighbours(true);
            // If the neighbour is walkable, create an edge to the relevant node.
            foreach (Tile neighbour in neighbours)
            {
                if (neighbour == null || !(neighbour.MovementCost > 0)) continue;
                // This neighbour exists and is walkable, so create an edge
                // But first, make sure we aren't clipping a diagonal or trying to squeeze inappropriately
                if (ClippingCorner(tile, neighbour))
                {
                    // Skip to the next neighbour with building an edge
                    continue;
                }

                Edge<Tile> edge = new Edge<Tile>
                {
                    Cost = neighbour.MovementCost,
                    Node = Nodes[neighbour]
                };

                // Add the edge to our edge list (which is converted to an array)
                edges.Add(edge);
            }
            node.Edges = edges.ToArray();
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