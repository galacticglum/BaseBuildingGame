function Precondition_Event_NewCrewMember( gameEvent, deltaTime )
	gameEvent.AddTimer(deltaTime)
	local timer = gameEvent.Timer
	if (timer >= 30.0) then
		gameEvent.ResetTimer()
		return true
	end
end

function Execute_Event_NewCrewMember( gameEvent )
	local tile = World.current.GetTileAt(World.current.Width / 2, World.current.Height / 2)
	c = World.current.CreateCharacter(tile)
	print("GameEvent: New Crew Member spawned named '" .. c.GetName() .. "'.")
end
