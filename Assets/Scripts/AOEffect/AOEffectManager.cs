using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using UnityEngine.Networking;
using System.Text;

[Serializable]
public class GameState
{
	public float TimeRemaining;
	public string GameMode;
	public List<AOEffectPlayer> players;
}



public class AOEffectManager : MonoBehaviour
{
	[Header("AOEffectManager")]
	public GameState gameState;
	public AOEffectPlayer playerPrefab;
	public GridManager gridManager;

	public string debugData;
	//Find a way to add players at the beginning while waiting -- maybe asking the status also before. That would be nice also to update timer while waiting and also to update lobby 

	[Header("Polling Settings")]

	public bool pollGameData = false;
	public float pollingIntervalDuringWaiting = 5;
	public float pollingIntervalDuringGame = 0;

	private void Start()
	{
		//ParseGameState(debugData);
		StartCoroutine(SendPostRequest());
	}

	public void ParseGameState(string jsonString)
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

			JSONNode timeRemaining = jsonNode["TimeRemaining"];
			gameState.TimeRemaining = timeRemaining.AsLong / 1000.0f;
			gameState.GameMode = jsonNode["GameMode"];

			JSONNode playersNode = jsonNode["Players"];
			
			if(gameState.GameMode.Equals("Playing"))
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
				}

				if(pollGameData)
				{
					StartCoroutine(SendPostRequest(pollingIntervalDuringGame));
				}
			}
			else
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

		}
		else
		{
			Debug.LogError("NO KEY MESSAGES: " + jsonString);
		}
	}

	IEnumerator SendPostRequest(float delay = 0)
	{
		if(delay > 0)
		{
			yield return new WaitForSeconds(delay);
		}

		// Define the URL
		string url = "https://cu.ao-testnet.xyz/dry-run?process-id=GxCjFNlapOKgFGhuNoT2bc7FY5FyXoLGXjTD6clXHcQ";

		// Define the request body
		string jsonBody = @"{
            ""Id"":""1234"",
            ""Target"":""GxCjFNlapOKgFGhuNoT2bc7FY5FyXoLGXjTD6clXHcQ"",
            ""Owner"":""1234"",
            ""Tags"":[{""name"":""Action"",""value"":""GetGameState""}]
        }";

		// Create a UnityWebRequest object
		UnityWebRequest request = new UnityWebRequest(url, "POST");

		// Set the request body
		byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
		request.uploadHandler = new UploadHandlerRaw(bodyRaw);
		request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

		// Set the request headers
		request.SetRequestHeader("Content-Type", "application/json");

		// Send the request and wait for the response
		yield return request.SendWebRequest();


		// Check for errors
		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("Error: " + request.error);
		}
		else
		{
			// Print the response
			ParseGameState(request.downloadHandler.text);
		}
	}

	//public void ParseGameStateOld(string jsonString)
	//{
	//	// Parse JSON string
	//	JSONNode jsonNode = JSON.Parse(jsonString);

	//	// Populate GameState
	//	gameState.TimeRemaining = jsonNode["TimeRemaining"].AsInt;
	//	gameState.GameMode = jsonNode["GameMode"];

	//	JSONNode playersNode = jsonNode["Players"];
	//	foreach (KeyValuePair<string, JSONNode> kvp in playersNode.AsObject)
	//	{
	//		string playerId = kvp.Key;
	//		JSONNode playerNode = kvp.Value;

	//		AOEffectPlayer player = gameState.players.Find(p => p.playerData.id == playerId);

	//		AOEffectPlayerData newData = new AOEffectPlayerData();

	//		newData.pos = new Vector2(playerNode["x"].AsFloat, playerNode["y"].AsFloat);
	//		newData.energy = playerNode["energy"].AsInt;
	//		newData.health = playerNode["health"].AsInt;

	//		player.UpdateData(newData);
	//	}

	//	//JSONNode playersNode = jsonNode["Players"];
	//	//foreach (KeyValuePair<string, JSONNode> kvp in playersNode.AsObject)
	//	//{
	//	//	string playerId = kvp.Key;
	//	//	JSONNode playerNode = kvp.Value;

	//	//	AOEffectPlayer player = gameState.players.Find(p => p.id == playerId);

	//	//	player.pos = new Vector2(playerNode["x"].AsFloat, playerNode["y"].AsFloat);
	//	//	player.energy = playerNode["energy"].AsInt;
	//	//	player.health = playerNode["health"].AsInt;
	//	//}
	//}

	// Decide how to update the game: maybe we can do a method in the player class
	public void UpdateGame()
	{

	}
}
