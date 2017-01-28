using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable, ISelectable
{
    public float X { get { return nextTile == null ? CurrentTile.X : Mathf.Lerp(CurrentTile.X, nextTile.X, movementPercentage); }}
    public float Y { get { return nextTile == null ? CurrentTile.Y : Mathf.Lerp(CurrentTile.Y, nextTile.Y, movementPercentage); }}

    private Tile currentTile;
    public Tile CurrentTile
    {
        get { return currentTile; }
        set
        {
            if (currentTile != null)
            {
                currentTile.Characters.Remove(this);
            }

            currentTile = value;
            currentTile.Characters.Add(this);
        }
    }

    public Inventory Inventory { get; set; } 

    private Tile destinationTile;
	private Tile DestinationTile
    {
		get { return destinationTile; }
		set {
		    if (destinationTile == value) return;

		    destinationTile = value;
		    pathfinder = null;	
		}
	}

    private Tile jobTile;
    private Tile JobTile { get { return jobTile ?? job.Tile; } }

	private Tile nextTile;
    private Job job;
    private Pathfinder pathfinder;

    private float movementPercentage;
	private const float Speed = 5f;

    public LuaEventManager EventManager { get; set; }

    public event CharacterChangedEventHandler CharacterChanged;
    public void OnCharacterChanged(CharacterEventArgs args)
    {
        CharacterChangedEventHandler characterChanged = CharacterChanged;
        if (characterChanged != null)
        {
            characterChanged(this, args);
        }

        EventManager.Trigger("CharacterChanged", this, args);
    }

    public Character()
    {
        EventManager = new LuaEventManager("CharacterChanged");
    }

	public Character(Tile tile) : this()
    {
		CurrentTile = DestinationTile = nextTile = tile;
	}

    public void Update(float deltaTime)
    {
        DoJob(deltaTime);
        DoMovement(deltaTime);

        OnCharacterChanged(new CharacterEventArgs(this));
    }

    private void DoJob(float deltaTime)
    {
        if (job == null)
        {
            GetNewJob();
            if (job == null)
            {
                return;
            }
        }

        if (!CheckForJobMaterials()) return;

        DestinationTile = JobTile;
        if (CurrentTile == DestinationTile)
        {
            job.DoWork(deltaTime);
        }
    }

    private bool CheckForJobMaterials()
    {
        if (job.HasAllMaterials())
        {
            return true;
        }

        Inventory desiredInventory = job.GetFirstDesiredInventory();
        if (!World.Current.InventoryManager.Check(desiredInventory.Type))
        {
            AbandonJob(true);
            return false;
        }

        if (Inventory != null)
        {
            if (job.GetDesiredInventoryAmount(Inventory) > 0)
            {
                if (currentTile == JobTile)
                {
                    World.Current.InventoryManager.Place(job, Inventory);
                    job.DoWork(0);
                    DumpExcessInventory();
                }
                else
                {
                    DestinationTile = JobTile;
                    return false;
                }
            }
            else
            {
                DumpExcessInventory();
            }
        }
        else
        {
                if (CurrentTile.Inventory != null &&
                    (job.CanTakeFromStockpile || CurrentTile.Furniture == null || CurrentTile.Furniture.IsStockpile() == false) &&
                    job.GetDesiredInventoryAmount(CurrentTile.Inventory) > 0)
            {
                // Pick up the stuff!
                World.Current.InventoryManager.Place(this, CurrentTile.Inventory, job.GetDesiredInventoryAmount(CurrentTile.Inventory));
            }
            else
            {
                if (currentTile != nextTile) return false;

                // Find the first thing in the Job that isn't satisfied.
                desiredInventory = job.GetFirstDesiredInventory();

                if (pathfinder != null && pathfinder.DestinationTile != null &&
                     pathfinder.DestinationTile.Inventory != null && pathfinder.DestinationTile.Furniture != null &&
                     !(job.CanTakeFromStockpile == false && pathfinder.DestinationTile.Furniture.IsStockpile()) &&
                     pathfinder.DestinationTile.Inventory.Type == desiredInventory.Type) return false;

                Pathfinder pathToClosestInventory = World.Current.InventoryManager.GetPathToClosestInventoryOfType(desiredInventory.Type, CurrentTile, job.CanTakeFromStockpile);
                if (pathToClosestInventory == null || pathToClosestInventory.Length < 1)
                {
                    AbandonJob(true);
                    return false;
                }

                DestinationTile = pathToClosestInventory.DestinationTile;
                pathfinder = pathToClosestInventory;
                nextTile = pathfinder.Dequeue();

                return false;
            }
        }

        return false;
    }

    private void DumpExcessInventory()
    {
        Inventory = null;
    }

    private void GetNewJob()
    {
        job = World.Current.JobQueue.Dequeue() ?? new Job(CurrentTile, "Idle", UnityEngine.Random.Range(0.1f, 0.5f), JobPriority.Low, null, null);

        DestinationTile = job.Tile;
        job.JobStopped += OnJobStopped;

        pathfinder = new Pathfinder(CurrentTile, DestinationTile);
        if (pathfinder != null && pathfinder.Length == 0)
        {
            Debug.LogError("Character::GetNewJob: Pathfinder returned no path to destination");
            AbandonJob(false);
            DestinationTile = CurrentTile;
        }

        if (job.WorkAdjacent)
        {
            IEnumerable<Tile> reversePath = pathfinder.Reverse();
            reversePath = reversePath.Skip(1);
            pathfinder = new Pathfinder(new Queue<Tile>(reversePath.Reverse()));

            DestinationTile = pathfinder.DestinationTile;
            jobTile = DestinationTile;
        }
        else
        {
            jobTile = job.Tile;
        }
    }

    public void AbandonJob(bool popIntoWaitingQueue)
    {
		nextTile = DestinationTile = CurrentTile;
        job.DropPriority();

        if (popIntoWaitingQueue)
        {
            World.Current.JobWaitingQueue.Enqueue(job);
            World.Current.InventoryManager.InventoryCreated += OnInventoryCreated;
            job.JobStopped -= OnJobStopped;
            job = null;
        }
        else
        {
            World.Current.JobQueue.Enqueue(job);
            job.JobStopped -= OnJobStopped;
            job = null;
        }
    }

    private void DoMovement(float deltaTime)
    {
        if (CurrentTile.X == DestinationTile.X && CurrentTile.Y == DestinationTile.Y)
        {
			pathfinder = null;
			return;	// We're already were we want to be.
		}

		if(nextTile == null || nextTile == CurrentTile)
        {
            // Get the next tile from the pathfinder.
            if (pathfinder == null || pathfinder.Length == 0)
            {
				// Generate a path to our destination
                pathfinder = new Pathfinder(CurrentTile, DestinationTile);	
				if(pathfinder.Length == 0)
                {
					Debug.LogError("Character::DoMovement: Pathfinder returned no path to destination!");
				    AbandonJob(false);
					return;
				}

				nextTile = pathfinder.Dequeue();
			}

			nextTile = pathfinder.Dequeue();
			if(nextTile == CurrentTile)
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
		    case TileEnterability.Wait:
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

    private void OnInventoryCreated(object sender, InventoryEventArgs args)
    {
        World.Current.InventoryManager.InventoryCreated -= OnInventoryCreated;
        Job job = World.Current.JobWaitingQueue.Dequeue();
        Inventory desiredInventory = job.GetFirstDesiredInventory();

        if (args.Inventory.Type == desiredInventory.Type)
        {
            World.Current.JobQueue.Enqueue(job);
        }
        else
        {
            World.Current.JobWaitingQueue.Enqueue(job);
            World.Current.InventoryManager.InventoryCreated += OnInventoryCreated;
        }
    }

    private void OnJobStopped(object sender, JobEventArgs args)
    {
		args.Job.JobStopped -= OnJobStopped;

		if(args.Job != job)
        {
			return;
		}

		job = null;
	}

    public string GetName()
    {
        return "Elessar Ole Tetlie";
    }

    public string GetDescription()
    {
        return "A fluffy dog who sleeps way to much.";
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        return null;
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
