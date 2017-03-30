function Precondition_Event_NewCrewMember(gameEvent, deltaTime)
	gameEvent.AddTimer(deltaTime)

	local timer = gameEvent.Timer
	if timer >= 30.0 then
		gameEvent.ResetTimer()
		return true
	end
end

function Execute_Event_NewCrewMember(gameEvent)
	local tile = World.Current.GetTileAt(World.Current.Width / 2, World.Current.Height / 2)
	character = World.Current.CharacterManager.Create(tile)
	
	print("GameEvent: New Crew Member spawned named '" .. character.Name .. "'.")
end
