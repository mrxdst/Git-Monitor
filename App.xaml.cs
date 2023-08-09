using Microsoft.Toolkit.Uwp.Notifications;
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

    private MainWindow? Win;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Load();
        Repositories.CollectionChanged += Repositories_CollectionChanged;

        Win = new MainWindow(new MainViewModel(this));
        Win.Show();

        ToastNotificationManagerCompat.OnActivated += NotificationActivated;

        UpdateStatusLoop();
    }

    private void NotificationActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        if (Win == null) return;
        Application.Current.Dispatcher.Invoke(() => {
            var args = ToastArguments.Parse(toastArgs.Argument);
            if (args.TryGetValue("repository", out string repositoryPath))
            {
                Win.VM.SelectedRepository = Repositories.FirstOrDefault(r => r.Path == repositoryPath);
            }

            args.TryGetValue("action", out string action);
            switch (action)
            {
                case "pull":
                    Win.VM.Pull();
                    break;
                case "log":
                    Win.VM.OpenLog();
                    break;
                case "open":
                    Win.VM.OpenFolder();
                    break;
                default:
                    Win?.Show();
                    break;
            }
         });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        TokenSource.Cancel();
        ToastNotificationManagerCompat.Uninstall();
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
        if (Win == null || TokenSource.IsCancellationRequested)
        {
            return;
        }
        if (!_updateNeeded && repository.UpdateNeeded)
        {
            new ToastContentBuilder()
                .AddArgument("repository", repository.Path)
                .AddText("Update needed")
                .AddText(repository.Path)
                .AddButton(new ToastButton()
                    .SetContent("Pull")
                    .AddArgument("action", "pull")
                )
                .AddButton(new ToastButton()
                    .SetContent("Log")
                    .AddArgument("action", "log")
                )
                .AddButton(new ToastButton()
                    .SetContent("Open")
                    .AddArgument("action", "open")
                )
                .Show();
        }
        Win.VM.UpdateNeeded = Repositories.Any(r => r.UpdateNeeded);
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
