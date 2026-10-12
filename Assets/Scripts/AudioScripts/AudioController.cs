using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioController is a Singleton (Monobehaviour) class that plays music and
/// sfx through an AudioMixer. It adds three AudioSource components to its
/// GameObject:
///   - music1AudioSource  → routed to the "Music1" mixer group
///   - music2AudioSource  → routed to the "Music2" mixer group
///   - sfxAudioSource     → routed to the "SFX" mixer group
///
/// Music cross-fades between the two music sources by switching which
/// AudioMixer snapshot (music1FullVolume / music2FullVolume / noMusic) is
/// active, so a track can fade out while another fades in.
/// </summary>
public class AudioController : SingletonMono<AudioController>
{
    /// <summary>The music track currently assigned to the active music source.</summary>
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
    /// <summary>Pause / resume the global AudioListener.</summary>
    /// <param name="_paused">Pause/unpause AudioListener.</param>
    public static void PauseAudioListener(bool _paused)
    {
        AudioListener.pause = _paused;
    }

    /// <summary>Converts a 0..1 volume percent to a decibel value in the -80..0 range.</summary>
    private float Percent01ToDb(float percent)
    {
        float dbRange = Mathf.Abs(minDb) + Mathf.Abs(maxDb);
        return minDb + percent * dbRange;
    }

    /// <summary>
    /// Play a sound effect on the SFX source (routed to the SFX mixer group).
    /// Optionally sets volume, pitch, and a delayed start.
    /// </summary>
    /// <param name="soundEffectClip">AudioClip to play.</param>
    /// <param name="sourceVolume">Optional output volume.</param>
    /// <param name="pitch">Optional pitch.</param>
    /// <param name="delayed">Optionally delay the start (seconds).</param>
    public void PlaySoundEffect(AudioClip soundEffectClip, float sourceVolume = 1f, float pitch = 1f, float delayed = 0f)
    {
        sfxAudioSource.pitch = pitch;

        if (delayed > 0)
        {
            sfxAudioSource.clip = soundEffectClip;
            sfxAudioSource.volume = sourceVolume;            
            sfxAudioSource.PlayDelayed(delayed);
        }
        else
            sfxAudioSource.PlayOneShot(soundEffectClip, sourceVolume);
    }

    /// <summary>
    /// Play a sound effect looked up by name in the FX playlist.
    /// </summary>
    /// <param name="soundEffectClipName">String name of the clip on the Audio FX playlist (ScriptableObject).</param>
    /// <param name="sourceVolume">Optional output volume.</param>
    /// <param name="pitch">Optional pitch.</param>
    public void PlaySoundEffect(string soundEffectClipName, float sourceVolume = 1f, float pitch = 1f)
    {
        AudioClip clip = fxPlayList.Tracks.FirstOrDefault(matchingClip => matchingClip.Name == soundEffectClipName).MusicClip;

        if (clip != null)
            PlaySoundEffect(clip, sourceVolume, pitch);
        else
            Debug.LogWarning("Audio effect '" + soundEffectClipName + "' not on Audio FX playlist!");
    }

    /// <summary>Stops the SFX source.</summary>
    public void StopSoundEffect()
    {
        sfxAudioSource.Stop();
    }

    /// <summary>
    /// Play a music track looked up by name in the music playlist, cross-faded
    /// with whatever is currently playing.
    /// </summary>
    /// <param name="trackName">Music track's string name (from MusicPlayList).</param>
    /// <param name="sourceVolume">Output volume.</param>
    /// <param name="transitionTime">Fade transition time (seconds).</param>
    /// <param name="loop">Play on loop.</param>
    /// <returns>True if the track was found and started.</returns>
    public bool PlayTrack(string trackName, float sourceVolume = 1f, float transitionTime = 0f, bool loop = true)
    {
        MusicTrack track = musicPlayList.Tracks.FirstOrDefault(matchingTrack => matchingTrack.Name == trackName);
        if (track != null)
            PlayTrack(track, sourceVolume, transitionTime, loop);

        return track != null; // guards against silent string typos
    }

