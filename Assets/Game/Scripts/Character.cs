using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

public class Character : IXmlSerializable
{
    public float X
    {
        get
        {
            return Mathf.Lerp(CurrentTile.X, nextTile.X, movementInterpolation);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(CurrentTile.Y, nextTile.Y, movementInterpolation);
        }
    }

    public Tile CurrentTile { get; protected set; }
    public event CharacterChangedEventHandler CharacterChanged;
    public void OnCharacterChanged(CharacterChangedEventArgs args)
    {
        CharacterChangedEventHandler characterChanged = CharacterChanged;
        if (characterChanged != null)
        {
            characterChanged(this, args);
        }
    }

    private Tile _destinationTile;
    private Tile destinationTile
    {
        get { return _destinationTile; }
        set
        {
            if (_destinationTile == value) return;

            _destinationTile = value;
            pathfinder = null; // Invalidate pathfinding.
        }
    }

    private Tile nextTile; // Next tile in the pathfinding sequence
    private Pathfinder pathfinder;

    private float movementInterpolation; // Goes from 0 to 1 as we move from current tile to destination tile
    private const float Speed = 5f; // Tiles per second 

    private Job job;
    public Inventory inventory { get; set; } // Current item we are carrying

    public Character() { }
    public Character(Tile tile)
    {
        CurrentTile = destinationTile = nextTile = tile;
    }

    public void Update(float deltaTime)
    {
        DoJob(deltaTime);
        DoMovement(deltaTime);
        OnCharacterChanged(new CharacterChangedEventArgs(this));
    }

    private void DoJob(float deltaTime)
    {
        if (job == null)
        {
            GetNewJob();
            if (job == null)
            {
                // There was no job available for us, just return.
                destinationTile = CurrentTile;
                return;
            }
        }

        // We have a reachable job!
        if (job.HasAllMaterials() == false)
        {
            if (inventory != null)
            {
                if (job.GetRequiredInventoryAmount(inventory) > 0)
                {
                    if (CurrentTile == job.Tile)
                    {
                        // At the job tile.
                        CurrentTile.World.InventoryManager.PlaceInventory(job, inventory);
                        if (inventory.StackSize == 0)
                        {
                            inventory = null;
                        }
                        else
                        {
                            Debug.LogError(
                                "Character::DoJob: Character is still carrying inventory; the character shouldn't be carrying any inventory!");
                            inventory = null;
                        }
                    }
                    else
                    {
                        // We still need to walk to the job tile.
                        destinationTile = job.Tile;
                        return;
                    }
                }
                else
                {
                    // We are carrying an item that the job doesn't want!
                    // Dump the inventory onto the ground or wherever closest.
                    // TODO: Pathfind nearest empty tile to dump item(s).
                    if (CurrentTile.World.InventoryManager.PlaceInventory(CurrentTile, inventory) == false)
                    {
                        Debug.LogError("Character::DoJob: Tried to 'dump' inventory onto an invalid tile.");
                        // FIXME: We are "leaking" inventory by setting the reference to null.
                        inventory = null;
                    }
                }
            }
            else
            {
                // FIXME: This is unoptimal implementation
                // The job still requires inventory but we don't have it!
                // Are we standing on a tile with inventory that is required by the job?
                int amount = job.GetRequiredInventoryAmount(CurrentTile.Inventory);
                if (CurrentTile.Inventory != null && amount > 0)
                {
                    // Pick up the inventory on 'CurrentTile'.
                    CurrentTile.World.InventoryManager.PlaceInventory(this, CurrentTile.Inventory, amount);
                }
                else
                {
                    // Find the first inventory in the job that hasn't been satisfied yet.
                    Inventory requiredInventory = job.GetFirstRequiredInventory();

                    int requiredInventoryAmount = requiredInventory.MaxStackSize - requiredInventory.StackSize;
                    Inventory closestInventory = CurrentTile.World.InventoryManager.GetClosestInventoryOfType(requiredInventory.Type, CurrentTile, requiredInventoryAmount);

                    if (closestInventory == null)
                    {
                        Debug.Log("Character::DoJob: No tile contains inventory of type: '" + requiredInventory.Type + "' to satisfy job.");
                        AbandonJob();
                        return;
                    }

                    destinationTile = closestInventory.Tile;
                    return;
                }
            }

            return;
        }

        destinationTile = job.Tile;
        if (CurrentTile == job.Tile)
        {
            job.DoWork(deltaTime);
        }
    }

    private void GetNewJob()
    {
        job = CurrentTile.World.JobQueue.Dequeue();
        if (job == null) return;

        destinationTile = job.Tile;
        job.JobComplete += JobEnded;
        job.JobCancel += JobEnded;

        // Check to see if job tile is reachable.
        pathfinder = new Pathfinder(CurrentTile.World, CurrentTile, destinationTile);
        if (pathfinder.Length != 0) return;

        Debug.LogError("Character::DoJob: Pathfinder returned no path to destination");
        AbandonJob();
        destinationTile = CurrentTile;
    }

    private void DoMovement(float deltaTime)
    {
        if(CurrentTile == destinationTile)
        {
            pathfinder = null;
            return;
        }

        if(nextTile == null || nextTile == CurrentTile)
        {
            if(pathfinder == null || pathfinder.Length == 0)
            {
                pathfinder = new Pathfinder(CurrentTile.World, CurrentTile, destinationTile);
                if(pathfinder.Length == 0)
                {
                    Debug.LogError("Character::DoMovement: Pathfinder returned no path to destination");
                    AbandonJob();
                    return;
                }

                // Ignore first tile because that's the tile we're currently in
                nextTile = pathfinder.Dequeue();
            }

            nextTile = pathfinder.Dequeue();
            if (nextTile == CurrentTile)
            {
                Debug.LogError("Character::DoMovement: nextTile is currentTile?");
            }
        }

        switch (nextTile.TryEnter())
        {
            case TileEnterability.Never:
            {
                Debug.LogError("Character::DoMovement: A character attempted to walk through an unwalkable tile.");
                nextTile = null;
                pathfinder = null;
                return;
            }
            case TileEnterability.Soon:
            {
                return;
            }
        }

        float distance = Mathf.Sqrt(Mathf.Pow(CurrentTile.X - nextTile.X, 2) + Mathf.Pow(CurrentTile.Y - nextTile.Y, 2));
        float currentDistance = Speed / nextTile.MovementCost * deltaTime;
        float interpolationThisFrame = currentDistance / distance;

        movementInterpolation += interpolationThisFrame;
        if (!(movementInterpolation >= 1)) return;
        // TODO: Get the next tile from the pathfinding system

        CurrentTile = nextTile;
        movementInterpolation = 0;
    }

    public void AbandonJob()
    {
        nextTile = destinationTile = CurrentTile;
        CurrentTile.World.JobQueue.Enqueue(job);
        job = null;
    }

    public void SetDestination(Tile tile)
    {
        if (CurrentTile.IsNeighbour(tile, true) == false)
        {
            Debug.Log("Character::SetDestination -- Our destination tile isn't actually our neighbour.");
        }

        destinationTile = tile;
    }

    private void JobEnded(object sender, JobEventArgs args)
    {
        // Job complete or was cancelled.
        if (args.Job != job)
        {
            Debug.LogError("Character being told about job that isn't his; might have forgot to unregister something.");
            return;
        }
        job = null;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", CurrentTile.X.ToString());
        writer.WriteAttributeString("Y", CurrentTile.Y.ToString());
    }

    public void ReadXml(XmlReader reader)
    {                   
    }
}
