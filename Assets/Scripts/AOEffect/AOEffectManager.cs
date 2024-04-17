using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using UnityEngine.Networking;
using System.Linq;

[Serializable]
public struct GameState
{
	public float timeRemaining;
	public GameMode gameMode;
	public List<AOEffectPlayer> players;
	public Dictionary<string, bool> waitingPlayers; 
}


[Serializable]
public class AOEffectInfo
{
	public string processID;
	public int height;
	public int width;
	public float maxEnergy;
	public float energyPerSec;
	public float health;
	public float averageMaxStrengthHitsToKill;
	public float waitTime;
	public float gameTime;
	public int minimumPlayers;
	public float paymentQty;
	public float bonusQty;
}

public enum GameMode
{
	None,
	Waiting,
	Playing,
	Restart
}

public class AOEffectManager : MonoBehaviour
{
	public static AOEffectManager main { get; private set; }

	public bool LocalPlayerExists => AOSManager.main != null && !string.IsNullOrEmpty(AOSManager.main.processID);
	public AOEffectPlayerState localPlayerState = AOEffectPlayerState.None;

	public Action<GameMode,GameMode> OnGameModeChanged;
	public bool waitingSupported;

	[Header("AOEffectManager")]
	public GameState gameState;
	public AOEffectInfo AOEffectInfo;
	public int lastPlayerAttack = -1;
	public string arenaManagerProcessID = "I0sKrk8f7uirSaPaMzf3NmhxAwmaXJFoDYuwYGZl7II";
	public List<AOEffectInfo> availableArenas = new List<AOEffectInfo>();
	public bool exitGameWhenError;

	[Header("References")]
	public AOEffectCanvasManager canvasManager;
	public GridManager gridManager;
	public AOEffectPlayer playerPrefab;

	//Find a way to add players at the beginning while waiting -- maybe asking the status also before. That would be nice also to update timer while waiting and also to update lobby 

	[Header("Polling Settings")]
	public string gameProcessID;
	public bool pollGameData = false;
	public float pollingIntervalDuringWaiting;
	public float pollingIntervalDuringGame;

	public bool doTick;
	public float tickTime;

	public bool optimizeTick;

	private float elapsedTickTime = 0;

