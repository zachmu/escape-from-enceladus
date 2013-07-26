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
        }

        /// <summary>
        /// Plays the sound effect with the given name the next time the game updates.
        /// </summary>
        public void PlaySoundEffect(string soundEffect) {
            _pendingCues.Add(soundEffect);
        }
    }
}
