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
    public class MusicManager {
        private static MusicManager _instance;

        public static MusicManager Instance {
            get { return _instance; }
        }

        private readonly AudioEngine _audioEngine;
        private SoundBank _soundBank;
        private WaveBank _waveBank;
        private Cue Cue { get; set; }
        private string CurrentTrack { get; set; }
        private string PreviousTrack { get; set; }

        public MusicManager(AudioEngine audioEngine) {
            _audioEngine = audioEngine;
            _instance = this;
        }

        public void LoadContent(ContentManager cm) {
            _waveBank = new WaveBank(_audioEngine, "Content/Music/Songs.xwb", 0, 16);
            _soundBank = new SoundBank(_audioEngine, "Content/Music/Songs.xsb");
            PlayerPositionMonitor.Instance.RoomChanged += RoomChanged;
        }

        public void Update() {
            if ( Cue != null && !Cue.IsPlaying && !Cue.IsPreparing ) {
                Cue.Play();
            }
        }

        public void SetMusicTrack(string track) {
            if ( Cue != null && Cue.IsPlaying ) {
                Cue.Stop(AudioStopOptions.Immediate);
                PreviousTrack = CurrentTrack;
            }
            if ( track.Length > 0 ) {
                CurrentTrack = track;
                Cue = _soundBank.GetCue(track);
            } else {
                Cue = null;
            }
        }

        public void ResumePrevTrack() {
            if ( Cue != null && Cue.IsPlaying ) {
                Cue.Stop(AudioStopOptions.Immediate);
            }
            CurrentTrack = PreviousTrack ?? CurrentTrack;
            Cue = _soundBank.GetCue(CurrentTrack);
        }

        public void RoomChanged(Room oldRoom, Room currentRoom) {
            if ( currentRoom.MusicTrack != null && currentRoom.MusicTrack != CurrentTrack ) {
                SetMusicTrack(currentRoom.MusicTrack);
            }
        }
    }
}
