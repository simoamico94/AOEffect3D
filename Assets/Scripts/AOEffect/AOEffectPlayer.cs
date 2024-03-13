using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public enum AOEffectPlayerState
{
    Nothing = 0,
    WaitingUnpaid = 1,
    WaitingPaid = 2,
    Playing = 3,
    Dead = 4
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
	
	[SerializeField] private AOEffectPlayerState state;
	public AOEffectPlayerState State
	{
		get { return state; }
		set { if (state != value) { state = value; OnStateChanged(); } }
	}

	[Header("Player Settings")]
	public float moveSpeed = 5f; // Speed of movement
	public float moveDuration = 1f; // Duration of movement in seconds

	[Header("UI")]
    [SerializeField] private GameObject infoCanvas;
	[SerializeField] private TMP_Text idText;
	[SerializeField] private TMP_Text healthText;
	[SerializeField] private TMP_Text energyText;

	//Do On Player State Changed

	private Vector3 targetPos;
	private float elapsedTime;

	private bool reachTargetPos = false;
	private bool hasMovedOnce = false;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(State == AOEffectPlayerState.Playing && reachTargetPos)
		{
			elapsedTime += Time.deltaTime;

			// Calculate the interpolation factor based on elapsed time and move duration
			float t = Mathf.Clamp01(elapsedTime / moveDuration);

			// Use Vector3.Lerp to interpolate between current position and target position
			transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);

			// Check if movement is complete
			if (t >= 1f)
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
				}
				else
				{
					reachTargetPos = true;
					elapsedTime = 0f;
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
			case AOEffectPlayerState.WaitingUnpaid:
				break;
			case AOEffectPlayerState.WaitingPaid:
				break;
			case AOEffectPlayerState.Playing:
				infoCanvas.SetActive(true);
				break;
			case AOEffectPlayerState.Dead:
				gameObject.SetActive(false);
				transform.position = Vector3.zero;
				hasMovedOnce = false;
				break;
		}
	}
}
