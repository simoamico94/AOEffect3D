using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class AOEffectCanvasManager : MonoBehaviour
{
	[Header("AOEffectCanvasManager")]
	public AOEffectManager manager;
	public float gameInfoTextShowTime = 8;

    [Header("IntroUI")]
    [SerializeField] private GameObject introPanel;
	[SerializeField] private Button playGameButton;
	[SerializeField] private TMP_InputField gameProcessInputField;

    [Header("GameUI")]
	[SerializeField] private GameObject gamePanel;
	[SerializeField] private Button exitGameButton;

	[SerializeField] private TMP_Text infoText;
	[SerializeField] private TMP_Text errorText;

    [Header("CommandsUI")]
	[SerializeField] private Button registerToGameButton;
	[SerializeField] private GameObject movePadPanel;
	[SerializeField] private GameObject movePadGrid;
	[SerializeField] private Button openMovePadButton;
	[SerializeField] private Button closeMovePadButton;
	[SerializeField] private TMP_Text gameInfoText;

	private bool waitForRegistration = false;

	private Coroutine gameInfoTextClearCoroutine;

	void Start()
    {
        AOSManager.OnAOSStateChanged += OnAOSStateChanged;

		playGameButton.onClick.AddListener(OnPlayGameButtonClicked);
        exitGameButton.onClick.AddListener(() => manager.ExitGame());

		registerToGameButton.onClick.AddListener(RegisterToGame);
		openMovePadButton.onClick.AddListener(OpenMovePad);
		closeMovePadButton.onClick.AddListener(CloseMovePad);
    }

	private void Update()
	{
		registerToGameButton.gameObject.SetActive(AOEffectManager.main.gameState.GameMode == GameMode.Waiting && AOEffectManager.main.LocalPlayerExists && !AOEffectManager.main.LocalPlayerIsRegistered && !waitForRegistration);
		movePadPanel.SetActive(AOEffectManager.main.gameState.GameMode == GameMode.Playing && AOEffectManager.main.LocalPlayerIsRegistered);
	}

	public void UpdateTimerText(float time)
    {
        if(manager.gameState.GameMode == GameMode.Playing)
        {
            infoText.text = $"Game will end in {Mathf.RoundToInt(time)} s";
		}
		else if (manager.gameState.GameMode == GameMode.Waiting)
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
        ShowIntroPanel();
		waitForRegistration = false;
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
		waitForRegistration = true;
		manager.RegisterToGame(RegistrationCallback);
    }

	private void RegistrationCallback(bool result)
	{
		waitForRegistration = false;
		if(result)
		{
			SetGameInfoText("Registration completed!");
		}
		else
		{
			SetGameInfoText("Problems with registration, retry");
		}
	}
}
