function OnUpdate_GasGenerator(furniture, deltaTime)
    if furniture.HasPower() == false then
        return
    end

	if furniture.Tile.Room == nil then
		return "Furniture's room was null."
	end

    local keys = furniture.Parameters["gas_gen"].Keys
    for discard, key in pairs(keys) do
        if furniture.Tile.room.GetGasPressure(key) < furniture.Parameters["gas_gen"][key]["gas_limit"].Float() then
            furniture.Tile.room.ModifyGasValue(key, furniture.Parameters["gas_per_second"].Float() * deltaTime * furniture.Parameters["gas_gen"][key]["gas_limit"].Float())
        else
            -- Do we go into a standby mode to save power?
        end
    end

	return
end

function OnUpdate_Door(furniture, deltaTime)
	if furniture.Parameters["is_opening"].Float() >= 1.0 then
        -- FIXME: Maybe a door open speed parameter?
		furniture.Parameters["openness"].ModifyFloatValue(deltaTime * 4)
		if furniture.Parameters["openness"].Float() >= 1  then
			furniture.Parameters["is_opening"].SetValue(0)
		end
	else
        furniture.Parameters["openness"].ModifyFloatValue(deltaTime * -4)
	end

	furniture.Parameters["openness"].SetValue(Mathf.Clamp01(furniture.Parameters["openness"].Float()))
	furniture.OnFurnitureChanged(FurnitureEventArgs.new(furniture));
end

function OnUpdate_Leak_Door(furniture, deltaTime)
    -- FIXME: We should not be hardcoding the gas-leak speed, instead should be a parameter of the world/furniture/room?
	furniture.Tile.EqualizeGas(deltaTime * 10.0 * (furniture.Parameters["openness"].Float() + 0.1))
end

function OnUpdate_Leak_Airlock(furniture, deltaTime)
    -- FIXME: We should not be hardcoding the gas-leak speed, instead should be a parameter of the world/furniture/room?
	furniture.Tile.EqualizeGas(deltaTime * 10.0 * (furniture.Parameters["openness"].Float()))
end

function IsEnterable_Door(furniture)
	furniture.Parameters["is_opening"].SetValue(1)
	if furniture.Parameters["openness"].Float() >= 1 then
		return TileEnterability.Immediate
	end

    return TileEnterability.Wait 
end

function GetSpriteName_Door(furniture)
	local openness = furniture.Parameters["openness"].Float()
	if furniture.VerticalDoor == true then
			-- Door is closed
		if openness < 0.1 then
			return "DoorVertical_0"
		end

		if openness < 0.25 then
			return "DoorVertical_1"
		end

		if openness < 0.5 then
			return "DoorVertical_2"
		end

		if openness < 0.75 then
			return "DoorVertical_3"
		end

		if openness < 0.9 then
			return "DoorVertical_4"
		end

		-- Door is a fully open
		return "DoorVertical_5"
	end


	-- Door is closed
	if openness < 0.1 then
		return "DoorHorizontal_0"
	end

	if openness < 0.25 then
		return "DoorHorizontal_1"
	end

	if openness < 0.5 then
		return "DoorHorizontal_2"
	end

	if openness < 0.75 then
		return "DoorHorizontal_3"
	end

	if openness < 0.9 then
		return "DoorHorizontal_4"
	end

	-- Door is a fully open
	return "DoorHorizontal_5"
end

function GetSpriteName_Airlock(furniture)
	local openness = furniture.Parameters["openness"].Float()
    
	-- Door is closed
	if openness < 0.1 then
		return "Airlock"
	end

	-- Door is a bit open
	if openness < 0.5 then
		return "Airlock_openness_1"
	end

	-- Door is a lot open
	if openness < 0.9 then
		return "Airlock_openness_2"
	end
	
    -- Door is a fully open
	return "Airlock_openness_3"
end

