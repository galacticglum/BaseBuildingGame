using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
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

	private Tile nextTile;
    private Job job;
    private Pathfinder pathfinder;

    private float movementPercentage;
	private const float Speed = 5f;
    private float jobSearchCooldown;

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
        jobSearchCooldown -= deltaTime;
        if (job == null)
		{
            if (jobSearchCooldown > 0) return;

            GetNewJob();

			if(job == null)
            {
                jobSearchCooldown = UnityEngine.Random.Range(0.1f, 0.5f);
                DestinationTile = CurrentTile;
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
						World.Current.InventoryManager.PlaceInventory(job, Inventory);
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
						DestinationTile = job.Tile;
						return;
					}
				}
				else
                {
					// TODO: Actually, walk to the nearest empty tile and dump it there.
                    if (World.Current.InventoryManager.PlaceInventory(CurrentTile, Inventory))
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
					World.Current.InventoryManager.PlaceInventory(this, CurrentTile.Inventory, job.GetDesiredInventoryAmount(CurrentTile.Inventory));

				}
				else
                {
                    if (currentTile != nextTile) return;

                    // Find the first thing in the Job that isn't satisfied.
                    Inventory desiredInventory = job.GetFirstDesiredInventory();

                    if(pathfinder != null && pathfinder.DestinationTile != null &&
                        pathfinder.DestinationTile.Inventory != null && pathfinder.DestinationTile.Furniture != null && 
                        !(job.CanTakeFromStockpile == false && pathfinder.DestinationTile.Furniture.IsStockpile()) && 
                        pathfinder.DestinationTile.Inventory.Type == desiredInventory.Type) return;

                    Pathfinder pathToClosestInventory = World.Current.InventoryManager.GetPathToClosestInventoryOfType(desiredInventory.Type, CurrentTile, job.CanTakeFromStockpile);

                    if (pathToClosestInventory == null || pathToClosestInventory.Length == 0)
                    {
                        AbandonJob();
                        return;
                    }

                    DestinationTile = pathToClosestInventory.DestinationTile;
                    pathfinder = pathToClosestInventory;
                    nextTile = pathfinder.Dequeue();

                    return;
				}

			}

			return; 
		}

		DestinationTile = job.Tile;
		if(CurrentTile == job.Tile)
        {
			job.DoWork(deltaTime);
		}
	}

    private void GetNewJob()
    {
        job = World.Current.JobQueue.Dequeue() ?? new Job(CurrentTile, "Idle", UnityEngine.Random.Range(0.1f, 0.5f), null, null);

        DestinationTile = job.Tile;
        job.JobStopped += OnJobStopped;

        pathfinder = new Pathfinder(CurrentTile, DestinationTile);	
        if (pathfinder.Length != 0) return;

        Debug.LogError("Character::GetNewJob: Pathfinder returned no path to destination");
        AbandonJob();
        DestinationTile = CurrentTile;
    }

    public void AbandonJob()
    {
		nextTile = DestinationTile = CurrentTile;
        World.Current.JobQueue.Enqueue(job);
		job = null;
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
				    AbandonJob();
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
