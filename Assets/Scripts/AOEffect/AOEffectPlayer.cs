using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public enum AOEffectPlayerState
{
    Nothing = 0,
    Waiting = 1,
    Playing = 2,
    Dead = 3
}

[Serializable]
public struct AOEffectPlayerData
{
	public string id;
	public Vector3 pos;
	public int energy;
	public int health;
}

[Serializable]
public class AOEffectPlayer : MonoBehaviour
{
	[Header("Player Info")]
	public AOEffectPlayerData data;
    public bool isLocalPlayer;
	public GameObject marker;
	
	[SerializeField] private AOEffectPlayerState state;
	public AOEffectPlayerState State
	{
		get { return state; }
		set { if (state != value) { state = value; OnStateChanged(); } }
	}

	[Header("Player Settings")]
	public float speed = 1f;

	[Header("UI")]
    [SerializeField] private GameObject infoCanvas;
	[SerializeField] private TMP_Text idText;
	[SerializeField] private TMP_Text healthText;
	[SerializeField] private TMP_Text energyText;

	private Vector3 startPosition;
	private Vector3 targetPosition;
	private float elapsedTime = 0f;
	private float moveTime = 1f;

	private bool reachTargetPos = false;
	private bool hasMovedOnce = false;

	void Start()
    {

    }

    void Update()
    {
		marker.SetActive(isLocalPlayer);

        if(State == AOEffectPlayerState.Playing && reachTargetPos)
		{
			if (elapsedTime < moveTime)
			{
				transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveTime);
				elapsedTime += Time.deltaTime;
			}
			// Check if movement is complete
			else
			{
				// Movement complete, reset elapsed time
				reachTargetPos = false;
				elapsedTime = 0f;
			}
		}
    }

    public void UpdateData(AOEffectPlayerData newData)
    {
        if(!string.IsNullOrEmpty(idText.text))
        {
            idText.text = newData.id;
        }

        energyText.text = newData.energy.ToString();
        healthText.text = newData.health.ToString();

		if (newData.health <= 0)
		{
			State = AOEffectPlayerState.Dead;
		}
		else
		{
			if (data.pos != newData.pos)
			{
				transform.LookAt(newData.pos);

				if(!hasMovedOnce)
				{
					transform.position = newData.pos;
					hasMovedOnce = true;
				}
				else
				{
					float dist = Vector3.Distance(transform.position, newData.pos);

					if (dist > 5) //Pacman effect -> teleport
                    {
						transform.position = newData.pos;
						reachTargetPos = false;
					}
					else
					{
						startPosition = transform.position;
						targetPosition = newData.pos;
						reachTargetPos = true;
						elapsedTime = 0f;
						moveTime = dist/speed;
					}
				}
			}
		}

		data = newData;
	}

	private void OnStateChanged()
    {
		switch (state)
		{
			case AOEffectPlayerState.Nothing:
				break;
			case AOEffectPlayerState.Waiting:
				gameObject.SetActive(false);
				break;
			case AOEffectPlayerState.Playing:
				gameObject.SetActive(true);
				break;
			case AOEffectPlayerState.Dead:
				gameObject.SetActive(false);
				transform.position = Vector3.zero;
				hasMovedOnce = false;
				break;
		}
	}
}
