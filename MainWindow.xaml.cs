using H.NotifyIcon;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace Git_Monitor;

public partial class MainWindow : Window
{
    public readonly MainViewModel VM;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = VM = vm;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var wp = JsonSerializer.Deserialize<WindowPlacementHelper.WindowPlacement>(Settings.Default.MainWindowPlacement);
        if (wp.length != 0)
        {
            WindowPlacementHelper.SetWindowPlacement(this, wp);
        }
    }

    private void PullClick(object sender, RoutedEventArgs e) => VM.OpenPull();

    private void FetchClick(object sender, RoutedEventArgs e) => VM.Fetch();

    private void PushClick(object sender, RoutedEventArgs e) => VM.OpenPush();

    private void OpenClick(object sender, RoutedEventArgs e) => VM.OpenFolder();

    private void LogClick(object sender, RoutedEventArgs e) => VM.OpenLog();

    private void ItemKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Delete:
                VM.Remove();
                break;
            case Key.Enter:
                VM.OpenLog();
                break;
        }
    }

    private void AddClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog() { Multiselect = true };
        var ok = dlg.ShowDialog();

        if (ok == true)
        {
            foreach (var path in dlg.SelectedPaths)
            {
                VM.Add(path);
            }
        }
    }

    private void RemoveClick(object sender, RoutedEventArgs e) => VM.Remove();

    private void ExitClick(object sender, RoutedEventArgs e) => VM.Exit();

    protected override void OnDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var items = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in items)
            {
                VM.Add(item);
            }
        }

        base.OnDrop(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Settings.Default.MainWindowPlacement = JsonSerializer.Serialize(WindowPlacementHelper.GetWindowPlacement(this));
        Settings.Default.Save();

        e.Cancel = true;
        WindowExtensions.Hide(this);
        base.OnClosing(e);
    }

    private void TrayDoubleClick(object sender, RoutedEventArgs e)
    {
        if (Visibility == Visibility.Visible)
        {
            WindowExtensions.Hide(this);
        }
        else
        {
            WindowExtensions.Show(this);
        }
    }
}
