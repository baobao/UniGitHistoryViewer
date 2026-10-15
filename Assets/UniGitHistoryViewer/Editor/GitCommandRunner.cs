using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace info.shibuya24
{
    internal sealed class GitCommandResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }

        public bool IsSuccess => ExitCode == 0;

        public GitCommandResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }
    }

    /// <summary>
    /// Runs the git executable synchronously. Call it from a worker thread so the editor does not freeze.
    /// </summary>
    internal static class GitCommandRunner
    {
        // Without these, non-ASCII paths are octal-escaped and Japanese commit messages are garbled on Windows.
        private const string EncodingOptions = "-c core.quotepath=false -c i18n.logOutputEncoding=UTF-8";

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <param name="gitPath">Full path from <see cref="GitExecutableLocator"/>.</param>
        public static GitCommandResult Run(string gitPath, string workingDirectory, string arguments, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = gitPath,
                Arguments = $"{EncodingOptions} {arguments}",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Utf8,
                StandardErrorEncoding = Utf8,
            };

            // The viewer only reads history, so git must never wait for credentials or take the index lock
            // that the user's own git client may need at the same time.
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            startInfo.EnvironmentVariables["GIT_OPTIONAL_LOCKS"] = "0";

            using (Process process = Process.Start(startInfo))
            {
                // Both pipes must be drained at the same time. Reading one to the end first deadlocks
                // as soon as git fills the other pipe's buffer.
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                ct.ThrowIfCancellationRequested();
                return new GitCommandResult(process.ExitCode, outputTask.Result, errorTask.Result);
            }
        }

        public static string Quote(string argument)
        {
            return $"\"{argument.Replace("\"", "\\\"")}\"";
        }
    }
}
