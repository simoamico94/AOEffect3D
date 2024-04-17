using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class AOProcess
{
	public string id;
	public string name;
	public string shortId;

	public Dictionary<string, string> tags = new Dictionary<string, string>(); 
}

public class AOConnectManager : MonoBehaviour
{
	public static AOConnectManager main { get; private set; }
	public string CurrentAddress { get => currentAddress; set { currentAddress = value; OnCurrentAddressChanged(); } }
	private string currentAddress = null;

	public AOProcess CurrentProcess { get => currentProcess; set { currentProcess = value; OnCurrentProcessChanged(); } }
	private AOProcess currentProcess;

	public List<AOProcess> processes = new List<AOProcess>();
	public List<string> availableLuaFiles = new List<string>();

	[Header("UI")]
	public GameObject walletConnectedPanel;
	public GameObject walletNotConnectedPanel;
	public GameObject loadingPanel;
	public Button connectWalletButton;
	public Button processSettingsButton;
	public TMP_Text activeWalletText;

	[Header("ProcessInfoPanel")]
	public ProcessInfoButton processInfoButtonPrefab;
	public List<ProcessInfoButton> processInfoButtons;
	public Transform processInfoButtonParent;
	public Button spawnNewProcessButton;
	public TMP_InputField spawnNewProcessInputField;

	[Header("SendMessage")]
	public TMP_InputField inputFieldPid;
	public TMP_InputField inputFieldData;
	public TMP_InputField inputFieldAction;
	public Button sendMessageButton;

	[Header("LuaSettings")]
	public LoadLuaElement loadLuaElementPrefab;
	public List<LoadLuaElement> loadLuaElements;
	public Transform loadLuaElementParent;

	[DllImport("__Internal")]
	private static extern void SendMessageJS(string pid, string data, string action);

	[DllImport("__Internal")]
	private static extern void ConnectWalletJS();

	[DllImport("__Internal")]
	private static extern void FetchProcessesJS(string address);

	[DllImport("__Internal")]
	private static extern void SpawnProcessJS(string name);

	[DllImport("__Internal")]
	private static extern void AlertMessageJS(string message);

