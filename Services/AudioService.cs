using Microsoft.JSInterop;
using Ongaku.Models;

namespace Ongaku.Services {
    public class AudioService {
        private IJSRuntime _js;
        private IJSObjectReference? _module;
        private Track? _currentTrack;
        private bool _isPaused;
        private double? _duration;

        public event Action<Track?>? OnTrackChanged;
        public event Action<bool>? OnPauseStateChanged;
        public event Action<double>? OnTimeChanged;
        public event Action<double>? OnDurationChanged;

        private System.Timers.Timer? _timer;

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

        private void StartTimer()
        {
            StopTimer();

            _timer = new System.Timers.Timer(100);
            _timer.Elapsed += async (_, _) =>
            {
                if (_module != null)
                {
                    try
                    {
                        double time = await GetCurrentTimeAsync();
                        OnTimeChanged?.Invoke(time);
                    }
                    catch { }
                }
            };
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        public async Task PlayAsync(Track track)
        {
            CurrentTrack = track;
            _duration = await _module!.InvokeAsync<double>("play", track.FilePath);
            IsPaused = false;

            OnDurationChanged?.Invoke((double)_duration);

            StartTimer();
        }

        public async Task PauseAsync()
        {
            await _module!.InvokeVoidAsync("pause");
            IsPaused = true;

            StopTimer();
        }

        public async Task SetVolumeAsync(double v)
        {
            await _module!.InvokeVoidAsync("setVolume", v);
        }

        public async Task ResumeAsync()
        {
            await _module!.InvokeVoidAsync("resume");
            IsPaused = false;

            StartTimer();
        }

        public async Task SetTimeAsync(double s)
        {
            await _module!.InvokeVoidAsync("setTime", s);
        }

        public async Task<double> GetCurrentTimeAsync()
        {
            return await _module!.InvokeAsync<double>("getCurrentTime");
        }

        public async Task<double> GetDurationAsync()
        {
            var result = await _module!.InvokeAsync<double?>("getDuration");
            Console.WriteLine($"Returned {result ?? 0}");
            return result ?? 0;
        }

        public async Task<bool> IsPausedAsync()
        {
            return await _module!.InvokeAsync<bool>("isPaused");
        }
    }
}
