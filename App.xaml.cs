using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Git_Monitor;

public partial class App : Application
{
    private readonly static TimeSpan UPDATE_INTERVAL = TimeSpan.FromSeconds(60);

    public ObservableCollection<GitRepository> Repositories { get; } = new ();

    private readonly CancellationTokenSource TokenSource = new ();

    private System.Windows.Forms.NotifyIcon? Tray;
    private MainWindow? Win;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Load();
        Repositories.CollectionChanged += Repositories_CollectionChanged;
        
        ShowWindow();

        Tray = new System.Windows.Forms.NotifyIcon()
        {
            Visible = true,
            Icon = Git_Monitor.Resources.icon_ok,
            Text = "Git-Monitor",
            ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
        };

        Tray.DoubleClick += (s, e) => ShowWindow();
        Tray.BalloonTipClicked += (s, e) => ShowWindow();

        UpdateStatusLoop();
    }

    private void ShowWindow()
    {
        Win ??= new MainWindow(new MainViewModel(this));
        Win.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        TokenSource.Cancel();
        Save();
    }

    private void Repositories_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Save();
    }

    private async void UpdateStatusLoop()
    {
        while (!TokenSource.IsCancellationRequested)
        {
            var start = DateTimeOffset.Now;
            for (int i = 0; i < Repositories.Count; i++)
            {
                var repo = Repositories[i];
                await UpdateRepositoryStatus(repo, true);
            }

            var delta = UPDATE_INTERVAL - (DateTimeOffset.Now - start);
            if (delta > TimeSpan.Zero)
            {
                await Task.Delay(delta);
            }
        }
    }

    public async Task UpdateRepositoryStatus(GitRepository repository, bool fetch)
    {
        var _updateNeeded = repository.UpdateNeeded;
        await repository.UpdateStatus(fetch);
        if (TokenSource.IsCancellationRequested)
        {
            return;
        }
        if (Tray == null || Win == null)
        {
            return;
        }
        if (!_updateNeeded && repository.UpdateNeeded)
        {
            Tray.ShowBalloonTip(1, "Update needed", repository.Path, System.Windows.Forms.ToolTipIcon.None);
        }

        if (Repositories.All(r => !r.UpdateNeeded))
        {
            Win.Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/icon_ok.png"));
            Tray.Icon = Git_Monitor.Resources.icon_ok;
        }
        else
        {
            Win.Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/icon_bad.png"));
            Tray.Icon = Git_Monitor.Resources.icon_bad;
        }
    }

    private void Load()
    {
        var repoPaths = JsonSerializer.Deserialize<List<string>>(Settings.Default.RepositoryPaths)!;
        repoPaths.Sort(StringComparer.CurrentCulture);

        foreach (var path in repoPaths)
        {
            Repositories.Add(new GitRepository(path));
        }
    }

    private void Save()
    {
        var repoPaths = Repositories.Select(r => r.Path).ToList();
        Settings.Default.RepositoryPaths = JsonSerializer.Serialize(repoPaths);
        Settings.Default.Save();
    }    
}
