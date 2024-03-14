using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using SimpleFileBrowser;
using System;

public enum AOSState
{
    LoggedOut = 0,
    Loading = 1,
    LoggedIn = 2
}

public class AOSManager : MonoBehaviour
{
	[Header("General")]
	public bool loadBlueprints;
    public bool userFriendlyLogs;
	public float loginTimeout;
	public bool useColorsInConsole;

    [SerializeField] private AOSState state;
	public AOSState State
	{
		get { return state; }
		set { if (state != value) { state = value;  OnStateChanged(); OnAOSStateChanged?.Invoke(state); } }
	}
	public static Action<AOSState> OnAOSStateChanged;

	[Header("Login UI")]
	public GameObject loginPanel;
	public GameObject loadingPanel;
	public TMP_InputField usernameInputField;
	public TMP_InputField walletLocationInputField;
	public Button walletFilePickerButton;
	public Toggle loadBlueprintsToggle;
	public Button loginButton;
	public TMP_Text errorText;

	[Header("Console UI")]
    public GameObject mainPanel;
    public GameObject console;
    public TMP_Text consoleText;
    public TMP_InputField consoleInputField;
	public ScrollRect scrollRect;
    public Button showConsoleButton;
    public Button hideConsoleButton;
    public Button clearConsoleButton;
    public Button logoutButton;

	[Header("UpdatePopupUI")]
	public GameObject updatePopup;
	public Button yesButton;
	public Button noButton;

	private InteractiveCMDShell shell;
	private List<string> newLines = new List<string>();

	private const string forceLogoutString = "FORCELOGOUT";
	private Coroutine waitAndStartCoroutine;
	private bool updating = false;
	private List<string> commandsHistory = new List<string>();
	private int commandsHistoryIndex = 0;

	[Header("Debug")]
	public bool debug;
	public bool logLines;
	[SerializeField] private List<string> lineBuffer = new List<string>();

	public static string ProcessName;

	void Start()
    {
		// Example Lua data
		//string luaData = "{TimeRemaining = 1183029,GameMode = \"Playing\", Players = {kA690iTy4O_Bk-NJtzTknuVpk1E-Fw3LHribdLO3mgs = {y = 22,x = 38,energy = 0,health = 100 }, IFFDYAq1dldTGljfjdd7GFf9sKvrOWwi0y59XzPolQA = {    y = 21,     x = 23,    energy = 0,    health = 100    }  } }";

		FileBrowser.SetFilters(true, new FileBrowser.Filter("Wallet", ".json")/*, new FileBrowser.Filter("Lua files", ".lua")*/);

		loadBlueprintsToggle.onValueChanged.AddListener((bool value) => loadBlueprints = value);
		loginButton.onClick.AddListener(() => Login());
		walletFilePickerButton.onClick.AddListener(() => OpenWalletBrowser());

		showConsoleButton.onClick.AddListener(() => console.SetActive(true));
		hideConsoleButton.onClick.AddListener(() => console.SetActive(false));
		clearConsoleButton.onClick.AddListener(() => ClearShell());
		logoutButton.onClick.AddListener(() => Logout());

		yesButton.onClick.AddListener(() => { shell.RunCommand("y"); updatePopup.SetActive(false); ClearShell(); updating = true; StartCoroutine(WaitUpdateAndLogin()); });
		noButton.onClick.AddListener(() => { shell.RunCommand("n"); updatePopup.SetActive(false); waitAndStartCoroutine = StartCoroutine(WaitAndFinalizeLogin()); });

        StartShell();
	}

    void Update()
    {
        if(shell != null)
        {
            GetRecentLines();
		}

        if(Input.GetKeyDown(KeyCode.Return))
        {
            RunInputFieldCommand();
		}

		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}