function Stockpile_UpdateAction(furniture, deltaTime)
    -- TODO: This function doesn't need to run each update. Once we get a lot
    -- of furniture in a running game, this will run a LOT more than required.
    -- Instead, it only really needs to run whenever:
    -- -- It gets created
    -- -- A good gets delivered (at which point we reset the job)
    -- -- A good gets picked up (at which point we reset the job)
    -- -- The UI's filter of allowed items gets changed

    if furniture.Tile.Inventory ~= nil and furniture.Tile.Inventory.StackSize >= furniture.Tile.Inventory.MaxStackSize then
        -- We are full so let's cancel any remaining jobs.'
        furniture.CancelJobs()
        return
    end

    -- Maybe we already have a job queued up?
    if furniture.JobCount > 0 then
        return
    end

    -- We currently are NOT full, but we don't have a job either.
    -- Two possibilities: Either we have SOME inventory, or we have NO inventory.
    -- Third possibility: Something is WHACK
    if furniture.Tile.Inventory ~= nil and furniture.Tile.Inventory.stackSize == 0 then
        furniture.CancelJobs()
        print("Stockpile has a zero-size stack.")
    end


	-- TODO: In the future, stockpiles -- rather than being a bunch of individual
	-- 1x1 tiles -- should manifest themselves as single, large objects.  This
	-- would respresent our first and probably only VARIABLE sized "furniture" --
	-- at what happenes if there's a "hole" in our stockpile because we have an
	-- actual piece of furniture (like a cooking stating) installed in the middle
	-- of our stockpile?
	-- In any case, once we implement "mega stockpiles", then the job-creation system
	-- could be a lot smarter, in that even if the stockpile has some stuff in it, it
	-- can also still be requestion different object types in its job creation.

    local itemsDesired = {}
	if furniture.tile.Inventory == nil then
		itemsDesired = furniture.GetStorageInventoryFilter()
	else
		destinationInventory = furniture.tile.Inventory.Clone()
		destinationInventory.maxStackSize = destinationInventory.maxStackSize - destinationInventory.stackSize
		destinationInventory.stackSize = 0

        itemsDesired = { destinationInventory }
    end

	local job = Job.new(
		furniture.tile,
		nil,
		nil,
		0,
		itemsDesired,
		JobPriority.Low,
		false
	)

	job.Description = "job_stockpile_moving_desc"
	job.AcceptsAnyInventoryItem = true

	-- TODO: Later on, add stockpile priorities, so that we can take from a lower
	-- priority stockpile for a higher priority one.
	job.CanTakeFromStockpile = false
	job.EventManager.AddHandler("JobWorked", Stockpile_JobWorked)
	furniture.AddJob(job)
end

function Stockpile_JobWorked(job)
    job.CancelJob()

    for k, inventory in pairs(job.InventoryRequirements) do
        if inventory.StackSize > 0 then
            World.Current.InventoryManager.Place(job.Tile, inventory)
            return 
        end
    end
end

function MiningDroneStation_UpdateAction(furniture, deltaTime)
    local spawnSpot = furniture.InventorySpawnTile
	if furniture.JobCount > 0 then
		if spawnSpot.Inventory ~= nil and spawnSpot.Inventory.stackSize >= spawnSpot.Inventory.maxStackSize then
			furniture.CancelJobs()
		end

		return
	end

	-- If we get here, then we have no current job. Check to see if our destination is full.
	if spawnSpot.Inventory ~= nil and spawnSpot.Inventory.stackSize >= spawnSpot.Inventory.maxStackSize then
		return
	end

	if furniture.InventorySpawnTile.Inventory ~= nil and furniture.InventorySpawnTile.Inventory.Type ~= furniture.Parameters["mine_type"].ToString() then
		return
	end

	-- If we get here, we need to CREATE a new job.
	local jobSpot = furniture.WorkTile
	local job = Job.new(
		jobSpot,
		nil,
		nil,
		1,
		nil,
		JobPriority.Medium,
		true	-- This job repeats until the destination tile is full.
	)

    job.EventManager.AddHandler("JobCompleted", MiningDroneStation_JobComplete)
	job.Description = "job_mining_drone_station_mining_desc"
	furniture.AddJob(job)
