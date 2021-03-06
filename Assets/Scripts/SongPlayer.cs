﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongPlayer : MonoBehaviour
{

    #region Private Fields

    // RPB: Components
    private AudioSource m_audioSource;

    // RPB: Time configurations
    private int m_partsToListenTo = 5;
    private float m_partPlaybackTimeSeconds = 5f;
    private float m_partPlaybackFadeTimeSeconds = 0.5f; // RPB: Must be less than half of playbacktime
    private float m_seekIntervalSeconds = 30f;

    // RPB: State
    public bool m_isHolding = false;
    private bool m_fullPlayOn = false;
    private Coroutine m_songPlayCoroutine;

    // RPB: Mappings
    private KeyCode m_holdSongKeyCode = KeyCode.Space;
    private KeyCode m_seekBackwardKeyCode = KeyCode.Comma;
    private KeyCode m_seekForwardKeyCode = KeyCode.Percent;

    #endregion

    #region Public Properties

    public bool IsPlaying
    {
        get { return m_audioSource.isPlaying; }
    }

    public bool IsHolding
    {
        get { return m_isHolding; }
    }

    public float PartPlaybackTimeSeconds
    {
        set { m_partPlaybackTimeSeconds = value; }
    }

    public bool FullPlayOn
    {
        set { m_fullPlayOn = value; }
    }

    #endregion

    #region Public Methods

    public void InitializeSettings(AudioSource audioSource, int parts, float fadeTimeSeconds, float sectionPlayTimeSeconds, float maxSectionPlaybackTime, float seekIntervalSeconds, KeyCode holdSongKeyCode, KeyCode seekForwardKeyCode, KeyCode seekBackwardKeyCode)
    {
        if (audioSource == null)
        {
            Debug.LogError("Audiosource cannot be null!");
            return;
        }

        m_partsToListenTo = parts;
        m_partPlaybackTimeSeconds = sectionPlayTimeSeconds;
        m_partPlaybackFadeTimeSeconds = fadeTimeSeconds;
        m_audioSource = audioSource;
        m_holdSongKeyCode = holdSongKeyCode;
        m_seekBackwardKeyCode = seekBackwardKeyCode;
        m_seekForwardKeyCode = seekForwardKeyCode;
        m_seekIntervalSeconds = seekIntervalSeconds;
    }

    public void PlaySong(AudioClip songClip)
    {
        if (m_audioSource == null)
        {
            Debug.LogError("m_audioSource cannot be null!");
            return;
        }

        if (m_songPlayCoroutine != null)
        {
            StopPlayback();
        }

        m_songPlayCoroutine = StartCoroutine(PlaySongCoroutine(songClip));
    }

    public void StopPlayback()
    {
        m_audioSource.Pause();
        StopCoroutine(m_songPlayCoroutine);
        AudioClip.Destroy(m_audioSource.clip); // RPB: IMPORTANT! If we don't have this, the app runs out of memory and crashes!!!
    }

    #endregion

    #region Private Methods

    private IEnumerator PlaySongCoroutine(AudioClip songClip)
    {
        if (m_audioSource == null)
        {
            Debug.LogError("m_audioSource cannot be null!");
            yield break;
        }

        if (songClip == null)
        {
            Debug.LogError("songClip cannot be null!");
            yield break;
        }


        var lengthPerPart = songClip.length / m_partsToListenTo;

        m_audioSource.clip = songClip;

        yield return new WaitUntil(() => m_audioSource.clip.loadState == AudioDataLoadState.Loaded);

        m_audioSource.time = 0f;

        while (true)
        {
            if (m_fullPlayOn)
            {
                if (m_audioSource.volume != 0f)
                {
                    m_audioSource.volume += (Time.deltaTime) / m_partPlaybackFadeTimeSeconds;
                }

                if (!m_audioSource.isPlaying)
                {
                    m_audioSource.Play();
                }

                if (m_audioSource.loop != true)
                {
                    m_audioSource.loop = true;
                }

                if (Input.GetKeyDown(m_seekBackwardKeyCode))
                {
                    // Seek Back
                    if (m_audioSource.time - m_seekIntervalSeconds > 0f)
                    {
                        m_audioSource.time -= m_seekIntervalSeconds;
                    }
                    else
                    {
                        m_audioSource.time = 0;
                    }
                }

                if (Input.GetKeyDown(m_seekForwardKeyCode))
                {
                    // Seek Forward
                    if (m_audioSource.time + m_seekIntervalSeconds < m_audioSource.clip.length)
                    {
                        m_audioSource.time += m_seekIntervalSeconds;
                    }
                }

                yield return null;
            }
            else
            {
                for (int i = 0; i < m_partsToListenTo; i++)
                {
                    m_audioSource.loop = false;

                    if (m_fullPlayOn)
                    {
                        break;
                    }

                    m_audioSource.volume = 0f;
                    var playheadTime = i * lengthPerPart;
                    m_audioSource.time = playheadTime;
                    m_audioSource.Play();

                    var crossfadeStartTime = Time.time;

                    while (Time.time < crossfadeStartTime + m_partPlaybackFadeTimeSeconds)
                    {
                        var progressTime = (Time.time - crossfadeStartTime) / m_partPlaybackFadeTimeSeconds;
                        m_audioSource.volume = Mathf.Lerp(0f, 1f, progressTime);
                        yield return null;
                    }

                    yield return new WaitForSeconds(m_partPlaybackTimeSeconds - 2 * m_partPlaybackFadeTimeSeconds);

                    m_isHolding = false;

                    while (Input.GetKey(m_holdSongKeyCode))
                    {
                        m_isHolding = true;
                        yield return null; // RPB: this lets us delay the fade
                    }

                    m_isHolding = false;

                    crossfadeStartTime = Time.time;

                    if (m_fullPlayOn)
                    {
                        break;
                    }

                    while (Time.time < crossfadeStartTime + m_partPlaybackFadeTimeSeconds)
                    {
                        var progressTime = (Time.time - crossfadeStartTime) / m_partPlaybackFadeTimeSeconds;
                        m_audioSource.volume = Mathf.Lerp(1f, 0f, progressTime);
                        yield return null;
                    }

                    yield return null;
                }
            }
        }
    }

    #endregion

}
