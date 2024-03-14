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
	[Header("AOEffectManager")]
	public GameState gameState;
	public AOEffectPlayer playerPrefab;
	public GridManager gridManager;
	public AOEffectCanvasManager canvasManager;

	//Find a way to add players at the beginning while waiting -- maybe asking the status also before. That would be nice also to update timer while waiting and also to update lobby 

	[Header("Polling Settings")]
	public string gameProcessID;
	public bool pollGameData = false;
	public float pollingIntervalDuringWaiting = 5;
	public float pollingIntervalDuringGame = 0;

	private string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";
	private string baseJsonBody = @"{
            ""Id"":""1234"",
            ""Target"":""ARENAPROCESSID"",
            ""Owner"":""1234"",
            ""Tags"":[{""name"":""Action"",""value"":""GetGameState""}]
        }";

	[Header("Debug")]
	public string debugData;

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

		canvasManager.ExitGame();
	}

	private void UpdateGameState(string jsonString)
	{
		// Parse JSON string
		JSONNode fullJsonNode = JSON.Parse(jsonString);

		// Populate GameState
		if(fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			Debug.Log(jsonString);

			JSONNode jsonNode = JSON.Parse(jsonString);
			var keys = jsonNode.Keys;

			if(jsonNode.HasKey("TimeRemaining"))
			{
				if(jsonNode["GameMode"] == "Playing")
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
							if(playerId == AOSManager.ProcessName)
							{
								player.isLocalPlayer = true;
							}

							gameState.players.Add(player);
						}

						AOEffectPlayerData newData = new AOEffectPlayerData();

						newData.id = playerId;
					
						Transform newPos = gridManager.GetGridPos(playerNode["x"].AsInt - 1, playerNode["y"].AsInt - 1);

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
					canvasManager.ExitGame("Error with AOEffect Game");
					Debug.LogError("Game State is NONE!");
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
					canvasManager.ExitGame("Error with AOEffect Game");
					Debug.LogError("NO KEY MESSAGES: " + jsonString);
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
				canvasManager.ExitGame("Error with AOEffect Game");
				Debug.LogError("NO KEY MESSAGES: " + jsonString);
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
		//string url = "https://cu.ao-testnet.xyz/dry-run?process-id=GxCjFNlapOKgFGhuNoT2bc7FY5FyXoLGXjTD6clXHcQ";
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
