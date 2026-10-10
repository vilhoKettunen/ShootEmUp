using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
///  AudioController is a Singleton (Monobehaviour) class that is used to play music and sfx through AudioMixer output. 
///  It adds three AudioSource components to a GameObject for mixing music tracks and playing sound effects.
/// </summary>
/// 
public class AudioController : SingletonMono<AudioController>
{
    public MusicTrack CurrentlyPlayingTrack { get; private set;}
    private MusicPlayList musicPlayList;
    private MusicPlayList fxPlayList;
    private AudioMixer mixer;

    //From -80db to 0db
    private const float minDb = -80.0f;
    private const float maxDb = 0.0f;

    private AudioMixerGroup masterMixerGroup;

    private AudioMixerSnapshot noMusic;
    private AudioMixerSnapshot music1FullVolume;
    private AudioMixerSnapshot music2FullVolume;

    private AudioSource music1AudioSource;
    private AudioSource music2AudioSource;
    private AudioSource currentMusicSource;
    private AudioSource sfxAudioSource;

    private float masterPitch;

    #region Interface
    /// <summary>
    /// Pause AudioListener.
    /// </summary>
    /// <param name="_paused">Pause/unpause AudioListener.</param>
    public static void PauseAudioListener(bool _paused)
    {
        AudioListener.pause = _paused;
    }

    private float Percent01ToDb(float percent)
    {
        float dbRange = Mathf.Abs(minDb) + Mathf.Abs(maxDb);
        return minDb + percent * dbRange;
    }

    /// <summary>
    /// Play sound effect with sfxAudioSource. Output through AudioMixer's SFX AudioMixerGroup
    /// </summary>
    /// <param name="soundEffectClip">AudioClip to play.</param>
    /// <param name="sourceVolume">Optinal sourceVolume Output volume.</param>
    /// <param name="pitch">Optional pitch.</param>
    /// <param name="delayd">Optionally play delayed.</param>
    public void PlaySoundEffect(AudioClip soundEffectClip, float sourceVolume = 1f, float pitch = 1f, float delayd = 0f)
    {
        sfxAudioSource.pitch = pitch;

        if (delayd > 0)
        {
            sfxAudioSource.clip = soundEffectClip;
            sfxAudioSource.volume = sourceVolume;            
            sfxAudioSource.PlayDelayed(delayd);
        }
        else
            sfxAudioSource.PlayOneShot(soundEffectClip, sourceVolume);
    }

    /// <summary>
    /// Play sound effect from Audio FX playlist with sfxAudioSource. Output through AudioMixer's SFX AudioMixerGroup
    /// </summary>
    /// <param name="soundEffectClipName">String name of the clip added to Audio FX playlist(ScriptableObject)</param>
    /// <param name="sourceVolume">Optional output volume.</param>
    /// <param name="pitch">Optional pitch</param>
    public void PlaySoundEffect(string soundEffectClipName, float sourceVolume = 1f, float pitch = 1f)
    {
        AudioClip clip = fxPlayList.Tracks.FirstOrDefault(matchingClip => matchingClip.Name == soundEffectClipName).MusicClip;

        if (clip != null)
            PlaySoundEffect(clip, sourceVolume, pitch);
        else
            Debug.LogWarning("Audio effect '" + soundEffectClipName + "' not on Audio FX playlist!");
    }

    /// <summary>
    /// Stop AudioMixer's SFX AudioMixerGroup.
    /// </summary>
    public void StopSoundEffect()
    {
        sfxAudioSource.Stop();
    }

    /// <summary>
    /// Play and mix music track with currently playing music track.     
    /// </summary>
    /// <param name="trackName">Music track's string name (from MusicPlayList).</param>
    /// <param name="sourceVolume">Output volume.</param>
    /// <param name="transitionTime">Transition time to this track.</param>
    /// <param name="loop">Play on loop.</param>
    /// <returns>Return success.</returns>

    public bool PlayTrack(string trackName, float sourceVolume = 1f, float transitionTime = 0f, bool loop = true)
    {
        MusicTrack track = musicPlayList.Tracks.FirstOrDefault(matchingTrack => matchingTrack.Name == trackName);
        if (track != null)
            PlayTrack(track, sourceVolume, transitionTime, loop);

        return track != null; //<-- to avoid string typos...
    }

