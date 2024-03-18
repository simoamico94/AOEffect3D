using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class AOEffectCanvasManager : MonoBehaviour
{
	public AOEffectManager manager => AOEffectManager.main;

	[Header("AOEffectCanvasManager")]
	public float gameInfoTextShowTime = 8;

    [Header("IntroUI")]
    [SerializeField] private GameObject introPanel;
	[SerializeField] private Button playGameButton;
	[SerializeField] private TMP_InputField gameProcessInputField;
	[SerializeField] private TMP_Text errorText;

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

	private Coroutine gameInfoTextClearCoroutine;

	void Start()
    {
        AOSManager.OnAOSStateChanged += OnAOSStateChanged;

		playGameButton.onClick.AddListener(OnPlayGameButtonClicked);
        exitGameButton.onClick.AddListener(() => manager.ExitGame());

		registerToGameButton.onClick.AddListener(RegisterToGame);
		registerToGameButtonOldGame.onClick.AddListener(RegisterToGame);
		openMovePadButton.onClick.AddListener(OpenMovePad);
		closeMovePadButton.onClick.AddListener(CloseMovePad);

		if(AOSManager.main == null)
		{
			SoundManager.main.PlayGameAudio();
		}

		manager.OnGameModeChanged += OnGameModeChanged;
	}

	private void Update()
	{
		registerToGameButtonOldGame.gameObject.SetActive(!manager.WaitingExists && manager.LocalPlayerExists && !waitForAnswer && manager.gameState.gameMode != GameMode.None && (manager.localPlayerState == AOEffectPlayerState.None || manager.localPlayerState == AOEffectPlayerState.Waiting));
		registerToGameButton.gameObject.SetActive(manager.WaitingExists && manager.LocalPlayerExists && !waitForAnswer && manager.gameState.gameMode != GameMode.None && (manager.localPlayerState == AOEffectPlayerState.None || manager.localPlayerState == AOEffectPlayerState.Waiting));
		movePadPanel.SetActive(manager.LocalPlayerExists && manager.gameState.gameMode == GameMode.Playing && manager.localPlayerState == AOEffectPlayerState.Playing);
		leaderboardAC.gameObject.SetActive(manager.gameState.gameMode == GameMode.Playing);
		waitingPanel.gameObject.SetActive(manager.gameState.gameMode == GameMode.Waiting && manager.WaitingExists);
	}

	private void OnGameModeChanged(GameMode oldGM, GameMode newGM)
	{
		if (oldGM == GameMode.Playing && newGM == GameMode.Waiting)
		{
			if(playerInfoCanvases != null && playerInfoCanvases.Count > 0)
			{
				//Winner
				winningPanel.SetActive(true);
				winningContentText.text = "";

				foreach (var player in playerInfoCanvases)
				{
					if(player.player.data.health > 0)
					{
						winningContentText.text += player.player.data.id + "\n\n";
					}

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
			winningPanel.SetActive(false);
			winningContentText.text = "";
		}

		if(newGM == GameMode.Waiting) //Maybe not working when game doesn't start
		{
			StartCoroutine(DelayButtonToRegister());
		}

		if(newGM == GameMode.Playing)
		{
			if (AOSManager.main != null && AOSManager.main.consoleListeners.ContainsKey("Congratulations! The game has ended."))
			{
				AOSManager.main.consoleListeners.Add("Congratulations! The game has ended.", ShowWinnerPanel);
			}
		}
		else if(oldGM == GameMode.Playing)
		{
			if (AOSManager.main != null && AOSManager.main.consoleListeners.ContainsKey("Congratulations! The game has ended."))
			{
				AOSManager.main.consoleListeners.Remove("Congratulations! The game has ended.");
			}
		}
	}

	private void ShowWinnerPanel(string winMessage)
	{

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
        if(manager.gameState.gameMode == GameMode.Playing)
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
        introPanel.SetActive(true);
		gamePanel.SetActive(false);
	}

	public void ShowGamePanel()
    {
		introPanel.SetActive(false);
		gamePanel.SetActive(true);
    }

    public void HideAll()
    {
		introPanel.SetActive(false);
		gamePanel.SetActive(false);
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
			errorText.text = error;
		}
        else
        {
			errorText.text = "";
		}
	}

	private void OnAOSStateChanged(AOSState state)
	{
		if (state == AOSState.LoggedIn)
		{
			ShowIntroPanel();
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
            errorText.text = "Arena process ID is empty! You need to insert a valid arena process ID to continue";
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
		waitForAnswer = true;
		yield return new WaitForSeconds(5);
		waitForAnswer = false;
	}

	private void RegistrationCallback(bool result)
	{
		waitForAnswer = false;
		if(result)
		{
			if(!manager.WaitingExists) SetGameInfoText("Registration completed!");
		}
		else
		{
			SetGameInfoText("Problems with registration, retry");
		}
	}
}
