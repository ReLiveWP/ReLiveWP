namespace ReLiveWP.Services.Support.Services;

public abstract class ContentStore<TSnapshot> : IDisposable where TSnapshot : class
{
    private readonly string _directory;
    private readonly string _pattern;
    private readonly Lock _gate = new();
    private FileSystemWatcher? _watcher;
    private Timer? _debounce;
    private TSnapshot? _current;

    protected ContentStore(string directory, string pattern, IWebHostEnvironment env, ILogger logger)
    {
        _directory = directory;
        _pattern = pattern;
        Logger = logger;

        if (env.IsDevelopment())
            StartWatching();
    }

    protected ILogger Logger { get; }

    protected TSnapshot Current
    {
        get
        {
            var snapshot = Volatile.Read(ref _current);
            if (snapshot is not null)
                return snapshot;

            lock (_gate)
                return _current ??= LoadAll();
        }
    }

    protected abstract TSnapshot Load(IReadOnlyList<string> files);

    public void Reload()
    {
        lock (_gate)
            Volatile.Write(ref _current, LoadAll());
    }

    private TSnapshot LoadAll()
    {
        if (!Directory.Exists(_directory))
        {
            Logger.LogWarning("Content directory {Directory} does not exist", _directory);
            return Load([]);
        }

        return Load(Directory.GetFiles(_directory, _pattern));
    }

    private void StartWatching()
    {
        if (!Directory.Exists(_directory))
            return;

        _debounce = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
        _watcher = new FileSystemWatcher(_directory, _pattern) { EnableRaisingEvents = true };

        // the editor writes a file as several events; coalesce them so a save reloads once
        void Touch(object _, FileSystemEventArgs __) => _debounce.Change(250, Timeout.Infinite);

        _watcher.Changed += Touch;
        _watcher.Created += Touch;
        _watcher.Deleted += Touch;
        _watcher.Renamed += (_, _) => _debounce.Change(250, Timeout.Infinite);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce?.Dispose();
        GC.SuppressFinalize(this);
    }
}
