-- AO EFFECT: Game Mechanics for AO Arena Game


-- Game grid dimensions
Width = 8 -- Width of the grid
Height = 6 -- Height of the grid
Range = 1 -- The distance for blast effect

-- Player energy settings
MaxEnergy = 100 -- Maximum energy a player can have
EnergyPerSec = 30 -- Energy gained per second

-- Attack settings
AverageMaxStrengthHitsToKill = 5 -- Average number of hits to eliminate a player

Health = 80

-- Initializes default player state
-- @return Table representing player's initial state
function playerInitState()
    return {
        x = math.random(1, Width),
        y = math.random(1, Height),
        health = Health,
        energy = 0
    }
end

-- Function to incrementally increase player's energy
-- Called periodically to update player energy
function onTick()
    if GameMode ~= "Playing" then
        return end  -- Only active during "Playing" state

    if LastTick == undefined then LastTick = Now end

    local Elapsed = Now - LastTick
    if Elapsed >= 1000 then  -- Actions performed every second
        for player, state in pairs(Players) do
            local newEnergy = math.floor(math.min(MaxEnergy, state.energy + (Elapsed * EnergyPerSec // 2000)))
            state.energy = newEnergy
        end
        LastTick = Now
    end
end

-- Handles player movement
-- @param msg: Message request sent by player with movement direction and player info
function move(msg)
    local playerToMove = msg.From
    local direction = msg.Tags.Direction

    local directionMap = {
        Up = {x = 0, y = -1}, Down = {x = 0, y = 1},
        Left = {x = -1, y = 0}, Right = {x = 1, y = 0},
        UpRight = {x = 1, y = -1}, UpLeft = {x = -1, y = -1},
        DownRight = {x = 1, y = 1}, DownLeft = {x = -1, y = 1}
    }

    -- calculate and update new coordinates
    if directionMap[direction] then
        local newX = Players[playerToMove].x + directionMap[direction].x
        local newY = Players[playerToMove].y + directionMap[direction].y

        -- updates player coordinates while checking for grid boundaries
        Players[playerToMove].x = (newX - 1) % Width + 1
        Players[playerToMove].y = (newY - 1) % Height + 1

        announce("Player-Moved", playerToMove .. " moved to " .. Players[playerToMove].x .. "," .. Players[playerToMove].y .. ".")
    else
        ao.send({Target = playerToMove, Action = "Move-Failed", Reason = "Invalid direction."})
    end
    onTick()  -- Optional: Update energy each move
end

-- Handles player attacks
-- @param msg: Message request sent by player with attack info and player state
function attack(msg)
    local player = msg.From
    local attackEnergy = tonumber(msg.Tags.AttackEnergy)

    -- get player coordinates
    local x = Players[player].x
    local y = Players[player].y

    -- check if player has enough energy to attack
    if Players[player].energy < attackEnergy then
        ao.send({Target = player, Action = "Attack-Failed", Reason = "Not enough energy. Only " .. Players[player].energy .. " < " ..attackEnergy })
        return
    end

    -- update player energy and calculate damage
    Players[player].energy = Players[player].energy - attackEnergy
    local damage = math.floor((math.random() * 2 * attackEnergy) * (1/AverageMaxStrengthHitsToKill))

    announce("Attack", player .. " has launched a " .. damage .. " damage attack from " .. x .. "," .. y .. "!")

    -- check if any player is within range and update their status
    for target, state in pairs(Players) do
        if target ~= player and inRange(x, y, state.x, state.y, Range) then
            local newHealth = state.health - damage
            CurrentAttacks = CurrentAttacks + 1
            LastPlayerAttacks[CurrentAttacks] =
            {
                Player = player,
                Target = target
            }
            if newHealth <= 0 then
                eliminatePlayer(target, player)
            else
                Players[target].health = newHealth
 
                ao.send({Target = target, Action = "Hit", Damage = tostring(damage), Health = tostring(newHealth)})
                ao.send({Target = player, Action = "Successful-Hit", Recipient = target, Damage = tostring(damage), Health = tostring(newHealth)})
            end
        end
    end
end

-- Helper function to check if a target is within range
-- @param x1, y1: Coordinates of the attacker
-- @param x2, y2: Coordinates of the potential target
-- @param range: Attack range
-- @return Boolean indicating if the target is within range
function inRange(x1, y1, x2, y2, range)
    return x2 >= (x1 - range) and x2 <= (x1 + range) and y2 >= (y1 - range) and y2 <= (y1 + range)
end

-- HANDLERS: Game state management for AO-Effect

-- Handler for player movement
Handlers.add("PlayerMove", Handlers.utils.hasMatchingTag("Action", "PlayerMove"), move)

-- Handler for player attacks
Handlers.add("PlayerAttack", Handlers.utils.hasMatchingTag("Action", "PlayerAttack"), attack)

-- Retrieves the current AOEffect info.
Handlers.add(
    "GetAOEffectInfo",
    Handlers.utils.hasMatchingTag("Action", "GetAOEffectInfo"),
    function (Msg)
        local json = require("json")
        local AOEffectInfo = json.encode({
            Width = Width,
            Height = Height,
            Range = Range,
            MaxEnergy = MaxEnergy,
            EnergyPerSec = EnergyPerSec,
            AverageMaxStrengthHitsToKill = AverageMaxStrengthHitsToKill
            })
        ao.send({
            Target = Msg.From,
            Action = "AOEffectInfo",
            Data = AOEffectInfo})
    end
)

-- Retrieves the current attacks that has been made in the game.
Handlers.add(
    "GetGameAttacksInfo",
    Handlers.utils.hasMatchingTag("Action", "GetGameAttacksInfo"),
    function (Msg)
        local json = require("json")
        local GameAttacksInfo = json.encode({
            LastPlayerAttacks = LastPlayerAttacks,
            })
        ao.send({
            Target = Msg.From,
            Action = "GameAttacksInfo",
            Data = GameAttacksInfo})
    end
)