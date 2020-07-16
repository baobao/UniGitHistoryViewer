using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace info.shibuya24
{
    /// <summary>
    /// git log Viewer in UnityEditor
    ///
    /// required : git
    ///
    /// </summary>
    public class UniGitHistoryViewer : EditorWindow
    {
        private static GistHistoryListView _gitHistoryListView;
        private static bool _isWaiting;
        private int _historyCount = 5;
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Tools/UniGitHistoryViewer")]
        private static EditorWindow ShowWindow()
        {
            var window = GetWindow<UniGitHistoryViewer>();
            window.titleContent = new GUIContent("UniGitHistoryViewer");
            window.Show();
            return window;
        }

        private static void ShowWindow(List<GitLogData> list)
        {
            var window = ShowWindow();
            _gitHistoryListView = new GistHistoryListView(list, window);
        }

        /// <summary>
        /// Rendering EditorWindow
        /// </summary>
        void OnGUI()
        {
            var style = new GUIStyle {richText = true};
            EditorGUILayout.LabelField("<color=white><size=20>UniGitHistoryViewer</size></color>", style);

            // set title margin
            GUILayout.Space(10f);

            EditorGUI.BeginDisabledGroup(_isWaiting);

            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(Selection.activeObject == null);

            EditorGUILayout.LabelField("Count : ", GUILayout.Width(54f));
            _historyCount = EditorGUILayout.IntField(_historyCount, GUILayout.Width(40f));

            // update git log button
            if (GUILayout.Button("Check History", GUILayout.Width(120f)))
            {
                CheckSelectionObjectHistory(_historyCount);
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            if (Selection.activeObject == null)
            {
                EditorGUILayout.HelpBox("Select Object in Project.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(AssetDatabase.GetAssetPath(Selection.activeObject), MessageType.Info);
            }

            if (_isWaiting)
            {
                EditorGUILayout.LabelField("Loading...");
            }
            else
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                // draw list
                _gitHistoryListView?.DoLayoutList(4f);

                GUILayout.EndScrollView();
            }

            EditorGUI.EndDisabledGroup();
        }

        private static async void CheckSelectionObjectHistory(int logCount)
        {
            var selectObject = Selection.activeObject;
            var path = AssetDatabase.GetAssetPath(selectObject);

            _isWaiting = true;
            var gitLogString = await GetGitLogString(path, logCount);
            _isWaiting = false;

            if (string.IsNullOrEmpty(gitLogString))
            {
                _gitHistoryListView = null;
                return;
            }

            var commitLogList = Parse(gitLogString);
            ShowWindow(commitLogList);
        }

        /// <summary>
        /// parse git log string.
        ///
        /// commit hash1
        /// Author: name1 <email1>
        /// Date:   YYYY-MM-DD
        ///
        /// Commit message.
        ///
        /// commit hash2
        /// Author: name2 <email2>
        /// Date:   YYYY-MM-DD
        ///
        /// Commit message.
        ///
        /// </summary>
        private static List<GitLogData> Parse(string log)
        {
            // List each commit.
            var rawGitLogs = log.Split(new[] {"commit "}, StringSplitOptions.None);
            var result = new List<GitLogData>();
            /**
             *
             * hash
             * Author: name <email>
             * Date:   2020-05-08
             *
             * commit message
             *
             */
            foreach (var rawGitLog in rawGitLogs)
            {
                // remove blank lines.
                var text = Regex.Replace(rawGitLog, "^[\r\n]+", string.Empty, RegexOptions.Multiline);
                using (var reader = new StringReader(text))
                {
                    // ignore only white space.
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var data = new GitLogData();
                    result.Add(data);
                    for (int i = 0; i < 4; i++)
                    {
                        if (reader.Peek() < 0) break;

                        var value = reader.ReadLine();
                        if (i == 0)
                            data.hash = value;
                        else if (i == 1)
                            data.author = ResolveAuthor(value);
                        else if (i == 2)
                            data.date = value.Replace("Date:", string.Empty).Trim();
                        else if (i == 3)
                            data.comment = value.Trim();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// form author string.
        /// Remove `Author:`, `<email>` and whitespace.
        /// </summary>
        private static string ResolveAuthor(string text)
        {
            try
            {
                return text.Substring(0, text.IndexOf("<", StringComparison.Ordinal))
                    .Replace("Author:", string.Empty)
                    .Trim();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return text;
            }
        }


        /// <summary>
        /// Get git log string.
        /// </summary>
        /// <param name="filePath">File or directory path starting with `Assets`.</param>
        /// <param name="count">Number to be acquired</param>
        /// <returns>git log raw string.</returns>
        private static async Task<string> GetGitLogString(string filePath, int count)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("filePath is null.");
                return null;
            }

            // Remove `Assets` from Application.dataPath
            var absolutePath = Application.dataPath.Remove(
                Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal));

            var targetPath = $"{absolutePath}{filePath}";
            var dirPath = Path.GetDirectoryName(targetPath);

            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                FileName = "git",
                Arguments = $"log --date=short -n {count.ToString()} -- {targetPath}",
                WorkingDirectory = dirPath
            };

            string result;

            using (Process process = Process.Start(processStartInfo))
            {
                process.StandardInput.Close();
                var streamOutput = process.StandardOutput;
                var standardError = process.StandardError;

                var errorString = standardError.ReadToEnd();

                if (string.IsNullOrEmpty(errorString) == false)
                {
                    Debug.LogError($"【UniGitHistoryViewer:Error】{errorString}");
                }

                result = await streamOutput.ReadToEndAsync();

                streamOutput.Close();
                standardError.Close();
                process.WaitForExit();
            }

            return result;
        }
    }

    public class GitLogData
    {
        public string author;
        public string hash;
        public string comment;
        public string date;

        public override string ToString()
        {
            return $"author : {author} | comment : {comment} | date : {date} | {hash}";
        }
    }
}