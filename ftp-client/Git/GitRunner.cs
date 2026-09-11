using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClient.Git
{
    /// <summary>Options for a single git invocation.</summary>
    public sealed class GitRunOptions
    {
        /// <summary>Text written to the process stdin (UTF-8), e.g. a commit message for <c>commit -F -</c>.</summary>
        public string StandardInput { get; set; }

        /// <summary>Receives stderr lines as they arrive (progress output for clone/fetch/push).</summary>
        public IProgress<string> Progress { get; set; }

        /// <summary>Extra environment variables for this invocation.</summary>
        public IDictionary<string, string> Environment { get; set; }

        /// <summary>When true, a non-zero exit code throws <see cref="GitException"/>.</summary>
        public bool ThrowOnError { get; set; }
    }

    /// <summary>
    /// Spawns git.exe and captures its output. All git access in the application goes through here
    /// so that every command can be logged to the output panel.
    /// </summary>
    public static class GitRunner
    {
        private static string _gitExecutable;

        /// <summary>Path to git.exe. Resolved from PATH / well-known install folders on first use.</summary>
        public static string GitExecutable
        {
            get => _gitExecutable ?? (_gitExecutable = FindGit());
            set => _gitExecutable = value;
        }

        /// <summary>Raised after every command finishes (on a thread-pool thread).</summary>
        public static event EventHandler<GitCommandEventArgs> CommandExecuted;

        public static string FindGit()
        {
            var candidates = new List<string>();
            var pathVar = System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathVar.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try { candidates.Add(Path.Combine(dir.Trim(), "git.exe")); } catch { }
            }
            var pf = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            var pf86 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86);
            var local = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            candidates.Add(Path.Combine(pf, "Git", "cmd", "git.exe"));
            candidates.Add(Path.Combine(pf86, "Git", "cmd", "git.exe"));
            candidates.Add(Path.Combine(local, "Programs", "Git", "cmd", "git.exe"));
            foreach (var c in candidates)
            {
                try { if (File.Exists(c)) return c; } catch { }
            }
            return "git.exe";
        }

        /// <summary>Runs git with the given arguments in <paramref name="workingDirectory"/>.</summary>
        public static Task<GitResult> RunAsync(string workingDirectory, params string[] args)
        {
            return RunAsync(workingDirectory, args, null, CancellationToken.None);
        }

        public static async Task<GitResult> RunAsync(string workingDirectory, IEnumerable<string> args, GitRunOptions options, CancellationToken cancellationToken)
        {
            options = options ?? new GitRunOptions();
            var arguments = BuildArguments(args);
            var psi = new ProcessStartInfo
            {
                FileName = GitExecutable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? System.Environment.CurrentDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
            };
            // Never block waiting for a terminal prompt; credential helpers still show their own UI.
            psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            psi.EnvironmentVariables["GIT_PAGER"] = "cat";
            psi.EnvironmentVariables["GIT_OPTIONAL_LOCKS"] = "0";
            if (options.Environment != null)
            {
                foreach (var kv in options.Environment) psi.EnvironmentVariables[kv.Key] = kv.Value;
            }

            var commandLine = "git " + arguments;
            var stopwatch = Stopwatch.StartNew();
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var exitSource = new TaskCompletionSource<int>();
            process.Exited += (s, e) => exitSource.TrySetResult(0);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                process.Dispose();
                var failed = new GitResult(commandLine, -1, string.Empty, "Could not start git (" + GitExecutable + "): " + ex.Message, stopwatch.Elapsed);
                CommandExecuted?.Invoke(null, new GitCommandEventArgs(workingDirectory, failed));
                if (options.ThrowOnError) throw new GitException(failed);
                return failed;
            }

            using (process)
            using (cancellationToken.Register(() => TryKill(process)))
            {
                // Feed stdin (if any) and close it so git never waits for more input.
                var stdinTask = Task.Run(async () =>
                {
                    try
                    {
                        using (var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
                        {
                            if (!string.IsNullOrEmpty(options.StandardInput))
                            {
                                await writer.WriteAsync(options.StandardInput).ConfigureAwait(false);
                            }
                        }
                    }
                    catch { /* the process may exit before reading stdin */ }
                });

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = ReadLinesAsync(process.StandardError, options.Progress);

                await Task.WhenAll(exitSource.Task, stdoutTask, stderrTask, stdinTask).ConfigureAwait(false);
                process.WaitForExit();
                stopwatch.Stop();

                var result = new GitResult(commandLine, process.ExitCode, stdoutTask.Result, stderrTask.Result, stopwatch.Elapsed);
                CommandExecuted?.Invoke(null, new GitCommandEventArgs(workingDirectory, result));
                if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
                if (options.ThrowOnError && !result.Succeeded) throw new GitException(result);
                return result;
            }
        }

        private static async Task<string> ReadLinesAsync(StreamReader reader, IProgress<string> progress)
        {
            if (progress == null) return await reader.ReadToEndAsync().ConfigureAwait(false);

            var all = new StringBuilder();
            var line = new StringBuilder();
            var buffer = new char[4096];
            int read;
            while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    char c = buffer[i];
                    all.Append(c);
                    if (c == '\n' || c == '\r')
                    {
                        if (line.Length > 0) progress.Report(line.ToString());
                        line.Clear();
                    }
                    else line.Append(c);
                }
            }
            if (line.Length > 0) progress.Report(line.ToString());
            return all.ToString();
        }

        private static void TryKill(Process process)
        {
            try { if (!process.HasExited) process.Kill(); } catch { }
        }

        /// <summary>Joins arguments into a Windows command line, quoting as CommandLineToArgvW expects.</summary>
        public static string BuildArguments(IEnumerable<string> args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (arg == null) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Quote(arg));
            }
            return sb.ToString();
        }

        public static string Quote(string arg)
        {
            if (arg.Length == 0) return "\"\"";
            bool needsQuotes = false;
            foreach (var c in arg)
            {
                if (char.IsWhiteSpace(c) || c == '"') { needsQuotes = true; break; }
            }
            if (!needsQuotes) return arg;

            var sb = new StringBuilder();
            sb.Append('"');
            int backslashes = 0;
            foreach (var c in arg)
            {
                if (c == '\\') { backslashes++; continue; }
                if (c == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                    backslashes = 0;
                    continue;
                }
                sb.Append('\\', backslashes);
                backslashes = 0;
                sb.Append(c);
            }
            sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }
    }
}
