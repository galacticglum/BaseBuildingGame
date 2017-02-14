using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Pathfinder
{
    public int Length { get { return path == null ? 0 : path.Count; } }
    public Tile DestinationTile { get { return path == null || path.Count == 0 ? null : path.Last(); } }

    private Queue<Tile> path;

    public Pathfinder(Queue<Tile> path)
    {
        if (path == null || !path.Any()) { return; }
        this.path = path;
    }

    public Pathfinder(World world, Tile tileStart, Tile tileEnd, string objectType = null, int desiredAmount = 0, bool canTakeFromStockpile = false, bool lookingForFurn = false)
    {
        if (world.TileGraph == null)
        {
            world.TileGraph = new TileGraph();
        }

        Dictionary<Tile, Node<Tile>> nodes = world.TileGraph.Nodes;
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Pathfinder::Pathfinder: The starting tile (param: Tile tileStart) isn't in the list of nodes!");
            return;
        }


        Node<Tile> start = nodes[tileStart];
        Node<Tile> goal = null;
        if (tileEnd != null)
        {
            if (nodes.ContainsKey(tileEnd) == false)
            {
                Debug.LogError("Pathfinder::Pathfinder: The goal tile (param: Tile tileGoal) isn't in the list of nodes!");
                return;
            }

            goal = nodes[tileEnd];
        }


        HashSet<Node<Tile>> closedSet = new HashSet<Node<Tile>>();
        PathfindingPriorityQueue<Node<Tile>> openSet = new PathfindingPriorityQueue<Node<Tile>>();
        openSet.Enqueue(start, 0);

        Dictionary<Node<Tile>, Node<Tile>> cameFrom = new Dictionary<Node<Tile>, Node<Tile>>();
        Dictionary<Node<Tile>, float> gCost = new Dictionary<Node<Tile>, float>();
        Dictionary<Node<Tile>, float> fCost = new Dictionary<Node<Tile>, float>();

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
                if (current.Data.Inventory != null && current.Data.Inventory.Type == objectType && lookingForFurn == false && current.Data.Inventory.Locked == false)
                {
                    if (canTakeFromStockpile || current.Data.Furniture == null || current.Data.Furniture.IsStockpile() == false)
                    {
                        ConstructPath(cameFrom, current);
                        return;
                    }
                }

                if (current.Data.Furniture != null && current.Data.Furniture.Type == objectType && lookingForFurn)
                {
                    ConstructPath(cameFrom, current);
                    return;
                }
            }

            closedSet.Add(current);
            foreach (Edge<Tile> neighbouringEdge in current.Edges)
            {
                Node<Tile> neighbor = neighbouringEdge.Node;
                if (closedSet.Contains(neighbor)) continue; 

                float movementCostToNeighbor = neighbor.Data.MovementCost * DistanceBetween(current, neighbor);
                float tentativeGScore = gCost[current] + movementCostToNeighbor;

                if (openSet.Contains(neighbor) && tentativeGScore >= gCost[neighbor]) continue;

                cameFrom[neighbor] = current;
                gCost[neighbor] = tentativeGScore;
                fCost[neighbor] = gCost[neighbor] + HeuristicCostEstimate(neighbor, goal);

                openSet.EnqueueOrUpdate(neighbor, fCost[neighbor]);
            } 
        } 
    }

    private static float HeuristicCostEstimate(Node<Tile> a, Node<Tile> b)
    {
        if (b == null)
        {
            return 0f;
        }

        return Mathf.Sqrt(Mathf.Pow(a.Data.X - b.Data.X, 2) + Mathf.Pow(a.Data.Y - b.Data.Y, 2));
    }

    private static float DistanceBetween(Node<Tile> a, Node<Tile> b)
    {
        if (Mathf.Abs(a.Data.X - b.Data.X) + Mathf.Abs(a.Data.Y - b.Data.Y) == 1)
        {
            return 1f;
        }

        // Diagonal neighbours have a distance of 1.41421356237
        if (Mathf.Abs(a.Data.X - b.Data.X) == 1 && Mathf.Abs(a.Data.Y - b.Data.Y) == 1)
        {
            return 1.41421356237f;
        }

        // Otherwise, do the actual math.
        return Mathf.Sqrt(Mathf.Pow(a.Data.X - b.Data.X, 2) + Mathf.Pow(a.Data.Y - b.Data.Y, 2));
    }

    private void ConstructPath(IDictionary<Node<Tile>, Node<Tile>> cameFrom, Node<Tile> current)
    {
        // At this point Current IS the goal.
        // What we want to do is walk backwards through the came from map, 
        // until we reach the "end" of the map (which will be our starting node).
        Queue<Tile> totalPath = new Queue<Tile>();
        totalPath.Enqueue(current.Data); 

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Enqueue(current.Data);
        }

        path = new Queue<Tile>(totalPath.Reverse());
    }

    public Tile Dequeue()
    {
        return path == null || path.Count <= 0 ? null : path.Dequeue();
    }

    public IEnumerable<Tile> Reverse()
    {
        return path == null ? null : path.Reverse();
    }

    public List<Tile> ToList()
    {
        return path.ToList();
    }
}
