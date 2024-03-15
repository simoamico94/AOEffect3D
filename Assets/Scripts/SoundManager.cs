using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager main;

    public AudioClip loginAudio;
    public AudioClip gameAudio;

    private AudioSource audioSource;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();

		if (main == null)
		{
			main = this;
			DontDestroyOnLoad(gameObject); // Optional: Keep the instance alive across scenes.
		}
		else
		{
			Destroy(gameObject); // Destroy if a duplicate exists.
		}
	}

	public void PlayLoginAudio()
	{
		audioSource.Stop();
		//audioSource.volume = 0.1f;
		//audioSource.clip = loginAudio;
		//audioSource.Play();
	}

	public void PlayGameAudio()
    {
        audioSource.Stop();
		audioSource.volume = 0.5f;
		audioSource.clip = gameAudio;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }
}
