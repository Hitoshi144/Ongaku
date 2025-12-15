using Microsoft.JSInterop;
using Ongaku.Enums;
using Ongaku.Models;
using System.Threading.Tasks;

namespace Ongaku.Services {
    public class AudioService {
        private readonly PlaylistService _playlistService;
        private readonly TrackService _trackService;

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
        public string _currentPlaylistName = "";
        private int _currentPlaylistId = -1;
        public string _currentArtistName = "";
        private int _currentArtistId = -1;

        private Random _random = new Random();
        private bool _isShuffeled = false;
        private List<Track> _originalQueue = new();

        public event Action<Track?>? OnTrackChanged;
        public event Action<bool>? OnPauseStateChanged;
        public event Action<bool>? OnLoadingStateChanged;
        public event Action<double>? OnTimeChanged;
        public event Action<double>? OnDurationChanged;
        public event Action<QueueModeEnum>? OnQueueModeChanged;
        public event Action<List<Track>>? OnQueueChanged;
        public event Action<bool>? OnShuffleStateChanged;
        public event Action? OnVolumeChanged;

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
            set
            {
                if (value == null) return;
                _queue = value;
                OnQueueChanged?.Invoke(value);
            }
        }

        public QueueSourceEnum QueueSource
        {
            get => _queueSource;
        }

        public bool IsShufeled
        {
            get => _isShuffeled;
            set
            {
                _isShuffeled = value;
                OnShuffleStateChanged?.Invoke(value);
            }
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

        public AudioService(IJSRuntime js, PlaylistService playlistService, TrackService trackService)
        {
            _js = js;
            _playlistService = playlistService;
            _playlistService.OnTrackAdded += HandlePlaylistTrackAdded;
            _playlistService.OnTrackDeleted += HandlePlaylistTrackDeleted;
            _playlistService.OnPlaylistDeleted += HandlePlaylistDelete;
            _playlistService.OnEditName += HandlePlaylistNameUpdate;

            _trackService = trackService;
            _trackService.OnTrackDelete += HandleTrackDelete;
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
            OnVolumeChanged?.Invoke();
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
            IsShufeled = false;
            _originalQueue.Clear();
        }

        public async Task PlayFromAsync(
            IEnumerable<Track> sourceTracks,
            Track track,
            QueueSourceEnum source,
            int playlistId = -1,
            string playlistName = "",
            int artistId = -1,
            string artistName = ""
            )
        {
            if (_queueSource == QueueSourceEnum.None 
                || _queueSource != source 
                || sourceTracks != _queue 
                || (_currentPlaylistId != playlistId)
                || (_currentArtistId != artistId))
            {
                if (_currentPlaylistId != playlistId)
                {
                    _currentPlaylistId = playlistId;
                    _currentPlaylistName = playlistName;
                }
                else if (_currentArtistId != artistId)
                {
                    _currentArtistId = artistId;
                    _currentArtistName = artistName;
                }

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

        public void ShuffleQueue()
        {
            if (_queue == null || _queue.Count == 0)
            {
                return;
            }

            if (!_isShuffeled)
            {
                _originalQueue = new List<Track>(_queue);
            }

            Track currentTrack = _queue[_currentIndex];
            List<Track> toShuffle = new List<Track>(_queue);
            toShuffle.RemoveAt(_currentIndex);

            int n = toShuffle.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                Track value = toShuffle[k];
                toShuffle[k] = toShuffle[n];
                toShuffle[n] = value;
            }

            toShuffle.Insert(0, currentTrack);

            _queue = toShuffle;
            _currentIndex = 0;
            IsShufeled = true;
        }

        public void UnshuffleQueue()
        {
            if (!_isShuffeled || _originalQueue.Count == 0)
            {
                return;
            }

            _queue = new List<Track>(_originalQueue);

            if (CurrentTrack != null)
            {
                _currentIndex = _queue.FindIndex(t => t.Id == CurrentTrack.Id);

                if (_currentIndex < 0)
                {
                    _currentIndex = 0;
                }
            }

            IsShufeled = false;
        }

        public void ToggleShuffle()
        {
            if (_isShuffeled)
            {
                UnshuffleQueue();
            }
            else
            {
                ShuffleQueue();
            }
        }

        private void HandlePlaylistTrackAdded(Track track, int playlistId)
        {
            if (_queueSource == QueueSourceEnum.Playlist && _currentPlaylistId == playlistId)
            {
                _queue.Add(track);
            }
        }

        private async void HandlePlaylistTrackDeleted(Track track, int playlistId)
        {
            if (_queueSource == QueueSourceEnum.Playlist && _currentPlaylistId == playlistId)
            {
                if (_currentTrack != null && _currentTrack.Id == track.Id)
                {
                    await PlayNextAsync();
                }

                _queue.Remove(track);
            }
        }

        private void HandleTrackDelete(Track track)
        {
            if (_queue.Contains(track))
            {
                _queue.Remove(track);
                _originalQueue.Remove(track);
            }
        }

        public void AddPlayNext(Track track)
        {
            if (track == null) return;

            bool trackExists = false;
            int existingIndex = -1;

            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].Id == track.Id)
                {
                    trackExists = true;
                    existingIndex = i;
                    break;
                }
            }

