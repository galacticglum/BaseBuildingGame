using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable
{
	public float X { get { return Mathf.Lerp( CurrentTile.X, nextTile.X, movementPercentage ); } }
	public float Y { get { return Mathf.Lerp( CurrentTile.Y, nextTile.Y, movementPercentage ); } }
		
	public Tile CurrentTile { get; protected set; }
    public Inventory Inventory { get; set; } 

    private Tile localDestinationTile;
	private Tile destinationTile
    {
		get { return localDestinationTile; }
		set {
		    if (localDestinationTile == value) return;

		    localDestinationTile = value;
		    pathfinder = null;	
		}
	}

	private Tile nextTile;
    private Job job;
    private Pathfinder pathfinder;

    private float movementPercentage;
	private const float Speed = 5f;

    public event CharacterChangedEventHandler CharacterChanged;
    public void OnCharacterChanged(CharacterChangedEventArgs args)
    {
        if (CharacterChanged != null)
        {
            CharacterChanged(this, args);
        }
    }

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
		if(job == null)
		{
			GetNewJob();

			if(job == null)
            {
				destinationTile = CurrentTile;
				return;
			}
		}

		if(job.HasAllMaterials() == false)
        {
			if(Inventory != null)
            {
				if(job.GetDesiredInventoryAmount(Inventory) > 0)
                {
					if(CurrentTile == job.Tile)
                    {
						CurrentTile.World.InventoryManager.PlaceInventory(job, Inventory);
						job.DoWork(0); 

						if(Inventory.StackSize == 0)
                        {
							Inventory = null;
						}
						else
                        {
							Debug.LogError("Character::DoJob: Character is still carrying inventory; the character shouldn't be carrying any inventory!");
							Inventory = null;
						}
					}
					else
                    {
						destinationTile = job.Tile;
						return;
					}
				}
				else
                {
					// TODO: Actually, walk to the nearest empty tile and dump it there.
                    if (CurrentTile.World.InventoryManager.PlaceInventory(CurrentTile, Inventory))
                    {
                        return; // We can't continue until all materials are satisfied.
                    }

                    Debug.LogError("Character::DoJob: Tried to 'dump' inventory onto an invalid tile.");
                    Inventory = null;
                }
			}
			else
            {
				if(CurrentTile.Inventory != null && 
                    (job.CanTakeFromStockpile || CurrentTile.Furniture == null || CurrentTile.Furniture.IsStockpile() == false) &&  
					job.GetDesiredInventoryAmount(CurrentTile.Inventory) > 0)
                {
					// Pick up the stuff!
					CurrentTile.World.InventoryManager.PlaceInventory(this, CurrentTile.Inventory, job.GetDesiredInventoryAmount(CurrentTile.Inventory));

				}
				else
                {
					// Find the first thing in the Job that isn't satisfied.
					Inventory desiredInventory = job.GetFirstDesiredInventory();
					Inventory closestInventory = CurrentTile.World.InventoryManager.GetClosestInventoryOfType(desiredInventory.Type, 
                        CurrentTile, desiredInventory.MaxStackSize - desiredInventory.StackSize, job.CanTakeFromStockpile);

					if(closestInventory == null)
                    {
						Debug.LogWarning("Character::DoJob: No tile contains inventory of type: '" + desiredInventory.Type + "' to satisfy job.");
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
		if(CurrentTile == job.Tile)
        {
			job.DoWork(deltaTime);
		}
	}

    private void GetNewJob()
    {
        job = CurrentTile.World.JobQueue.Dequeue();
        if (job == null) return;

        destinationTile = job.Tile;
        job.JobComplete += OnJobEnded;
        job.JobCancel += OnJobEnded;

        pathfinder = new Pathfinder(CurrentTile.World, CurrentTile, destinationTile);	
        if (pathfinder.Length != 0) return;

        Debug.LogError("Character::DoJob: Pathfinder returned no path to destination");
        AbandonJob();
        destinationTile = CurrentTile;
    }

    public void AbandonJob()
    {
		nextTile = destinationTile = CurrentTile;
		CurrentTile.World.JobQueue.Enqueue(job);
		job = null;
	}

    private void DoMovement(float deltaTime)
    {
		if(CurrentTile == destinationTile)
        {
			pathfinder = null;
			return;	// We're already were we want to be.
		}

		if(nextTile == null || nextTile == CurrentTile)
        {
			// Get the next tile from the pathfinder.
			if(pathfinder == null || pathfinder.Length == 0)
            {
				// Generate a path to our destination
				pathfinder = new Pathfinder(CurrentTile.World, CurrentTile, destinationTile);	
				if(pathfinder.Length == 0)
                {
					Debug.LogError("Character::DoMovement: Pathfinder returned no path to destination!");
					AbandonJob();
					return;
				}

				nextTile = pathfinder.Dequeue();
			}

			nextTile = pathfinder.Dequeue();
			if( nextTile == CurrentTile )
            {
				Debug.LogError("Character::DoMovement: nextTile is currentTile?");
			}
		}

		switch (nextTile.TryEnter())
		{
		    case TileEnterability.Never:
		        Debug.LogError("Character::DoMovement: A character attempted to walk through an unwalkable tile.");

		        nextTile = null;	
		        pathfinder = null;	       
		        return;
		    case TileEnterability.Soon:
		        return;
		}

        float distance = Mathf.Sqrt(Mathf.Pow(CurrentTile.X - nextTile.X, 2) + Mathf.Pow(CurrentTile.Y - nextTile.Y, 2));
        float frameDistance = Speed / nextTile.MovementCost * deltaTime;
		float frameInterpolation = frameDistance / distance;
		movementPercentage += frameInterpolation;

        if (!(movementPercentage >= 1)) return;
        CurrentTile = nextTile;
        movementPercentage = 0;
    }

	public void SetDestination(Tile tile)
    {
		if(CurrentTile.IsNeighbour(tile, true) == false)
        {
			Debug.LogError("Character::SetDestination: Our destination tile isn't actually our neighbour.");
		}

		destinationTile = tile;
	}

    private void OnJobEnded(object sender, JobEventArgs args)
    {
		args.Job.JobComplete -= OnJobEnded;
		args.Job.JobCancel -= OnJobEnded;

		if(args.Job != job)
        {
			Debug.LogError("Character::OnJobEnded: Character being told about job that isn't his; might have forgot to unregister something.");
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

    public void ReadXml(XmlReader reader) { }
}
