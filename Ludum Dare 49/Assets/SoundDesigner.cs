using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LevelSoundDesign
{
    public AudioClip BackgroundMusic;
    public InputSoundDesign[] InputSounds;
    public int InputSoundIndex;

    public bool PlayRandomInputSounds;
}

[System.Serializable]
public struct InputSoundDesign
{
    public AudioClip EffectSound;
    [Range(0.0f, 100f)]
    public float EffectVolumne;
}

public enum CurrentPlayingTrack
{
    MainTheme,
    LoseTheme,
    GameTheme,
}

public class SoundDesigner : MonoBehaviour
{
    public static SoundDesigner Instance { get; set; }
    public AudioClip MainMenuTheme;
    public AudioClip YouLoseTheme;

    public AudioClip MainMenuItemHovered;
    [Range(0.0f, 100f)]
    public float ItemHoveredVolume;
    public AudioClip MainMenuItemSelected;
    [Range(0.0f, 100f)]
    public float ItemSelectedVolume;

    public LevelSoundDesign[] LevelSoundDesigns;
    public LevelSoundDesign CurrentLevelSoundDesign;

    [Range(0.0f, 100f)]
    public float MaxGameVolume;
    public AudioSource MainThemeSource { get; set; }
    public AudioSource LoseThemeSource { get; set; }
    public AudioSource LevelThemeSource { get; set; }

    private List<AudioSource> SoundEffectSources { get; set; } = new List<AudioSource>();

    [Range(0.0f, 10f)]
    public float FadeOutDuration;

    [Range(0.0f, 10f)]
    public float FadeInDuration;

    private int currentlyPlayingLevel = 1;
    private CurrentPlayingTrack currentTrack = CurrentPlayingTrack.MainTheme;

    private void Awake()
    {
        Instance = this;
        MainThemeSource = gameObject.AddComponent<AudioSource>();
        MainThemeSource.clip = MainMenuTheme;
        MainThemeSource.volume = MaxGameVolume;
        MainThemeSource.Play();


        LoseThemeSource = gameObject.AddComponent<AudioSource>();
        LoseThemeSource.clip = YouLoseTheme;
        LoseThemeSource.volume = 0;

        LevelThemeSource = gameObject.AddComponent<AudioSource>();
        LevelThemeSource.volume = 0;
    }

    public void BeginMainTheme()
    {
        if (currentTrack == CurrentPlayingTrack.MainTheme)
            return;

        switch (currentTrack)
        {
            case CurrentPlayingTrack.MainTheme:
                //StartCoroutine(FadeAudioSource.StartFade(MainThemeSource, FadeDuration, 0));
                break;
            case CurrentPlayingTrack.LoseTheme:
                StartCoroutine(FadeAudioSource.StartFade(LoseThemeSource, FadeOutDuration, 0));
                break;
            case CurrentPlayingTrack.GameTheme:
                StartCoroutine(FadeAudioSource.StartFade(LevelThemeSource, FadeOutDuration, 0));
                break;
        }

        currentTrack = CurrentPlayingTrack.MainTheme;
        MainThemeSource.Stop();
        MainThemeSource.Play();
        MainThemeSource.loop = true;

        StartCoroutine(FadeAudioSource.StartFade(MainThemeSource, FadeInDuration, MaxGameVolume, FadeOutDuration));
    }

    public void BeginLoseTheme()
    {
        if (currentTrack == CurrentPlayingTrack.LoseTheme)
            return;

        switch (currentTrack)
        {
            case CurrentPlayingTrack.MainTheme:
                StartCoroutine(FadeAudioSource.StartFade(MainThemeSource, FadeOutDuration, 0));
                break;
            case CurrentPlayingTrack.LoseTheme:
                //StartCoroutine(FadeAudioSource.StartFade(LoseThemeSource, FadeDuration, 0));
                break;
            case CurrentPlayingTrack.GameTheme:
                StartCoroutine(FadeAudioSource.StartFade(LevelThemeSource, FadeOutDuration, 0));
                break;
        }

        currentTrack = CurrentPlayingTrack.LoseTheme;
        LoseThemeSource.Stop();
        LoseThemeSource.Play();
        LoseThemeSource.loop = true;

        StartCoroutine(FadeAudioSource.StartFade(LoseThemeSource, FadeInDuration, MaxGameVolume, FadeOutDuration));
    }

    public void BeingLevelTheme(int level)
    {
        if (currentTrack == CurrentPlayingTrack.GameTheme && level == currentlyPlayingLevel && LevelSoundDesigns.Length <= level)
            return;

        switch (currentTrack)
        {
            case CurrentPlayingTrack.MainTheme:
                StartCoroutine(FadeAudioSource.StartFade(MainThemeSource, FadeOutDuration, 0));
                break;
            case CurrentPlayingTrack.LoseTheme:
                StartCoroutine(FadeAudioSource.StartFade(LoseThemeSource, FadeOutDuration, 0));
                break;
            case CurrentPlayingTrack.GameTheme:
                //StartCoroutine(FadeAudioSource.StartFade(LevelThemeSource, FadeDuration, 0));
                break;
        }

        currentTrack = CurrentPlayingTrack.GameTheme;
        LevelThemeSource.clip = LevelSoundDesigns[level - 1].BackgroundMusic;
        LevelThemeSource.Stop();
        LevelThemeSource.Play();
        LevelThemeSource.loop = true;
        CurrentLevelSoundDesign = LevelSoundDesigns[level - 1];
        StartCoroutine(FadeAudioSource.StartFade(LevelThemeSource, FadeInDuration, MaxGameVolume, FadeOutDuration));
    }

    public void PlayMenuHoveredEffect()
    {
        AudioSource.PlayClipAtPoint(MainMenuItemHovered, new Vector3(), ItemHoveredVolume);
    }

    public void PlayOptionSelected()
    {
        AudioSource.PlayClipAtPoint(MainMenuItemSelected, new Vector3(), ItemSelectedVolume);
    }

    public void PlayInputEffect()
    {
        if (CurrentLevelSoundDesign.InputSounds.Length > 0)
        {
            if (CurrentLevelSoundDesign.PlayRandomInputSounds)
            {
                CurrentLevelSoundDesign.InputSoundIndex = Random.Range(0, CurrentLevelSoundDesign.InputSounds.Length);
            }
            else
            {
                CurrentLevelSoundDesign.InputSoundIndex = WrapIndex(CurrentLevelSoundDesign.InputSoundIndex + 1, 0, CurrentLevelSoundDesign.InputSounds.Length);
            }

            AudioSource.PlayClipAtPoint(CurrentLevelSoundDesign.InputSounds[CurrentLevelSoundDesign.InputSoundIndex].EffectSound, new Vector3(), CurrentLevelSoundDesign.InputSounds[CurrentLevelSoundDesign.InputSoundIndex].EffectVolumne);
        }        
    }

    private int WrapIndex(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - -min);
        }
    }
}
