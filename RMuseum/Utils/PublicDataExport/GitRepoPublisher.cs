using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;

namespace RMuseum.Utils.PublicDataExport
{
    /// <summary>
    /// Options for publishing the public export tree to a git remote.
    /// Bind this from the "PublicDataExport" configuration section.
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
    }

    public class GitRepoPublisher
    {
        private readonly GitRepoPublisherOptions _options;

        public GitRepoPublisher(GitRepoPublisherOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Ensures the local working copy exists and is up to date with the remote before the
        /// export job starts writing files into it. Clones on first run, fetches + hard-resets
        /// to the remote branch on subsequent runs (this working copy is bot-owned; nobody
        /// should be hand-editing it, so a hard reset is safe and keeps the job idempotent).
        /// </summary>
        public void EnsureWorkingCopyUpToDate()
        {
            if (!Directory.Exists(_options.LocalWorkingCopyPath) ||
                !Directory.Exists(Path.Combine(_options.LocalWorkingCopyPath, ".git")))
            {
                Directory.CreateDirectory(_options.LocalWorkingCopyPath);
                var cloneOptions = new CloneOptions();
                if (RemoteRequiresAuth())
                {
                    cloneOptions.FetchOptions.CredentialsProvider = CredentialsHandler;
                }
                Repository.Clone(_options.RemoteUrl, _options.LocalWorkingCopyPath, cloneOptions);
                return;
            }

            using var repo = new Repository(_options.LocalWorkingCopyPath);
            var remote = repo.Network.Remotes["origin"];
            if (remote == null)
            {
                // .git existed (e.g. a manual `git init`, or a previous run that failed before
                // completing its clone) but has no "origin" configured — fix it up rather than
                // crash, so a half-set-up working copy self-heals on the next run.
                repo.Network.Remotes.Add("origin", _options.RemoteUrl);
                remote = repo.Network.Remotes["origin"];
            }

            var fetchOptions = new FetchOptions();
            if (RemoteRequiresAuth())
            {
                fetchOptions.CredentialsProvider = CredentialsHandler;
            }
            Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), fetchOptions, null);

            var remoteBranch = repo.Branches[$"origin/{_options.Branch}"];
            if (remoteBranch != null)
            {
                repo.Reset(ResetMode.Hard, remoteBranch.Tip);
            }
        }

        /// <summary>
        /// Stages every change under the working copy and, if anything actually changed,
        /// commits and (if enabled) pushes. Returns the number of files touched (0 means
        /// nothing changed since last run — a normal, expected outcome on most nightly runs).
        /// </summary>
        public int CommitAndPush(string commitMessage)
        {
            using var repo = new Repository(_options.LocalWorkingCopyPath);

            Commands.Stage(repo, "*");

            var status = repo.RetrieveStatus();
            int changedCount = status.Count(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered);
            if (changedCount == 0)
            {
                return 0;
            }

            var signature = new Signature(_options.CommitAuthorName, _options.CommitAuthorEmail, DateTimeOffset.UtcNow);
            repo.Commit(commitMessage, signature, signature);

            if (_options.PushEnabled)
            {
                var pushOptions = new PushOptions();
                if (RemoteRequiresAuth())
                {
                    pushOptions.CredentialsProvider = CredentialsHandler;
                }
                var branch = repo.Branches[_options.Branch] ?? repo.CreateBranch(_options.Branch);
                repo.Network.Push(branch, pushOptions);
            }

            return changedCount;
        }

        private bool RemoteRequiresAuth() => !string.IsNullOrEmpty(_options.GitToken);

        private Credentials CredentialsHandler(string url, string usernameFromUrl, SupportedCredentialTypes types)
        {
            return new UsernamePasswordCredentials
            {
                Username = string.IsNullOrEmpty(_options.GitUserName) ? _options.GitToken : _options.GitUserName,
                Password = _options.GitToken,
            };
        }
    }
}
