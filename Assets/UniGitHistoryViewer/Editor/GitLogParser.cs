using System;
using System.Collections.Generic;
using System.Globalization;

namespace info.shibuya24
{
    /// <summary>
    /// Parses git output produced with machine-readable separators.
    /// Splitting on control characters keeps parsing correct even when a commit message contains "commit " or blank lines.
    /// </summary>
    internal static class GitLogParser
    {
        public const string LogFormat = "%H%x1f%h%x1f%P%x1f%an%x1f%ae%x1f%aI%x1f%s%x1f%b%x1e";

        private const char FieldSeparator = '\u001f';
        private const char RecordSeparator = '\u001e';
        private const int FieldCount = 8;

        public static List<GitCommit> ParseLog(string output)
        {
            List<GitCommit> commits = new List<GitCommit>();
            foreach (string record in output.Split(RecordSeparator))
            {
                string trimmed = record.TrimStart('\r', '\n');
                if (trimmed.Length == 0)
                {
                    continue;
                }

                string[] fields = trimmed.Split(FieldSeparator);
                if (fields.Length != FieldCount)
                {
                    throw new FormatException($"Unexpected git log record ({fields.Length} fields): {trimmed}");
                }

                commits.Add(new GitCommit(
                    hash: fields[0],
                    shortHash: fields[1],
                    parentHashes: fields[2].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries),
                    authorName: fields[3],
                    authorEmail: fields[4],
                    authorDate: DateTimeOffset.Parse(fields[5], CultureInfo.InvariantCulture),
                    subject: fields[6],
                    body: fields[7].Trim()));
            }

            return commits;
        }

        /// <summary>
        /// Parses `git diff-tree -z --name-status` output.
        /// Renames and copies carry two paths (old, new), every other status carries one.
        /// </summary>
        public static List<GitChangedFile> ParseChangedFiles(string output)
        {
            List<GitChangedFile> files = new List<GitChangedFile>();
            string[] tokens = output.Split('\0');
            int index = 0;
            while (index < tokens.Length)
            {
                string status = tokens[index];
                if (status.Length == 0)
                {
                    index++;
                    continue;
                }

                char kind = status[0];
                if (kind == 'R' || kind == 'C')
                {
                    files.Add(new GitChangedFile(kind, tokens[index + 2], tokens[index + 1]));
                    index += 3;
                }
                else
                {
                    files.Add(new GitChangedFile(kind, tokens[index + 1], null));
                    index += 2;
                }
            }

            return files;
        }
    }
}
