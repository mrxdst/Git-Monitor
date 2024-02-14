using Microsoft.Toolkit.Uwp.Notifications;
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

public class GitRepository : INotifyPropertyChanged, IDisposable
{
    public string Path { get; }

    public uint CommitsAhead { get; private set; }
    public uint CommitsBehind { get; private set; }
    public uint UncommittedChanges { get; private set; }
    public string? ErrorText { get; private set; }

    public bool Error => !string.IsNullOrEmpty(ErrorText);
    public bool UpdateNeeded => !Error && (CommitsAhead + CommitsBehind > 0);
    public bool IsUpdatingStatus => UpdateTask != null;
    public bool IsFetching { get; private set; }
    public string StatusText
    {
        get
        {
            if (IsFetching)
                return "Fetching";
            if (IsUpdatingStatus)
                return "Updating";
            if (Error)
                return "Error";
            if (!InitialLoaded)
                return "";
            if (UpdateNeeded || UncommittedChanges > 0)
                return $"{CommitsBehind}↓ / {CommitsAhead}↑ / {UncommittedChanges}*";
            return "Up to date";
        }
    }

    private Task? UpdateTask;
    private bool InitialLoaded = false;
    private readonly CancellationTokenSource TokenSource = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public GitRepository(string path)
    {
        Path = path;
    }

    public async Task UpdateStatus(bool fetch, bool notify)
    {
        if (UpdateTask != null)
        {
            await UpdateTask;
            return;
        }

        var token = CancellationTokenSource.CreateLinkedTokenSource(TokenSource.Token, new CancellationTokenSource(Settings.Default.Timeout).Token).Token;

        if (token.IsCancellationRequested) return;

        var _updateNeeded = UpdateNeeded;

        UpdateTask = Task.Run(async () =>
        {
            IsFetching = fetch;
            ReportStatusChange();

            try
            {
                if (fetch)
                {
                    var (fetchErr, _) = await RunCommand("git", "fetch --all --quiet", token);
                    if (!string.IsNullOrEmpty(fetchErr))
                    {
                        ErrorText = fetchErr;
                        return;
                    }
                }

                var (behindErr, behindOut) = await RunCommand("git", "rev-list --count HEAD..@{u}", token);
                if (!string.IsNullOrEmpty(behindErr))
                {
                    ErrorText = behindErr;
                    return;
                }

                var (aheadErr, aheadOut) = await RunCommand("git", "rev-list --count @{u}..HEAD", token);
                if (!string.IsNullOrEmpty(aheadErr))
                {
                    ErrorText = aheadErr;
                    return;
                }

                var (statusdErr, statusOut) = await RunCommand("git", "status --untracked-files=all --no-renames --porcelain=1", token);
                if (!string.IsNullOrEmpty(statusdErr))
                {
                    ErrorText = statusdErr;
                    return;
                }

                ErrorText = null;
                CommitsBehind = uint.Parse(behindOut, CultureInfo.InvariantCulture); 
                CommitsAhead = uint.Parse(aheadOut, CultureInfo.InvariantCulture);
                UncommittedChanges = (uint)statusOut.Split('\n').Where(s => s != "").Count();
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                InitialLoaded = true;
                IsFetching = false;
            }
        });

        await UpdateTask;
        UpdateTask = null;
        ReportStatusChange();

        if (token.IsCancellationRequested) return;

        if (notify && !_updateNeeded && UpdateNeeded)
        {
            new ToastContentBuilder()
                .AddArgument("repository", Path)
                .AddText("Update needed")
                .AddText(Path)
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
                .Show(toast =>
                {
                    toast.Tag = GetHashCode().ToString();
                });
        }
        else if (_updateNeeded && !UpdateNeeded)
        {
            RemoveNotification();
        }
    }

    private void ReportStatusChange()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommitsAhead)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommitsBehind)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UncommittedChanges)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateNeeded)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUpdatingStatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFetching)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
    }

    private void RemoveNotification() => ToastNotificationManagerCompat.History.Remove(GetHashCode().ToString());

    public void OpenFolder()
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo()
            {
                FileName = Path + System.IO.Path.DirectorySeparatorChar,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception) { }
    }

    public async void OpenLog()
    {
        try
        {
            using var ps = Process.Start("TortoiseGitProc.exe", $"/command:log /path:\"{Path}\"");
            ps.EnableRaisingEvents = true;
            await ps.WaitForExitAsync();
        }
        catch (Exception) { }
        await UpdateStatus(fetch: false, notify: false);
    }

    public async void OpenPull()
    {
        try
        {
            using var ps = Process.Start("TortoiseGitProc.exe", $"/command:pull /path:\"{Path}\"");
            ps.EnableRaisingEvents = true;
            await ps.WaitForExitAsync();
        }
        catch (Exception) { }
        await UpdateStatus(fetch: false, notify: false);
    }

    public async void OpenPush()
    {
        try
        {
            using var ps = Process.Start("TortoiseGitProc.exe", $"/command:push /path:\"{Path}\"");
            ps.EnableRaisingEvents = true;
            await ps.WaitForExitAsync();
        }
        catch (Exception) { }
        await UpdateStatus(fetch: false, notify: false);
    }

    public void Dispose()
    {
        TokenSource.Cancel();
        RemoveNotification();
        GC.SuppressFinalize(this);
    }

    private async Task<(string stdErr, string stdOut)> RunCommand(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var ps = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = Path,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        })!;
        var stdErr = (await ps.StandardError.ReadToEndAsync(cancellationToken)).TrimEnd('\r', '\n');
        var stdOut = (await ps.StandardOutput.ReadToEndAsync(cancellationToken)).TrimEnd('\r', '\n');
        await ps.WaitForExitAsync(cancellationToken);
        return (stdErr, stdOut);
    }
}
