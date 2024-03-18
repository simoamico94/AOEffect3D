using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public enum AOEffectPlayerState
{
    None = 0,
    Waiting = 1,
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
	public GameObject marker;
	public int ranking;

	[SerializeField] private AOEffectPlayerState state;
	public AOEffectPlayerState State
	{
		get { return state; }
		set { if (state != value) { state = value; OnStateChanged(); } }
	}

	[Header("Player Settings")]
	public float speed = 1f;
	public float delayInfoUpdate = 0f;

	[Header("Model References")]
	public Transform modelTarget;
	[SerializeField] private Animator animator;
	[SerializeField] private Transform leftSpawnProj;
	[SerializeField] private Transform rightSpawnProj;
	[SerializeField] private GameObject projectilePrefab;
	public Transform cameraTarget;

	[Header("UI")]
    [SerializeField] private AOEffectPlayerInfoCanvas infoCanvas;

	[Header("Debug")]
	public Transform debugTarget;

	private Vector3 startPosition;
	private Vector3 targetPosition;
	private float elapsedTime = 0f;
	private float moveTime = 1f;

	private bool reachTargetPos = false;
	private bool hasMovedOnce = false;
	private bool shooting = false;

	private AudioSource audioSource;


	private List<AOEffectPlayer> pendingTargetsToShoot = new List<AOEffectPlayer>();

	private bool firstUpdate = true;

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
			else
			{
				reachTargetPos = false;
				elapsedTime = 0f;
			}
		}
    }

	[ContextMenu("Shoot")]
	public void DebugShoot()
	{
		StartCoroutine(Shoot(debugTarget, debugTarget));
	}

	public void ShootTarget(AOEffectPlayer target)
	{
		if(shooting)
		{
			pendingTargetsToShoot.Add(target);
		}
		else
		{
			StartCoroutine(Shoot(target.transform, target.modelTarget));
		}
	}

	IEnumerator Shoot(Transform lookAtTarget, Transform moveTarget)
	{
		animator.SetTrigger("Shoot");
		transform.LookAt(lookAtTarget);
		shooting = true;

		yield return new WaitForSeconds(0.5f);

		audioSource.Play();

		for(int i = 0; i < 6; i++)
		{
			StartCoroutine(MoveTowardsTarget(leftSpawnProj.position, moveTarget, 0.3f));

			yield return null;
			yield return null;

			StartCoroutine(MoveTowardsTarget(rightSpawnProj.position, moveTarget, 0.3f));

			yield return new WaitForSeconds(0.08f);

		}

		shooting = false;

		if(pendingTargetsToShoot != null && pendingTargetsToShoot.Count > 0)
		{
			ShootTarget(pendingTargetsToShoot[0]);
			pendingTargetsToShoot.RemoveAt(0);
		}
		else
		{
			if(transform.position != data.pos)
			{
				Debug.Log(Vector3.Distance(transform.position, data.pos));
				//transform.LookAt(data.pos);
			}
		}
	}

	IEnumerator MoveTowardsTarget(Vector3 spawnPos, Transform target, float duration)
	{
		Transform obj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity).transform;
		obj.parent = transform;

		float elapsedTime = 0; 

		while (elapsedTime < duration)
		{
			obj.position = Vector3.Lerp(spawnPos, target.position, elapsedTime / duration);
			obj.LookAt(target);

			elapsedTime += Time.deltaTime;
			yield return null;
		}

		Destroy(obj.gameObject);
	}

	public void UpdateRanking(int ranking)
	{
		this.ranking = ranking;
		infoCanvas.SetRanking(ranking.ToString());
	}

	public void UpdateData(AOEffectPlayerData newData)
    {
		infoCanvas.SetID(newData.id);
		infoCanvas.SetEnergy(newData.energy.ToString());
		infoCanvas.SetHealth(newData.health.ToString());

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
				Debug.Log(dist);
				if (dist > 1.5f) //Pacman effect -> teleport
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

		data = newData;
	}

	private void OnStateChanged()
    {
		switch (state)
		{
			case AOEffectPlayerState.None:
				break;
			case AOEffectPlayerState.Waiting:
				gameObject.SetActive(false);
				break;
			case AOEffectPlayerState.Playing:
				gameObject.SetActive(true);
				break;
			case AOEffectPlayerState.Dead:
				data.health = 0;
				infoCanvas.SetHealth("0");
				StartCoroutine(DelayedDestroy());
				//transform.position = Vector3.zero;
				hasMovedOnce = false;
				break;
		}
	}

	private IEnumerator DelayedDestroy()
	{

		yield return new WaitForSeconds(1);
		animator.SetTrigger("Die");
		yield return new WaitForSeconds(5);

		AOEffectManager.main.RemovePlayer(this);
		Destroy(gameObject);
	}
}