end

function MiningDroneStation_JobComplete(job)
	if job.Furniture.InventorySpawnTile.Inventory == nil or job.Furniture.InventorySpawnTile.Inventory.Type == job.Furniture.Parameters["mine_type"].ToString() then
		World.Current.InventoryManager.Place(job.Furniture.InventorySpawnTile, Inventory.new(job.Furniture.Parameters["mine_type"].ToString(), 50, 20))
	else
		job.CancelJob()
	end
end

function MiningDroneStation_Change_to_Raw_Iron(furniture, character)
	furniture.Parameters["mine_type"].SetValue("Raw Iron")
end

function MiningDroneStation_Change_to_Raw_Copper(furniture, character)
	furniture.Parameters["mine_type"].SetValue("raw_copper")
end

function MetalSmelter_UpdateAction(furniture, deltaTime)
    local spawnSpot = furniture.InventorySpawnTile  
    if spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize >= 5 then
        furniture.Parameters["smelttime"].ModifyFloatValue(deltaTime)
        if furniture.Parameters["smelttime"].ToFloat() >= furniture.Parameters["smelttime_required"].ToFloat() then
            furniture.Parameters["smelttime"].SetValue(0)

            local outputSpot = World.Current.GetTileAt(spawnSpot.X + 2, spawnSpot.y)
            if outputSpot.Inventory == nil then
                World.Current.InventoryManager.Place(outputSpot, Inventory.new("Steel Plate", 50, 5))
                spawnSpot.Inventory.StackSize = spawnSpot.Inventory.StackSize - 5
            else
                if outputSpot.Inventory.StackSize <= 45 then
                    outputSpot.Inventory.StackSize = outputSpot.Inventory.StackSize + 5
                    spawnSpot.Inventory.StackSize = spawnSpot.Inventory.StackSize - 5
                end
            end

            if spawnSpot.Inventory.StackSize <= 0 then
                spawnSpot.Inventory = nil
            end
        end
    end

    if spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize == spawnSpot.Inventory.MaxStackSize then
        furniture.CancelJobs()
        return
    end

    if furniture.JobCount > 0 then
        return
    end

    local desiredStackSize = 50
    local itemsDesired = { Inventory.new("Raw Iron", desiredStackSize, 0) }
    if spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize < spawnSpot.Inventory.MaxStackSize then
        desiredStackSize = spawnSpot.Inventory.MaxStackSize - spawnSpot.Inventory.StackSize
        itemsDesired.maxStackSize = desiredStackSize
    end

    print("MetalSmelter: Creating job for " .. desiredStackSize .. " raw iron.")
    local jobSpot = furniture.WorkTile
    local job = Job.new(
        jobSpot,
        nil,
        nil,
        0.4,
        itemsDesired,
        JobPriority.Medium,
        false
    )

    job.EventManager.AddHandler("JobWorked", MetalSmelter_JobWorked)
    furniture.AddJob(job)
    
    return
end

function MetalSmelter_JobWorked(job)
    job.CancelJob()

    local spawnSpot = job.Tile.Furniture.InventorySpawnTile
    for k, inventory in pairs(job.InventoryRequirements) do
        if inventory ~= nil and inv.stackSize > 0 then
            World.Current.InventoryManager.Place(spawnSpot, inventory)
            spawnSpot.Inventory.Locked = true
            return
        end
    end
end

