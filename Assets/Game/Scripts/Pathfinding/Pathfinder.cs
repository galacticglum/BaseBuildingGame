using System;
using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;
using System.Linq;

// Mixed algorithm of dijkstra and A*
public class Pathfinder
{
    public int Length { get { return path == null ? 0 : path.Count; }}
    public Tile DestinationTile { get { return path == null || path.Count == 0 ? null : path.Last(); }}

    private Queue<Tile> path;

    public Pathfinder(Tile tileStart, string inventoryType, bool canTakeFromStockpile = false) { Pathfind(tileStart, null, inventoryType, canTakeFromStockpile); }
	public Pathfinder(Tile tileStart, Tile tileGoal) { Pathfind(tileStart, tileGoal, null, false); }

    private void Pathfind(Tile tileStart, Tile tileGoal, string inventoryType, bool canTakeFromStockpile)
    {
        if (World.Current.TileGraph == null)
        {
            World.Current.TileGraph = new TileGraph(World.Current);
        }

        Dictionary<Tile, Node<Tile>> nodes = World.Current.TileGraph.Nodes;
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Pathfinder::Pathfinder: The starting tile (param: Tile tileStart) isn't in the list of nodes!");
            return;
        }

        // Grab a copy of our start and goal node(s) from out dictionary
        Node<Tile> start = nodes[tileStart];
        Node<Tile> goal = null;
        if (tileGoal != null)
        {
            if (nodes.ContainsKey(tileGoal) == false)
            {
                Debug.LogError("Pathfinder::Pathfinder: The goal tile (param: Tile tileGoal) isn't in the list of nodes!");
                return;
            }

            goal = nodes[tileGoal];
        }

        List<Node<Tile>> closedSet = new List<Node<Tile>>();
        SimplePriorityQueue<Node<Tile>> openSet = new SimplePriorityQueue<Node<Tile>>();
        openSet.Enqueue(start, 0);

        Dictionary<Node<Tile>, Node<Tile>> cameFrom = new Dictionary<Node<Tile>, Node<Tile>>();

        Dictionary<Node<Tile>, float> gCost = new Dictionary<Node<Tile>, float>();
        Dictionary<Node<Tile>, float> fCost = new Dictionary<Node<Tile>, float>();
        foreach (Node<Tile> node in nodes.Values)
        {
            gCost[node] = Mathf.Infinity;
            fCost[node] = Mathf.Infinity;
        }

        gCost[start] = 0;
        fCost[start] = HeuristicCostEstimate(start, goal);

        while (openSet.Count > 0)
        {
            Node<Tile> current = openSet.Dequeue();

            if (goal != null)
            {
                if (current == goal)
                {
                    ConstructPath(cameFrom, current);
                    return;
                }
            }
            else 
            {
                if (current.Tile.Inventory != null && current.Tile.Inventory.Type == inventoryType)
                {
                    if (canTakeFromStockpile || current.Tile.Furniture == null || current.Tile.Furniture.IsStockpile() == false)
                    {
                        ConstructPath(cameFrom, current);
                        return;
                    }
                }
            }

            closedSet.Add(current);
            foreach (Edge<Tile> neighbouringEdge in current.Edges)
            {
                Node<Tile> neighbour = neighbouringEdge.Node;

                if (closedSet.Contains(neighbour))
                {
                    continue;
                }

                float tentativeGCost = gCost[current] + (neighbour.Tile.MovementCost * DistanceBetween(current, neighbour));
                if (openSet.Contains(neighbour) && tentativeGCost >= gCost[neighbour])
                {
                    continue;
                }

                cameFrom[neighbour] = current;
                gCost[neighbour] = tentativeGCost;
                fCost[neighbour] = gCost[neighbour] + HeuristicCostEstimate(neighbour, goal);

                if (openSet.Contains(neighbour) == false)
                {
                    openSet.Enqueue(neighbour, fCost[neighbour]);
                }
                else
                {
                    openSet.UpdatePriority(neighbour, fCost[neighbour]);
                }
            }
        }
    }

    private static float HeuristicCostEstimate(Node<Tile> start, Node<Tile> goal)
    {
        return goal == null ? 0f : Mathf.Sqrt(Mathf.Pow(start.Tile.X - goal.Tile.X, 2) + Mathf.Pow(start.Tile.Y - goal.Tile.Y, 2));
    }

    private static float DistanceBetween(Node<Tile> start, Node<Tile> goal)
    {
        if ((Mathf.Abs(start.Tile.X - goal.Tile.X) + Mathf.Abs(start.Tile.Y - goal.Tile.Y)) == 1)
        {
            return 1;
        }

        // Diagonal neighbours have a distance of 1.41421356237
        if (Mathf.Abs(start.Tile.X - goal.Tile.X) == 1 && Mathf.Abs(start.Tile.Y - goal.Tile.Y) == 1)
        {
            return 1.41421356237f;
        }

        // Otherwise do the actual math
        return Mathf.Sqrt(Mathf.Pow(start.Tile.X - goal.Tile.X, 2) + Mathf.Pow(start.Tile.Y - goal.Tile.Y, 2));
    }

    private void ConstructPath(IDictionary<Node<Tile>, Node<Tile>> cameFrom, Node<Tile> current)
    {
        // At this point current IS the goal.
        // What we want to do is walk backwards through the came from map, until we reach the "end" of the map (which will be our starting node).
        Queue<Tile> totalPath = new Queue<Tile>();
        totalPath.Enqueue(current.Tile);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Enqueue(current.Tile);
        }

        path = new Queue<Tile>(totalPath.Reverse());
    }

    public Tile Dequeue()
    {
        return path == null || path.Count == 0 ? null : path.Dequeue();
    }
}
