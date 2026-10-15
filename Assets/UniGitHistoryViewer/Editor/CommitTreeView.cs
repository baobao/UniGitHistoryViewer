using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_2_OR_NEWER
// Unity 6.2 made TreeView generic and deprecated the int-only version.
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace info.shibuya24
{
    /// <summary>
    /// Commit list with resizable columns, zebra rows, keyboard navigation and search.
    /// Item ids are indexes into the commit list.
    /// </summary>
    internal sealed class CommitTreeView : TreeView
    {
        // Message comes first so it stays visible when the window is narrow.
        private enum Column
        {
            Message,
            Author,
            Date,
            Hash,
        }

        private const float RowHeight = 22f;
        private const float AuthorDotSize = 8f;

        private sealed class CommitItem : TreeViewItem
        {
            public GitCommit Commit { get; }

            public CommitItem(int id, GitCommit commit) : base(id, 0, commit.Subject)
            {
                Commit = commit;
            }
        }

        private List<GitCommit> _commits = new List<GitCommit>();
        private readonly Action<GitCommit> _onSelectionChanged;
        private readonly Action<GitCommit> _onCopyRequested;

        public CommitTreeView(
            TreeViewState state,
            MultiColumnHeader header,
            Action<GitCommit> onSelectionChanged,
            Action<GitCommit> onCopyRequested) : base(state, header)
        {
            _onSelectionChanged = onSelectionChanged;
            _onCopyRequested = onCopyRequested;
            rowHeight = RowHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        public static MultiColumnHeaderState CreateHeaderState()
        {
            MultiColumnHeaderState.Column[] columns =
            {
                CreateColumn("Message", 400f, 120f, true),
                CreateColumn("Author", 130f, 70f, false),
                CreateColumn("Date", 116f, 90f, false),
                CreateColumn("Commit", 64f, 56f, false),
            };
            return new MultiColumnHeaderState(columns);
        }

        private static MultiColumnHeaderState.Column CreateColumn(string title, float width, float minWidth, bool isStretch)
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(title),
                headerTextAlignment = TextAlignment.Left,
                width = width,
                minWidth = minWidth,
                autoResize = isStretch,
                canSort = false,
                allowToggleVisibility = isStretch == false,
            };
        }

        public void SetCommits(List<GitCommit> commits)
        {
            _commits = commits;
            Reload();
        }

        /// <summary>
        /// Does not raise the selection callback; the caller already knows which commit it selected.
        /// </summary>
        public void SelectCommit(int index)
        {
            SetSelection(new[] {index}, TreeViewSelectionOptions.None);

            // Framing right after a reload uses a zero-height view and scrolls the first row out of sight.
            if (index == 0)
            {
                state.scrollPos = Vector2.zero;
                return;
            }

            FrameItem(index);
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem(-1, -1, "Root");
            root.children = new List<TreeViewItem>(_commits.Count);
            for (int i = 0; i < _commits.Count; i++)
            {
                root.AddChild(new CommitItem(i, _commits[i]));
            }

            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            GitCommit commit = ((CommitItem) item).Commit;
            return Contains(commit.Subject, search)
                   || Contains(commit.Body, search)
                   || Contains(commit.AuthorName, search)
                   || commit.Hash.StartsWith(search, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string text, string search)
        {
            return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            _onSelectionChanged(selectedIds.Count == 0 ? null : _commits[selectedIds[0]]);
        }

        protected override void ContextClickedItem(int id)
        {
            GitCommit commit = _commits[id];
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Commit Hash"), false, () => _Copy(commit.Hash));
            menu.AddItem(new GUIContent("Copy Short Hash"), false, () => _Copy(commit.ShortHash));
            menu.AddItem(new GUIContent("Copy Message"), false, () => _Copy(FullMessage(commit)));
            menu.ShowAsContext();

            void _Copy(string text)
            {
                GUIUtility.systemCopyBuffer = text;
                _onCopyRequested(commit);
            }
        }

        public static string FullMessage(GitCommit commit)
        {
            return commit.Body.Length == 0 ? commit.Subject : $"{commit.Subject}\n\n{commit.Body}";
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            GitCommit commit = ((CommitItem) args.item).Commit;
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                Rect cellRect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref cellRect);
                DrawCell(cellRect, commit, (Column) args.GetColumn(i), args.selected && args.focused);
            }
        }

        private static void DrawCell(Rect rect, GitCommit commit, Column column, bool isFocusedSelection)
        {
            // Secondary gray loses contrast against the blue selection background, so selected rows use the normal label color.
            GUIStyle secondary = isFocusedSelection ? EditorStyles.label : ViewerStyles.SecondaryLabel;
            switch (column)
            {
                case Column.Hash:
                    GUI.Label(rect, commit.ShortHash, secondary);
                    break;
                case Column.Date:
                    string relative = RelativeTimeFormatter.FormatRelative(commit.AuthorDate, DateTimeOffset.Now);
                    GUI.Label(rect, new GUIContent(RelativeTimeFormatter.FormatAbsolute(commit.AuthorDate), relative), secondary);
                    break;
                case Column.Author:
                    Rect dotRect = new Rect(rect.x + 2f, rect.center.y - AuthorDotSize * 0.5f, AuthorDotSize, AuthorDotSize);
                    ViewerStyles.DrawDot(dotRect, ViewerStyles.AuthorColor(commit.AuthorName));
                    rect.xMin += AuthorDotSize + 6f;
                    GUI.Label(rect, new GUIContent(commit.AuthorName, commit.AuthorEmail));
                    break;
                case Column.Message:
                    GUI.Label(rect, new GUIContent(commit.Subject, FullMessage(commit)));
                    break;
            }
        }
    }
}
