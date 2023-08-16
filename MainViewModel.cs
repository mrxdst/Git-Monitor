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

    private readonly static BitmapImage IconOK = new (new Uri("pack://application:,,,/Assets/icon_ok.ico"));
    private readonly static BitmapImage IconBad = new (new Uri("pack://application:,,,/Assets/icon_bad.ico"));

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

    public void Add(string path) => App.AddRepository(path);

    public void Remove()
    {
        if (SelectedRepository == null) return;
        App.RemoveRepository(SelectedRepository);
    }

    public void OpenFolder() => SelectedRepository?.OpenFolder();

    public void OpenLog() => SelectedRepository?.OpenLog();

    public void OpenPull() => SelectedRepository?.OpenPull();

    public void OpenPush() => SelectedRepository?.OpenPush();

    public void Fetch() => SelectedRepository?.UpdateStatus(fetch: true, notify: true);

    public void Exit() => App.Shutdown();
}
