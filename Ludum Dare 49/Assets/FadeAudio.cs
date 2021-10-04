using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FadeAudioSource
{

    public static IEnumerator StartFade(AudioSource audioSource, float duration, float targetVolume, float delayBeforeEffectBegin = 0f)
    {
        
        float currentTime = 0;
        float start = audioSource.volume;
        float delayStart = 0f;
        while (currentTime < duration)
        {
            while (delayStart < delayBeforeEffectBegin)
            {
                delayStart += Time.deltaTime;
                yield return null;
            }
            
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.SmoothStep(start, targetVolume, currentTime / duration);
            yield return null;
        }

        if (targetVolume == 0)
            audioSource.Stop();

        yield break;
    }
}