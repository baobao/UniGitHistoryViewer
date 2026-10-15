using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_2_OR_NEWER
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace info.shibuya24
{
    /// <summary>
    /// EditorWindow that shows the git history of the asset selected in the Project window.
    /// git runs on a worker thread and the result is picked up in EditorApplication.update,
    /// so the editor never freezes and no async void is needed.
    /// </summary>
    public sealed class UniGitHistoryViewer : EditorWindow, IHasCustomMenu
    {
        private const string WindowTitle = "Git History";
        private const int DefaultMaxCount = 100;
        private const int MaxCountLimit = 10000;
        private const float SplitterThickness = 5f;
        private const float MinListHeight = 60f;
        private const float MinDetailHeight = 80f;

        [SerializeField] private string _targetAssetPath;
        [SerializeField] private string _selectedHash;
        [SerializeField] private bool _isLocked;
        [SerializeField] private int _maxCount = DefaultMaxCount;
        [SerializeField] private float _detailHeight = 240f;
        [SerializeField] private TreeViewState _treeViewState;
        [SerializeField] private MultiColumnHeaderState _headerState;

        private CommitTreeView _treeView;
        private SearchField _searchField;
        private CommitDetailView _detailView;
        private GUIStyle _lockButtonStyle;

        private GitHistoryTarget _target;
        private GitHistoryResult _history;
        private GitCommit _selectedCommit;
        private readonly Dictionary<string, List<ChangedFileRow>> _changedFilesCache = new Dictionary<string, List<ChangedFileRow>>();
        private string _changedFilesError;

        private CancellationTokenSource _historyCts;
        private Task<GitHistoryResult> _historyTask;
        private CancellationTokenSource _filesCts;
        private Task<GitChangedFilesResult> _filesTask;
        private GitCommit _filesTaskCommit;

        private bool IsLoadingHistory => _historyTask != null;

        [MenuItem("Tools/UniGitHistoryViewer")]
        private static void Open()
        {
            UniGitHistoryViewer window = GetWindow<UniGitHistoryViewer>();
            window.ShowTargetFromSelection();
        }

        [MenuItem("Assets/Show Git History", false, 2000)]
        private static void OpenFromProjectWindow()
        {
            UniGitHistoryViewer window = GetWindow<UniGitHistoryViewer>();
            window.SetTarget(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem("Assets/Show Git History", true)]
        private static bool CanOpenFromProjectWindow()
        {
            return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject)) == false;
        }

        // EditorWindow rebuilds itself after every domain reload, so non-serialized state has to be recreated here.
        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);

            if (_treeViewState == null)
            {
                _treeViewState = new TreeViewState();
            }

            MultiColumnHeaderState headerState = CommitTreeView.CreateHeaderState();
            bool isFirstLayout = _headerState == null;
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_headerState, headerState))
            {
                MultiColumnHeaderState.OverwriteSerializedFields(_headerState, headerState);
            }

            _headerState = headerState;
            MultiColumnHeader header = new MultiColumnHeader(headerState);
            if (isFirstLayout)
            {
                header.ResizeToFit();
            }

            _treeView = new CommitTreeView(_treeViewState, header, OnCommitSelected, commit => NotifyCopied(commit.ShortHash));
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            _detailView = new CommitDetailView(Copy, PingAsset);

            EditorApplication.update += OnEditorUpdate;

            if (string.IsNullOrEmpty(_targetAssetPath) == false)
            {
                LoadHistory(_targetAssetPath);
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            CancelHistoryTask();
            CancelFilesTask();
        }

        private void OnSelectionChange()
        {
            if (_isLocked)
            {
                return;
            }

            ShowTargetFromSelection();
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Lock"), _isLocked, ToggleLock);
        }

        // Called by Unity to draw the lock icon in the tab header, the same convention the Inspector uses.
        private void ShowButton(Rect rect)
        {
            if (_lockButtonStyle == null)
            {
                _lockButtonStyle = "IN LockButton";
            }

            bool isLocked = GUI.Toggle(rect, _isLocked, GUIContent.none, _lockButtonStyle);
            if (isLocked != _isLocked)
            {
                ToggleLock();
            }
        }

        private void ToggleLock()
        {
            _isLocked = !_isLocked;
            if (_isLocked == false)
            {
                ShowTargetFromSelection();
            }
        }

        private void ShowTargetFromSelection()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            // Keep the current history when a scene object is selected, otherwise the list flickers away.
            if (string.IsNullOrEmpty(assetPath) || assetPath == _targetAssetPath)
            {
                return;
            }

            SetTarget(assetPath);
        }

        private void SetTarget(string assetPath)
        {
            _targetAssetPath = assetPath;
            _selectedHash = null;
            _history = null;
            _treeView.searchString = string.Empty;
            LoadHistory(assetPath);
        }

        private void LoadHistory(string assetPath)
        {
            CancelHistoryTask();
            CancelFilesTask();
            _changedFilesCache.Clear();

            GitHistoryTarget target = GitHistoryTarget.FromAssetPath(assetPath);
            int maxCount = _maxCount;
            _target = target;
            _historyCts = new CancellationTokenSource();
            CancellationToken ct = _historyCts.Token;
            _historyTask = Task.Run(() => GitHistoryService.LoadHistory(target, maxCount, ct), ct);
            Repaint();
        }

        private void LoadChangedFiles(GitCommit commit)
        {
            CancelFilesTask();
            _changedFilesError = null;
            if (_changedFilesCache.ContainsKey(commit.Hash))
            {
                return;
            }

            string repositoryRoot = _history.RepositoryRoot;
            _filesCts = new CancellationTokenSource();
            CancellationToken ct = _filesCts.Token;
            _filesTaskCommit = commit;
            _filesTask = Task.Run(() => GitHistoryService.LoadChangedFiles(repositoryRoot, commit, ct), ct);
        }

        private void CancelHistoryTask()
        {
            if (_historyCts == null)
            {
                return;
            }

            _historyCts.Cancel();
            _historyCts.Dispose();
            _historyCts = null;
            _historyTask = null;
        }

        private void CancelFilesTask()
        {
            if (_filesCts == null)
            {
                return;
            }

            _filesCts.Cancel();
            _filesCts.Dispose();
            _filesCts = null;
            _filesTask = null;
            _filesTaskCommit = null;
        }

        private void OnEditorUpdate()
        {
            if (_historyTask != null && _historyTask.IsCompleted)
            {
                Task<GitHistoryResult> task = _historyTask;
                CancelHistoryTask();
                ApplyHistory(task.IsFaulted ? GitHistoryResult.Failure(task.Exception.InnerException.Message) : task.Result);
                LogIfFaulted(task);
                Repaint();
            }

            if (_filesTask != null && _filesTask.IsCompleted)
            {
                Task<GitChangedFilesResult> task = _filesTask;
                GitCommit commit = _filesTaskCommit;
                CancelFilesTask();
                ApplyChangedFiles(commit, task.IsFaulted ? GitChangedFilesResult.Failure(task.Exception.InnerException.Message) : task.Result);
                LogIfFaulted(task);
                Repaint();
            }
        }

        // A faulted task is shown in the window and also logged with its stack trace, so failures are never silent.
        private static void LogIfFaulted(Task task)
        {
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception.InnerException);
            }
        }

        private void ApplyHistory(GitHistoryResult history)
        {
            _history = history;
            _treeView.SetCommits(history.Commits);
            if (history.Commits.Count == 0)
            {
                OnCommitSelected(null);
                return;
            }

            int index = Mathf.Max(0, history.Commits.FindIndex(commit => commit.Hash == _selectedHash));
            _treeView.SelectCommit(index);
            OnCommitSelected(history.Commits[index]);
        }

        private void ApplyChangedFiles(GitCommit commit, GitChangedFilesResult result)
        {
            if (result.IsSuccess == false)
            {
                _changedFilesError = result.ErrorMessage;
                return;
            }

            string targetRelativePath = ToRepositoryRelativePath(_target.FullPath);
            List<ChangedFileRow> rows = new List<ChangedFileRow>(result.Files.Count);
            foreach (GitChangedFile file in result.Files)
            {
                string assetPath = _target.ToAssetPath($"{_history.RepositoryRoot}/{file.Path}");
                bool isTarget = IsTargetFile(GitHistoryTarget.Precompose(file.Path), targetRelativePath)
                                || (file.OldPath != null && IsTargetFile(GitHistoryTarget.Precompose(file.OldPath), targetRelativePath));
                rows.Add(new ChangedFileRow(file, assetPath == null ? null : FindExistingAssetPath(assetPath), isTarget));
            }

            _changedFilesCache[commit.Hash] = rows;
        }

        private string ToRepositoryRelativePath(string fullPath)
        {
            return GitHistoryTarget.GetRelativePath(fullPath, _history.RepositoryRoot) ?? string.Empty;
        }

        // Unity keeps decomposed (NFD) names on macOS, so the precomposed path from git may not match the asset path as is.
        private static string FindExistingAssetPath(string assetPath)
        {
            foreach (string candidate in new[] {assetPath, assetPath.Normalize(NormalizationForm.FormD)})
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(candidate)) == false)
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool IsTargetFile(string repositoryRelativePath, string targetRelativePath)
        {
            if (_target.IsDirectory)
            {
                return targetRelativePath.Length == 0 || repositoryRelativePath.StartsWith(targetRelativePath + "/", StringComparison.Ordinal);
            }

            return repositoryRelativePath == targetRelativePath;
        }

        private void OnCommitSelected(GitCommit commit)
        {
            _selectedCommit = commit;
            _selectedHash = commit?.Hash;
            _detailView.ResetScroll();
            if (commit != null)
            {
                LoadChangedFiles(commit);
            }
        }

        private void Copy(string text, string notification)
        {
            GUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent(notification));
        }

        private void NotifyCopied(string shortHash)
        {
            ShowNotification(new GUIContent($"Copied {shortHash}"));
        }

        private static void PingAsset(string assetPath)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(assetPath));
        }

        private void OnGUI()
        {
            HandleCopyCommand();

            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            DrawToolbar(new Rect(0f, 0f, position.width, toolbarHeight));

            Rect contentRect = new Rect(0f, toolbarHeight, position.width, position.height - toolbarHeight);
            if (_target == null)
            {
                DrawEmptyState(contentRect, "Select a file or folder in the Project window\nto see its git history.");
                return;
            }

            if (_history == null)
            {
                DrawEmptyState(contentRect, "Loading history…");
                return;
            }

            if (_history.IsSuccess == false)
            {
                GUILayout.BeginArea(contentRect);
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox(_history.ErrorMessage, MessageType.Error);
                GUILayout.EndArea();
                return;
            }

            if (_history.Commits.Count == 0)
            {
                DrawEmptyState(contentRect, "No commits found for this path.");
                return;
            }

            DrawSplitView(contentRect);
        }

        // Lets Cmd/Ctrl+C copy the selected commit hash while the list has keyboard focus.
        private void HandleCopyCommand()
        {
            Event current = Event.current;
            bool isCommand = current.type == EventType.ValidateCommand || current.type == EventType.ExecuteCommand;
            if (isCommand == false || current.commandName != "Copy" || _selectedCommit == null || _treeView.HasFocus() == false)
            {
                return;
            }

            if (current.type == EventType.ExecuteCommand)
            {
                Copy(_selectedCommit.Hash, $"Copied {_selectedCommit.ShortHash}");
            }

            current.Use();
        }

        private void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawTargetButton();
                GUILayout.FlexibleSpace();

                if (IsLoadingHistory)
                {
                    GUILayout.Label("Loading…", EditorStyles.miniLabel);
                }
                else if (_history != null && _history.IsSuccess)
                {
                    int count = _history.Commits.Count;
                    GUILayout.Label(count == 1 ? "1 commit" : $"{count} commits", EditorStyles.miniLabel);
                }

                _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString, GUILayout.MinWidth(80f), GUILayout.MaxWidth(220f));

                GUILayout.Label(new GUIContent("Max", "Maximum number of commits to load"), EditorStyles.miniLabel);
                int maxCount = Mathf.Clamp(EditorGUILayout.DelayedIntField(_maxCount, EditorStyles.toolbarTextField, GUILayout.Width(44f)), 1, MaxCountLimit);
                if (maxCount != _maxCount)
                {
                    _maxCount = maxCount;
                    ReloadCurrentTarget();
                }

                using (new EditorGUI.DisabledScope(_target == null))
                {
                    GUIContent refresh = EditorGUIUtility.IconContent("Refresh");
                    refresh.tooltip = "Reload history";
                    if (GUILayout.Button(refresh, EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    {
                        ReloadCurrentTarget();
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void DrawTargetButton()
        {
            if (_target == null)
            {
                GUILayout.Label("No target", EditorStyles.miniLabel);
                return;
            }

            GUIContent content = new GUIContent(
                GitHistoryTarget.Precompose(_target.AssetPath),
                AssetDatabase.GetCachedIcon(_target.AssetPath),
                _isLocked ? "Locked. Click to show in Project window" : "Click to show in Project window");
            Vector2 iconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(14f, 14f));
            if (GUILayout.Button(content, ViewerStyles.ToolbarTarget, GUILayout.MaxWidth(position.width * 0.5f)))
            {
                PingAsset(_target.AssetPath);
            }

            EditorGUIUtility.SetIconSize(iconSize);
        }

        private void ReloadCurrentTarget()
        {
            if (_target != null)
            {
                LoadHistory(_target.AssetPath);
            }
        }

        private void DrawSplitView(Rect rect)
        {
            float maxDetailHeight = Mathf.Max(MinDetailHeight, rect.height - MinListHeight - SplitterThickness);
            _detailHeight = Mathf.Clamp(_detailHeight, MinDetailHeight, maxDetailHeight);

            Rect listRect = new Rect(rect.x, rect.y, rect.width, rect.height - _detailHeight - SplitterThickness);
            Rect splitterRect = new Rect(rect.x, listRect.yMax, rect.width, SplitterThickness);
            Rect detailRect = new Rect(rect.x, splitterRect.yMax, rect.width, _detailHeight);

            _treeView.OnGUI(listRect);
            HandleSplitter(splitterRect, rect);

            if (_selectedCommit == null)
            {
                DrawEmptyState(detailRect, "Select a commit to see its details.");
                return;
            }

            List<ChangedFileRow> files;
            _changedFilesCache.TryGetValue(_selectedCommit.Hash, out files);
            _detailView.Draw(detailRect, _selectedCommit, files, _changedFilesError);
        }

        private void HandleSplitter(Rect splitterRect, Rect containerRect)
        {
            EditorGUI.DrawRect(new Rect(splitterRect.x, splitterRect.center.y, splitterRect.width, 1f), ViewerStyles.SplitterColor);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && splitterRect.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        _detailHeight = containerRect.yMax - current.mousePosition.y - SplitterThickness * 0.5f;
                        current.Use();
                        Repaint();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }

                    break;
            }
        }

        private static void DrawEmptyState(Rect rect, string message)
        {
            GUI.Label(rect, message, ViewerStyles.EmptyState);
        }
    }
}
