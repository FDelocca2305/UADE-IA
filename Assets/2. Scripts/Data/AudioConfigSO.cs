using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Game/Audio Config")]
public class AudioConfig : ScriptableObject
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Background Music")]
    public AudioClip gameplayBackground;
    public AudioClip titleBackground;

    [Header("NPC SFX")]
    public AudioClip maleHurtSFX;
    public AudioClip maleDeathSFX;
    public AudioClip femaleHurtSFX;
    public AudioClip femaleDeathSFX;

    [Header("Item SFX")]
    public AudioClip collectItemSFX;
    public AudioClip cashRegisterSFX;
    public AudioClip canEscapeSFX;

    [Header("Game State")]
    public AudioClip winSFX;
    public AudioClip lostSFX;
    public AudioClip escapeSFX;

    [Header("UI SFX")]
    public AudioClip clickButtonSFX;
    public AudioClip hoverButtonSFX;

    [Header("Combat SFX")]
    public AudioClip playerPistolSingleShotSFX;
    public AudioClip enemyPistolSingleShotSFX;
    public AudioClip punchSFX;

    [Header("Ambient SFX")]
    public AudioClip heartBeatingSFX;
    public AudioClip concreteFootstepsSFX;
}