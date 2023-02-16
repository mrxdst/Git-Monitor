using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Git_Monitor;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly App App;

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

    public void OpenLog()
    {
        if (SelectedRepository == null)
            return;
        try
        {
            using var _ = Process.Start("TortoiseGitProc.exe", $"/command:log /path:\"{SelectedRepository.Path}\"");
        }
        catch (Exception) { }
    }

    public void Add(string path)
    {
        var existing = Repositories.Select(s => s.Path).ToHashSet();
        if (existing.Contains(path))
            return;
        var repo = new GitRepository(path);
        var _ = App.UpdateRepositoryStatus(repo);
        Repositories.Add(repo);
    }

    public void Remove()
    {
        if (SelectedRepository == null)
            return;
        Repositories.Remove(SelectedRepository);
    }

    public void Pull()
    {
        if (SelectedRepository == null)
            return;
        try
        {
            using var _ = Process.Start("TortoiseGitProc.exe", $"/command:pull /path:\"{SelectedRepository.Path}\"");
        }
        catch (Exception) { }
    }

    public void Fetch()
    {
        if (SelectedRepository == null)
            return;
        var _ = App.UpdateRepositoryStatus(SelectedRepository);
    }

    public void Push()
    {
        if (SelectedRepository == null)
            return;
        try
        {
            using var _ = Process.Start("TortoiseGitProc.exe", $"/command:push /path:\"{SelectedRepository.Path}\"");
        }
        catch (Exception) { }
    }

    public void Exit()
    {
        App.Shutdown();
    }
}
