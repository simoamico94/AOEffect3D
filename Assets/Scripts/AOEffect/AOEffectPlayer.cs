using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

	[Header("Model References")]
	public Transform modelTarget;
	[SerializeField] private Animator animator;
	[SerializeField] private Transform leftSpawnProj;
	[SerializeField] private Transform rightSpawnProj;
	[SerializeField] private GameObject projectilePrefab;

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
	private bool shooting = false;

	private AudioSource audioSource;

	public Transform debugTarget;

	void Start()
    {
		audioSource = GetComponent<AudioSource>();
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

	[ContextMenu("Shoot")]
	public void DebugShoot()
	{
		StartCoroutine(Shoot(debugTarget));
	}

	public void ShootTarget(AOEffectPlayer target)
	{
		StartCoroutine(Shoot(target.modelTarget));
	}

	IEnumerator Shoot(Transform target)
	{
		animator.SetTrigger("Shoot");
		transform.LookAt(target);
		shooting = true;

		yield return new WaitForSeconds(1.1f);

		audioSource.Play();

		for(int i = 0; i < 6; i++)
		{
			StartCoroutine(MoveTowardsTarget(leftSpawnProj.position, target, 0.3f));

			yield return null;
			yield return null;

			StartCoroutine(MoveTowardsTarget(rightSpawnProj.position, target, 0.3f));

			yield return new WaitForSeconds(0.08f);

		}

		shooting = false;

		if(transform.position != data.pos)
		{
			transform.LookAt(data.pos);
		}
	}

	IEnumerator MoveTowardsTarget(Vector3 spawnPos, Transform target, float duration)
	{
		Transform obj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity).transform;

		float elapsedTime = 0; // Track the elapsed time

		while (elapsedTime < duration)
		{
			// Move projectile towards the target
			obj.position = Vector3.Lerp(spawnPos, target.position, elapsedTime / duration);
			// Make the projectile look at the target
			obj.LookAt(target);

			elapsedTime += Time.deltaTime;
			yield return null;
		}

		// Ensure the projectile is exactly at the target position when the loop is done
		Destroy(obj.gameObject);
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
				if(!shooting)
				{
					transform.LookAt(newData.pos);
				}

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