    /// <summary>
    /// Play a music track, cross-faded with the currently playing track.
    /// </summary>
    /// <param name="track">The MusicTrack to play.</param>
    /// <param name="sourceVolume">Output volume.</param>
    /// <param name="transitionTime">Fade transition time (seconds).</param>
    /// <param name="loop">Play on loop.</param>
    public void PlayTrack(MusicTrack track, float sourceVolume = 1f, float transitionTime = 0f, bool loop = true)
    {
        CurrentlyPlayingTrack = track;
        TransitionMixers(track, sourceVolume, transitionTime, loop);
    }

    /// <summary>Fades out the current music by transitioning to the silent mixer snapshot.</summary>
    /// <param name="_transitionTime">Transition time in seconds.</param>
    public void FadeOutMusic(float _transitionTime)
    {
        TransitionMixers(null, 0, _transitionTime, false, true);
    }

    /// <summary>Fades the current music back in.</summary>
    /// <param name="_transitionTime">Transition time in seconds.</param>
    public void FadeInMusic(float _transitionTime)
    {
        TransitionMixers(null, 1f, _transitionTime);
    }

    /// <summary>Set the master mixer volume (0..1 percent).</summary>
    /// <param name="volume">Volume as percent 0-1.</param>
    public void SetMasterVolume(float volume)
    {
        // Delayed by one frame to work around a Unity quirk where mixer
        // parameters set during Awake are sometimes ignored.
        StartCoroutine(SetVolumeDelayed("MasterVolume", volume));
    }

    /// <summary>Set the SFX mixer volume (0..1 percent).</summary>
    /// <param name="volume">Volume as percent 0-1.</param>
    public void SetSfxVolume(float volume)
    {
        StartCoroutine(SetVolumeDelayed("SfxVolume", volume));
    }

    /// <summary>Set the music mixer volume (0..1 percent).</summary>
    /// <param name="volume">Volume as percent 0-1.</param>
    public void SetMusicVolume(float volume)
    {
        StartCoroutine(SetVolumeDelayed("MusicVolume", volume));
    }

    /// <summary>
    /// Applies a mixer volume parameter one frame later (see Unity quirk note
    /// on the Set*Volume methods).
    /// </summary>
    IEnumerator SetVolumeDelayed(string volumeName, float volume)
    {
        yield return null;
        mixer.SetFloat(volumeName, Percent01ToDb(volume));        
    }

    #endregion

    #region Private
    /// <summary>
    /// Cross-fades between the two music sources by switching AudioMixer
    /// snapshots: the source that is NOT currently playing is faded up to full
    /// volume (and started if a track is provided), the other is faded down.
    /// If _stop is true, the current source is stopped outright (fade out).
    /// </summary>
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
    /// <summary>
    /// One-time setup: loads the AudioMixer, the music/FX playlists, wires the
    /// three AudioSources to their mixer groups, and starts with the silent
    /// snapshot active.
    /// </summary>
    private void Awake()
    {
        Debug.Log("MIXER PRESENT");
        mixer = Resources.Load("GameAudioMixer") as AudioMixer;
        mixer.GetFloat("MasterPitch", out masterPitch);

        masterMixerGroup = mixer.FindMatchingGroups("Master").First();

        AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");

        // Get the cross-fade snapshots from the mixer.
        noMusic = mixer.FindSnapshot("NoMusic");
        music1FullVolume = mixer.FindSnapshot("Music1FullVolume");
        music2FullVolume = mixer.FindSnapshot("Music2FullVolume");

        // Start silent (no music) until a track is explicitly played.
        noMusic.TransitionTo(0);

        musicPlayList = Resources.Load("SoundtrackPlaylist") as MusicPlayList;
        fxPlayList = Resources.Load("AudioFxPlaylist") as MusicPlayList;

        // Music 1 source → "Music1" mixer group.
        music1AudioSource = Instance.gameObject.AddComponent<AudioSource>();
        music1AudioSource.outputAudioMixerGroup = musicGroups[1];
        music1AudioSource.loop = true;

        // Music 2 source → "Music2" mixer group.
        music2AudioSource = Instance.gameObject.AddComponent<AudioSource>();
        music2AudioSource.outputAudioMixerGroup = musicGroups[2];
        music2AudioSource.loop = true;

        // SFX source → "SFX" mixer group (one-shot, no loop).
        sfxAudioSource = Instance.gameObject.AddComponent<AudioSource>();
        sfxAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX").First();
        sfxAudioSource.loop = false;

        currentMusicSource = music1AudioSource;
    }

    #endregion
}
