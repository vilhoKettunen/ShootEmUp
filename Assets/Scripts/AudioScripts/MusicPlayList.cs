using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///    MusicPlayList is a ScriptableObject class that can be used to create a playlist for all the music used in the game.
///    Demo scenes doesn't include music tracks, but you can use this for your projects by creating a new Scriptable object
///    from the class and by adding music AudioClips that match your scene names.
/// </summary>
/// 
[CreateAssetMenu(fileName = "New Music Playlist", menuName = "Music Playlist", order = 54)]
public class MusicPlayList : ScriptableObject
{
    [SerializeField]
    private List<MusicTrack> tracks;

    public List<MusicTrack> Tracks { get { return tracks; } }
}

[Serializable]
public class MusicTrack
{
    public string Name;
    public AudioClip MusicClip;
}



