using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class Character : IXmlSerializable, ISelectable, IContextActionProvider
{
    public float X { get { return nextTile == null ? CurrentTile.X : Mathf.Lerp(CurrentTile.X, nextTile.X, movementPercentage); } }
    public float Y { get { return nextTile == null ? CurrentTile.Y : Mathf.Lerp(CurrentTile.Y, nextTile.Y, movementPercentage); } }

    public string Name { get; set; }
    public bool IsWalking { get; private set; }
    public CharacterDirection Direction { get; private set; }

    public Job Job { get; private set; }
    public Inventory Inventory { get; set; }
    public CharacterAnimator Animator;

    private Tile currentTile;
    public Tile CurrentTile
    {
        get  { return currentTile; }
        private set
        {
            if (currentTile != null)
            {
                currentTile.Characters.Remove(this);
            }

            currentTile = value;
            currentTile.Characters.Add(this);
        }
    }

    private Tile destinationTile;
    private Tile DestinationTile
    {
        get {  return destinationTile; }
        set
        {
            if (destinationTile == value) return;
            destinationTile = value;
            pathfinder = null;   
        }
    }

    private Tile jobTile;
    public Tile JobTile
    {
        get
        {
            if (jobTile == null && Job == null)
            {
                return null;
            }

            return jobTile ?? Job.Tile;
        }
    }

    private bool isSelected;
    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            if (value == false)
            {
                VisualPath.Instance.RemoveVisualPoints(Name);
            }

            isSelected = value;
        }
    }

    private Tile nextTile;
    private Pathfinder pathfinder;
    private Need[] needs;

    private float movementPercentage;
    private const float speed = 5f;

    private Color colour;

    // TODO: Remove me!
    public event Action<Character> cbCharacterChanged;

    public Character()
    {
        needs = new Need[World.Current.NeedPrototypes.Count];
        InitializeNeeds();
    }

    public Character(Tile tile)
    {
        CurrentTile = DestinationTile = nextTile = tile;
        InitializeNeeds();
        colour = new Color(UnityEngine.Random.Range (0f, 1f), UnityEngine.Random.Range (0f, 1f), UnityEngine.Random.Range (0f, 1f), 1.0f);
    }

    public Character(Tile tile, Color color)
    {
        CurrentTile = DestinationTile = nextTile = tile;
        colour = color;
        InitializeNeeds();
    }

    private void InitializeNeeds()
    {
        needs = new Need[World.Current.NeedPrototypes.Count];
        World.Current.NeedPrototypes.Values.CopyTo (needs, 0);
        for (int i = 0; i < World.Current.NeedPrototypes.Count; i++)
        {
            Need need = needs[i];
            needs[i] = need.Clone();
            needs[i].Character = this;
        }
    }

    public void Update(float deltaTime)
    {
        DoJob(deltaTime);
        UpdateNeeds(deltaTime);
        DoMovement(deltaTime);

        if (cbCharacterChanged != null)
        {
            cbCharacterChanged(this);
        }

        Animator.Update(deltaTime);
    }

    private void DoJob(float deltaTime)
    {
        // Check if we already have a job.
        if (Job == null)
        {
            GetNewJob();
            if (Job == null)
            {
                return;
            }
        }

        // Make sure we have all materials.
        if (!HasJobMaterials()) return;

        DestinationTile = JobTile;
        if (CurrentTile == DestinationTile)
        {
            Job.DoWork(deltaTime);
        }
    }

    private bool HasJobMaterials()
    {
        if (Job != null && Job.IsNeed && Job.Critical == false)
        {
            Job.Tile = jobTile = new Pathfinder(World.Current, CurrentTile, null, Job.Type, 0, false, true).DestinationTile;
        }

        if (Job == null || Job.NeedsMaterial())
        {
            return true; 
        }

        List<string> desiredInventories = GetDesiredInventories(Job);
        if (desiredInventories == null || !desiredInventories.Any())
        {
            AbandonJob(true);
            return false;
        }

        if (Inventory != null)
        {
            if (Job.GetDesiredInventoryAmount(Inventory) > 0)
            {
                if (CurrentTile == JobTile)
                {
                    World.Current.InventoryManager.Place(Job, Inventory);
                    Job.DoWork(0); 
                    DumpExcessInventory();
                }
                else
                {
                    // Walk to the job site.
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
            // Get desired inventories.
            if (CurrentTile.Inventory != null &&
                Job.GetDesiredInventoryAmount(CurrentTile.Inventory) > 0 && !CurrentTile.Inventory.Locked &&
                (Job.CanTakeFromStockpile || CurrentTile.Furniture == null || CurrentTile.Furniture.IsStockpile() == false))
            {
                World.Current.InventoryManager.Place(
                    this,
                    CurrentTile.Inventory,
                    Job.GetDesiredInventoryAmount(CurrentTile.Inventory));
            }
            else
            {
                // Walk towards a tile containing the desired inventories.
                if (CurrentTile != nextTile)
                {
                    return false;
                }

                if (WalkingToDesiredInventory() && desiredInventories.Contains(pathfinder.DestinationTile.Inventory.Type))
                {
                    return false;
                }

                Pathfinder pathToClosestInventory = null;
                foreach (string itemType in desiredInventories)
                {
                    Inventory desiredInventory = Job.InventoryRequirements[itemType];
                    pathToClosestInventory = World.Current.InventoryManager.GetPathToClosestInventoryOfType(desiredInventory.Type, CurrentTile, 
                        desiredInventory.MaxStackSize - desiredInventory.StackSize,
                        Job.CanTakeFromStockpile);

                    if (pathToClosestInventory == null || pathToClosestInventory.Length < 1)
                    {
                        continue;
                    }

                    break;
                }

                if (pathToClosestInventory == null || pathToClosestInventory.Length < 1)
                {
                    AbandonJob(true);
                    return false;
                }

                DestinationTile = pathToClosestInventory.DestinationTile;
                pathfinder = pathToClosestInventory;
                nextTile = pathToClosestInventory.Dequeue();

                return false;
            }
        }

        return false; // We can't continue until all materials are satisfied.
    }

    private void DumpExcessInventory()
    {
        // TODO: Look for others desiring the inventory in the following order:
        // - Jobs also needing the inventory.
        // - Stockpiles.

        Inventory = null;
    }

    private static List<string> GetDesiredInventories(Job job) 
    {
        List<string> desiredInventories = new List<string>();
        foreach (Inventory inventory in job.InventoryRequirements.Values)
        {
            if (job.AcceptsAnyInventoryItem == false)
            {
                if (World.Current.InventoryManager.QuickCheck(inventory.Type) == false)
                {
                    return null;
                }

                desiredInventories.Add(inventory.Type);
            }
            else if (World.Current.InventoryManager.QuickCheck(inventory.Type))
            {
                desiredInventories.Add(inventory.Type);
            }
        }

        return desiredInventories;
    }

    private bool WalkingToDesiredInventory()
    {
        bool hasInventory = pathfinder != null && pathfinder.DestinationTile != null && pathfinder.DestinationTile.Inventory != null;
        return hasInventory && !(pathfinder.DestinationTile.Furniture != null && (Job.CanTakeFromStockpile == false && pathfinder.DestinationTile.Furniture.IsStockpile() == true));
    }


    private void GetNewJob()
    {
        float needPercent = 0;
        Need currentNeed = null;

        foreach (Need need in needs)
        {
            if (!(need.Amount > needPercent)) continue;
            currentNeed = need;
            needPercent = need.Amount;
        }

        if (needPercent > 50 && needPercent < 100 && currentNeed != null)
        {
            Job = new Job(null, currentNeed.RestorationFurniture.Type, currentNeed.CompleteJob, currentNeed.RestorationTime, null, JobPriority.High, false, true, false);
        }

        if (needPercent == 100 && currentNeed != null && currentNeed.CompleteOnFail)
        {
            Job = new Job(CurrentTile, null, currentNeed.CompleteJobCritical, currentNeed.RestorationTime * 10, null, JobPriority.High, false, true, true);
        }

        // Get the first job on the queue.
        if (Job == null)
        {
            Job = World.Current.JobQueue.Dequeue();
        }

        if (Job == null)
        {
            Job = new Job(CurrentTile, "Idle", null, UnityEngine.Random.Range(0.1f, 0.5f), null, JobPriority.Low)
            {
                Description = "job_waiting_desc"
            };
        }

        DestinationTile = Job.Tile;

        // If the destination tile does not have neighbours that are walkable then we can't walk on it.
        if (DestinationTile != null && DestinationTile.GetNeighbours().Any(tile => tile.MovementCost > 0) == false)
        {
            AbandonJob(false);
            return;
        }

        Job.cbJobStopped += OnJobStopped;
        pathfinder = Job.IsNeed && currentNeed != null ? new Pathfinder(World.Current, CurrentTile, DestinationTile, currentNeed.RestorationFurniture.Type, 0, false, true) :
            new Pathfinder(World.Current, CurrentTile, DestinationTile);

        if (pathfinder != null && pathfinder.Length == 0)
        {
            AbandonJob(false);
            return;
        }

        if (Job.WorkAdjacent)
        {
            IEnumerable<Tile> reversed = pathfinder.Reverse();
            reversed = reversed.Skip(1);
            pathfinder = new Pathfinder(new Queue<Tile>(reversed.Reverse()));
            DestinationTile = pathfinder.DestinationTile;
            jobTile = DestinationTile;
        }
        else
        {
            jobTile = Job.Tile;
        }
    }

    public void AbandonJob(bool popIntoWaitingQueue)
    {
        if (Job == null)
        {
            return;
        }

        if (Job.IsNeed)
        {
            Job.cbJobStopped -= OnJobStopped;
            Job = null;
            return;
        }

        nextTile = DestinationTile = CurrentTile;
        Job.DropPriority();

        if (popIntoWaitingQueue)
        {
            World.Current.JobWaitingQueue.Enqueue(Job);
            World.Current.cbInventoryCreated += OnInventoryCreated;
            Job.cbJobStopped -= OnJobStopped;
            Job = null;
        }
        else
        {
            World.Current.JobQueue.Enqueue(Job);
            Job.cbJobStopped -= OnJobStopped;
            Job = null;
        }
    }

    private void UpdateNeeds(float deltaTime)
    {
        foreach (Need need in needs)
        {
            need.Update(deltaTime);
        }
    }

    private void DoMovement(float deltaTime)
    {
        if (CurrentTile == DestinationTile)
        {
            pathfinder = null;
            IsWalking = false;
            VisualPath.Instance.RemoveVisualPoints(Name);
            return; // We're already were we want to be.
        }

        if (nextTile == null || nextTile == CurrentTile)
        {
            // Get the next tile from the pathfinder.
            if (pathfinder == null || pathfinder.Length == 0)
            {
                // Generate a path to our destination
                pathfinder = new Pathfinder(World.Current, CurrentTile, DestinationTile);  
                if (pathfinder.Length == 0)
                {
                    AbandonJob(false);
                    return;
                }

                // Ignore the first tile because that's the tile we're currently in.
                nextTile = pathfinder.Dequeue();
            }

            if (IsSelected)
            {
                VisualPath.Instance.SetVisualPoints(Name, pathfinder.ToList());
            }

            IsWalking = true;
            nextTile = pathfinder.Dequeue();

            if (nextTile == CurrentTile)
            {
                IsWalking = false;
            }
        }

        CalculateCharacterDirection();
        switch (nextTile.TryEnter())
        {
            case TileEnterability.Never:
                nextTile = null;    
                pathfinder = null;  
                return;
            case TileEnterability.Wait:
                return;
        }

        float distance = Mathf.Sqrt(Mathf.Pow(CurrentTile.X - nextTile.X, 2) + Mathf.Pow(CurrentTile.Y - nextTile.Y, 2));
        float frameDistance = speed / nextTile.MovementCost * deltaTime;
        float frameInterpolation = frameDistance / distance;
        movementPercentage += frameInterpolation;

        if (!(movementPercentage >= 1)) return;
        CurrentTile = nextTile;
        movementPercentage = 0;
    }

    private void CalculateCharacterDirection()
    {
        if (nextTile.X > CurrentTile.X)
        {
            Direction = CharacterDirection.East;
        }
        else if (nextTile.X < CurrentTile.X)
        {
            Direction = CharacterDirection.West;
        }
        else if (nextTile.Y > CurrentTile.Y)
        {
            Direction = CharacterDirection.North;
        }
        else
        {
            Direction = CharacterDirection.South;
        }
    }

    private static void OnInventoryCreated(Inventory inventory)
    {
        World.Current.cbInventoryCreated -= OnInventoryCreated;
        Job job = World.Current.JobWaitingQueue.Dequeue();

        if (job == null) return;

        List<string> desiredInventories = GetDesiredInventories(job);
        if (desiredInventories.Contains(inventory.Type))
        {
            World.Current.JobQueue.Enqueue(job);
        }
        else
        {
            World.Current.JobWaitingQueue.Enqueue(job);
            World.Current.cbInventoryCreated += OnInventoryCreated;
        }
    }

    private void OnJobStopped(Job job)
    {
        job.cbJobStopped -= OnJobStopped;
        if (job != Job)
        {
            return;
        }

        Job = null;
    }

    public IEnumerable<ContextMenuAction> GetContextMenuActions(ContextMenu contextMenu)
    {
        yield return new ContextMenuAction
        {
            Text = "Poke " + GetName(),
            RequiresCharacterSelection = false,
            Action = (contextMenuAction, character) => Debug.Log(string.Format("Character: {0}", GetDescription()))
        };
    }

    public string GetName()
    {
        return Name;
    }

    public string GetDescription()
    {
        string needText = needs.Aggregate("", (name, need) => name + "\n" + LocalizationTable.GetLocalization(need.LocalizationCode, need.PercentageAmount));
        return "A human astronaut." + needText;
    }

    public string GetHitPointString()
    {
        return "100/100";
    }

    public Color GetCharacterColor()
    {
        return colour;
    }

    public string GetJobDescription()
    {
        return Job == null ? "job_no_job_desc" : Job.Description;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", CurrentTile.X.ToString());
        writer.WriteAttributeString("Y", CurrentTile.Y.ToString());
        string needData = "";

        foreach (Need need in needs)
        {
            int storeAmount = (int)(need.Amount * 10);
            needData = needData + need.Type + ";" + storeAmount + ":";
        }

        writer.WriteAttributeString("needs", needData);
        writer.WriteAttributeString("r", colour.r.ToString());
        writer.WriteAttributeString("b", colour.b.ToString());
        writer.WriteAttributeString("g", colour.g.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        if (reader.GetAttribute("needs") == null) return;

        string attribute = reader.GetAttribute("needs");
        if (attribute == null) return;

        string[] needIdentifiers = attribute.Split(':');
        foreach (string needName in needIdentifiers)
        {
            string[] needData = needName.Split(';');
            foreach (Need need in needs)
            {
                if (need.Type != needData[0]) continue;

                int storeAmount;
                if (int.TryParse(needData[1], out storeAmount))
                {
                    need.Amount = (float)storeAmount / 10;
                }
            }
        }
    }
}
