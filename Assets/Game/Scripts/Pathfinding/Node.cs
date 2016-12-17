public class Node<T>
{
    public T Tile { get; set; }
    // Nodes leading OUT from this node.
    public Edge<T>[] Edges { get; set; } 
}
