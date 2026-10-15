using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace info.shibuya24
{
    /// <summary>
    /// The asset whose history is shown, resolved to physical paths.
    /// Must be created on the main thread because it reads AssetDatabase and Package Manager.
    /// </summary>
    internal sealed class GitHistoryTarget
    {
        public string AssetPath { get; }
        public string FullPath { get; }
        public bool IsDirectory { get; }
        public string WorkingDirectory { get; }
        public string Pathspec { get; }

        // Physical folder that corresponds to AssetRoot. Used to turn paths reported by git back into asset paths.
        private readonly string _physicalRoot;
        private readonly string _assetRoot;

        private GitHistoryTarget(string assetPath, string fullPath, bool isDirectory, string physicalRoot, string assetRoot)
        {
            AssetPath = assetPath;
            FullPath = fullPath;
            IsDirectory = isDirectory;
            _physicalRoot = physicalRoot;
            _assetRoot = assetRoot;

            // Running git inside the target folder with a relative pathspec avoids "outside repository" errors
            // when the project path goes through a symlink that git resolves differently from Unity.
            WorkingDirectory = isDirectory ? fullPath : NormalizePath(Path.GetDirectoryName(fullPath));
            Pathspec = isDirectory ? "." : Path.GetFileName(fullPath);
        }

        public static GitHistoryTarget FromAssetPath(string assetPath)
        {
            string projectRoot = NormalizePath(Directory.GetParent(Application.dataPath).FullName);
            bool isDirectory = AssetDatabase.IsValidFolder(assetPath);

            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (package == null)
            {
                return new GitHistoryTarget(assetPath, $"{projectRoot}/{assetPath}", isDirectory, projectRoot, string.Empty);
            }

            // Local (file:) packages live outside the project, so "Packages/<name>" has to be mapped to resolvedPath.
            string packageRoot = NormalizePath(package.resolvedPath);
            string fullPath = packageRoot + assetPath.Substring(package.assetPath.Length);
            return new GitHistoryTarget(assetPath, fullPath, isDirectory, packageRoot, package.assetPath);
        }

        /// <summary>
        /// Returns the asset path for a physical path, or null when the file is not visible to this project.
        /// </summary>
        public string ToAssetPath(string fullPath)
        {
            string relative = GetRelativePath(fullPath, _physicalRoot);
            if (relative == null)
            {
                return null;
            }

            return _assetRoot.Length == 0 ? relative : $"{_assetRoot}/{relative}";
        }

        public static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// Converts to the precomposed (NFC) form. Git on macOS reports NFC names, while Unity keeps the decomposed (NFD)
        /// names read from the file system, so Japanese names such as "ダ" must be compared in one form.
        /// </summary>
        public static string Precompose(string path)
        {
            return path.Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Returns <paramref name="path"/> relative to <paramref name="root"/> in NFC form, or null when it is outside.
        /// Comparison is ordinal because culture-aware comparison treats NFC and NFD as equal even though their lengths differ.
        /// </summary>
        public static string GetRelativePath(string path, string root)
        {
            string precomposedPath = Precompose(path);
            string prefix = Precompose(root) + "/";
            return precomposedPath.StartsWith(prefix, StringComparison.Ordinal) ? precomposedPath.Substring(prefix.Length) : null;
        }
    }
}