	private string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";
	private string baseJsonBody = @"{
            ""Id"":""1234"",
            ""Target"":""GAMEPROCESSID"",
            ""Owner"":""1234"",
            ""Tags"":[{""name"":""Action"",""value"":""ACTIONID""}]
        }";

	[Header("Debug")]
	public string debugData;
	public bool logGameState;
	public bool waitAOEffectData;

	private bool readyToPlay = false;
	private bool registered = false;

	private bool hasAlreadyFoundErrorGetGameState = false;
	private bool hasAlreadyFoundErrorGetAOEffectInfo = false;
	private bool hasAlreadyFoundErrorCheckPlayerAttack = false;

	private bool canCheckPlayerAttacks = true;

	private bool startedToCheckPlayerAttacks = false;

	private float lastTimeRemaining = -1;

	private float tickThresholdTime = 5;

	private float acceptedTimeDifference = 30;

	private int baseTimeout = 5;
	private int extendedTimeout = 30;
	private int actualTimeout = 5;

	void Awake()
	{
		if (main == null)
		{
			main = this;
			DontDestroyOnLoad(gameObject); // Optional: Keep the instance alive across scenes.
		}
		else
		{
			Destroy(gameObject); // Destroy if a duplicate exists.
		}
	}

	private void Update()
	{
		if(gameState.gameMode != GameMode.None)
		{
			if(AOSManager.main != null && doTick)
			{
				if(elapsedTickTime > tickTime)
				{
					AOSManager.main.RunCommand("Send({ Target = \"" + gameProcessID + "\", Action = \"Tick\"})");
					elapsedTickTime = 0;
				}

				elapsedTickTime += Time.deltaTime;
			}

			if(gameState.timeRemaining != 0 && gameState.timeRemaining != -1)
			{
				gameState.timeRemaining -= Time.deltaTime;

				if(gameState.timeRemaining < 0)
				{ 
					gameState.timeRemaining = 0;
				}

				canvasManager.UpdateTimerText(gameState.timeRemaining);
			}

			if(optimizeTick)
			{
				if(gameState.timeRemaining <= tickThresholdTime)
				{
					doTick = true;
				}
				else
				{
					doTick = false;
				}
			}
		}

		if(gameState.gameMode == GameMode.Playing)
		{
			if(!startedToCheckPlayerAttacks && canCheckPlayerAttacks)
			{
				startedToCheckPlayerAttacks = true;
				StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks));
			}
		}
		else
		{
			lastPlayerAttack = -1;
			startedToCheckPlayerAttacks = false;
		}

		if(waitingSupported && gameState.waitingPlayers != null && !readyToPlay && LocalPlayerExists && gameState.waitingPlayers.ContainsKey(AOSManager.main.processID) && gameState.waitingPlayers[AOSManager.main.processID])
		{
			if(gameState.waitingPlayers[AOSManager.main.processID])
			{
				readyToPlay = true;
				if (registerToGameCoroutine != null) //We know is registered from waiting list
				{
					StopCoroutine(registerToGameCoroutine);
					registerToGameCoroutine = null;
					canvasManager.RegistrationCallback(true);
				}
			}
			else
			{
				registered = true;
			}
		}
	}

	public void LoadGame(string processID)
	{
		StopAllCoroutines();
		gameProcessID = processID;

		StartCoroutine(LoadGameCoroutine());
	}

	private IEnumerator LoadGameCoroutine()
	{
		if(AOSManager.main != null)
		{
			bool done = false;
			float elapsedTime = 0;
			float timeOut = 10;

			Action<string> callback = (string message) => { done = true; };

			AOSManager.main.RunCommand($"Game = \"{gameProcessID}\"", callback, "undefined");

			while (!done && elapsedTime < timeOut)
			{
				elapsedTime += Time.deltaTime;
				yield return null;
			}
			if (!done)
			{
				Debug.LogError("Error while assigning Game variable");
			}
			else
			{
				AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
				yield return new WaitForSeconds(2);
			}
		}

		StartCoroutine(SendPostRequest("GetAOEffectInfo", UpdateAOEffectInfo));

		if(!waitAOEffectData)
		{
			StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
		}
	}

	public void ExitGame(string error = null)
	{
		StopAllCoroutines();

		if(gameState.gameMode != GameMode.None)
		{
			GameMode old = gameState.gameMode;
			gameState.gameMode = GameMode.None;
			OnGameModeChanged?.Invoke(old, gameState.gameMode);

			if(gameState.players != null && gameState.players.Count > 0)
			{
				foreach(AOEffectPlayer p in gameState.players)
				{
					Destroy(p.gameObject);
				}
				gameState.players.Clear();
			}
			gameState.timeRemaining = 0;
		}

		readyToPlay = false;
		registered = false;
		hasAlreadyFoundErrorGetGameState = false;
		hasAlreadyFoundErrorGetAOEffectInfo = false;
		hasAlreadyFoundErrorCheckPlayerAttack = false;

		canCheckPlayerAttacks = true;

		gridManager.DestroyAndClearGridObjects();
		AOEffectInfo = null;
		gameProcessID = "";
		waitingSupported = false;

		canvasManager.ExitGame(error);
	}

	private Coroutine registerToGameCoroutine;

	public void RegisterToGame(Action<bool> callback, bool onlyPay)
	{
		registerToGameCoroutine = StartCoroutine(RegisterToGameCoroutine(callback, onlyPay));
	}

	public void UpdateLocalPlayerState()
	{
		if (LocalPlayerExists)
		{
			if(gameState.players != null && gameState.players.Count > 0 && gameState.players.Exists(p => p.isLocalPlayer))
			{
				localPlayerState = AOEffectPlayerState.Playing;
			}
			else if (gameState.waitingPlayers == null)
			{
				//BaseVersion of Arena with no info about WaitingPlayers
				if (readyToPlay)
				{
					localPlayerState = AOEffectPlayerState.WaitingPaid;
				}
				else if(localPlayerState == AOEffectPlayerState.Playing || localPlayerState == AOEffectPlayerState.WaitingPaid || localPlayerState == AOEffectPlayerState.Waiting) //He did subscription before
				{
					localPlayerState = AOEffectPlayerState.Waiting;
				}
				else
				{
					localPlayerState = AOEffectPlayerState.None;
				}
			}
			else if(gameState.waitingPlayers.Count > 0 && gameState.waitingPlayers.ContainsKey(AOSManager.main.processID))
			{
				if (gameState.waitingPlayers[AOSManager.main.processID])
				{
					localPlayerState = AOEffectPlayerState.WaitingPaid;
				}
				else
				{
					localPlayerState = AOEffectPlayerState.Waiting;
				}
			}
			else
			{
				localPlayerState = AOEffectPlayerState.None;
			}
		}
		else
		{
			localPlayerState = AOEffectPlayerState.None;
		}
	}

	public void LocalPlayerAttack()
	{
		AOEffectPlayer localPlayer = gameState.players.Find(p => p.isLocalPlayer);
		if(localPlayer != null)
		{
			AOSManager.main.RunCommand("Send({ Target = Game, Action = \"PlayerAttack\", Player = ao.id, AttackEnergy = \"" + localPlayer.data.energy.ToString() + "\"})");
		}
		else
		{
			Debug.LogError("Local Player not found");
		}
	}

	public void RemovePlayer(AOEffectPlayer p)
	{
		if(gameState.players != null && gameState.players.Contains(p))
		{
			gameState.players.Remove(p);
		}
	}

	private IEnumerator RegisterToGameCoroutine(Action<bool> registrationCallback, bool onlyPay)
	{
		yield return null;

		bool done = false;
		float elapsedTime = 0;
		float timeOut = 10;

		Action<string> callback = (string message) => { done = true; };

		if (!onlyPay)
		{
			AOSManager.main.RunCommand($"Game = \"{gameProcessID}\"", callback, "undefined");
			canvasManager.SetGameInfoText("Assigning Game variable..");

			while (!done && elapsedTime < timeOut)
			{
				elapsedTime += Time.deltaTime;
				yield return null;
			}
			if (!done)
			{
				registrationCallback.Invoke(false);
				yield break;
			}

			done = false;
			elapsedTime = 0;

			AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Register\" })", callback, "Registered");
			canvasManager.SetGameInfoText("Registering..");
		
			while (!done && elapsedTime < timeOut)
			{
				elapsedTime += Time.deltaTime;
				yield return null;
			}
			if (!done)
			{
				registrationCallback.Invoke(false);
				yield break;
			}

			registered = true;

			done = false;
			elapsedTime = 0;

			AOSManager.main.RunCommand("Send({ Target = Game, Action = \"RequestTokens\"})", callback, "Credit-Notice");
			canvasManager.SetGameInfoText("Requesting tokens..");

			while (!done && elapsedTime < timeOut)
			{
				elapsedTime += Time.deltaTime;
				yield return null;
			}
			if (!done)
			{
				registrationCallback.Invoke(false);
				yield break;
			}
			done = false;
			elapsedTime = 0;
		}

		AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Transfer\", Recipient = Game, Quantity = \"1000\"})", callback, "Payment-Received");
		canvasManager.SetGameInfoText("Sending tokens..");

		while (!done && elapsedTime < timeOut)
		{
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		if (!done)
		{
			if(onlyPay) //Maybe the problem is that he finished tokens
			{
				done = false;
				elapsedTime = 0;

				AOSManager.main.RunCommand("Send({ Target = Game, Action = \"RequestTokens\"})", callback, "Credit-Notice");
				canvasManager.SetGameInfoText("Requesting tokens..");

				while (!done && elapsedTime < timeOut)
				{
					elapsedTime += Time.deltaTime;
					yield return null;
				}

				if (!done)
				{
					registrationCallback.Invoke(false);
					yield break;
				}

				done = false;
				elapsedTime = 0;

				AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Transfer\", Recipient = Game, Quantity = \"1000\"})", callback, "Payment-Received");
				canvasManager.SetGameInfoText("Sending tokens..");

				while (!done && elapsedTime < timeOut)
				{
					elapsedTime += Time.deltaTime;
					yield return null;
				}

				if (!done)
				{
					registrationCallback.Invoke(false);
					yield break;
				}
				else
				{
					Debug.Log("Registered!");
					readyToPlay = true;

					yield return new WaitForSeconds(1); //Wait other two seconds to be sure that everything has been saved

					registrationCallback.Invoke(true);
				}
			}
			else
			{
				registrationCallback.Invoke(false);
				yield break;
			}
		}
		else
		{
			Debug.Log("Registered!");
			readyToPlay = true;

			yield return new WaitForSeconds(1); //Wait other two seconds to be sure that everything has been saved

			registrationCallback.Invoke(true);
		}
	}

	private void CheckPlayerAttacks(bool result, string jsonString)
	{
		if (!result)
		{
			if (exitGameWhenError && hasAlreadyFoundErrorCheckPlayerAttack)
			{
				Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
				canCheckPlayerAttacks = false;
			}
			else
			{
				hasAlreadyFoundErrorCheckPlayerAttack = true;
				if (AOSManager.main != null)
				{
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
				}
				StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks, 3));
			}
			return;
		}

		if(gameState.gameMode != GameMode.Playing)
		{
			Debug.Log("Checking player attacks but game is not in playing mode, ignoring");
			return;
		}

		// Parse JSON string
		JSONNode fullJsonNode = JSON.Parse(jsonString);

		// Populate GameState
		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				if (exitGameWhenError && hasAlreadyFoundErrorCheckPlayerAttack)
				{
					Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
					canCheckPlayerAttacks = false;
				}
				else
				{
					hasAlreadyFoundErrorCheckPlayerAttack = true;
					if (AOSManager.main != null)
					{
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					}
					StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks, 3));
				}

				return;
			}

			if (logGameState)
			{
				Debug.Log("PLAYER ATTACKS INFO: " + jsonString);
			}

			JSONNode jsonNode = JSON.Parse(jsonString);
			var keys = jsonNode.Keys;

			if (jsonNode.HasKey("LastPlayerAttacks"))
			{
				hasAlreadyFoundErrorCheckPlayerAttack = false;

				JSONArray lastPlayerAttacksArray = jsonNode["LastPlayerAttacks"].AsArray;

				if(lastPlayerAttacksArray.Count > 0)
				{
					if (lastPlayerAttacksArray[lastPlayerAttacksArray.Count].HasKey("id")) //The Grid
					{
						int id = lastPlayerAttacksArray[lastPlayerAttacksArray.Count]["id"].AsInt;

						if(lastPlayerAttack == -1)
						{
							lastPlayerAttack = id;
						}
						else if(lastPlayerAttack != id)
						{
							for (int i = 0; i < lastPlayerAttacksArray.Count; i++)
							{
								id = lastPlayerAttacksArray[i]["id"];

								if(id > lastPlayerAttack)
								{
									lastPlayerAttack = id;
									string player = lastPlayerAttacksArray[i]["Player"];
									string target = lastPlayerAttacksArray[i]["Target"];

									HandleAttack(player, target);
								}
							}
						}
					}
					else 
					{
						if (lastPlayerAttack >= 0 && lastPlayerAttacksArray.Count > lastPlayerAttack)
						{
							for (int i = lastPlayerAttack; i < lastPlayerAttacksArray.Count; i++)
							{
								string player = lastPlayerAttacksArray[i]["Player"];
								string target = lastPlayerAttacksArray[i]["Target"];

								HandleAttack(player, target);
							}
						}

						lastPlayerAttack = lastPlayerAttacksArray.Count;
					}
				}
				
				if(lastPlayerAttack == -1) //We know that we checked once 
				{
					lastPlayerAttack = 0;
				}
			}
			else
			{
				if (exitGameWhenError && hasAlreadyFoundErrorCheckPlayerAttack)
				{
					Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
					canCheckPlayerAttacks = false;
				}
				else
				{
					hasAlreadyFoundErrorCheckPlayerAttack = true;
					if (AOSManager.main != null)
					{
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					}
					StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks, 3));
				}
			}
		}
		else
		{
			if (exitGameWhenError && hasAlreadyFoundErrorCheckPlayerAttack)
			{
				Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
				canCheckPlayerAttacks = false;
			}
			else
			{
				hasAlreadyFoundErrorCheckPlayerAttack = true;
				if (AOSManager.main != null)
				{
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
				}
				StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks, 3));
			}
		}

		if(gameState.gameMode == GameMode.Playing && canCheckPlayerAttacks)
		{
			StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks));
		}
	}

	private void HandleAttack(string player, string target)
	{
		AOEffectPlayer p = gameState.players.Find(p => p.data.id == player);
		AOEffectPlayer t = gameState.players.Find(p => p.data.id == target);

		bool foundPlayers = true;

		if (p == null)
		{
			foundPlayers = false;
			Debug.LogError("Can't find player " + player);
		}

		if (t == null)
		{
			foundPlayers = false;
			Debug.LogError("Can't find player " + target);
		}

		if (foundPlayers)
		{
			p.ShootTarget(t);
		}
	}

	private void UpdateAOEffectInfo(bool result, string jsonString)
	{
		if (!result)
		{
			Debug.LogWarning("Base AOEffect Game not supporting additional info");
			gridManager.CreateGrid();
			if(waitAOEffectData) StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
			return;
		}

		// Parse JSON string
		JSONNode fullJsonNode = JSON.Parse(jsonString);

		// Populate GameState
		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				if (hasAlreadyFoundErrorGetAOEffectInfo || AOSManager.main == null)
				{
					Debug.LogWarning("Base AOEffect Game not supporting additional info");
					gridManager.CreateGrid();
					if (waitAOEffectData) StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
				}
				else
				{
					hasAlreadyFoundErrorGetAOEffectInfo = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					StartCoroutine(SendPostRequest("GetAOEffectInfo", UpdateAOEffectInfo, 3));
				}

				return;
			}

			if (logGameState)
			{
				Debug.Log("AOEFFECT INFO: " + jsonString);
			}

			JSONNode jsonNode = JSON.Parse(jsonString);
			var keys = jsonNode.Keys;

			if (jsonNode.HasKey("Width"))
			{
				hasAlreadyFoundErrorGetAOEffectInfo = false;

				AOEffectInfo = ParseAOEffectInfo(jsonNode);
				AOEffectInfo.processID = gameProcessID;

				gridManager.CreateGrid(AOEffectInfo.height,AOEffectInfo.width);
				if (waitAOEffectData) StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
			}
			else
			{
				if (hasAlreadyFoundErrorGetAOEffectInfo || AOSManager.main == null)
				{
					Debug.LogWarning("Base AOEffect Game not supporting additional info");
					gridManager.CreateGrid();
					if (waitAOEffectData) StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
					return;
				}
				else
				{
					hasAlreadyFoundErrorGetAOEffectInfo = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					StartCoroutine(SendPostRequest("GetAOEffectInfo", UpdateAOEffectInfo, 3));
				}
			}
		}
		else
		{
			if (hasAlreadyFoundErrorGetAOEffectInfo || AOSManager.main == null)
			{
				Debug.LogWarning("Base AOEffect Game not supporting additional info");
				gridManager.CreateGrid();
				if (waitAOEffectData) StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
				return;
			}
			else
			{
				hasAlreadyFoundErrorGetAOEffectInfo = true;
				AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
				StartCoroutine(SendPostRequest("GetAOEffectInfo", UpdateAOEffectInfo, 3));
			}
		}
	}

	private AOEffectInfo ParseAOEffectInfo(JSONNode jsonNode)
	{
		AOEffectInfo info = new AOEffectInfo();

		info.width = jsonNode.HasKey("Width") ? jsonNode["Width"] : -1;
		info.height = jsonNode.HasKey("Height") ? jsonNode["Height"] : -1;
		info.maxEnergy = jsonNode.HasKey("MaxEnergy") ? jsonNode["MaxEnergy"] : -1;
		info.energyPerSec = jsonNode.HasKey("EnergyPerSec") ? jsonNode["EnergyPerSec"] : -1;
		info.health = jsonNode.HasKey("Health") ? jsonNode["Health"] : -1;
		info.averageMaxStrengthHitsToKill = jsonNode.HasKey("AverageMaxStrengthHitsToKill") ? jsonNode["AverageMaxStrengthHitsToKill"] : -1;
		info.waitTime = jsonNode.HasKey("WaitTime") ? jsonNode["WaitTime"].AsLong / 60000 : -1;
		info.gameTime = jsonNode.HasKey("GameTime") ? jsonNode["GameTime"].AsLong / 60000 : -1;
		info.minimumPlayers = jsonNode.HasKey("MinimumPlayers") ? jsonNode["MinimumPlayers"] : -1;
		info.paymentQty = jsonNode.HasKey("PaymentQty") ? jsonNode["PaymentQty"] : -1;
		info.bonusQty = jsonNode.HasKey("BonusQty") ? jsonNode["BonusQty"] : -1;

		return info;
	}

	private void UpdateGameState(bool result, string jsonString)
	{
        if (!result)
        {
			if (exitGameWhenError && hasAlreadyFoundErrorGetGameState)
			{
				Debug.LogError("Error while loading AOEffect Game state. Check game process ID or internet connection");
				ExitGame("Error while loading AOEffect Game state. Check game process ID or internet connection");
			}
			else
			{
				if(hasAlreadyFoundErrorGetGameState)
				{
					canvasManager.SetGameErrorText("Error while loading AOEffect Game state. Check game process ID or internet connection");
				}

				hasAlreadyFoundErrorGetGameState = true;
				if (AOSManager.main != null)
				{
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
				}
				StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
			}
			return;
        }

        // Parse JSON string
        JSONNode fullJsonNode = JSON.Parse(jsonString);

		// Populate GameState
		if(fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if(string.IsNullOrEmpty(jsonString))
			{
				if (exitGameWhenError && hasAlreadyFoundErrorGetGameState)
				{
					Debug.LogError("Game State Data is null!");
					ExitGame("Game State Data is null!");
				}
				else
				{
					if (hasAlreadyFoundErrorGetGameState)
					{
						canvasManager.SetGameErrorText("Game State Data is null!");
					}
					hasAlreadyFoundErrorGetGameState = true;
					if (AOSManager.main != null)
					{
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					}
					StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
				}

				return;
			}

			if (logGameState)
			{
				Debug.Log(jsonString);
			}

			JSONNode jsonNode = JSON.Parse(jsonString);
			var keys = jsonNode.Keys;

			if(jsonNode.HasKey("GameMode"))
			{
				hasAlreadyFoundErrorGetGameState = false;

				float newTimeRemaining = -1;

				if (jsonNode.HasKey("TimeRemaining"))
				{
					JSONNode timeRemaining = jsonNode["TimeRemaining"];
					newTimeRemaining = timeRemaining.AsLong / 1000.0f;
				}

				if (jsonNode["GameMode"] == "Playing")
				{
					GameMode oldGameMode = gameState.gameMode;
					gameState.gameMode = GameMode.Playing;

					if(oldGameMode != gameState.gameMode)
					{
						OnGameModeChanged?.Invoke(oldGameMode, gameState.gameMode);
					}	 
				}
				else if(jsonNode["GameMode"] == "Waiting")
				{
					GameMode oldGameMode = gameState.gameMode;
					gameState.gameMode = GameMode.Waiting;

					if (oldGameMode != gameState.gameMode) //Start Waiting
					{
						readyToPlay = false;
						OnGameModeChanged?.Invoke(oldGameMode, gameState.gameMode);
					}
					else if(gameState.timeRemaining + acceptedTimeDifference < newTimeRemaining) //Restart Waiting because no enough player
					{
						readyToPlay = false;
						OnGameModeChanged?.Invoke(GameMode.Restart, gameState.gameMode);
					}
				}
				else
				{
					Debug.LogError("Game mode is " + jsonNode["GameMode"].ToString());
				}

				bool updateTimerText = false;

				if(newTimeRemaining == -1)
				{
					gameState.timeRemaining = -1;
					updateTimerText = true;
					//canvasManager.UpdateTimerText(newTimeRemaining);
				}

				if (lastTimeRemaining != newTimeRemaining) //Only if is changed
                {
					lastTimeRemaining = newTimeRemaining;
					gameState.timeRemaining = newTimeRemaining;
					updateTimerText = true;
					//canvasManager.UpdateTimerText(gameState.timeRemaining);
                }

				if (jsonNode.HasKey("WaitingPlayers"))
				{
					waitingSupported = true;

					JSONNode waitingPlayers = jsonNode["WaitingPlayers"];

					if(gameState.waitingPlayers == null)
					{
						gameState.waitingPlayers = new Dictionary<string, bool>();
					}


					foreach (KeyValuePair<string, JSONNode> kvp in waitingPlayers)
					{
						string key = kvp.Key;
						bool value = kvp.Value.AsBool;

						if(gameState.waitingPlayers.ContainsKey(key))
						{
							gameState.waitingPlayers[key] = value;
						}
						else
						{
							gameState.waitingPlayers.Add(key, value);
						}
					}

					List<string> keysToRemove = new List<string>();

					foreach (string key in gameState.waitingPlayers.Keys)
					{
						if (!waitingPlayers.HasKey(key))
						{
							keysToRemove.Add(key);
						}
					}

					foreach (string key in keysToRemove)
					{
						gameState.waitingPlayers.Remove(key);
					}

					canvasManager.UpdateWaitingPanel();
				}
				else
				{
					waitingSupported = false;
				}

				JSONNode playersNode = jsonNode["Players"];
			
				if(gameState.gameMode == GameMode.Playing)
				{
					foreach (KeyValuePair<string, JSONNode> kvp in playersNode.AsObject)
					{
						string playerId = kvp.Key;
						JSONNode playerNode = kvp.Value;

						AOEffectPlayer player = gameState.players.Find(p => p.data.id == playerId);

						if (player == null)
						{
							player = Instantiate(playerPrefab, this.transform);
							if(AOSManager.main != null && playerId == AOSManager.main.processID)
							{
								player.isLocalPlayer = true;
							}

							gameState.players.Add(player);
						}

						AOEffectPlayerData newData = new AOEffectPlayerData();

						newData.id = playerId;
					
						Transform newPos = gridManager.GetGridPos(playerNode["y"].AsInt - 1, playerNode["x"].AsInt - 1);

						if(newPos != null)
						{
							newData.pos = newPos.position;
						}

						newData.energy = playerNode["energy"].AsInt;
						newData.health = playerNode["health"].AsInt;

						if(playerNode.HasKey("lastTurn"))
						{
							string lastTurn = "";

							if (!string.IsNullOrEmpty(playerNode["lastTurn"]))
							{
								long timestamp = playerNode["lastTurn"].AsLong;

								DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;

								TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

								DateTime localDateTime = TimeZoneInfo.ConvertTime(dateTime, localTimeZone);

								string format;
								if (localTimeZone.Id.StartsWith("en-US"))
								{
									format = "MM/dd/yyyy h:mm:ss tt"; 
								}
								else
								{
									format = "dd/MM/yyyy HH:mm:ss"; 
								}

								lastTurn = localDateTime.ToString(format);
							}
							
							newData.lastTurn = lastTurn;
						}
						else
						{
							newData.lastTurn = "";
						}

						if (playerNode.HasKey("name"))
						{
							newData.name = playerNode["name"];
						}
						else
						{
							newData.name = "";
						}

						player.UpdateData(newData);
						player.State = AOEffectPlayerState.Playing;
					}

					List<AOEffectPlayer> playersToRemove = new List<AOEffectPlayer>();

					foreach (AOEffectPlayer p in gameState.players)
					{
						if (!playersNode.HasKey(p.data.id))
						{
							playersToRemove.Add(p);
						}
					}

					foreach (AOEffectPlayer p in playersToRemove)
					{
						p.State = AOEffectPlayerState.Dead;
					}
					
					gameState.players = gameState.players.OrderByDescending((p) => p.data.health).ToList();
					int count = 1;
					foreach (AOEffectPlayer player in gameState.players)
					{
						player.UpdateRanking(count);
						count++;
					}

					canvasManager.UpdateLeaderboard();

					if (pollGameData)
					{
						StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, pollingIntervalDuringGame));
					}
				}
				else if (gameState.gameMode == GameMode.Waiting)
				{
					if (gameState.players != null && gameState.players.Count > 0)
					{
						foreach (AOEffectPlayer p in gameState.players)
						{
							Destroy(p.gameObject);
						}
						gameState.players.Clear();
					}

					if (pollGameData)
					{
						if(gameState.timeRemaining > pollingIntervalDuringWaiting)
						{
							StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, pollingIntervalDuringWaiting));
						}
						else
						{
							StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, pollingIntervalDuringGame));
						}
					}
				}
				else
				{
					if (exitGameWhenError && hasAlreadyFoundErrorGetGameState)
					{
						ExitGame("Game state not recognized");
						Debug.LogError("Game state not recognized");
						return;
					}
					else
					{
						if (hasAlreadyFoundErrorGetGameState)
						{
							canvasManager.SetGameErrorText("Game state not recognized");
						}
						hasAlreadyFoundErrorGetGameState = true;
						if (AOSManager.main != null)
						{
							AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
						}
						StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
					}
				}

				if(updateTimerText)
				{
					canvasManager.UpdateTimerText(gameState.timeRemaining);
				}
			}
			else
			{
				if(jsonString.Contains("Not enough players registered!") || jsonString.Contains("The game has started"))
				{
					Debug.LogWarning("Skipping State: " + jsonString);
					StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 3));
				}
				else
				{
					if (exitGameWhenError && hasAlreadyFoundErrorGetGameState)
					{
						ExitGame("GameMode not found!");
						Debug.LogError("NO KEY GameMode: " + jsonString);
						return;
					}
					else
					{
						if (hasAlreadyFoundErrorGetGameState)
						{
							canvasManager.SetGameErrorText("GameMode not found!");
						}
						hasAlreadyFoundErrorGetGameState = true;
						if(AOSManager.main != null)
						{
							AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
						}
						StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
					}
				}
			}
		}
		else
		{
			if (jsonString.Contains("Not enough players registered!") || jsonString.Contains("The game has started"))
			{
				Debug.LogWarning("Skipping State: " + jsonString);
				StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 3));
			}
			else
			{
				if (exitGameWhenError && hasAlreadyFoundErrorGetGameState)
				{
					ExitGame("Messages key not found!");
					Debug.LogError("NO KEY Messages: " + jsonString);
					return;
				}
				else
				{
					if (hasAlreadyFoundErrorGetGameState)
					{
						canvasManager.SetGameErrorText("Messages key not found!");
					}
					hasAlreadyFoundErrorGetGameState = true;
					if (AOSManager.main != null)
					{
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					}
					StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
				}
			}
		}

		UpdateLocalPlayerState();
	}

	public void GetAvailableArenas()
	{
		StartCoroutine(SendPostRequest("GetSubscribers", UpdateAvailableArenas, 0, arenaManagerProcessID));
	}

	private void UpdateAvailableArenas(bool result, string jsonString)
	{
		if (!result)
		{
			Debug.LogWarning("Problem in getting available arenas, result false");
			StartCoroutine(SendPostRequest("GetSubscribers", UpdateAvailableArenas, 3, arenaManagerProcessID)); //Retry
			return;
		}

		// Parse JSON string
		JSONNode fullJsonNode = JSON.Parse(jsonString);

		// Populate GameState
		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				StartCoroutine(SendPostRequest("GetSubscribers", UpdateAvailableArenas, 3, arenaManagerProcessID)); //Retry
				Debug.LogWarning("Problem in getting available arenas. Messages Data is null");
				return;
			}

			if (logGameState)
			{
				Debug.Log("Arena Subscribers INFO: " + jsonString);
			}

			JSONNode jsonNode = JSON.Parse(jsonString);

			availableArenas.Clear();

			foreach (KeyValuePair<string, JSONNode> kvp in jsonNode.AsObject)
			{
				string gameID = kvp.Key;
				JSONNode aoEffectInfoNode = JSONNode.Parse(kvp.Value);

				AOEffectInfo info = ParseAOEffectInfo(aoEffectInfoNode);
				info.processID = gameID;

				availableArenas.Add(info);
			}

			canvasManager.UpdateAvailableArenas();
		}
		else
		{
			StartCoroutine(SendPostRequest("GetSubscribers", UpdateAvailableArenas, 3, arenaManagerProcessID)); //Retry
			Debug.LogWarning("Problem in getting available arenas. Message is null");
		}
	}

	private IEnumerator SendPostRequest(string actionID, Action<bool,string> callback, float delay = 0, string targetProcessID = "")
	{
		if(delay > 0)
		{
			yield return new WaitForSeconds(delay);
		}

		if (string.IsNullOrEmpty(targetProcessID))
		{
			targetProcessID = gameProcessID;
		}

		string url = baseUrl + targetProcessID;

		string jsonBody = baseJsonBody.Replace("GAMEPROCESSID", targetProcessID).Replace("ACTIONID", actionID);

		UnityWebRequest request = new UnityWebRequest(url, "POST");

		byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
		request.uploadHandler = new UploadHandlerRaw(bodyRaw);
		request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
		request.timeout = actualTimeout;
		request.SetRequestHeader("Content-Type", "application/json");

		yield return request.SendWebRequest();

		if (request.result != UnityWebRequest.Result.Success)
		{
			if(request.error.Contains("timeout"))
			{
				actualTimeout = extendedTimeout;
			}
			Debug.LogError("Error: " + request.error);
			callback.Invoke(false, "Error: " + request.error);
		}
		else
		{
			actualTimeout = baseTimeout;

			if (string.IsNullOrEmpty(request.downloadHandler.text))
			{
				Debug.LogError("JSON is null!");
				callback.Invoke(false, "JSON is null!");
			}
			else
			{
				callback.Invoke(true, request.downloadHandler.text);
			}
		}
	}
}
