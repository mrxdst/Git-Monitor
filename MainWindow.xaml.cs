using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Git_Monitor;

public partial class MainWindow : Window
{
    private readonly MainViewModel VM;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = VM = vm;
    }

    private void FetchClick(object sender, RoutedEventArgs e) => VM.Fetch();

    private void OpenClick(object sender, RoutedEventArgs e) => VM.OpenFolder();

    private void LogClick(object sender, RoutedEventArgs e) => VM.OpenLog();

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

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var items = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in items)
            {
                VM.Add(item);
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
