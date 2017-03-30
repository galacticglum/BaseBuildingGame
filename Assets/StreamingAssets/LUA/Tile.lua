function MovementCost_Standard (tile)
	if tile.furniture == nil then
		return tile.Type.BaseMovementCost
	end

	return tile.Type.BaseMovementCost * tile.Furniture.MovementCost
end

function CanBuildHere_Standard (tile)
	return true
end

function CanBuildHere_Ladder (tile)
	return tile.Room.IsOutsideRoom()
end
