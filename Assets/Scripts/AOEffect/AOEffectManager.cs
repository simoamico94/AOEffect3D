using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using UnityEngine.Networking;
using System.Text;

[Serializable]
public struct GameState
{
	public float TimeRemaining;
	public GameMode GameMode;
	public List<AOEffectPlayer> players;
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
	public bool LocalPlayerIsRegistered => registered || (AOSManager.main != null && !string.IsNullOrEmpty(AOSManager.main.processID) && gameState.players != null && gameState.players.Count > 0 && gameState.players.Exists(p => p.isLocalPlayer));

	[Header("AOEffectManager")]
	public GameState gameState;
	public AOEffectCanvasManager canvasManager;
	public GridManager gridManager;
	public AOEffectPlayer playerPrefab;

	//Find a way to add players at the beginning while waiting -- maybe asking the status also before. That would be nice also to update timer while waiting and also to update lobby 

	[Header("Polling Settings")]
	public string gameProcessID;
	public bool pollGameData = false;
	public float pollingIntervalDuringWaiting = 5;
	public float pollingIntervalDuringGame = 0;

	public bool doTick;
	public float tickTime;
	private float elapsedTickTime = 0;

	private string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";
	private string baseJsonBody = @"{
            ""Id"":""1234"",
            ""Target"":""ARENAPROCESSID"",
            ""Owner"":""1234"",
            ""Tags"":[{""name"":""Action"",""value"":""GetGameState""}]
        }";

	[Header("Debug")]
	public string debugData;
	public bool logGameState;

	private Coroutine sendPostRequest;
	private Coroutine regiterCoroutine;

	private bool registered = false;

	private bool hasAlreadyFoundError = false;

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
		if(gameState.GameMode != GameMode.None && AOSManager.main != null && doTick)
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

			if(gameState.TimeRemaining != 0)
			{
				gameState.TimeRemaining -= Time.deltaTime;

				if(gameState.TimeRemaining < 0)
				{ 
					gameState.TimeRemaining = 0;
				}

				canvasManager.UpdateTimerText(gameState.TimeRemaining);
			}
		}
	}

	public void LoadGame(string processID)
	{
		StopAllCoroutines();
		gameProcessID = processID;
		StartCoroutine(SendPostRequest());
	}

	public void ExitGame()
	{
		StopAllCoroutines();

		if(gameState.GameMode != GameMode.None)
		{
			gameState.GameMode = GameMode.None;
			if(gameState.players != null && gameState.players.Count > 0)
			{
				foreach(AOEffectPlayer p in gameState.players)
				{
					Destroy(p.gameObject);
				}
				gameState.players.Clear();
			}
			gameState.TimeRemaining = 0;
		}

		registered = false;
		hasAlreadyFoundError = false;

		canvasManager.ExitGame();
	}

	public void RegisterToGame(Action<bool> callback)
	{
		regiterCoroutine = StartCoroutine(RegisterToGameCoroutine(callback));
	}

	private IEnumerator RegisterToGameCoroutine(Action<bool> registrationCallback)
	{
		yield return null;

		bool done = false;
		float elapsedTime = 0;
		float timeOut = 10;

		Action callback = () => { done = true; }; 

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
			registrationCallback.Invoke(true);
		}
	}

	private void UpdateGameState(string jsonString)
	{
		// Parse JSON string
		JSONNode fullJsonNode = JSON.Parse(jsonString);

		// Populate GameState
		if(fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if(string.IsNullOrEmpty(jsonString))
			{
				if(hasAlreadyFoundError || AOSManager.main == null)
				{
					Debug.LogError("Game State Data is null!");
					canvasManager.ExitGame("Game State Data is null!");
				}
				else
				{
					hasAlreadyFoundError = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					StartCoroutine(SendPostRequest(5));
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
				hasAlreadyFoundError = false;

				if (jsonNode["GameMode"] == "Playing")
				{
					gameState.GameMode = GameMode.Playing;
				}
				else if(jsonNode["GameMode"] == "Waiting")
				{
					gameState.GameMode = GameMode.Waiting;
				}
				else
				{
					Debug.LogError("Game mode is " + jsonNode["GameMode"].ToString());
				}

				JSONNode timeRemaining = jsonNode["TimeRemaining"];
				gameState.TimeRemaining = timeRemaining.AsLong / 1000.0f;
				canvasManager.UpdateTimerText(gameState.TimeRemaining);

				JSONNode playersNode = jsonNode["Players"];
			
				if(gameState.GameMode == GameMode.Playing)
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

						newData.pos = newPos.position;
						newData.energy = playerNode["energy"].AsInt;
						newData.health = playerNode["health"].AsInt;

						player.UpdateData(newData);
						player.State = AOEffectPlayerState.Playing;
					}

					if(pollGameData)
					{
						StartCoroutine(SendPostRequest(pollingIntervalDuringGame));
					}
				}
				else if (gameState.GameMode == GameMode.Waiting)
				{
					if(gameState.players != null && gameState.players.Count > 0)
					{
						foreach(AOEffectPlayer player in gameState.players)
						{
							player.State = AOEffectPlayerState.Waiting;
						}
					}
					
					if(pollGameData)
					{
						if(gameState.TimeRemaining > pollingIntervalDuringWaiting)
						{
							StartCoroutine(SendPostRequest(pollingIntervalDuringWaiting));
						}
						else
						{
							StartCoroutine(SendPostRequest(pollingIntervalDuringGame));
						}
					}

					//foreach (JSONNode playerIdNode in playersNode.AsArray)
					//{
					//	string playerId = playerIdNode.Value;

					//	AOEffectPlayer player = gameState.players.Find(p => p.playerData.id == playerId);

					//	if(player == null)
					//	{
					//		player = new AOEffectPlayer();
					//	}

					//	AOEffectPlayerData newData = new AOEffectPlayerData();
					//	newData.id = playerId;
					//	player.playerState = AOEffectPlayerState.WaitingPaid;
					//	player.playerData = newData;


					//	gameState.players.Add(player);
					//}
				}
				else
				{
					if (hasAlreadyFoundError || AOSManager.main == null)
					{
						canvasManager.ExitGame("Game state not recognized");
						Debug.LogError("Game state not recognized");
						return;
					}
					else
					{
						hasAlreadyFoundError = true;
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
						StartCoroutine(SendPostRequest(5));
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
					if (hasAlreadyFoundError || AOSManager.main == null)
					{
						canvasManager.ExitGame("TimeRemaining not found!");
						Debug.LogError("NO KEY TimeRemaining: " + jsonString);
						return;
					}
					else
					{
						hasAlreadyFoundError = true;
						AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
						StartCoroutine(SendPostRequest(5));
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
				if (hasAlreadyFoundError || AOSManager.main == null)
				{
					canvasManager.ExitGame("Messages key not found!");
					Debug.LogError("NO KEY Messages: " + jsonString);
					return;
				}
				else
				{
					hasAlreadyFoundError = true;
					AOSManager.main.RunCommand("Send({ Target = Game, Action = \"Tick\"})");
					StartCoroutine(SendPostRequest(5));
				}
			}
		}
	}

	private IEnumerator SendPostRequest(float delay = 0)
	{
		if(delay > 0)
		{
			yield return new WaitForSeconds(delay);
		}

		// Define the URL
		string url = baseUrl + gameProcessID;

		// Define the request body
		string jsonBody = baseJsonBody.Replace("ARENAPROCESSID", gameProcessID);

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
			canvasManager.ExitGame("Error while loading AOEffect Game state. Check game process ID or internet connection");
			Debug.LogError("Error: " + request.error);
		}
		else
		{
			// Print the response
			if(string.IsNullOrEmpty(request.downloadHandler.text))
			{
				canvasManager.ExitGame("Error while loading AOEffect Game state. Check game process ID or internet connection");
				Debug.LogError("JSON is null!");
			}
			else
			{
				UpdateGameState(request.downloadHandler.text);
			}
		}
	}
}
