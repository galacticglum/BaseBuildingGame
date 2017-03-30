function Quest_Have_Furniture_Built(goal)
    goal.IsCompleted = false
    type = goal.Parameters["objectType"].Value
    amount = goal.Parameters["amount"].Int()
    amountFound = World.Current.FurnitureManager.FurnituresWithTypeCount(type)

    if amountFound >= amount then
        goal.IsCompleted = true
    end
end

function Quest_Spawn_Inventory(reward)
 tile = World.Current.GetCentreTileWithNoInventory(6)
 if tile == nil then
  return
 end

 type = reward.Parameters["objectType"].Value
 amount = reward.Parameters["amount"].Int()
 inventory = Inventory.new(type, amount, amount)
 World.Current.InventoryManager.Place(tile, inventory)
 reward.IsCollected = true 
end
