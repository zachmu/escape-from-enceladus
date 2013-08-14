using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Map;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Enceladus {

    /// <summary>
    /// Global manager to control music playback.
    /// </summary>
    public class SoundEffectManager {
        private static SoundEffectManager _instance;

        public static SoundEffectManager Instance {
            get { return _instance; }
        }

        private AudioEngine _audioEngine;
        private SoundBank _soundBank;
        private WaveBank _waveBank;
        private readonly List<string> _pendingCues = new List<string>();
        private readonly Dictionary<string, Cue> _ongoingCues = new Dictionary<string, Cue>();

        public SoundEffectManager() {
            _instance = this;
        }

        public void LoadContent(AudioEngine audioEngine, ContentManager cm) {
            _audioEngine = audioEngine;
            _waveBank = new WaveBank(_audioEngine, "Content/Music/SoundEffects.xwb");
            _soundBank = new SoundBank(_audioEngine, "Content/Music/SoundEffects.xsb");
        }

        // Once per update, we play each sound requested in the previous update cycle.
        public void Update() {
            foreach ( var pendingCue in _pendingCues ) {
                Cue cue = _soundBank.GetCue(pendingCue);
                cue.Play();
            }
            _pendingCues.Clear();
            
            // Start any new ongoing sounds
            if ( _ongoingCues.ContainsValue(null) ) {
                Dictionary<string, Cue> newCues = new Dictionary<string, Cue>();
                foreach ( var pendingCue in _ongoingCues.Keys ) {
                    if ( _ongoingCues[pendingCue] == null ) {
                        Cue cue = _soundBank.GetCue(pendingCue);
                        cue.Play();
                        newCues[pendingCue] = cue;
                    }
                }
                foreach ( var cue in newCues.Keys ) {
                    _ongoingCues[cue] = newCues[cue];
                }
            }
        }

        /// <summary>
        /// Plays the sound effect with the given name the next time the game updates.
        /// </summary>
        public void PlaySoundEffect(string soundEffect) {
            _pendingCues.Add(soundEffect);
        }

        /// <summary>
        /// Starts playing a sound that will need to be manually stopped later
        /// </summary>
        public void PlayOngoingEffect(string soundEffect) {
            if ( !_ongoingCues.ContainsKey(soundEffect) ) {
                _ongoingCues.Add(soundEffect, null);
            }
        }

        /// <summary>
        /// Stops the sound effect given, if it's playing
        /// </summary>
        public void StopOngoingEffect(string soundEffect) {
            if ( _ongoingCues.ContainsKey(soundEffect) ) {
                Cue ongoingCue = _ongoingCues[soundEffect];
                if ( ongoingCue != null && ongoingCue.IsPlaying ) {
                    ongoingCue.Stop(AudioStopOptions.AsAuthored);
                    _ongoingCues.Remove(soundEffect);
                }
            }
        }
    }
}
