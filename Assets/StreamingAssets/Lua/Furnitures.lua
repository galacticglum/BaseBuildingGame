function GetStockpileItemFilter()
    return { Inventory.new("Steel Plate", 50, 0) }
end

function BuildFurniture(sender, args)
    World.Current.FurnitureManager.Place(args.Job.Type, args.Job.Tile)
    args.Job.Tile.PendingFurnitureJob = nil
end

function UpdateStockpile(sender, args)
    if args.Furniture.Tile.Inventory ~= nil and args.Furniture.Tile.Inventory.StackSize >= args.Furniture.Tile.Inventory.MaxStackSize then
        args.Furniture.CancelJobs()
        return
    end

    if args.Furniture.JobCount > 0 then
        return
    end

    if args.Furniture.Tile.Inventory ~= nil and args.Furniture.Tile.Inventory.StackSize == 0 then
        print("FurnitureBehaviours::UpdateStockpile: Stockpile has a zero-size stack.")
        args.Furniture.CancelJobs()
    end

    itemFilter = {}
    if args.Furniture.Tile.Inventory == nil then
        itemFilter = GetStockpileItemFilter()
    else
        desiredInventory = args.Furniture.Tile.Inventory.Clone()
        desiredInventory.MaxStackSize = desiredInventory.MaxStackSize - desiredInventory.StackSize
        desiredInventory.StackSize = 0

        itemFilter = { desiredInventory }
    end

    job = Job.new(args.Furniture.Tile, nil, 0, JobPriority.Medium, nil, itemFilter, false, false)
    job.AcceptsAnyInventoryItem = true
    job.CanTakeFromStockpile = false

    job.EventManager.AddHandler("JobWorked", StockpileJobWorked)
    args.Furniture.AddJob(job)
end

function StockpileJobWorked(sender, args)
    args.Job.CancelJob()

    for i, inventory in pairs(args.Job.InventoryRequirements) do
        if inventory.StackSize > 0 then
            World.Current.InventoryManager.Place(args.Job.Tile, inventory)
            return
        end
    end
end

function UpdateDoor(sender, args)
    if args.Furniture.GetParameter("is_opening") >= 1.0 then
        args.Furniture.ModifyParameter("openness", args.DeltaTime * 4.0)
        if args.Furniture.GetParameter("openness") >= 1.0 then
            args.Furniture.SetParameter("is_opening", 0)
        end
    else
        args.Furniture.ModifyParameter("openness", args.DeltaTime * -4.0)
    end

    args.Furniture.SetParameter("openness", Mathf.Clamp01(args.Furniture.GetParameter("openness")))
    args.Furniture.OnFurnitureChanged(FurnitureEventArgs.new(args.Furniture))
end

function DoorTryEnter(furniture)
    furniture.SetParameter("is_opening", 1)

    if furniture.GetParameter("openness") >= 1 then
        return TileEnterability.Immediate
    end

    return TileEnterability.Wait
end

function UpdateOxygenGenerator(sender, args)
    if args.Furniture.Tile.Room == nil then
        return
    end

    if args.Furniture.Tile.Room.GetGasValue("O2") < 0.20 then
        args.Furniture.Tile.Room.ModifyGasValue("O2", 0.01 * args.DeltaTime)
    else
        -- Standby??
    end
end

function UpdateMiningDrone(sender, args)
    inventorySpawnTile = args.Furniture.InventorySpawnTile
    if args.Furniture.JobCount > 0 then
        if inventorySpawnTile.Inventory ~= nil and inventorySpawnTile.Inventory.StackSize >= inventorySpawnTile.Inventory.MaxStackSize then
            args.Furniture.CancelJobs()
        end

        return
    end

    if inventorySpawnTile.Inventory ~= nil and inventorySpawnTile.Inventory.StackSize >= inventorySpawnTile.Inventory.MaxStackSize then
        args.Furniture.CancelJobs()
        return
    end

    workTile = args.Furniture.WorkTile
    if workTile.Inventory ~= nil and workTile.Inventory.StackSize >= workTile.Inventory.MaxStackSize then
        return
    end


    job = Job.new(workTile, nil, 1, JobPriority.Medium, MiningDroneJobCompleted, nil, true, false)
    args.Furniture.AddJob(job)
end

function MiningDroneJobCompleted(sender, args)
    World.Current.InventoryManager.Place(args.Job.Furniture.InventorySpawnTile, Inventory.new("Steel Plate", 50, 10))
end