	void Awake()
	{
		if (main == null)
		{
			main = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	IEnumerator Start()
	{
		spawnNewProcessButton.onClick.AddListener(SpawnProcess);
		connectWalletButton.onClick.AddListener(ConnectWallet);
		sendMessageButton.onClick.AddListener(SendCustomMessage);
		processSettingsButton.onClick.AddListener(TryOpenProcessSettings);

		if(!Application.isEditor && Application.platform == RuntimePlatform.WebGLPlayer)
		{
			Debug.LogError("Retrieving Lua Files...");
			foreach(string fileName in availableLuaFiles)
			{
				string url = Application.streamingAssetsPath + "/" + fileName + ".lua";

				using (UnityWebRequest request = UnityWebRequest.Get(url))
				{
					yield return request.SendWebRequest();

					if (request.isNetworkError || request.isHttpError)
					{
						Debug.LogError("Error: " + request.error);
					}
					else
					{
						// Successfully loaded the file
						string fileContent = request.downloadHandler.text;

						LoadLuaElement lle = Instantiate(loadLuaElementPrefab, loadLuaElementParent);
						lle.SetInfo(fileName, fileContent);
						loadLuaElements.Add(lle);
					}
				}
			}
		}
	}

	public void SendCustomMessage()
	{
		SendMessageToProcess(inputFieldPid.text, inputFieldData.text, inputFieldAction.text);
	}

	public void ConnectWallet() //Ask if possible to check through arconnect when someone changes wallet
	{
		ConnectWalletJS();
	}

	public void FetchProcesses(string address)
	{
		FetchProcessesJS(address);
		loadingPanel.SetActive(true);
	}

	public void SpawnProcess()
	{
		if(processes.Find(p => p.name == inputFieldPid.text) == null)
		{
			SpawnProcessJS(spawnNewProcessInputField.text);
		}
		else
		{
			AlertMessageJS("Process with that name already exists!");
		}
	}

	public void LoadLua(string lua)
	{
		if(CurrentProcess != null)
		{
			LoadLua(CurrentProcess.id, lua);
		}
		else
		{
			Debug.LogError("Current process can't be null!!");
		}
	}

	public void LoadLua(string pid, string lua)
	{
		SendMessageToProcess(pid, lua, "Eval");
	}

	public void SendMessageToProcess(string pid, string data, string action)
	{
		SendMessageJS(pid, data, action);
	}

	public void LoadProcess(AOProcess p)
	{
		if(CurrentProcess != null)
		{
			ProcessInfoButton pib = processInfoButtons.Find((process) => process.process == CurrentProcess);
			if(pib != null)
			{
				pib.ToggleHighlight(false);
			}
		}

		CurrentProcess = p;
		//StartCoroutine(LoadTextFile("token.lua", p.id));
	}

	private void TryOpenProcessSettings()
	{
		if(CurrentProcess != null)
		{
			processSettingsButton.GetComponent<Animator>().SetTrigger("Open");
		}
		else
		{
			AlertMessageJS("Select a process first!");
		}
	}

	private void OnCurrentAddressChanged()
	{
		if(string.IsNullOrEmpty(currentAddress))
		{
			activeWalletText.text = "Please connect wallet";
		}
		else
		{
			activeWalletText.text = currentAddress;
		}

		walletConnectedPanel.SetActive(currentAddress != null);
	}

	private void OnCurrentProcessChanged()
	{
		processSettingsButton.gameObject.SetActive(currentProcess != null);
	}

	// CALLBACKS ////////////////////////////////////////////////////////

	public void UpdateWallet(string wallet)
	{
		if(wallet == "Error")
		{
			Debug.LogError("Error with Wallet!!!!");
			CurrentAddress = null;
			//Back To Home
			return;
		}

		if(wallet != CurrentAddress)
		{
			bool wasEmpty = string.IsNullOrEmpty(CurrentAddress);

			CurrentAddress = wallet;
			Debug.LogError("New wallet: " + wallet);

			if(!wasEmpty)
			{
				//Back To Home if necessary
			}
			
			FetchProcesses(CurrentAddress);
		}
		else
		{
			Debug.LogError("Same Wallet: " + wallet);
		}
	}

	private bool gotErrorOnce = false;

	public void UpdateProcesses(string jsonString)
	{
		loadingPanel.SetActive(false);
		if (jsonString == "Error")
		{
			if(gotErrorOnce)
			{
				AlertMessageJS("Error in fetching processes");
			}
			else
			{
				gotErrorOnce = true;
				FetchProcesses(CurrentAddress);
			}
			return;
		}
		else if(jsonString == "[]")
		{
			AlertMessageJS("No processes found!");
			return;
		}

		gotErrorOnce = false;

		if(processInfoButtons != null && processInfoButtons.Count > 0)
		{
			foreach(var button in processInfoButtons)
			{
				Destroy(button.gameObject);
			}
		}

		processInfoButtons = new List<ProcessInfoButton>();

		//Debug.LogError("Unity processes: " + jsonString);

		var processesNode = JSON.Parse(jsonString);
		processes = new List<AOProcess>();

		for (int i = 0; i < processesNode.Count; i++)
		{
			var process = processesNode[i];
			string processId = process["id"];
			var tagsNode = process["tags"];

			Dictionary<string, string> tags = new Dictionary<string, string>();
			for (int j = 0; j < tagsNode.Count; j++)
			{
				var tag = tagsNode[j];
				string tagName = tag["name"];
				string tagValue = tag["value"];
				tags.Add(tagName, tagValue);
			}

			AOProcess p = new AOProcess();
			p.id = processId;
			p.tags = tags;
			p.shortId = ShortenProcessID(p.id);

			if (p.tags.ContainsKey("Name"))
			{
				p.name = tags["Name"];
			}
			else
			{
				p.name = p.shortId;
			}

			processes.Add(p);

			ProcessInfoButton pib = Instantiate(processInfoButtonPrefab, processInfoButtonParent);
			pib.SetInfo(p);
			processInfoButtons.Add(pib);
		}
	}

	public void SpawnProcessCallback(string processId)
	{
		Debug.LogError("New process id: " + processId);
		FetchProcesses(CurrentAddress);
	}

	public void MessageCallback(string result)
	{
		Debug.LogError("Message Callback: " + result);
	}

	// UTILS ///////////////////////////////

	private string ShortenProcessID(string input)
	{
		string start = input.Substring(0, 5);

		string end = input.Substring(input.Length - 5, 5);

		return start + "..." + end;
	}
}
