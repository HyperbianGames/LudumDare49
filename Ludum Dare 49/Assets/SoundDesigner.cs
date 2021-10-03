using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelSoundDesign
{
    public AudioClip[] BackgroundTracks;
    public bool PlayRandomInputSounds;
    public InputSoundDesign[] InputSounds;    

    public HardDropSoundDesign[] HardDropSounds;

    public int InputSoundIndex { get; set; } = 0;
}

[System.Serializable]
public struct InputSoundDesign
{
    public AudioClip EffectSound;
    [Range(0.0f, 100f)]
    public float EffectVolumne;
}

[System.Serializable]
public struct HardDropSoundDesign
{
    public Tetromino Shape;    
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
    private LevelSoundDesign currentLevelSoundDesign;

    [Range(0.0f, 100f)]
    public float MaxGameVolume;
    public AudioSource MainThemeSource { get; set; }
    public AudioSource LoseThemeSource { get; set; }
    //public AudioSource LevelThemeSource { get; set; }

    private List<AudioSource> SoundEffectSources { get; set; } = new List<AudioSource>();

    [Range(0.0f, 10f)]
    public float FadeOutDuration;

    [Range(0.0f, 10f)]
    public float FadeInDuration;

    private int currentlyPlayingLevel = 1;
    private CurrentPlayingTrack currentTrack = CurrentPlayingTrack.MainTheme;
    private Dictionary<int, AudioSource> levelAudioSources = new Dictionary<int, AudioSource>();

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

        //LevelThemeSource = gameObject.AddComponent<AudioSource>();
        //LevelThemeSource.volume = 0;
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
                StartCoroutine(FadeAudioSource.StartFade(levelAudioSources[currentlyPlayingLevel], FadeOutDuration, 0));
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
                StartCoroutine(FadeAudioSource.StartFade(levelAudioSources[currentlyPlayingLevel], FadeOutDuration, 0));
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
        if (currentTrack == CurrentPlayingTrack.GameTheme && level == currentlyPlayingLevel)
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
                StartCoroutine(FadeAudioSource.StartFade(levelAudioSources[currentlyPlayingLevel], FadeOutDuration, 0));
                break;
        }

        currentTrack = CurrentPlayingTrack.GameTheme;

        if (!levelAudioSources.ContainsKey(level))
        {
            //LevelThemeSource = gameObject.AddComponent<AudioSource>();
            //LevelThemeSource.volume = 0;
            levelAudioSources.Add(level, gameObject.AddComponent<AudioSource>());
            levelAudioSources[currentlyPlayingLevel].volume = 0;
        }

        int levelMusicIndex = level < LevelSoundDesigns.Length ? level : Random.Range(1, LevelSoundDesigns.Length);

        levelAudioSources[level].clip = LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks[0];
        levelAudioSources[level].Stop();
        levelAudioSources[level].Play();

        //if (LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks.Length == 1)
        levelAudioSources[level].loop = (LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks.Length == 1);

        if (!levelAudioSources[level].loop)
        {
            float totalDelay = LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks[0].length;
            for (int soundIndex = 1; soundIndex < LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks.Length; soundIndex++)
            {
                StartCoroutine(QueueSoundEffects(levelAudioSources[level], LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks[soundIndex], totalDelay, soundIndex == LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks.Length-1));
                totalDelay += LevelSoundDesigns[levelMusicIndex - 1].BackgroundTracks[soundIndex].length;
            }
        }
        currentLevelSoundDesign = LevelSoundDesigns[levelMusicIndex - 1];
        StartCoroutine(FadeAudioSource.StartFade(levelAudioSources[level], FadeInDuration, MaxGameVolume, FadeOutDuration));

        currentlyPlayingLevel = level;
    }

    IEnumerator QueueSoundEffects(AudioSource source, AudioClip clip, float Length, bool queueLoop)
    {
        yield return new WaitForSeconds(Length);
        if (source.volume > 0)
        {
            source.clip = clip;
            source.loop = queueLoop;
            source.Play();
        }
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
        if (currentLevelSoundDesign.InputSounds.Length > 0)
        {
            if (currentLevelSoundDesign.PlayRandomInputSounds)
            {
                currentLevelSoundDesign.InputSoundIndex = Random.Range(0, currentLevelSoundDesign.InputSounds.Length);
            }
            else
            {
                currentLevelSoundDesign.InputSoundIndex = WrapIndex(currentLevelSoundDesign.InputSoundIndex + 1, 0, currentLevelSoundDesign.InputSounds.Length);
            }

            AudioSource.PlayClipAtPoint(currentLevelSoundDesign.InputSounds[currentLevelSoundDesign.InputSoundIndex].EffectSound, new Vector3(), currentLevelSoundDesign.InputSounds[currentLevelSoundDesign.InputSoundIndex].EffectVolumne);
        }        
    }

    public void PlayHardDropEffect(Tetromino shapeToPlay)
    {
        if (currentLevelSoundDesign.HardDropSounds.Length > 0)
        {
            for (int soundIndex = 0; soundIndex < currentLevelSoundDesign.HardDropSounds.Length; soundIndex++)
            {
                if (currentLevelSoundDesign.HardDropSounds[soundIndex].Shape == shapeToPlay)
                {
                    AudioSource.PlayClipAtPoint(currentLevelSoundDesign.HardDropSounds[soundIndex].EffectSound, new Vector3(), currentLevelSoundDesign.HardDropSounds[soundIndex].EffectVolumne);
                    return;
                }

                // The shape we asked to play didn't exist, so we are going to play a random sound
                int randomIndex = Random.Range(0, currentLevelSoundDesign.HardDropSounds.Length - 1);
                AudioSource.PlayClipAtPoint(currentLevelSoundDesign.HardDropSounds[randomIndex].EffectSound, new Vector3(), currentLevelSoundDesign.HardDropSounds[randomIndex].EffectVolumne);
            }
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
