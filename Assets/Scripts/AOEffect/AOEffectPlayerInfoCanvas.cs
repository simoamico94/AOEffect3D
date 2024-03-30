using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class AOEffectPlayerInfoCanvas : MonoBehaviour
{
	public TMP_Text idText;
	public TMP_Text nameText;
	public TMP_Text lastTurnText;
	public TMP_Text healthText;
	public TMP_Text energyText;
	public TMP_Text rankingText;

	public Button moveToPlayerButton;

	public AOEffectPlayer player;

	private void Start()
	{
		if(moveToPlayerButton != null)
		{
			moveToPlayerButton.onClick.AddListener(MoveToPlayer);
		}
	}

	public void MoveToPlayer()
	{
		Camera.main.transform.position = player.cameraTarget.position;
		Camera.main.transform.LookAt(player.transform);
		Camera.main.transform.eulerAngles = new Vector3(30, Camera.main.transform.eulerAngles.y, 0);
	}

	public void SetID(string id)
	{
		idText.text = id;
	}

	public void SetHealth(string health)
	{
		healthText.text = health;
	}

	public void SetEnergy(string energy)
	{
		energyText.text = energy;
	}

	public void SetRanking(string ranking)
	{
		rankingText.text = ranking;
	}

	public void SetLastTurn(string lastTurn)
	{
		if(!string.IsNullOrEmpty(lastTurn))
		{
			lastTurnText.text = "Last Turn: " + lastTurn;
			lastTurnText.gameObject.SetActive(true);
		}
		else
		{
			lastTurnText.gameObject.SetActive(false);
		}
	}

	public void SetName(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			nameText.text = name;
			nameText.gameObject.SetActive(true);
		}
		else
		{
			nameText.gameObject.SetActive(false);
		}
	}
}
