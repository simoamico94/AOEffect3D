using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AOEffectCanvasManager : MonoBehaviour
{
	[Header("AOEffectCanvasManager")]
	public AOEffectManager manager;

    [Header("IntroUI")]
    [SerializeField] private GameObject introPanel;
	[SerializeField] private Button playGameButton;
	[SerializeField] private TMP_InputField gameProcessInputField;

    [Header("GameUI")]
	[SerializeField] private GameObject gamePanel;
	[SerializeField] private Button exitGameButton;

	[SerializeField] private TMP_Text infoText;
	[SerializeField] private TMP_Text errorText;

    void Start()
    {
        AOSManager.OnAOSStateChanged += OnAOSStateChanged;

		playGameButton.onClick.AddListener(OnPlayGameButtonClicked);
        exitGameButton.onClick.AddListener(() => manager.ExitGame());
    }

    private void OnAOSStateChanged(AOSState state)
    {
        if(state == AOSState.LoggedIn)
        {
            ShowIntroPanel();
        }     
        else
        {
            manager.ExitGame();
            HideAll();
		}
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

		if (error != null)
        {
			errorText.text = error;
		}
        else
        {
			errorText.text = "";
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
}
