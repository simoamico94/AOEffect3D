using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using JetBrains.Annotations;
using System.Security.Cryptography;

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
	public int height;
	public int width;
	public int maxEnergy;
	public int energyPerSec;
	public int range;
	public int averageMaxStrengthHitsToKill;
}

public enum GameMode
{
	None,
	Waiting,
	Playing
}

public class AOEffectManager : MonoBehaviour
{
	public static AOEffectManager main { get; private set; }

	public bool LocalPlayerExists => AOSManager.main != null && !string.IsNullOrEmpty(AOSManager.main.processID);
	public bool WaitingExists => gameState.waitingPlayers != null && gameState.waitingPlayers.Count > 0;
	public AOEffectPlayerState localPlayerState = AOEffectPlayerState.None;

	public Action<GameMode,GameMode> OnGameModeChanged;

	[Header("AOEffectManager")]
	public GameState gameState;
	public AOEffectInfo AOEffectInfo;
	public int playersAttacks = 0;
	
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

	private bool registered = false;

	private bool hasAlreadyFoundErrorGetGameState = false;
	private bool hasAlreadyFoundErrorGetAOEffectInfo = false;
	private bool hasAlreadyFoundErrorCheckPlayerAttack = false;

	private bool canCheckPlayerAttacks = true;

	private bool startedToCheckPlayerAttacks = false;

	private float lastTimeRemaining = -1;

	private float tickThresholdTime = 5;

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

			if(gameState.timeRemaining != 0)
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
			playersAttacks = 0;
			startedToCheckPlayerAttacks = false;
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

		registered = false;
		hasAlreadyFoundErrorGetGameState = false;
		hasAlreadyFoundErrorGetAOEffectInfo = false;
		hasAlreadyFoundErrorCheckPlayerAttack = false;

		canCheckPlayerAttacks = true;

		gridManager.DestroyAndClearGridObjects();
		AOEffectInfo = null;
		gameProcessID = "";

