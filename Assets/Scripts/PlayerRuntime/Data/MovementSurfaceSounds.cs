using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[System.Serializable]
public sealed class MovementSurfaceSounds
{
    public string surfaceName;
    [Tooltip("Exact GameObject names on the supporting collider or its parents. Case insensitive.")]
    public string[] colliderObjectNames = new string[0];
    [Tooltip("Visual-only Tilemaps sampled at the player's supporting foot position.")]
    public string[] overlayTilemapNames = new string[0];
    public SoundCue footstepSound = new SoundCue();
    public SoundCue landingSound = new SoundCue();
}
