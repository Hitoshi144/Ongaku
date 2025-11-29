using Microsoft.JSInterop;
using Ongaku.Models;

namespace Ongaku.Services {
    public class AudioService {
        private IJSRuntime _js;
        private IJSObjectReference? _module;
        private Track? _currentTrack;
        private bool _isPaused;

        public event Action<Track?>? OnTrackChanged;
        public event Action<bool>? OnPauseStateChanged;

        public Track? CurrentTrack
        {
            get => _currentTrack;
            private set
            {
                _currentTrack = value;
                OnTrackChanged?.Invoke(value);
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                _isPaused = value;
                OnPauseStateChanged?.Invoke(value);
            }
        }

        public AudioService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task InitAsync()
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "/js/audio.js");
        }

        public async Task PlayAsync(Track track)
        {
            CurrentTrack = track;
            await _module!.InvokeVoidAsync("play", track.FilePath);
            IsPaused = false;
        }

        public async Task PauseAsync()
        {
            await _module!.InvokeVoidAsync("pause");
            IsPaused = true;
        }

        public async Task SetVolumeAsync(double v)
        {
            await _module!.InvokeVoidAsync("setVolume", v);
        }

        public async Task ResumeAsync()
        {
            await _module!.InvokeVoidAsync("resume");
            IsPaused = false;
        }

        public async Task setTimeAsync(double s)
        {
            await _module!.InvokeVoidAsync("setTime", s);
        }

        public async Task<double> GetCurrentTimeAsync()
        {
            return await _module!.InvokeAsync<double>("getCurrentTime");
        }

        public async Task<double> GetDurationAsync()
        {
            return await _module!.InvokeAsync<double>("getDuration");
        }

        public async Task<bool> IsPausedAsync()
        {
            return await _module!.InvokeAsync<bool>("isPaused");
        }
    }
}