		if(Input.GetKeyDown(KeyCode.UpArrow))
		{
			if(commandsHistoryIndex >= 1)
			{
				commandsHistoryIndex--;
				consoleInputField.text = commandsHistory[commandsHistoryIndex];
			}
		}

		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			if(commandsHistoryIndex < commandsHistory.Count - 1)
			{
				commandsHistoryIndex++;
				consoleInputField.text = commandsHistory[commandsHistoryIndex];
			}
			else if( commandsHistoryIndex == commandsHistory.Count - 1)
			{
				commandsHistoryIndex++;
				consoleInputField.text = "";
			}
		}
	}

	private void OnDestroy()
	{
		StopShell();
	}

	public void StartShell()
    {
		if (shell == null)
        {
			shell = new InteractiveCMDShell();
        }
        else
        {
            RestartShell();
        }
	}

    public void RestartShell()
    {
		StopShell();
		StartShell();
	}

    public void StopShell()
    {
		if(shell != null)
		{
			shell.Stop();
			shell = null;
		}
	}

	public void ClearShell()
	{
		consoleText.text = "";
		lineBuffer.Clear();
		newLines.Clear();
	}

    public void Login()
    {
		errorText.text = "";
		State = AOSState.Loading;

        string loginString = "aos";

        if(!string.IsNullOrEmpty(usernameInputField.text))
        {
            loginString += " " + usernameInputField.text;
		}

		if (!string.IsNullOrEmpty(walletLocationInputField.text))
		{
			loginString += " --wallet \"" + walletLocationInputField.text + "\"" ;
		}

		Debug.Log("Logging with: " + loginString);
		shell.RunCommand(loginString);
	}

    public void Logout()
    {
		StopAllCoroutines();
		ClearShell();
		RestartShell();
		State = AOSState.LoggedOut;
	}

	public void LogLine(string line)
	{
		consoleText.text += line + "\n";
		if(debug)
		{
			lineBuffer.Add(line);
		}
	}

	public string EvaulateLine(string line)
	{
		string evaluatedLine = "";

		if (line.StartsWith("aos process: "))
		{
			ProcessName = line.Replace("aos process:", "").Trim();
			evaluatedLine = "Welcome " + ProcessName;

			waitAndStartCoroutine = StartCoroutine(WaitAndFinalizeLogin());
		}
		else if (line.Contains("An Error occurred trying to boot AOS"))
		{
			Debug.LogError(line);
			errorText.text += line + "\n";
			evaluatedLine = forceLogoutString;
		}
		else if(line.Contains("New version") && line.Contains("available. Would you like to update [Y/n]?"))
		{
			if(waitAndStartCoroutine != null)
			{
				StopCoroutine(waitAndStartCoroutine);
			}
			else
			{
				Debug.LogError("Shouldn't be null coroutine");
			}

			updatePopup.SetActive(true);

			evaluatedLine = line;
		}
		else if (line.Contains("Updated"))
		{
			updating = false;
			evaluatedLine = line;
		}
		else
		{
			evaluatedLine = line;
		}

		if (!userFriendlyLogs) //Ignore formatting
		{
			evaluatedLine = line;
		}

		return evaluatedLine;
	}

	protected void RunInputFieldCommand()
	{
		if (!string.IsNullOrEmpty(consoleInputField.text))
		{
			shell.RunCommand(consoleInputField.text);
			commandsHistory.Add(consoleInputField.text);
			commandsHistoryIndex = commandsHistory.Count;
			consoleInputField.text = "";
		}
	}

	protected void GetRecentLines()
	{
		shell.GetRecentLines(newLines);
		if (newLines.Count > 0)
		{
			foreach (string line in newLines)
			{
				if(logLines)
				{
					Debug.Log(line);
				}

				string convertedLine = ANSIConverter.ConvertANSIToTextMeshPro(line, useColorsInConsole);
				string evaluated = EvaulateLine(convertedLine);

				if (evaluated == forceLogoutString)
				{
					Logout();
					break;
				}

				if (!string.IsNullOrEmpty(evaluated))
				{
					LogLine(evaluated);
				}
			}

			newLines.Clear();
			StartCoroutine(ScrollDown());
		}
	}

	protected IEnumerator ScrollDown()
	{
		yield return null;
		scrollRect.ScrollToBottom();
	}

	protected void OnStateChanged()
	{
		switch (state)
		{
			case AOSState.LoggedOut:
				loginPanel.SetActive(true);
				loadingPanel.SetActive(false);
				mainPanel.SetActive(false);
				break;
			case AOSState.Loading:
				loginPanel.SetActive(false);
				loadingPanel.SetActive(true);
				mainPanel.SetActive(false);
				break;
			case AOSState.LoggedIn:
				loginPanel.SetActive(false);
				loadingPanel.SetActive(false);
				mainPanel.SetActive(true);
				break;
		}
	}

	protected IEnumerator WaitAndFinalizeLogin()
	{
		string currentShellLine = ANSIConverter.ConvertANSIToTextMeshPro(shell.GetCurrentLine(), useColorsInConsole);
		float timeElapsed = 0;

		while (!currentShellLine.Contains(">") && timeElapsed < loginTimeout)
		{
			currentShellLine = ANSIConverter.ConvertANSIToTextMeshPro(shell.GetCurrentLine(), useColorsInConsole);
			timeElapsed += Time.deltaTime;
			yield return null;
		}

		if(timeElapsed >= loginTimeout)
		{
			errorText.text += "Timed out. Retry\nIf problems persist check if there are any AOS updates available through normal cmd";
			Logout();
		}
		else
		{
			Debug.Log(currentShellLine);

			yield return new WaitForSeconds(0.5f);

			State = AOSState.LoggedIn;

			if (loadBlueprints)
			{
				StartCoroutine(LoadBlueprints());
			}
		}
	}

	protected IEnumerator WaitUpdateAndLogin()
	{
		while(updating)
		{
			yield return null;
		}

		yield return null;

		Login();
	}

	protected IEnumerator LoadBlueprints()
    {
		string directoryPath = Application.dataPath + "/StreamingAssets";

		// Check if the directory exists
		if (Directory.Exists(directoryPath))
		{
			// Get all files in the directory
			string[] files = Directory.GetFiles(directoryPath);

			// Iterate through each file
			foreach (string file in files)
			{
				// Check if the file has a .lua extension
				if (Path.GetExtension(file).Equals(".lua", System.StringComparison.OrdinalIgnoreCase))
				{
					// Get the name of the file
					string fileName = Path.GetFileName(file);
					Debug.Log("Loading lua file found: " + fileName);

					shell.RunCommand(".load " + fileName);

					yield return new WaitForSeconds(0.5f);
				}
			}
		}
		else
		{
			Debug.LogError("Directory not found: " + directoryPath);
		}
	}

	protected void OpenWalletBrowser()
	{
		FileBrowser.SetDefaultFilter(".json");
		StartCoroutine(ShowLoadWalletDialogCoroutine());
	}

	protected IEnumerator ShowLoadWalletDialogCoroutine()
	{
		// Show a load file dialog and wait for a response from user
		// Load file/folder: file, Allow multiple selection: true
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Load File", Submit button text: "Load"
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Select Wallet", "Load");

		// Dialog is closed
		// Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
		if (FileBrowser.Success)
		{
			walletLocationInputField.text = FileBrowser.Result[0];
		}
		else
		{
			Debug.LogError("Nothing selected");
		}	 
	}

	public IEnumerator RegisterAndPay()
	{
		yield return null;
	}

	public IEnumerator Pay()
	{
		yield return null;
	}
}
