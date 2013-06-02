using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Enceladus {

    /// <summary>
    /// Global manager to control music playback.
    /// </summary>
    public class MusicManager {
        private static MusicManager _instance;

        public static MusicManager Instance {
            get { return _instance; }
        }

        private readonly AudioEngine _audioEngine;
        private SoundBank _soundBank;
        private WaveBank _waveBank;
        private Cue Cue { get; set; }

        public MusicManager(AudioEngine audioEngine) {
            _audioEngine = audioEngine;
        }

        public void LoadContent(ContentManager cm) {
            _waveBank = new WaveBank(_audioEngine, "Content/Music/Songs.xwb", 0, 16);
            _soundBank = new SoundBank(_audioEngine, "Content/Music/Songs.xsb");
        }

        public void Update() {
            if ( !Cue.IsPlaying ) {
                Cue.Play();
            }
        }

        public void SetMusicTrack(string track) {
            if ( Cue != null && Cue.IsPlaying ) {
                Cue.Stop(AudioStopOptions.Immediate);
            }
            Cue = _soundBank.GetCue(track);
        }
    }
}
