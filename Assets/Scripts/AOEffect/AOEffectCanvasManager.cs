using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AOEffectCanvasManager : MonoBehaviour
{
	public AOEffectManager manager => AOEffectManager.main;

	[Header("AOEffectCanvasManager")]
	public float gameInfoTextShowTime = 8;
	public float gameErrorTextShowTime = 8;
	
    [Header("IntroUI")]
    [SerializeField] private GameObject introPanel;
	[SerializeField] private Button playGameButton;
	[SerializeField] private TMP_InputField gameProcessInputField;
	[SerializeField] private TMP_Text errorText;
	[SerializeField] private GameObject generalPanel;

	[Header("Available Arenas")]
	[SerializeField] private Button showAvailableArenasButton;
	[SerializeField] private GameObject availableArenasPanel;
	[SerializeField] private Transform availableArenasParentTransform;
	[SerializeField] private AOEffectInfoPanel arenaInfoPrefab;
	private List<AOEffectInfoPanel> availableArenasObjs = new List<AOEffectInfoPanel>();

	[Header("GameUI")]
	[SerializeField] private GameObject gamePanel;
	[SerializeField] private Button exitGameButton;
	[SerializeField] private TMP_Text infoText;

	[SerializeField] private GameObject winningPanel;
	[SerializeField] private TMP_Text winningContentText;

	[Header("Leaderboard")]
	[SerializeField] private Animator leaderboardAC;
	[SerializeField] private Transform leaderboardContent;
	[SerializeField] private AOEffectPlayerInfoCanvas playerInfoCanvasPrefab;
	private List<AOEffectPlayerInfoCanvas> playerInfoCanvases = new List<AOEffectPlayerInfoCanvas>();

	[Header("Waiting")]
	[SerializeField] private GameObject waitingPanel;
	[SerializeField] private TMP_Text waitingListEntryPrefab;
	[SerializeField] private Transform waitingContent;
	[SerializeField] private Color paidColor;
	[SerializeField] private Color unPaidColor;
	private List<TMP_Text> waitingEntries = new List<TMP_Text>();

    [Header("CommandsUI")]
	[SerializeField] private Button registerToGameButton;
	[SerializeField] private Button registerToGameButtonOldGame;
	[SerializeField] private GameObject movePadPanel;
	[SerializeField] private GameObject movePadGrid;
	[SerializeField] private Button openMovePadButton;
	[SerializeField] private Button closeMovePadButton;
	[SerializeField] private TMP_Text gameInfoText;

	private bool waitForAnswer = false;
	private int eliminationState = -1;

	private Coroutine gameInfoTextClearCoroutine;
	private Coroutine gameErrorTextClearCoroutine;

	void Start()
    {
        AOSManager.OnAOSStateChanged += OnAOSStateChanged;

		playGameButton.onClick.AddListener(OnPlayGameButtonClicked);
        exitGameButton.onClick.AddListener(() => manager.ExitGame());

		showAvailableArenasButton.onClick.AddListener(() => availableArenasPanel.SetActive(true));

		registerToGameButton.onClick.AddListener(RegisterToGame);
		registerToGameButtonOldGame.onClick.AddListener(RegisterToGame);
		openMovePadButton.onClick.AddListener(OpenMovePad);
		closeMovePadButton.onClick.AddListener(CloseMovePad);

		if(AOSManager.main == null)
		{
			SoundManager.main.PlayGameAudio();
			ShowIntroPanel();
			generalPanel.SetActive(true);
		}

		manager.OnGameModeChanged += OnGameModeChanged;

		infoText.text = "Loading The Grid ...";
		exitGameButton.gameObject.SetActive(false);
		ShowGamePanel();
		manager.LoadGame("03I7E-3wkTZa__Bn1Qq5flYrtEQ7NkcoD9Ctg4o2mNI");
	}

	private void Update()
	{
		showAvailableArenasButton.gameObject.SetActive(manager.availableArenas != null && manager.availableArenas.Count > 0);

		if(manager.availableArenas == null || manager.availableArenas.Count == 0)
		{
			availableArenasPanel.SetActive(false);
		}

		registerToGameButtonOldGame.gameObject.SetActive(!manager.waitingSupported && manager.LocalPlayerExists && !waitForAnswer && manager.gameState.gameMode != GameMode.None && (manager.localPlayerState == AOEffectPlayerState.None || manager.localPlayerState == AOEffectPlayerState.Waiting));
		registerToGameButton.gameObject.SetActive(manager.waitingSupported && manager.LocalPlayerExists && !waitForAnswer && manager.gameState.gameMode != GameMode.None && (manager.localPlayerState == AOEffectPlayerState.None || manager.localPlayerState == AOEffectPlayerState.Waiting));
		movePadPanel.SetActive(manager.LocalPlayerExists && manager.gameState.gameMode == GameMode.Playing && manager.localPlayerState == AOEffectPlayerState.Playing);
		leaderboardAC.gameObject.SetActive(manager.gameState.gameMode == GameMode.Playing);
		waitingPanel.gameObject.SetActive(manager.gameState.gameMode == GameMode.Waiting && manager.waitingSupported);
	}

	private void OnGameModeChanged(GameMode oldGM, GameMode newGM)
	{
		if (oldGM == GameMode.Playing && newGM == GameMode.Waiting)
		{
			if(playerInfoCanvases != null && playerInfoCanvases.Count > 0)
			{
				////Winner
				//winningPanel.SetActive(true);
				//winningContentText.text = "";

				foreach (var player in playerInfoCanvases)
				{
					//if(player.player.data.health > 0)
					//{
					//	winningContentText.text += player.player.data.id + "\n\n";
					//}

					Destroy(player.gameObject);
				}

				playerInfoCanvases.Clear();
			}

			leaderboardAC.SetTrigger("Reset");
			leaderboardAC.gameObject.SetActive(false);
		}
		else if(oldGM == GameMode.Waiting && newGM == GameMode.Playing)
		{
			if (waitingEntries != null && waitingEntries.Count > 0)
			{
				foreach (var player in waitingEntries)
				{
					Destroy(player.gameObject);
				}

				waitingEntries.Clear();
			}

			waitingPanel.SetActive(false);
		}

		if(newGM == GameMode.Waiting) //Maybe not working when game doesn't start
		{
			StartCoroutine(DelayButtonToRegister());
		}

		if(newGM == GameMode.Playing) //Start Playing
		{
			if (AOSManager.main != null)
			{
				if (!AOSManager.main.consoleListeners.ContainsKey("Eliminated"))
				{
					AOSManager.main.consoleListeners.Add("Eliminated", SetElimination);
				}

				eliminationState = 0;
			}
			else //We are not playing
			{
				eliminationState = -1;
			}

			winningPanel.SetActive(false);
			winningContentText.text = "";
		}
		else if(oldGM == GameMode.Playing) //Finished Playing
		{
			if (AOSManager.main != null && AOSManager.main.consoleListeners.ContainsKey("Eliminated"))
			{
				AOSManager.main.consoleListeners.Remove("Eliminated");
			}

			//Not working as expected
			//if (eliminationState == 0)
			//{
			//	winningContentText.text = "Congratulations, you won! :D";
			//}
			//else if(eliminationState == 1)
			//{
			//	winningContentText.text = "Sorry, you lose! :(";
			//}
			//else
			//{
			//	winningContentText.text = "";
			//}

			//winningPanel.SetActive(true);

		}
	}

	private void SetElimination(string winMessage)
	{
		Debug.LogWarning("Eliminated");
		eliminationState = 1;
	}

	public void UpdateAvailableArenas()
	{
		if(availableArenasObjs != null && availableArenasObjs.Count > 0)
		{
			foreach (AOEffectInfoPanel arena in availableArenasObjs)
			{
				Destroy(arena.gameObject);
			}
		}

		availableArenasObjs = new List<AOEffectInfoPanel>();

		foreach (AOEffectInfo arena in manager.availableArenas)
		{
			AOEffectInfoPanel panel = Instantiate(arenaInfoPrefab, availableArenasParentTransform);
			panel.UpdateInfo(arena);
			availableArenasObjs.Add(panel);
		}
	}

	public void UpdateLeaderboard()
	{
		List<string> activePlayerIDs = new List<string>();

		foreach (AOEffectPlayer p in manager.gameState.players)
		{
			AOEffectPlayerInfoCanvas c = playerInfoCanvases.Find(canvas => canvas.idText.text == p.data.id);

			if (c == null)
			{
				c = Instantiate(playerInfoCanvasPrefab, leaderboardContent);
				playerInfoCanvases.Add(c);
			}

			c.SetEnergy(p.data.energy.ToString());
			c.SetHealth(p.data.health.ToString());
			c.SetID(p.data.id.ToString());
			c.SetLastTurn(p.data.lastTurn);
			c.SetName(p.data.name);
			c.SetRanking(p.ranking.ToString());
			c.player = p;

			activePlayerIDs.Add(p.data.id);
		}

		for (int i = playerInfoCanvases.Count - 1; i >= 0; i--)
		{
			AOEffectPlayerInfoCanvas canvas = playerInfoCanvases[i];

			if (!activePlayerIDs.Contains(canvas.idText.text))
			{
				Destroy(canvas.gameObject);
				playerInfoCanvases.RemoveAt(i);
			}
		}

		playerInfoCanvases = playerInfoCanvases.OrderBy(p => p.player.ranking).ToList();

		for(int i = 0; i < playerInfoCanvases.Count; i++)
		{
			playerInfoCanvases[i].transform.SetSiblingIndex(i);
		}
	}

	public void UpdateWaitingPanel()
	{
		List<string> activePlayerIDs = new List<string>();

		foreach (var kvp in manager.gameState.waitingPlayers)
		{
			TMP_Text t = waitingEntries.Find(text => text.text == kvp.Key);

			if (t == null)
			{
				t = Instantiate(waitingListEntryPrefab, waitingContent);
				waitingEntries.Add(t);
			}

			t.color = kvp.Value ? paidColor : unPaidColor;
			t.text = kvp.Key;

			activePlayerIDs.Add(kvp.Key);
		}

		for (int i = waitingEntries.Count - 1; i >= 0; i--)
		{
			TMP_Text textElement = waitingEntries[i];

			if (!activePlayerIDs.Contains(textElement.text))
			{
				waitingEntries.RemoveAt(i);
				Destroy(textElement.gameObject);
			}
		}
	}

	public void UpdateTimerText(float time)
    {
		if (time == -1)
		{
			if(manager.gameState.players.Count > 0)
			{
				//The Grid
				infoText.text = $"Remaining players: {manager.gameState.players.Count}";
			}
		}
		else if (manager.gameState.gameMode == GameMode.Playing)
		{
			infoText.text = $"Game will end in {Mathf.RoundToInt(time)} s";
		}
		else if (manager.gameState.gameMode == GameMode.Waiting)
		{
			infoText.text = $"Next game in {Mathf.RoundToInt(time)} s";
		}
		else
		{
			infoText.text = "Problems with AOEffect loading";
		}
	}

	public void SetGameErrorText(string text)
	{
		errorText.text = text;
		if (gameErrorTextClearCoroutine != null)
		{
			StopCoroutine(gameErrorTextClearCoroutine);
		}

		gameErrorTextClearCoroutine = StartCoroutine(GameErrorTextClear());
	}

	IEnumerator GameErrorTextClear()
	{
		yield return new WaitForSeconds(gameErrorTextShowTime);
		errorText.text = "";
	}

	public void SetGameInfoText(string text)
	{
		gameInfoText.text = text;
		if(gameInfoTextClearCoroutine != null)
		{
			StopCoroutine(gameInfoTextClearCoroutine);
		}

		gameInfoTextClearCoroutine = StartCoroutine(GameInfoTextClear());
	}

	IEnumerator GameInfoTextClear()
	{
		yield return new WaitForSeconds(gameInfoTextShowTime);
		gameInfoText.text = "";
	}

	public void ShowIntroPanel()
    {
		manager.GetAvailableArenas();

		introPanel.SetActive(true);
		gamePanel.SetActive(false);
	}

	public void ShowGamePanel()
    {
		introPanel.SetActive(false);
		gamePanel.SetActive(true);
		availableArenasPanel.SetActive(false);
	}

	public void HideAll()
    {
		introPanel.SetActive(false);
		gamePanel.SetActive(false);
		generalPanel.SetActive(false);
	}

	public void ExitGame(string error = null)
    {
		leaderboardAC.SetTrigger("Reset");

		CloseMovePad();

		ShowIntroPanel();
		waitForAnswer = false;
		SetGameInfoText("");

		if (error != null)
        {
			SetGameErrorText(error);
		}
        else
        {
			errorText.text = "";
		}
	}

	public void LogError(string error)
	{

	}

	public void LoadAvailableArena(string gameID)
	{
		gameProcessInputField.text = gameID;
		OnPlayGameButtonClicked();
	}

	private void OnAOSStateChanged(AOSState state)
	{
		if (state == AOSState.LoggedIn)
		{
			ShowIntroPanel();
			generalPanel.SetActive(true);
			SoundManager.main.PlayGameAudio();
		}
		else
		{
			manager.ExitGame();
			HideAll();
		}
	}

	private void OnPlayGameButtonClicked()
    {
        if(string.IsNullOrEmpty(gameProcessInputField.text))
        {
			SetGameErrorText("Arena process ID is empty! You need to insert a valid arena process ID to continue");
        }
        else
        {
			infoText.text = "Loading AOEffect Arena ...";
            ShowGamePanel();
			manager.LoadGame(gameProcessInputField.text);
		}
	}

    private void OpenMovePad()
    {
        movePadGrid.SetActive(true);
        openMovePadButton.gameObject.SetActive(false);
        closeMovePadButton.gameObject.SetActive(true);
    }

	private void CloseMovePad()
	{
		movePadGrid.SetActive(false);
		openMovePadButton.gameObject.SetActive(true);
		closeMovePadButton.gameObject.SetActive(false);
	}

    private void RegisterToGame()
    {
		waitForAnswer = true;

		if(manager.localPlayerState == AOEffectPlayerState.None)
		{
			manager.RegisterToGame(RegistrationCallback, false);
		}
		else if(manager.localPlayerState == AOEffectPlayerState.Waiting)
		{
			manager.RegisterToGame(RegistrationCallback, true);
		}
		else
		{
			Debug.LogError("Should be already registered, but localPlayerState is " + manager.localPlayerState);
		}
    }

	IEnumerator DelayButtonToRegister()
	{
		if(!waitForAnswer)
		{
			waitForAnswer = true;
			yield return new WaitForSeconds(2);
			waitForAnswer = false;
		}
	}

	public void RegistrationCallback(bool result)
	{
		waitForAnswer = false;
		if(result)
		{
			if (!manager.waitingSupported)
			{
				SetGameInfoText("Registration completed!");
			}
			else
			{
				SetGameInfoText("");
			}
		}
		else
		{
			SetGameInfoText("Problems with registration, retry");
		}
	}
}
