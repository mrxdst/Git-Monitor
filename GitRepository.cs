using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Git_Monitor;

public class GitRepository : INotifyPropertyChanged
{
    public string Path { get; }

    public uint CommitsAhead { get; private set; }
    public uint CommitsBehind { get; private set; }
    public string? ErrorText { get; private set; }

    public bool Error => !string.IsNullOrEmpty(ErrorText);
    public bool UpdateNeeded => CommitsAhead + CommitsBehind > 0;
    public bool IsUpdatingStatus => UpdateTask != null;
    public string StatusText
    {
        get
        {
            if (Error)
                return "Error";
            if (IsUpdatingStatus)
                return "Fetching";
            if (!InitialLoaded)
                return "";
            if (!UpdateNeeded)
                return "Up to date";
            return $"{CommitsBehind}↓ / {CommitsAhead}↑";
        }
    }

    private Task? UpdateTask;
    private bool InitialLoaded = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public GitRepository(string path)
    {
        Path = path;
    }

    public async Task UpdateStatus()
    {
        if (UpdateTask != null)
        {
            await UpdateTask;
            return;
        }

        UpdateTask = Task.Run(async () =>
        {
            ReportStatusChange();

            try
            {
                var (fetchErr, _) = await RunCommand("git", "fetch --all --quiet");
                if (!string.IsNullOrEmpty(fetchErr))
                {
                    ErrorText = fetchErr;
                    return;
                }

                var (behindErr, behindOut) = await RunCommand("git", "rev-list --count HEAD..@{u}");
                if (!string.IsNullOrEmpty(behindErr))
                {
                    ErrorText = behindErr;
                    return;
                }

                var (aheadErr, aheadOut) = await RunCommand("git", "rev-list --count @{u}..HEAD");
                if (!string.IsNullOrEmpty(aheadErr))
                {
                    ErrorText = aheadErr;
                    return;
                }

                ErrorText = null;
                CommitsBehind = uint.Parse(behindOut, CultureInfo.InvariantCulture); 
                CommitsAhead = uint.Parse(aheadOut, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                InitialLoaded = true;
            }
        });

        await UpdateTask;
        UpdateTask = null;
        ReportStatusChange();
    }

    private void ReportStatusChange()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommitsAhead)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommitsBehind)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateNeeded)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUpdatingStatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
    }

    private async Task<(string stdErr, string stdOut)> RunCommand(string fileName, string arguments)
    {
        using var ps = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = Path,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        })!;
        var stdErr = (await ps.StandardError.ReadToEndAsync()).TrimEnd('\r', '\n');
        var stdOut = (await ps.StandardOutput.ReadToEndAsync()).TrimEnd('\r', '\n');
        await ps.WaitForExitAsync();
        return (stdErr, stdOut);
    }
}