function PowerCellPress_UpdateAction(furniture, deltaTime)
    local spawnSpot = furniture.GetSpawnSpotTile()
    if spawnSpot.Inventory == nil then
        if furniture.JobCount == 0 then
            local itemsDesired = {Inventory.new("Steel Plate", 10, 0)}
            local jobSpot = furniture.GetJobSpotTile()
            local job = Job.new(
                jobSpot,
                nil,
                nil,
                1,
                itemsDesired,
                JobPriority.Medium,
                false
            )

            job.EventManager.AddHandler("JobCompleted", PowerCellPress_JobComplete)
            job.Description = "job_power_cell_fulling_desc"
            furniture.AddJob(job)
        end
    else
        furniture.Parameters["presstime"].ModifyFloatValue(deltaTime)
        if furniture.Parameters["presstime"].ToFloat() >= furniture.Parameters["presstime_required"].ToFloat() then
            furniture.Parameters["presstime"].SetValue(0)

            local outputSpot = World.current.GetTileAt(spawnSpot.X + 2, spawnSpot.y)
            if outputSpot.Inventory == nil then
                World.Current.InventoryManager.Place(outputSpot, Inventory.new("Power Cell", 5, 1))
                spawnSpot.Inventory.StackSize = spawnSpot.Inventory.StackSize - 10
            else
                if outputSpot.Inventory.StackSize <= 4 then
                    outputSpot.Inventory.StackSize = outputSpot.Inventory.StackSize + 1
                    spawnSpot.Inventory.StackSize = spawnSpot.Inventory.StackSize - 10
                end
            end

            if spawnSpot.Inventory.StackSize <= 0 then
                spawnSpot.Inventory = nil
            end
        end
    end
end

function PowerCellPress_JobComplete(job)
    local spawnSpot = job.Tile.Furniture.InventorySpawnTile
    for k, inventory in pairs(job.inventoryRequirements) do
        if inventory.StackSize > 0 then
            World.Current.InventoryManager.Place(spawnSpot, inventory)
            return
        end
    end
end

function CloningPod_UpdateAction(furniture, deltaTime)
    if furniture.JobCount > 0 then
        return
    end

    local job = Job.new(
        furniture.WorkTile,
        nil,
        nil,
        10,
        nil,
        JobPriority.Medium,
        false
    )

    job.EventManager.AddHandler("JobCompleted", CloningPod_JobComplete)
	job.Description = "job_cloning_pod_cloning_desc"
    furniture.AddJob(job)
end

function CloningPod_JobComplete(job)
    World.Current.CharacterManager.Create(job.Furniture.InventorySpawnTile)
    job.furniture.Deconstruct()
end

function PowerGenerator_UpdateAction(furniture, deltatime)
    if furniture.JobCount < 1 and furniture.Parameters["burnTime"].Float() == 0 then
        furniture.PowerValue = 0

        local itemsDesired = {Inventory.new("Uranium", 5, 0)}
        local job = Job.new(
            furniture.WorkTile,
            nil,
            nil,
            0.5,
            itemsDesired,
            JobPriority.High,
            false
        )

        job.EventManager.AddHandler("JobCompleted", PowerGenerator_JobComplete)
        job.Description = "job_power_generator_fulling_desc"
        furniture.AddJob(job)
    else
        furniture.Parameters["burnTime"].ModifyFloatValue(-deltatime)
        if furniture.Parameters["burnTime"].Float() < 0 then
            furniture.Parameters["burnTime"].SetValue(0)
        end
    end
end

function PowerGenerator_JobComplete(job)
    job.furniture.Parameters["burnTime"].SetValue(job.furniture.Parameters["burnTimeRequired"].Float())
    job.furniture.PowerValue = 5
end

