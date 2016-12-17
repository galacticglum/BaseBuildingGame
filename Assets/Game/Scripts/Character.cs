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

    private Tile destinationTile; // If we aren't moving, then destnation tile = current tile
    private Tile nextTile; // Next tile in the pathfinding sequence
    private Pathfinder pathfinder;

    private float movementInterpolation; // Goes from 0 to 1 as we move from current tile to destination tile
    private const float Speed = 5f; // Tiles per second

    private Job job;

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
            job = CurrentTile.World.JobQueue.Dequeue();

            if (job != null)
            {
                // TODO: Check to see if the job is REACHABLE!

                destinationTile = job.Tile;
                job.JobComplete += JobEnded;
                job.JobCancel += JobEnded;
            }
        }

        if (CurrentTile != destinationTile) return;
        if (job != null)
        {
            job.DoWork(deltaTime);
        }
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
                    Debug.LogError("Character: Pathfinder returned no path to destination");
                    // FIXME: Job should be popped back into the job queue
                    AbandonJob();
                    pathfinder = null;
                    return;
                }

                // Ignore first tile because that's the tile we're currently in
                nextTile = pathfinder.Dequeue();
            }

            nextTile = pathfinder.Dequeue();
            if (nextTile == CurrentTile)
            {
                Debug.LogError("Character::DoMovement -- nextTile is CurrentTile??");
            }
        }

        switch (nextTile.TryEnter())
        {
            case TileEnterability.Never:
            {
                Debug.LogError("FIXME! Character::DoMovement: A character attempted to walk through an unwalkable tile.");
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
        pathfinder = null;
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
