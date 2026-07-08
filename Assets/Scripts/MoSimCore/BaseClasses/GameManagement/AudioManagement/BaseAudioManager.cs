﻿using UnityEngine;

namespace MoSimCore.BaseClasses.GameManagement.AudioManagement
{
    /// <summary>
    /// Abstract base class for managing match audio cues.
    /// Provides methods to play sounds at key match moments.
    /// Inherit from this class and define game-specific audio clips.
    /// </summary>
    public abstract class BaseAudioManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The AudioSource component used to play match audio cues.")]
        protected AudioSource audioSource;
        
        /// <summary>Gets the audio clip to play at match start.</summary>
        protected abstract AudioClip MatchStartClip { get; }
        
        /// <summary>Gets the audio clip to play at teleop start.</summary>
        protected abstract AudioClip TeleopStartClip { get; }
        
        /// <summary>Gets the audio clip to play at endgame start.</summary>
        protected abstract AudioClip EndgameStartClip { get; }
        
        /// <summary>Gets the audio clip to play at match end.</summary>
        protected abstract AudioClip MatchEndClip { get; }

        /// <summary>
        /// Initializes the AudioSource component.
        /// </summary>
        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Plays the match start audio cue.
        /// Override to customize audio behavior.
        /// </summary>
        public virtual void PlayMatchStart()
        {
            audioSource.PlayOneShot(MatchStartClip);
        }
        
        /// <summary>
        /// Plays the teleop start audio cue.
        /// Override to customize audio behavior.
        /// </summary>
        public virtual void PlayTeleopStart()
        {
            audioSource.PlayOneShot(TeleopStartClip);
        }
        
        /// <summary>
        /// Plays the endgame start audio cue.
        /// Override to customize audio behavior.
        /// </summary>
        public virtual void PlayEndgameStart()
        {
            audioSource.PlayOneShot(EndgameStartClip);
        }
        
        /// <summary>
        /// Plays the match end audio cue.
        /// Override to customize audio behavior.
        /// </summary>
        public virtual void PlayMatchEnd()
        {
            audioSource.PlayOneShot(MatchEndClip);
        }

        /// <summary>
        /// Stops all currently playing audio.
        /// </summary>
        public virtual void StopAll()
        {
            audioSource.Stop();
        }
    }
}