    /// <summary>
    /// Play and mix music track with currently playing music track.     
    /// </summary>
    /// <param name="trackName">Music track's string name (from MusicPlayList).</param>
    /// <param name="sourceVolume">Output volume.</param>
    /// <param name="transitionTime">Transition time to this track.</param>
    /// <param name="loop">Play on loop.</param>
    public void PlayTrack(MusicTrack track, float sourceVolume = 1f, float transitionTime = 0f, bool loop = true)
    {
        CurrentlyPlayingTrack = track;
        TransitionMixers(track, sourceVolume, transitionTime, loop);
    }

    /// <summary>
    /// Fade out music by transitioning to non-playing mixer channel.
    /// </summary>
    /// <param name="_transitionTime">Transition time in seconds.</param>
    public void FadeOutMusic(float _transitionTime)
    {
        TransitionMixers(null, 0, _transitionTime, false, true);
    }

    /// <summary>
    /// Fade in music
    /// </summary>
    /// <param name="_transitionTime">Transition time in seconds.</param>
    public void FadeInMusic(float _transitionTime)
    {
        TransitionMixers(null, 1f, _transitionTime);
    }

    /// <summary>
    /// Set AudioMixer's MasterVolume.
    /// </summary>
    /// <param name="volume">Set volume as percent 0-1.</param>
    public void SetMasterVolume(float volume)
    {
        //TODO: Yet another piece of shit Unity bug workaround:
        StartCoroutine(SetVolumeDelayed("MasterVolume", volume));
    }

    /// <summary>
    /// Set AudioMixer's SfxVolume.
    /// </summary>
    /// <param name="volume">Set volume as percent 0-1.</param>
    public void SetSfxVolume(float volume)
    {
        //TODO: Yet another piece of shit Unity bug workaround:
        StartCoroutine(SetVolumeDelayed("SfxVolume", volume));
    }

    /// <summary>
    /// Set AudioMixer's MusicVolume    
    /// </summary>
    /// <param name="volume">Set volume as percent 0-1.</param>
    public void SetMusicVolume(float volume)
    {
        //TODO: Yet another piece of shit Unity bug workaround:
        StartCoroutine(SetVolumeDelayed("MusicVolume", volume));        
    }

    IEnumerator SetVolumeDelayed(string volumeName, float volume)
    {
        yield return null;
        mixer.SetFloat(volumeName, Percent01ToDb(volume));        
    }

    #endregion

    #region Private
    private void TransitionMixers(MusicTrack _track, float _sourceVolume = 1f, float _transitionTime = 0f, bool _loop = true, bool _stop = false)
    {
        if (currentMusicSource == music1AudioSource)
        {
            currentMusicSource = music2AudioSource;
            music2FullVolume.TransitionTo(_transitionTime);
        }
        else
        {
            currentMusicSource = music1AudioSource;
            music1FullVolume.TransitionTo(_transitionTime);
        }

        if (_stop)
        {
            currentMusicSource.Stop();
        }
        else if (_track != null)
        {
            currentMusicSource.clip = _track.MusicClip;
            currentMusicSource.Play();
            currentMusicSource.loop = _loop;
            currentMusicSource.volume = _sourceVolume;
        }
    }

    #endregion

    #region Unity
    private void Awake()
    {
        Debug.Log("MIXER PRESENT");
        mixer = Resources.Load("GameAudioMixer") as AudioMixer;
        mixer.GetFloat("MasterPitch", out masterPitch);

        masterMixerGroup = mixer.FindMatchingGroups("Master").First();

        AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");

        //Get snapshots from mixer
        noMusic = mixer.FindSnapshot("NoMusic");
        music1FullVolume = mixer.FindSnapshot("Music1FullVolume");
        music2FullVolume = mixer.FindSnapshot("Music2FullVolume");

        //Start with no music snapshot...
        noMusic.TransitionTo(0);

        musicPlayList = Resources.Load("SoundtrackPlaylist") as MusicPlayList;
        fxPlayList = Resources.Load("AudioFxPlaylist") as MusicPlayList;

        //Add source (and correct output) for Music 1
        music1AudioSource = Instance.gameObject.AddComponent<AudioSource>();
        music1AudioSource.outputAudioMixerGroup = musicGroups[1]; //<-- Music1 in mixer
        music1AudioSource.loop = true;

        //Add source (and correct output) for Music 2
        music2AudioSource = Instance.gameObject.AddComponent<AudioSource>();
        music2AudioSource.outputAudioMixerGroup = musicGroups[2];  //<-- Music2 in mixer
        music2AudioSource.loop = true;

        //Add sfx source and correct output
        sfxAudioSource = Instance.gameObject.AddComponent<AudioSource>();
        sfxAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX").First();
        sfxAudioSource.loop = false;

        currentMusicSource = music1AudioSource;
    }

    #endregion
}
