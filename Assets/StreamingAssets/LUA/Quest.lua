function Quest_Have_Furniture_Built(goal)
    goal.IsCompleted = false
    objectType = goal.Parameters["objectType"].Value
    amount = goal.Parameters["amount"].ToInt()
    amountFound = World.current.CountFurnitureType(objectType)
    if(amountFound >= amount) then
        goal.IsCompleted = true
    end
end

function Quest_Spawn_Inventory(reward)
 --tile = World.current.GetCenterTile()
 tile = World.current.GetFirstCenterTileWithNoInventory(6)
 if(tile == nil) then
  return
 end
 objectType = reward.Parameters["objectType"].Value
 amount = reward.Parameters["amount"].ToInt()
 inv = Inventory.new(objectType, amount, amount)
 World.current.inventoryManager.PlaceInventory( tile, inv)
 reward.IsCollected = true;
end