            if (trackExists)
            {
                if (existingIndex == _currentIndex + 1 && existingIndex < _queue.Count)
                {
                    return;
                }

                _queue.RemoveAt(existingIndex);

                if (existingIndex < _currentIndex)
                {
                    _currentIndex--;
                }
            }

            int insertPosition;
            if (_queue.Count == 0)
            {
                insertPosition = 0;
                _currentIndex = -1;
            }
            else if (_currentIndex >= 0 && _currentIndex < _queue.Count)
            {
                insertPosition = _currentIndex + 1;
            }
            else
            {
                insertPosition = _queue.Count;
            }

            _queue.Insert(insertPosition, track);

            if (_isShuffeled && _originalQueue.Count > 0)
            {
                if (trackExists)
                {
                    int originalIndex = _originalQueue.FindIndex(t => t.Id == track.Id);
                    if (originalIndex >= 0)
                    {
                        _originalQueue.RemoveAt(originalIndex);
                    }
                }

                int currentInOriginalIndex = -1;
                if (_currentTrack != null)
                {
                    currentInOriginalIndex = _originalQueue.FindIndex(t => t.Id == _currentTrack.Id);
                }

                if (currentInOriginalIndex >= 0)
                {
                    _originalQueue.Insert(currentInOriginalIndex + 1, track);
                }
                else
                {
                    _originalQueue.Add(track);
                }
            }

            else if (_isShuffeled && !trackExists)
            {
                _originalQueue.Add(track);
            }

            OnQueueChanged?.Invoke(_queue);
        }

        public void AddTrackToQueue(Track track)
        {
            if (track == null) return;

            if (!_queue.Any(t => t.Id == track.Id))
            {
                _queue.Add(track);

                if (_isShuffeled)
                {
                    _originalQueue.Add(track);
                }

                OnQueueChanged?.Invoke(_queue);
            }
        }

        public void ChangeQueueOrder(Track track, int newOrder)
        {
            if (track == null || _queue == null || _queue.Count == 0)
                return;

            var oldIndex = _queue.FindIndex(t => t.Id == track.Id);
            if (oldIndex == -1) return;

            newOrder = Math.Max(0, Math.Min(newOrder, _queue.Count - 1));

            if (oldIndex == newOrder) return;

            _queue.RemoveAt(oldIndex);

            _queue.Insert(newOrder, track);

            if (_currentTrack != null)
            {
                if (oldIndex < newOrder)
                {
                    if (_currentIndex == oldIndex)
                    {
                        _currentIndex = newOrder;
                    }
                }
                else
                {
                    if (oldIndex > _currentIndex && newOrder <= _currentIndex)
                    {
                        _currentIndex++;
                    }
                    else if (_currentIndex == oldIndex)
                    {
                        _currentIndex = newOrder;
                    }
                    else if (oldIndex < _currentIndex)
                    {
                        _currentIndex--;
                    }
                }
            }

            if (_isShuffeled && _originalQueue != null && _originalQueue.Count > 0)
            {
                var originalIndex = _originalQueue.FindIndex(t => t.Id == track.Id);
                if (originalIndex != -1)
                {
                    _originalQueue.RemoveAt(originalIndex);

                    if (_currentTrack != null)
                    {
                        var currentOriginalIndex = _originalQueue.FindIndex(t => t.Id == _currentTrack.Id);
                        if (currentOriginalIndex != -1)
                        {
                            _originalQueue.Insert(currentOriginalIndex + 1, track);
                        }
                        else
                        {
                            _originalQueue.Add(track);
                        }
                    }
                    else
                    {
                        _originalQueue.Add(track);
                    }
                }
            }

            OnQueueChanged?.Invoke(_queue);
        }

        public async void RemoveTrackFromQueue(Track track)
        {
            if (track == null || _queue == null) return;

            var index = _queue.FindIndex(t => t.Id == track.Id);
            if (index == -1) return;

            _queue.RemoveAt(index);

            if (index < _currentIndex)
            {
                _currentIndex--;
            }
            else if (index == _currentIndex)
            {
                if (_queue.Count > 0)
                {
                    _currentIndex = Math.Min(_currentIndex, _queue.Count - 1);
                    _currentTrack = _queue[_currentIndex];

                    await PlayAsync(_currentTrack);

                    OnTrackChanged?.Invoke(_currentTrack);
                }
                else
                {
                    _currentIndex = -1;
                    _currentTrack = null;
                }
            }

            if (_isShuffeled && _originalQueue != null)
            {
                var originalIndex = _originalQueue.FindIndex(t => t.Id == track.Id);
                if (originalIndex != -1)
                {
                    _originalQueue.RemoveAt(originalIndex);
                }
            }

            OnQueueChanged?.Invoke(_queue);
        }

        public async Task ClearState()
        {
            _queue.Clear();
            _isShuffeled = false;
            _originalQueue.Clear();
            _currentTrack = null;
            _currentPlaylistId = -1;
            _currentPlaylistName = "";
            _queueMode = QueueModeEnum.Loop;
            _queueSource = QueueSourceEnum.None;
            await PauseAsync();
            StopTimer();
            OnTrackChanged?.Invoke(null);
        }

        private async void HandlePlaylistDelete(int id)
        {
            if (_currentPlaylistId == id)
            {
                await ClearState();
            }
        }

        private void HandlePlaylistNameUpdate(int id, string name)
        {
            if (_currentPlaylistId == id) _currentPlaylistName = name;
        }
    }
}
