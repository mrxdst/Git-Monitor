using H.NotifyIcon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Git_Monitor;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly App App;

    private static BitmapImage IconOK = new BitmapImage(new Uri("pack://application:,,,/Assets/icon_ok.ico"));
    private static BitmapImage IconBad = new BitmapImage(new Uri("pack://application:,,,/Assets/icon_bad.ico"));

    public ObservableCollection<GitRepository> Repositories => App.Repositories;

    private GitRepository? _Repository;
    public GitRepository? SelectedRepository {
        get
        {
            return _Repository;
        }
        set
        {
            _Repository = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRepository)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelection)));
        }
    }

    public bool HasSelection => SelectedRepository != null;

    private bool _UpdateNeeded = false;
    public bool UpdateNeeded
    {
        get
        {
            return _UpdateNeeded;
        }
        set
        {
            _UpdateNeeded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateNeeded)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusIcon)));
        }
    }

    public BitmapImage StatusIcon => UpdateNeeded  ? IconBad : IconOK;

    public MainViewModel(App app)
    {
        App = app;
    }

    public void OpenFolder()
    {
        if (SelectedRepository == null)
            return;
        try
        {
            using var _ = Process.Start(new ProcessStartInfo()
            {
                FileName = SelectedRepository.Path + Path.DirectorySeparatorChar,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception) { }
    }

    public async void OpenLog()
    {
        var repo = SelectedRepository;
        if (repo == null)
            return;
        try
        {
            using var ps = Process.Start("TortoiseGitProc.exe", $"/command:log /path:\"{repo.Path}\"");
            ps.EnableRaisingEvents = true;
            await ps.WaitForExitAsync();
        }
        catch (Exception) { }
        await App.UpdateRepositoryStatus(repo, false);
    }

    public void Add(string path)
    {
        if (Repositories.Any(r => r.Path == path))
            return;
        var repo = new GitRepository(path);
        var _ = App.UpdateRepositoryStatus(repo, true);
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

    public void Remove()
    {
        if (SelectedRepository == null)
            return;
        Repositories.Remove(SelectedRepository);
    }

    public async void Pull()
    {
        var repo = SelectedRepository;
        if (repo == null)
            return;
        try
        {
            using var ps = Process.Start("TortoiseGitProc.exe", $"/command:pull /path:\"{repo.Path}\"");
            ps.EnableRaisingEvents = true;
            await ps.WaitForExitAsync();
        }
        catch (Exception) { }
        await App.UpdateRepositoryStatus(repo, false);
    }

    public void Fetch()
    {
        if (SelectedRepository == null)
            return;
        var _ = App.UpdateRepositoryStatus(SelectedRepository, true);
    }

    public async void Push()
    {
        var repo = SelectedRepository;
        if (repo == null)
            return;
        try
        {
            using var ps = Process.Start("TortoiseGitProc.exe", $"/command:push /path:\"{repo.Path}\"");
            ps.EnableRaisingEvents = true;
            await ps.WaitForExitAsync();
        }
        catch (Exception) { }
        await App.UpdateRepositoryStatus(repo, false);
    }

    public void Exit()
    {
        App.Shutdown();
    }
}
