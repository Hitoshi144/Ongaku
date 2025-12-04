using Microsoft.JSInterop;
using Ongaku.Enums;
using Ongaku.Models;

namespace Ongaku.Services {
    public class AudioService {
        private IJSRuntime _js;
        private IJSObjectReference? _module;
        private Track? _currentTrack;
        private bool _isPaused;
        private bool _isLoading;
        private double? _duration;

        private List<Track> _queue = new();
        private int _currentIndex = -1;
        private QueueSourceEnum _queueSource = QueueSourceEnum.None;
        private QueueModeEnum _queueMode = QueueModeEnum.Loop;

        public event Action<Track?>? OnTrackChanged;
        public event Action<bool>? OnPauseStateChanged;
        public event Action<bool>? OnLoadingStateChanged;
        public event Action<double>? OnTimeChanged;
        public event Action<double>? OnDurationChanged;
        public event Action<QueueModeEnum>? OnQueueModeChanged;

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

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnLoadingStateChanged?.Invoke(value);
            }
        }

        public List<Track>? Queue
        {
            get => _queue;
        }

        public QueueSourceEnum QueueSource
        {
            get => _queueSource;
        }

        public QueueModeEnum QueueMode
        {
            get => _queueMode;
            set
            {
                if (_queueMode != value)
                {
                    _queueMode = value;
                    OnQueueModeChanged?.Invoke(value);
                }
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
            _currentIndex = _queue.IndexOf(track);
            IsPaused = true;
            IsLoading = true;
            _duration = await _module!.InvokeAsync<double>("playWithFullLoad", track.FilePath);
            IsLoading = false;
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

        public async Task<double> GetVolumeAsync()
        {
            return await _module!.InvokeAsync<double>("getVolume");
        }

        public void BuildQueue(IEnumerable<Track> tracks, Track startTrack, QueueSourceEnum source)
        {
            _queue = tracks.ToList();
            _currentIndex = _queue.FindIndex(t => t.Id == startTrack.Id);

            if (_currentIndex < 0)
            {
                throw new Exception("Start track not found in queue!");
            }

            _queueSource = source;
        }

        public async Task PlayFromAsync(
            IEnumerable<Track> sourceTracks,
            Track track,
            QueueSourceEnum source
            )
        {
            if (_queueSource == QueueSourceEnum.None || _queueSource != source || sourceTracks != _queue)
            {
                BuildQueue(sourceTracks, track, source);
            }
            else
            {
                _currentIndex = _queue.FindIndex(t => t.Id == track.Id);
            }

            await PlayAsync(track);
        }

        [JSInvokable("PlayNext")]
        public async Task PlayNextAsync()
        {
            if (_queueMode == QueueModeEnum.LoopOne)
            {
                await PlayAsync(_queue[_currentIndex]);
            }
            else if (_queue != null && _queueSource != QueueSourceEnum.None)
            {
                if (_currentIndex != _queue.Count - 1)
                {
                    _currentIndex++;
                    await PlayAsync(_queue[_currentIndex]);
                }
                else
                {
                    if (_queueMode == QueueModeEnum.Loop)
                    {
                        _currentIndex = 0;
                        await PlayAsync(_queue[_currentIndex]);
                    }
                }
            }
        }

        public async Task PlayPrevAsync()
        {
            if (_queue != null && _queueSource != QueueSourceEnum.None)
            {
                if (_currentIndex != 0)
                {
                    _currentIndex--;
                    await PlayAsync(_queue[_currentIndex]);
                }
                else
                {
                    _currentIndex = _queue.Count - 1;
                    await PlayAsync(_queue[_currentIndex]);
                }
            }
        }

        [JSInvokable]
        public void OnLoadProgress(double percent)
        {
            Console.WriteLine($"Load progress: {percent}%");
        }
    }
}
