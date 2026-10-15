using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace info.shibuya24
{
    /// <summary>
    /// A changed file prepared for drawing. AssetPath and IsTarget are resolved once when the result arrives
    /// because looking them up every OnGUI is too slow for large commits.
    /// </summary>
    internal sealed class ChangedFileRow
    {
        public GitChangedFile File { get; }
        public string AssetPath { get; }
        public bool IsTarget { get; }

        public ChangedFileRow(GitChangedFile file, string assetPath, bool isTarget)
        {
            File = file;
            AssetPath = assetPath;
            IsTarget = isTarget;
        }
    }

    /// <summary>
    /// Lower pane: full message, author, hash and the files changed by the selected commit.
    /// </summary>
    internal sealed class CommitDetailView
    {
        private const float Padding = 8f;
        private const float LineHeight = 18f;
        private const float FileRowHeight = 20f;
        private const float BadgeWidth = 18f;
        private const float CopyButtonWidth = 48f;
        private const float HeaderLabelWidth = 56f;

        private readonly Action<string, string> _onCopy;
        private readonly Action<string> _onPing;
        private Vector2 _scrollPosition;

        /// <param name="onCopy">Receives the text to copy and a short label for the notification.</param>
        /// <param name="onPing">Receives an asset path to highlight in the Project window.</param>
        public CommitDetailView(Action<string, string> onCopy, Action<string> onPing)
        {
            _onCopy = onCopy;
            _onPing = onPing;
        }

        public void ResetScroll()
        {
            _scrollPosition = Vector2.zero;
        }

        /// <param name="files">null while the changed files are still loading.</param>
        public void Draw(Rect rect, GitCommit commit, List<ChangedFileRow> files, string filesError)
        {
            GUILayout.BeginArea(rect);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Leave room for the vertical scrollbar so wrapped text never hides under it.
            float width = rect.width - Padding * 2f - GUI.skin.verticalScrollbar.fixedWidth;

            GUILayout.Space(Padding);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
                {
                    DrawHeader(commit, width);
                    DrawBody(commit, width);
                    GUILayout.Space(10f);
                    DrawChangedFiles(files, filesError, width);
                }
            }

            GUILayout.Space(Padding);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHeader(GitCommit commit, float width)
        {
            float subjectHeight = ViewerStyles.Subject.CalcHeight(new GUIContent(commit.Subject), width);
            EditorGUILayout.SelectableLabel(commit.Subject, ViewerStyles.Subject, GUILayout.Height(subjectHeight));

            GUILayout.Space(2f);
            Rect authorLine = GUILayoutUtility.GetRect(width, LineHeight);
            Rect dotRect = new Rect(authorLine.x + 1f, authorLine.center.y - 4f, 8f, 8f);
            ViewerStyles.DrawDot(dotRect, ViewerStyles.AuthorColor(commit.AuthorName));

            GUIContent author = new GUIContent(commit.AuthorName);
            Rect authorRect = new Rect(dotRect.xMax + 6f, authorLine.y, EditorStyles.boldLabel.CalcSize(author).x, LineHeight);
            GUI.Label(authorRect, author, EditorStyles.boldLabel);
            Rect emailRect = new Rect(authorRect.xMax + 4f, authorLine.y, authorLine.xMax - authorRect.xMax - 4f, LineHeight);
            GUI.Label(emailRect, $"<{commit.AuthorEmail}>", ViewerStyles.SecondaryLabel);

            Rect dateLine = GUILayoutUtility.GetRect(width, LineHeight);
            GUI.Label(new Rect(dateLine.x, dateLine.y, HeaderLabelWidth, LineHeight), "Date", ViewerStyles.SecondaryLabel);
            string date = $"{RelativeTimeFormatter.FormatAbsolute(commit.AuthorDate)}  " +
                          $"({RelativeTimeFormatter.FormatRelative(commit.AuthorDate, DateTimeOffset.Now)})";
            GUI.Label(new Rect(dateLine.x + HeaderLabelWidth, dateLine.y, dateLine.width - HeaderLabelWidth, LineHeight), date);

            Rect hashLine = GUILayoutUtility.GetRect(width, LineHeight);
            Rect hashLabelRect = new Rect(hashLine.x, hashLine.y, HeaderLabelWidth, LineHeight);
            GUI.Label(hashLabelRect, commit.IsMerge ? "Merge" : "Commit", ViewerStyles.SecondaryLabel);
            Rect hashRect = new Rect(hashLabelRect.xMax, hashLine.y, hashLine.width - hashLabelRect.width - CopyButtonWidth - 4f, LineHeight);
            EditorGUI.SelectableLabel(hashRect, commit.Hash);
            Rect copyRect = new Rect(hashLine.xMax - CopyButtonWidth, hashLine.y, CopyButtonWidth, LineHeight);
            if (GUI.Button(copyRect, "Copy", EditorStyles.miniButton))
            {
                _onCopy(commit.Hash, $"Copied {commit.ShortHash}");
            }
        }

        private static void DrawBody(GitCommit commit, float width)
        {
            if (commit.Body.Length == 0)
            {
                return;
            }

            GUILayout.Space(6f);
            float height = ViewerStyles.Body.CalcHeight(new GUIContent(commit.Body), width);
            Rect bodyRect = GUILayoutUtility.GetRect(width, height);
            EditorGUI.DrawRect(bodyRect, ViewerStyles.BodyBackgroundColor);
            EditorGUI.SelectableLabel(bodyRect, commit.Body, ViewerStyles.Body);
        }

        private void DrawChangedFiles(List<ChangedFileRow> files, string filesError, float width)
        {
            string header = files == null ? "CHANGED FILES" : $"CHANGED FILES ({files.Count})";
            GUILayout.Label(header, ViewerStyles.SectionHeader);

            if (filesError != null)
            {
                EditorGUILayout.HelpBox(filesError, MessageType.Error);
                return;
            }

            if (files == null)
            {
                GUILayout.Label("Loading…", ViewerStyles.SecondaryLabel);
                return;
            }

            foreach (ChangedFileRow row in files)
            {
                DrawFileRow(GUILayoutUtility.GetRect(width, FileRowHeight), row);
            }
        }

        private void DrawFileRow(Rect rect, ChangedFileRow row)
        {
            if (row.IsTarget)
            {
                EditorGUI.DrawRect(rect, ViewerStyles.TargetFileHighlightColor);
            }

            Rect badgeRect = new Rect(rect.x + 2f, rect.center.y - 8f, BadgeWidth, 16f);
            ViewerStyles.DrawBadge(badgeRect, row.File.Status.ToString(), ViewerStyles.StatusColor(row.File.Status));

            string path = row.File.OldPath == null ? row.File.Path : $"{row.File.OldPath}  →  {row.File.Path}";
            Rect pathRect = new Rect(badgeRect.xMax + 6f, rect.y, rect.xMax - badgeRect.xMax - 6f, rect.height);
            GUIStyle style = row.IsTarget ? ViewerStyles.TargetFilePath : ViewerStyles.FilePath;

            if (row.AssetPath == null)
            {
                // Deleted files and files outside this Unity project cannot be pinged.
                GUI.Label(pathRect, new GUIContent(path, path), ViewerStyles.SecondaryLabel);
                return;
            }

            EditorGUIUtility.AddCursorRect(pathRect, MouseCursor.Link);
            if (GUI.Button(pathRect, new GUIContent(path, "Click to show in Project window"), style))
            {
                _onPing(row.AssetPath);
            }
        }
    }
}
