using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A data asset holding an ordered list of music/sound tracks. Create one per
/// category (e.g. a soundtrack playlist and an audio-FX playlist) by adding
/// named AudioClip entries. The AudioController looks tracks up by their
/// string Name at play time.
/// </summary>
[CreateAssetMenu(fileName = "New Music Playlist", menuName = "Music Playlist", order = 54)]
public class MusicPlayList : ScriptableObject
{
    [SerializeField]
    private List<MusicTrack> tracks;

    public List<MusicTrack> Tracks { get { return tracks; } }
}

/// <summary>
/// One entry in a MusicPlayList: a human-readable Name plus the AudioClip it
/// points to.
/// </summary>
[Serializable]
public class MusicTrack
{
    public string Name;
    public AudioClip MusicClip;
}
