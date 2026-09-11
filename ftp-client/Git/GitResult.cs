using System;

namespace FtpClient.Git
{
    /// <summary>Outcome of a single git process invocation.</summary>
    public sealed class GitResult
    {
        public GitResult(string commandLine, int exitCode, string standardOutput, string standardError, TimeSpan duration)
        {
            CommandLine = commandLine;
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
            Duration = duration;
        }

        public string CommandLine { get; }
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
        public TimeSpan Duration { get; }
        public bool Succeeded => ExitCode == 0;

        /// <summary>Stderr if present, otherwise stdout. Useful for user-facing messages.</summary>
        public string Message
        {
            get
            {
                var err = StandardError.Trim();
                if (err.Length > 0) return err;
                return StandardOutput.Trim();
            }
        }

        /// <summary>True when git reported merge/cherry-pick/rebase conflicts.</summary>
        public bool HasConflicts
        {
            get
            {
                if (Succeeded) return false;
                var all = StandardOutput + "\n" + StandardError;
                return all.IndexOf("CONFLICT", StringComparison.Ordinal) >= 0
                    || all.IndexOf("conflict", StringComparison.OrdinalIgnoreCase) >= 0
                    || all.IndexOf("could not apply", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>
        /// True when git could not authenticate against a remote. Because the client runs git
        /// without a console, a missing credential helper surfaces as "terminal prompts disabled".
        /// </summary>
        public bool NeedsCredentials
        {
            get
            {
                if (Succeeded) return false;
                var all = StandardOutput + "\n" + StandardError;
                return all.IndexOf("could not read Username", StringComparison.OrdinalIgnoreCase) >= 0
                    || all.IndexOf("could not read Password", StringComparison.OrdinalIgnoreCase) >= 0
                    || all.IndexOf("terminal prompts disabled", StringComparison.OrdinalIgnoreCase) >= 0
                    || all.IndexOf("Authentication failed", StringComparison.OrdinalIgnoreCase) >= 0
                    || all.IndexOf("could not read Credential", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public GitResult ThrowIfFailed()
        {
            if (!Succeeded) throw new GitException(this);
            return this;
        }
    }

    public sealed class GitException : Exception
    {
        public GitException(GitResult result)
            : base(BuildMessage(result))
        {
            Result = result;
        }

        public GitException(string message) : base(message) { }

        public GitResult Result { get; }

        private static string BuildMessage(GitResult result)
        {
            var message = result.Message;
            if (string.IsNullOrEmpty(message)) message = "git exited with code " + result.ExitCode;
            return message;
        }
    }

    public sealed class GitCommandEventArgs : EventArgs
    {
        public GitCommandEventArgs(string workingDirectory, GitResult result)
        {
            WorkingDirectory = workingDirectory;
            Result = result;
        }

        public string WorkingDirectory { get; }
        public GitResult Result { get; }
    }
}
