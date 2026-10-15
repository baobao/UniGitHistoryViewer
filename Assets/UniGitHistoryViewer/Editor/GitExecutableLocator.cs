using System;
using System.IO;

namespace info.shibuya24
{
    /// <summary>
    /// Finds the git executable installed on this machine. Git is never bundled with the package.
    /// The path is resolved explicitly instead of passing "git" to Process.Start because on Windows
    /// CreateProcess looks in the current directory first, which would run a git.exe placed inside the project.
    /// </summary>
    internal static class GitExecutableLocator
    {
        // Unity started from Unity Hub or Finder gets a minimal PATH on macOS, which misses Homebrew installs.
        private static readonly string[] MacFallbackPaths =
        {
            "/opt/homebrew/bin/git",
            "/usr/local/bin/git",
            "/usr/bin/git",
        };

        private static readonly string[] WindowsFallbackPaths =
        {
            @"C:\Program Files\Git\cmd\git.exe",
            @"C:\Program Files (x86)\Git\cmd\git.exe",
        };

        private static string _cachedPath;

        /// <summary>
        /// Returns the full path of git, or null when it is not installed.
        /// Only a successful lookup is cached so installing git later works without restarting Unity.
        /// </summary>
        public static string Find()
        {
            if (_cachedPath != null)
            {
                return _cachedPath;
            }

            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            string fileName = isWindows ? "git.exe" : "git";
            string found = FindInPath(Environment.GetEnvironmentVariable("PATH"), fileName)
                           ?? FindFirstExisting(isWindows ? WindowsFallbackPaths : MacFallbackPaths);
            _cachedPath = found;
            return found;
        }

        internal static string FindInPath(string pathVariable, string fileName)
        {
            if (string.IsNullOrEmpty(pathVariable))
            {
                return null;
            }

            foreach (string entry in pathVariable.Split(Path.PathSeparator))
            {
                string directory = entry.Trim().Trim('"');

                // A relative PATH entry would resolve against the project directory, which is exactly what we avoid.
                if (directory.Length == 0 || directory.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || Path.IsPathRooted(directory) == false)
                {
                    continue;
                }

                string candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string FindFirstExisting(string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