		canvasManager.ExitGame(error);
	}

	public void RegisterToGame(Action<bool> callback, bool onlyPay)
	{
		StartCoroutine(RegisterToGameCoroutine(callback, onlyPay));
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
				if (registered)
				{
					localPlayerState = AOEffectPlayerState.WaitingPaid;
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
					registered = true;

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
			registered = true;

			yield return new WaitForSeconds(1); //Wait other two seconds to be sure that everything has been saved

			registrationCallback.Invoke(true);
		}
	}

	private void CheckPlayerAttacks(bool result, string jsonString)
	{
		if (!result)
		{
			Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
			canCheckPlayerAttacks = false;
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
				if (hasAlreadyFoundErrorCheckPlayerAttack || AOSManager.main == null)
				{
					Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
					canCheckPlayerAttacks = false;
				}
				else
				{
					hasAlreadyFoundErrorCheckPlayerAttack = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
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

				if(lastPlayerAttacksArray.Count > playersAttacks)
				{
					for(int i = playersAttacks; i <  lastPlayerAttacksArray.Count; i++)
					{
						string player = lastPlayerAttacksArray[i]["Player"];
						string target = lastPlayerAttacksArray[i]["Target"];

						AOEffectPlayer p = gameState.players.Find(p => p.data.id == player);
						AOEffectPlayer t = gameState.players.Find(p => p.data.id == target);

						bool canContinue = true;

						if(p == null)
						{
							canContinue = false;
							Debug.LogError("Can't find player " + player);
						}

						if (t == null)
						{
							canContinue = false;
							Debug.LogError("Can't find player " + target);
						}

						if(!canContinue)
						{
							return;
						}

						p.ShootTarget(t);
					}

					playersAttacks = lastPlayerAttacksArray.Count;
				}
			}
			else
			{
				if (hasAlreadyFoundErrorCheckPlayerAttack || AOSManager.main == null)
				{
					Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
					canCheckPlayerAttacks = false;
				}
				else
				{
					hasAlreadyFoundErrorCheckPlayerAttack = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks, 3));
				}
			}
		}
		else
		{
			if (hasAlreadyFoundErrorCheckPlayerAttack || AOSManager.main == null)
			{
				Debug.LogWarning("Base AOEffect Game not supporting additional info player attacks");
				canCheckPlayerAttacks = false;
			}
			else
			{
				hasAlreadyFoundErrorCheckPlayerAttack = true;
				AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
				StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks, 3));
			}
		}

		if(gameState.gameMode == GameMode.Playing && canCheckPlayerAttacks)
		{
			StartCoroutine(SendPostRequest("GetGameAttacksInfo", CheckPlayerAttacks));
		}
	}

	private void UpdateAOEffectInfo(bool result, string jsonString)
	{
		if (!result)
		{
			Debug.LogWarning("Base AOEffect Game not supporting additional info");
			gridManager.CreateGrid();
			if(waitAOEffectData) StartCoroutine(SendPostRequest("GetGameState", UpdateGameState));
			//canvasManager.ExitGame("Error while loading AOEffect Game state. Check game process ID or internet connection");
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

				AOEffectInfo = new AOEffectInfo();
				AOEffectInfo.width = jsonNode["Width"];
				AOEffectInfo.height = jsonNode["Height"];
				AOEffectInfo.maxEnergy = jsonNode["MaxEnergy"];
				AOEffectInfo.energyPerSec = jsonNode["EnergyPerSec"];
				AOEffectInfo.range = jsonNode["Range"];
				AOEffectInfo.averageMaxStrengthHitsToKill = jsonNode["AverageMaxStrengthHitsToKill"];

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

	private void UpdateGameState(bool result, string jsonString)
	{
        if (!result)
        {
			ExitGame("Error while loading AOEffect Game state. Check game process ID or internet connection");
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
				if(hasAlreadyFoundErrorGetGameState || AOSManager.main == null)
				{
					Debug.LogError("Game State Data is null!");
					canvasManager.ExitGame("Game State Data is null!");
				}
				else
				{
					hasAlreadyFoundErrorGetGameState = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
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

			if(jsonNode.HasKey("TimeRemaining"))
			{
				hasAlreadyFoundErrorGetGameState = false;

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

					if (oldGameMode != gameState.gameMode)
					{
						OnGameModeChanged?.Invoke(oldGameMode, gameState.gameMode);
					}
				}
				else
				{
					Debug.LogError("Game mode is " + jsonNode["GameMode"].ToString());
				}

				JSONNode timeRemaining = jsonNode["TimeRemaining"];
				float newTimeRemaining = timeRemaining.AsLong / 1000.0f;

                if (lastTimeRemaining != newTimeRemaining) //Only if is changed
                {
					lastTimeRemaining = newTimeRemaining;
					gameState.timeRemaining = timeRemaining.AsLong / 1000.0f;
					canvasManager.UpdateTimerText(gameState.timeRemaining);
                }

				if (jsonNode.HasKey("WaitingPlayers"))
				{
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
					if (hasAlreadyFoundErrorGetGameState || AOSManager.main == null)
					{
						canvasManager.ExitGame("Game state not recognized");
						Debug.LogError("Game state not recognized");
						return;
					}
					else
					{
						hasAlreadyFoundErrorGetGameState = true;
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
						StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
					}
				}
			}
			else
			{
				if(jsonString.Contains("Not enough players registered!"))
				{
					Debug.LogWarning("Skipping State: " + jsonString);
				}
				else
				{
					if (hasAlreadyFoundErrorGetGameState || AOSManager.main == null)
					{
						canvasManager.ExitGame("TimeRemaining not found!");
						Debug.LogError("NO KEY TimeRemaining: " + jsonString);
						return;
					}
					else
					{
						hasAlreadyFoundErrorGetGameState = true;
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
						StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
					}
				}
			}
		}
		else
		{
			if (jsonString.Contains("Not enough players registered!"))
			{
				Debug.LogWarning("Skipping State: " + jsonString);
			}
			else
			{
				if (hasAlreadyFoundErrorGetGameState || AOSManager.main == null)
				{
					canvasManager.ExitGame("Messages key not found!");
					Debug.LogError("NO KEY Messages: " + jsonString);
					return;
				}
				else
				{
					hasAlreadyFoundErrorGetGameState = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					StartCoroutine(SendPostRequest("GetGameState", UpdateGameState, 5));
				}
			}
		}

		UpdateLocalPlayerState();
	}

	private IEnumerator SendPostRequest(string actionID, Action<bool,string> callback, float delay = 0)
	{
		if(delay > 0)
		{
			yield return new WaitForSeconds(delay);
		}

		// Define the URL
		string url = baseUrl + gameProcessID;

		// Define the request body
		string jsonBody = baseJsonBody.Replace("GAMEPROCESSID", gameProcessID).Replace("ACTIONID", actionID);

		// Create a UnityWebRequest object
		UnityWebRequest request = new UnityWebRequest(url, "POST");

		// Set the request body
		byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
		request.uploadHandler = new UploadHandlerRaw(bodyRaw);
		request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
		request.timeout = 5;
		// Set the request headers
		request.SetRequestHeader("Content-Type", "application/json");

		// Send the request and wait for the response
		yield return request.SendWebRequest();


		// Check for errors
		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("Error: " + request.error);
			callback.Invoke(false, "Error: " + request.error);
		}
		else
		{
			// Print the response
			if(string.IsNullOrEmpty(request.downloadHandler.text))
			{
				Debug.LogError("JSON is null!");
				callback.Invoke(false, "JSON is null!");
			}
			else
			{
				callback.Invoke(true, request.downloadHandler.text);
				//UpdateGameState(request.downloadHandler.text);
			}
		}
	}
}
