using UnityEngine;

public struct PotentialStock
{
    public string Type { get; set; }

    public int MinimumQuantity { get; set; }
    public int MaximumQuantity { get; set; }

    [Range(0, 1)]
    public float Rarity;
}