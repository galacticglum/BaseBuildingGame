function GetStockpileItemFilter()
    return { Inventory.new("Steel Plate", 50, 0) }
end

function BuildFurniture(sender, args)
    World.Current.PlaceFurniture(args.Job.Type, args.Job.Tile)
    args.Job.Tile.PendingFurnitureJob = nil
end

function UpdateStockpile(furniture, deltaTime)
    if furniture.Tile.Inventory ~= nil and furniture.Tile.Inventory.StackSize >= furniture.Tile.Inventory.MaxStackSize then
        furniture.CancelJobs()
        return
    end

    if furniture.JobCount > 0 then
        return
    end

    if furniture.Tile.Inventory ~= nil and furniture.Tile.Inventory.StackSize == 0 then
        print("FurnitureBehaviours::UpdateStockpile: Stockpile has a zero-size stack.")
        furniture.CancelJobs()
    end

    itemFilter = {}
    if furniture.Tile.Inventory == nil then
        itemFilter = GetStockpileItemFilter()
    else
        desiredInventory = furniture.Tile.Inventory.Clone()
        desiredInventory.MaxStackSize = desiredInventory.MaxStackSize - desiredInventory.StackSize
        desiredInventory.StackSize = 0

        itemFilter = { desiredInventory }
    end

    job = Job.new(furniture.Tile, nil, nil, 0, itemFilter)
    job.CanTakeFromStockpile = false

    --job.JobWorked = job.JobWorked + StockpileJobWorked
    Callback.Add(furniture.JobWorked, StockpileJobWorked)
    furniture.AddJob(job)
end

function StockpileJobWorked(sender, args)
    args.Job.CancelJob()

    for i, inventory in pairs(args.Job.InventoryRequirements) do
        if inventory.StackSize > 0 then
            World.Current.InventoryManager.PlaceInventory(args.Job.Tile, inventory)
            return
        end
    end
end

function UpdateDoor(furniture, deltaTime)
    if furniture.GetParameter("is_opening") >= 1.0 then
        furniture.ModifyParameter("openness", deltaTime * 4.0)
        if furniture.GetParameter("openness") >= 1.0 then
            furniture.SetParameter("is_opening", 0)
        end
    else
        furniture.ModifyParameter("openness", deltaTime * -4.0)
    end

    furniture.SetParameter("openness", Mathf.Clamp01(furniture.GetParameter("openness")))
    furniture.OnFurnitureChanged(FurnitureEventArgs.new(furniture))
end

function DoorTryEnter(furniture)
    furniture.SetParameter("is_opening", 1)

    if furniture.GetParameter("openness") >= 1 then
        return TileEnterability.Immediate
    end

    return TileEnterability.Wait
end

function UpdateOxygenGenerator(furniture, deltaTime)
    if furniture.Tile.Room == nil then
        return
    end

    if furniture.Tile.Room.GetGasValue("O2") < 0.20 then
        furniture.Tile.Room.ModifyGasValue("O2", 0.01 * deltaTime)
    else
        -- Standby??
    end
end

function UpdateMiningDrone(furniture, deltaTime)
    inventorySpawnTile = furniture.InventorySpawnTile
    if furniture.JobCount > 0 then
        if inventorySpawnTile.Inventory ~= nil and inventorySpawnTile.Inventory.StackSize >= inventorySpawnTile.Inventory.MaxStackSize then
            furniture.CancelJobs()
        end

        return
    end

    if inventorySpawnTile.Inventory ~= nil and inventorySpawnTile.Inventory.StackSize >= inventorySpawnTile.Inventory.MaxStackSize then
        furniture.CancelJobs()
        return
    end

    workTile = furniture.WorkTile
    if workTile.Inventory ~= nil and workTile.Inventory.StackSize >= workTile.Inventory.MaxStackSize then
        return
    end

    job = Job.new(furniture.WorkTile, nil, MiningDroneJobCompleted, 1, nil, true)
    furniture.AddJob(job)
end

function MiningDroneJobCompleted(sender, args)
    World.Current.InventoryManager.PlaceInventory(args.Job.Furniture.InventorySpawnTile, Inventory.new("Steel Plate", 50, 10))
end


