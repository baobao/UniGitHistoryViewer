using System;

namespace info.shibuya24
{
    /// <summary>
    /// One commit read from `git log`. Immutable so it can be built on a worker thread and handed to the GUI thread.
    /// </summary>
    public sealed class GitCommit
    {
        public string Hash { get; }
        public string ShortHash { get; }
        public string[] ParentHashes { get; }
        public string AuthorName { get; }
        public string AuthorEmail { get; }
        public DateTimeOffset AuthorDate { get; }
        public string Subject { get; }
        public string Body { get; }

        public bool IsMerge => ParentHashes.Length > 1;

        public GitCommit(
            string hash,
            string shortHash,
            string[] parentHashes,
            string authorName,
            string authorEmail,
            DateTimeOffset authorDate,
            string subject,
            string body)
        {
            Hash = hash;
            ShortHash = shortHash;
            ParentHashes = parentHashes;
            AuthorName = authorName;
            AuthorEmail = authorEmail;
            AuthorDate = authorDate;
            Subject = subject;
            Body = body;
        }
    }

    /// <summary>
    /// One entry of `git diff-tree --name-status`. OldPath is set only for renames and copies.
    /// </summary>
    public sealed class GitChangedFile
    {
        public char Status { get; }
        public string Path { get; }
        public string OldPath { get; }

        public GitChangedFile(char status, string path, string oldPath)
        {
            Status = status;
            Path = path;
            OldPath = oldPath;
        }
    }
}
