﻿using H.NotifyIcon;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Git_Monitor;

public partial class App : Application
{
    public ObservableCollection<GitRepository> Repositories { get; } = new ();

    private readonly CancellationTokenSource TokenSource = new ();

    private MainWindow? Win;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Load();
        Repositories.CollectionChanged += (sender, e) => Save();

        Win = new MainWindow(new MainViewModel(this));
        WindowExtensions.Show(Win);

        ToastNotificationManagerCompat.OnActivated += NotificationActivated;

        UpdateStatusLoop();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Save();
        TokenSource.Cancel();
        ToastNotificationManagerCompat.Uninstall();
        base.OnExit(e);
    }

    private void NotificationActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        if (Win == null) return;
        Application.Current.Dispatcher.Invoke(() => {
            var args = ToastArguments.Parse(toastArgs.Argument);
            GitRepository? repo = null;
            if (args.TryGetValue("repository", out string repositoryPath))
            {
                repo = Repositories.FirstOrDefault(r => r.Path == repositoryPath);
            }
            Win.VM.SelectedRepository = repo;

            args.TryGetValue("action", out string action);
            switch (action)
            {
                case "pull":
                    repo?.OpenPull();
                    break;
                case "log":
                    repo?.OpenLog();
                    break;
                case "open":
                    repo?.OpenFolder();
                    break;
                default:
                    WindowExtensions.Show(Win);
                    break;
            }
         });
    }

    private async void UpdateStatusLoop()
    {
        while (!TokenSource.IsCancellationRequested)
        {
            var start = DateTimeOffset.Now;
            for (int i = 0; i < Repositories.Count; i++)
            {
                var repo = Repositories[i];
                await repo.UpdateStatus(fetch: true, notify: true);
                Win!.VM.UpdateNeeded = Repositories.Any(r => r.UpdateNeeded);
            }

            var delta = Settings.Default.UpdateInterval - (DateTimeOffset.Now - start);
            if (delta > TimeSpan.Zero)
            {
                await Task.Delay(delta);
            }
        }
    }

    public void AddRepository(string path)
    {
        if (Repositories.Any(r => r.Path == path))
            return;
        var repo = new GitRepository(path);
        var _ = repo.UpdateStatus(fetch: true, notify: false);
        int i = 0;
        for (; i < Repositories.Count; i++)
        {
            if (StringComparer.CurrentCulture.Compare(Repositories[i].Path, repo.Path) > 0)
            {
                break;
            }
        }
        Repositories.Insert(i, repo);
    }

    public void RemoveRepository(GitRepository repository)
    {
        Repositories.Remove(repository);
        repository.Dispose();
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