function LandingPad_Temp_UpdateAction(furniture, deltaTime)
    if not furniture.Tile.Room.IsOutsideRoom() then
        return
    end

    local spawnSpot = furniture.InventorySpawnTile
    local jobSpot = furniture.WorkTile
    local inputSpot = World.current.GetTileAt(jobSpot.X, jobSpot.y - 1)

    if inputSpot.Inventory == nil then
        if furniture.JobCount == 0 then

            local itemsDesired = {Inventory.new("Steel Plate", furniture.Parameters["tradeinamount"].ToFloat())}
            local job = Job.new(
                inputSpot,
                nil,
                nil,
                0.4,
                itemsDesired,
                JobPriority.Medium,
                false
            )

            job.Furniture = furniture
            job.EventManager.AddHandler("JobCompleted", LandingPad_Temp_JobComplete)
			job.Description = "job_landing_pad_fulling_desc"
            furniture.AddJob(job)
        end
    else
        furniture.Parameters["tradetime"].ModifyFloatValue(deltaTime)
		if furniture.Parameters["tradetime"].ToFloat() >= furniture.Parameters["tradetime_required"].ToFloat() then
			furniture.Parameters["tradetime"].SetValue(0)

            local outputSpot = World.Current.GetTileAt(spawnSpot.X + 1, spawnSpot.y)
            if outputSpot.Inventory == nil then
                World.Current.InventoryManager.Place(outputSpot, Inventory.new("Steel Plate", 50, furniture.Parameters["tradeoutamount"].ToFloat()))
                inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - furniture.Parameters["tradeinamount"].ToFloat()
            else
                if outputSpot.Inventory.StackSize <= 50 - outputSpot.Inventory.StackSize + furniture.Parameters["tradeoutamount"].ToFloat() then
                    outputSpot.Inventory.StackSize = outputSpot.Inventory.StackSize + furniture.Parameters["tradeoutamount"].ToFloat()
                    inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - furniture.Parameters["tradeoutamount"].ToFloat()
                end
            end

            if inputSpot.Inventory.stackSize <= 0 then
                inputSpot.Inventory = nil
            end
        end
    end
end

function LandingPad_Temp_JobComplete(job)
    local jobSpot = job.Furniture.WorkTile
    local inputSpot = World.Current.GetTileAt(jobSpot.X, jobSpot.y - 1)

    for k, inventory in pairs(job.InventoryRequirements) do
        if inventory.StackSize > 0 then
            World.Current.InventoryManager.Place(inputSpot, inventory)
            inputSpot.Inventory.Locked = true
            return
        end
    end
end

function LandingPad_Test_ContextMenuAction(furniture, character)
   furniture.Deconstruct()
end

function Heater_InstallAction(furniture, deltaTime)
	furniture.EventManager.AddHandler("TemperatureUpdated", "Heater_UpdateTemperature")
	World.Current.Temperature.RegisterSinkOrSource(furniture)
end

function Heater_UninstallAction(furniture, deltaTime)
	furniture.eventActions.Unregister("TemperatureUpdated", "Heater_UpdateTemperature")
	World.Current.Temperature.UnregisterSinkOrSource(furniture)
end

function Heater_UpdateTemperature(furniture, deltaTime)
    -- FIXME: Don't hardcode the heater's "target" temperature. 
	World.Current.Temperature.SetTemperature(furniture.tile.X, furniture.tile.Y, 300)
end

function OxygenCompressor_OnUpdate(furniture, deltaTime)
    local room = furniture.Tile.Room
    local pressure = room.GetGasPressure("O2")
    local gasAmount = furniture.Parameters["flow_rate"].ToFloat() * deltaTime

    if pressure < furniture.Parameters["give_threshold"].ToFloat() then
        if furniture.Parameters["gas_content"].ToFloat() > 0 then
            furniture.Parameters["gas_content"].ModifyFloatValue(-gasAmount)
            room.ModifyGasValue("O2", gasAmount / room.Size)
            furniture.OnFurnitureChanged(FurnitureEventArgs.new(furniture))
        end
    elseif pressure > furniture.Parameters["take_threshold"].ToFloat() then
        if furniture.Parameters["gas_content"].ToFloat() < furniture.Parameters["max_gas_content"].ToFloat() then
            furniture.Parameters["gas_content"].ModifyFloatValue(gasAmount)
            room.ModifyGasValue("O2", -gasAmount / room.Size)
            furniture.OnFurnitureChanged(FurnitureEventArgs.new(furniture))
        end
    end
end

function OxygenCompressor_GetSpriteName(furniture)
    local baseName = furniture.objectType
    local suffix = 0
    if furniture.Parameters["gas_content"].ToFloat() > 0 then
        idxAsFloat = 8 * (furniture.Parameters["gas_content"].ToFloat() / furniture.Parameters["max_gas_content"].ToFloat())
        suffix = Mathf.FloorToInt(idxAsFloat)
    end

    return baseName .. "_" .. suffix
end