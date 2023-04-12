using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is an audio manager singleton that will contain all audio that is used in the game
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public Sound[] music, sfx; 

    public AudioSource musicSource, sfxSource;

    void Awake() {
        //Initialize the singleton of the audio manager
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //Play music on start
    void Start()
    {
        PlayMusic("Main Theme");
    }

    //Play a music track with matching name
    public void PlayMusic(string name)
    {
        //Treats array as dictionary and is technically faster since nothing is added to the array during runtime
        Sound s = Array.Find(music, x=> x.soundName == name);

        //Play music track if it is found
        if(s != null)
        {
            musicSource.clip = s.clip;
            musicSource.Play();
        }
    }

    //Play a sound effect with matching name
    public void PlaySound(string name)
    {
        //Treats array as dictionary and is technically faster since nothing is added to the array during runtime
        Sound s = Array.Find(sfx, x=> x.soundName == name);

        //Play sound if it is found
        if(s != null)
        {
            sfxSource.PlayOneShot(s.clip);
        }
    }


}
