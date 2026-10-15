using System.Collections.Generic;
using System.Threading;

namespace info.shibuya24
{
    internal sealed class GitHistoryResult
    {
        public string ErrorMessage { get; }
        public List<GitCommit> Commits { get; }

        // Repository root as seen from Unity (not git's symlink-resolved path), so it can be mapped to asset paths.
        public string RepositoryRoot { get; }

        public bool IsSuccess => ErrorMessage == null;

        private GitHistoryResult(string errorMessage, List<GitCommit> commits, string repositoryRoot)
        {
            ErrorMessage = errorMessage;
            Commits = commits;
            RepositoryRoot = repositoryRoot;
        }

        public static GitHistoryResult Success(List<GitCommit> commits, string repositoryRoot)
        {
            return new GitHistoryResult(null, commits, repositoryRoot);
        }

        public static GitHistoryResult Failure(string errorMessage)
        {
            return new GitHistoryResult(errorMessage, new List<GitCommit>(), null);
        }
    }

    internal sealed class GitChangedFilesResult
    {
        public string ErrorMessage { get; }
        public List<GitChangedFile> Files { get; }

        public bool IsSuccess => ErrorMessage == null;

        private GitChangedFilesResult(string errorMessage, List<GitChangedFile> files)
        {
            ErrorMessage = errorMessage;
            Files = files;
        }

        public static GitChangedFilesResult Success(List<GitChangedFile> files)
        {
            return new GitChangedFilesResult(null, files);
        }

        public static GitChangedFilesResult Failure(string errorMessage)
        {
            return new GitChangedFilesResult(errorMessage, new List<GitChangedFile>());
        }
    }

    /// <summary>
    /// git queries used by the viewer. Every method blocks, so run them on a worker thread.
    /// </summary>
    internal static class GitHistoryService
    {
        public static GitHistoryResult LoadHistory(GitHistoryTarget target, int maxCount, CancellationToken ct)
        {
            GitCommandResult location = GitCommandRunner.Run(
                target.WorkingDirectory, "rev-parse --show-toplevel --show-prefix", ct);
            if (location.IsSuccess == false)
            {
                return GitHistoryResult.Failure(location.Error.Trim());
            }

            string repositoryRoot = ResolveRepositoryRoot(target.WorkingDirectory, location.Output);

            // --follow tracks renames but git accepts it only for a single file.
            string follow = target.IsDirectory ? string.Empty : "--follow ";
            string arguments = "--literal-pathspecs log " +
                               $"--max-count={maxCount} {follow}--pretty=format:{GitLogParser.LogFormat} " +
                               $"-- {GitCommandRunner.Quote(target.Pathspec)}";
            GitCommandResult log = GitCommandRunner.Run(target.WorkingDirectory, arguments, ct);
            if (log.IsSuccess == false)
            {
                return GitHistoryResult.Failure(log.Error.Trim());
            }

            return GitHistoryResult.Success(GitLogParser.ParseLog(log.Output), repositoryRoot);
        }

        public static GitChangedFilesResult LoadChangedFiles(string repositoryRoot, GitCommit commit, CancellationToken ct)
        {
            // A merge has no single diff. Comparing with the first parent shows what the merge brought into the branch.
            string arguments = commit.IsMerge
                ? $"diff-tree -r -z -M --name-status {commit.ParentHashes[0]} {commit.Hash}"
                : $"diff-tree -r -z -M --name-status --no-commit-id --root {commit.Hash}";
            GitCommandResult result = GitCommandRunner.Run(repositoryRoot, arguments, ct);
            if (result.IsSuccess == false)
            {
                return GitChangedFilesResult.Failure(result.Error.Trim());
            }

            return GitChangedFilesResult.Success(GitLogParser.ParseChangedFiles(result.Output));
        }

        private static string ResolveRepositoryRoot(string workingDirectory, string revParseOutput)
        {
            string[] lines = revParseOutput.Replace("\r", string.Empty).Split('\n');
            string topLevel = GitHistoryTarget.NormalizePath(lines[0]);
            string prefix = lines.Length > 1 ? GitHistoryTarget.NormalizePath(lines[1]) : string.Empty;
            if (prefix.Length == 0)
            {
                return workingDirectory;
            }

            string suffix = "/" + prefix;
            return workingDirectory.EndsWith(suffix)
                ? workingDirectory.Substring(0, workingDirectory.Length - suffix.Length)
                : topLevel;
        }
    }
}
