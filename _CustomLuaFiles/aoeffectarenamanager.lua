Arenas = Arenas or { }

-- Registers new arenas.
Handlers.add(
    "Register",
    Handlers.utils.hasMatchingTag("Action", "Register"),
    function(msg)
        Arenas[msg.From] = "Registered"
        ao.send({
            Target = msg.From,
            Action = "Registered"
        })
        ao.send({
            Target = msg.From,
            Action = "GetAOEffectInfo"
        })
        print("New Arena Registered! " .. msg.From .. " has joined.")
    end
)

-- Unregisters arena.
Handlers.add(
    "Unregister",
    Handlers.utils.hasMatchingTag("Action", "Unregister"),
    function(msg)
        Arenas[msg.From] = nil
        ao.send({
            Target = msg.From,
            Action = "Unregistered"
        })  
        print("Arena Unregistered! " .. msg.From .. " has been removed.")
    end
)

-- Get AOEffect info when new arena is subscribed
Handlers.add(
  "UpdateAOEffectInfo",
  Handlers.utils.hasMatchingTag("Action", "AOEffectInfo"),
  function (msg)
    Arenas[msg.From] = msg.Data
    ao.send({Target = msg.From, Action = "UpdatedAOEffectInfo"}) --Mettere Data forse, provare così prima
    print("Arena AOEffect info updated! " .. msg.From .. " has joined.")
  end
)

-- Send Subscribers AOEffect info 
Handlers.add(
  "GetSubscribers",
  Handlers.utils.hasMatchingTag("Action", "GetSubscribers"),
  function (msg)
    local json = require("json")
    Data = json.encode(Arenas)
    ao.send({Target = msg.From, Action = "SubscribersInfo", Data = Data})
  end
)