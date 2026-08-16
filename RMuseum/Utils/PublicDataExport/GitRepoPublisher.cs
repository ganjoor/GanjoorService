using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RMuseum.Utils.PublicDataExport
{
    /// <summary>
    /// Options for publishing the public export tree to a git remote.
    /// Bind this from the "PublicDataExport" (or "TajikPublicDataExport") configuration section.
    /// </summary>
    public class GitRepoPublisherOptions
    {
        /// <summary>
        /// local working copy path (e.g. C:\ganjoor-public-data or /var/ganjoor/public-data)
        /// </summary>
        public string LocalWorkingCopyPath { get; set; }

        /// <summary>
        /// remote URL, e.g. https://github.com/ganjoor/ganjoor-data.git
        /// </summary>
        public string RemoteUrl { get; set; }

        public string Branch { get; set; } = "main";

        public string CommitAuthorName { get; set; } = "Ganjoor Export Bot";

        public string CommitAuthorEmail { get; set; } = "bot@ganjoor.net";

        /// <summary>
        /// if false, everything is written and committed locally but never pushed —
        /// useful for a first dry run before wiring up real credentials
        /// </summary>
        public bool PushEnabled { get; set; }

        /// <summary>
        /// git username for the push (for GitHub, a PAT is used as both username-independent
        /// and as the token below — GitHub only checks the token)
        /// </summary>
        public string GitUserName { get; set; }

        /// <summary>
        /// personal access token / app token with push rights to RemoteUrl. Keep this in
        /// user-secrets / environment variables, never committed to appsettings.json.
        /// </summary>
        public string GitToken { get; set; }

        /// <summary>
        /// Full path to git.exe, e.g. "C:\Program Files\Git\cmd\git.exe". Optional — leave empty
        /// to resolve "git" via PATH (with a fallback to common Git-for-Windows install
        /// locations if that fails, see GitRepoPublisher.ResolveGitExecutable). Set this
        /// explicitly if the process running this job has a PATH that doesn't include git for
        /// any reason (e.g. a Windows service running under a service account with its own PATH).
        /// </summary>
        public string GitExecutablePath { get; set; }

        /// <summary>
        /// how long a single git command is allowed to run before it's killed and the job fails
        /// with a clear timeout error, instead of hanging forever (e.g. if git ever ends up
        /// waiting on an interactive prompt with no terminal to answer it — see
        /// GIT_TERMINAL_PROMPT in GitRepoPublisher)
        /// </summary>
        public int CommandTimeoutMinutes { get; set; } = 45;
    }

    /// <summary>
    /// Thin wrapper around the native <c>git</c> CLI (not LibGit2Sharp) for the export job: sync a
    /// local working copy, stage, commit, push.
    ///
    /// This shells out to git.exe deliberately rather than using LibGit2Sharp's own transport.
    /// LibGit2Sharp's built-in HTTP push turned out to be unreliable at this job's actual scale
    /// (hundreds of thousands of small files, a correspondingly large push) — it repeatedly failed
    /// mid-push with "error receiving data from socket: An existing connection was forcibly closed
    /// by the remote host", a transport-level failure. The native git CLI (what every other git
    /// client actually uses) handles large pushes far more robustly, so this class runs it as a
    /// subprocess instead. Requires Git for Windows (or any git install) to be on PATH — the same
    /// requirement as running `git` from a normal command prompt.
    ///
    /// The auth token is passed only via a one-off `-c http.extraHeader=...` argument on whichever
    /// single invocation needs it (clone/fetch/push) — it's never written into .git/config, so it
    /// never touches disk.
    /// </summary>
    public class GitRepoPublisher
    {
        private readonly GitRepoPublisherOptions _options;
        private string _resolvedGitExecutable;

        public GitRepoPublisher(GitRepoPublisherOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Ensures the local working copy exists and is up to date with the remote before the
        /// export job starts writing files into it. Clones on first run, fetches + hard-resets
        /// to the remote branch on subsequent runs (this working copy is bot-owned; nobody should
        /// be hand-editing it, so a hard reset is safe and keeps the job idempotent).
        /// </summary>
        public void EnsureWorkingCopyUpToDate()
        {
            if (!Directory.Exists(_options.LocalWorkingCopyPath) ||
                !Directory.Exists(Path.Combine(_options.LocalWorkingCopyPath, ".git")))
            {
                Directory.CreateDirectory(_options.LocalWorkingCopyPath);
                RunGit(_options.LocalWorkingCopyPath, $"{BuildAuthArgs()}clone \"{_options.RemoteUrl}\" .");
                return;
            }

            RemoveStaleLockFileIfAny();
            EnsureRemoteConfigured();

            RunGit(_options.LocalWorkingCopyPath, $"{BuildAuthArgs()}fetch origin {_options.Branch}");
            RunGit(_options.LocalWorkingCopyPath, $"reset --hard origin/{_options.Branch}");
        }

        /// <summary>
        /// A .git/index.lock left behind by an abruptly-terminated git process (e.g. an app pool
        /// recycle, or a previous run of this same job that got killed mid-command rather than
        /// finishing) blocks every future git command in this working copy with a confusing
        /// "Another git process seems to be running" error, even when nothing actually is.
        /// GanjoorService's TryStartExclusiveExportJob already guarantees only one run of a given
        /// export job (main or Tajik) executes at a time *within this process* — so reaching this
        /// method at all means the current call is the sole legitimate owner of this working copy
        /// right now, and any lock file found here can only be a leftover from something that is
        /// no longer running (that in-process guard doesn't survive an app pool/process restart,
        /// which is exactly the scenario that leaves an orphaned lock file in the first place).
        /// Safe to remove unconditionally on that basis.
        /// </summary>
        private void RemoveStaleLockFileIfAny()
        {
            string lockPath = Path.Combine(_options.LocalWorkingCopyPath, ".git", "index.lock");
            if (File.Exists(lockPath))
            {
                File.Delete(lockPath);
            }
        }

        /// <summary>
        /// Stages every change under the working copy and, if anything actually changed, commits
        /// and (if enabled) pushes. Returns the number of files touched (0 means nothing changed
        /// since last run — a normal, expected outcome on most nightly runs).
        /// </summary>
        public int CommitAndPush(string commitMessage)
        {
            RunGit(_options.LocalWorkingCopyPath, "add -A");

            // "diff --cached --quiet" exits 1 if anything is staged, 0 if not — used purely for
            // the exit code here, never prints anything either way
            var diffResult = RunGitAllowNonZero(_options.LocalWorkingCopyPath, "diff --cached --quiet");
            if (diffResult.ExitCode == 0)
            {
                return 0;
            }

            string statusOutput = RunGit(_options.LocalWorkingCopyPath, "status --porcelain").StandardOutput;
            int changedCount = statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

            string escapedMessage = commitMessage.Replace("\"", "\\\"");
            RunGit(_options.LocalWorkingCopyPath,
                $"-c user.name=\"{_options.CommitAuthorName}\" -c user.email=\"{_options.CommitAuthorEmail}\" commit -m \"{escapedMessage}\"");

            if (_options.PushEnabled)
            {
                // push whatever HEAD points to, regardless of the local branch's own name, to the
                // configured remote branch — avoids depending on the locally checked-out branch
                // happening to be named the same as _options.Branch
                RunGit(_options.LocalWorkingCopyPath, $"{BuildAuthArgs()}push origin HEAD:{_options.Branch}");
            }

            return changedCount;
        }

        private void EnsureRemoteConfigured()
        {
            var result = RunGitAllowNonZero(_options.LocalWorkingCopyPath, "remote get-url origin");
            if (result.ExitCode != 0)
            {
                // .git existed (e.g. a manual `git init`, or a previous run that failed before
                // completing its clone) but has no "origin" configured — fix it up rather than
                // fail, so a half-set-up working copy self-heals on the next run
                RunGit(_options.LocalWorkingCopyPath, $"remote add origin \"{_options.RemoteUrl}\"");
            }
            else if (result.StandardOutput.Trim() != _options.RemoteUrl)
            {
                RunGit(_options.LocalWorkingCopyPath, $"remote set-url origin \"{_options.RemoteUrl}\"");
            }
        }

        private string BuildAuthArgs()
        {
            if (string.IsNullOrEmpty(_options.GitToken))
                return "";

            string username = string.IsNullOrEmpty(_options.GitUserName) ? _options.GitToken : _options.GitUserName;
            string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{_options.GitToken}"));
            return $"-c http.extraHeader=\"AUTHORIZATION: basic {basicAuth}\" ";
        }

        private GitProcessResult RunGit(string workingDirectory, string arguments)
        {
            var result = RunGitAllowNonZero(workingDirectory, arguments);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"git {RedactAuth(arguments)} failed (exit code {result.ExitCode}) in '{workingDirectory}':{Environment.NewLine}{result.StandardError}{Environment.NewLine}{result.StandardOutput}");
            }
            return result;
        }

        private GitProcessResult RunGitAllowNonZero(string workingDirectory, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = ResolveGitExecutable(),
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Without this, git can end up trying to prompt interactively for credentials or a
            // host-key confirmation. There is no interactive terminal in this process (it runs
            // under IIS/a background job) — an unanswered prompt just hangs forever with no error
            // and no timeout, which is exactly what happened here. This makes git fail fast with
            // a real error instead.
            psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
            {
                // Reading both redirected streams asynchronously (rather than e.g.
                // StandardOutput.ReadToEnd() before WaitForExit()) is required here, not optional:
                // git writes most of its push/fetch progress to stderr, and synchronously draining
                // stdout first while nobody drains stderr is a classic .NET Process deadlock if
                // stderr's OS pipe buffer fills up — the child blocks writing to stderr, the parent
                // blocks reading stdout, and both wait on each other forever. That deadlock (not a
                // slow network) is almost certainly what actually produced the multi-day hang this
                // was fixed after.
                process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                try
                {
                    process.Start();
                }
                catch (Win32Exception exp)
                {
                    throw new InvalidOperationException(
                        "Could not start 'git'. This process's PATH may be stale (common right " +
                        "after installing Git while Visual Studio/IIS Express was already " +
                        "running — fully restart them so they pick up the new PATH), or git " +
                        "genuinely isn't installed. As a workaround, set " +
                        "PublicDataExport:GitExecutablePath (or TajikPublicDataExport:...) to " +
                        "the full path of git.exe.", exp);
                }

                process.StandardInput.Close(); // nothing will ever be typed to it
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool exited = process.WaitForExit(_options.CommandTimeoutMinutes * 60 * 1000);
                if (!exited)
                {
                    TryKill(process);
                    throw new TimeoutException(
                        $"git {RedactAuth(arguments)} did not finish within {_options.CommandTimeoutMinutes} minutes and was killed. " +
                        "This is a hang, not normal slowness for this job — most likely git was waiting on a prompt " +
                        "with no terminal to answer it (should no longer happen with GIT_TERMINAL_PROMPT=0 set above) " +
                        "or a genuine network stall.");
                }

                // let the async read handlers finish flushing after the process has exited
                process.WaitForExit();

                return new GitProcessResult { ExitCode = process.ExitCode, StandardOutput = stdout.ToString(), StandardError = stderr.ToString() };
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best-effort - if it's already gone, or can't be killed, there's nothing more to do
            }
        }

        /// <summary>
        /// Resolves which git executable to launch, cached for the lifetime of this instance.
        /// Prefers an explicitly configured path; otherwise tries "git" (PATH resolution via
        /// Process/CreateProcess) and, only if that file genuinely doesn't exist anywhere on
        /// PATH, falls back to checking the usual Git-for-Windows install locations directly —
        /// covers the common case where the process that's running this job has a stale PATH
        /// snapshot from before git was installed (e.g. Visual Studio/IIS Express started before
        /// the PATH environment variable was updated).
        /// </summary>
        private string ResolveGitExecutable()
        {
            if (_resolvedGitExecutable != null)
                return _resolvedGitExecutable;

            if (!string.IsNullOrEmpty(_options.GitExecutablePath))
            {
                _resolvedGitExecutable = _options.GitExecutablePath;
                return _resolvedGitExecutable;
            }

            if (IsOnPath("git"))
            {
                _resolvedGitExecutable = "git";
                return _resolvedGitExecutable;
            }

            string[] fallbackCandidates =
            {
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Git\cmd\git.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Git\bin\git.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Git\cmd\git.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Git\bin\git.exe"),
                Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Programs\Git\cmd\git.exe"),
            };

            foreach (var candidate in fallbackCandidates)
            {
                if (File.Exists(candidate))
                {
                    _resolvedGitExecutable = candidate;
                    return _resolvedGitExecutable;
                }
            }

            // nothing found - fall through with plain "git" so the resulting Win32Exception
            // message (with its own guidance) is what the caller sees
            _resolvedGitExecutable = "git";
            return _resolvedGitExecutable;
        }

        /// <summary>
        /// checks every directory on this process's own PATH for the given executable, without
        /// actually starting it — used only to decide whether to bother trying the Git-for-Windows
        /// fallback locations
        /// </summary>
        private static bool IsOnPath(string executableName)
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;

                try
                {
                    string candidate = Path.Combine(dir, executableName + ".exe");
                    if (File.Exists(candidate))
                        return true;
                }
                catch (ArgumentException)
                {
                    // malformed PATH entry - ignore and keep checking the rest
                }
            }
            return false;
        }

        /// <summary>
        /// keeps the auth token out of exception messages that might end up logged somewhere
        /// </summary>
        private static string RedactAuth(string arguments)
        {
            int idx = arguments.IndexOf("http.extraHeader", StringComparison.OrdinalIgnoreCase);
            if (idx == -1)
                return arguments;

            int firstQuote = arguments.IndexOf('"', idx);
            int endQuote = firstQuote >= 0 ? arguments.IndexOf('"', firstQuote + 1) : -1;
            if (firstQuote == -1 || endQuote == -1)
                return arguments;

            return arguments.Substring(0, idx) + "http.extraHeader=<redacted> " + arguments.Substring(endQuote + 1);
        }

        private class GitProcessResult
        {
            public int ExitCode { get; set; }
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
        }
    }
}
