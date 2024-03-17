using System.Collections;
using TMPro;
using UnityEngine;
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

	private bool waitForAnswer = false;

	private Coroutine gameInfoTextClearCoroutine;

	void Start()
    {
        AOSManager.OnAOSStateChanged += OnAOSStateChanged;

		playGameButton.onClick.AddListener(OnPlayGameButtonClicked);
        exitGameButton.onClick.AddListener(() => manager.ExitGame());

		registerToGameButton.onClick.AddListener(RegisterToGame);
		openMovePadButton.onClick.AddListener(OpenMovePad);
		closeMovePadButton.onClick.AddListener(CloseMovePad);

		if(AOSManager.main == null)
		{
			SoundManager.main.PlayGameAudio();
		}
	}

	private void Update()
	{
		//registerToGameButton.gameObject.SetActive(manager.gameState.GameMode == GameMode.Waiting && manager.LocalPlayerExists && !manager.LocalPlayerIsRegistered && !waitForRegistration);
		registerToGameButton.gameObject.SetActive(manager.LocalPlayerExists && !waitForAnswer && manager.gameState.gameMode != GameMode.None && (manager.localPlayerState == AOEffectPlayerState.None || manager.localPlayerState == AOEffectPlayerState.Waiting));
		movePadPanel.SetActive(manager.LocalPlayerExists && manager.gameState.gameMode == GameMode.Playing && manager.localPlayerState == AOEffectPlayerState.Playing);
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

	private void RegistrationCallback(bool result)
	{
		waitForAnswer = false;
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
