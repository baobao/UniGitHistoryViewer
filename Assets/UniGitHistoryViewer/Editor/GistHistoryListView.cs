using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace info.shibuya24
{
    /// <summary>
    /// Custom list in EditorWindow
    /// </summary>
    public class GistHistoryListView
    {
        private float TotalHeight => (_logList.Count + 1) * LineHeight;

        private float ContentHeight { get; } = EditorGUIUtility.singleLineHeight;

        private float LineHeight { get; } =
            EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 4;

        /// <summary>
        /// draw callback
        /// </summary>
        public event System.Action<Rect, GitLogData> onDrawElementCallback;

        private readonly List<GitLogData> _logList;

        public GistHistoryListView(List<GitLogData> logList, EditorWindow window)
        {
            _logList = logList;

            // drawList
            onDrawElementCallback = (rect, data) =>
            {
                rect.width = 20;
                if (GUI.Button(rect, "C"))
                {
                    // copy commit hash
                    GUIUtility.systemCopyBuffer = data.hash;
                    window.ShowNotification(new GUIContent($"Copy Commit Hash\n{data.hash}"));
                }

                var tmp = GUI.color;
                GUI.color = Color.black;

                // draw date
                rect.x += 20;
                rect.width = 90f;
                EditorGUI.LabelField(rect, data.date);

                // draw author
                rect.x += 90f;
                EditorGUI.LabelField(rect, data.author);
                rect.width = 100f;


                // commit comment
                rect.x += 100f;
                rect.width = 400f;
                EditorGUI.LabelField(rect, data.comment);

                // revert color
                GUI.color = tmp;
            };
        }

        public void DoLayoutList(float marginY)
        {
            var lastRect = GUILayoutUtility.GetRect(new GUIContent(""), GUIStyle.none);
            var position = GUILayoutUtility.GetRect(lastRect.width, TotalHeight);

            position.y = marginY;

            // define color
            var backgroundDefaultColor = GUI.backgroundColor;
            var backgroundLightColor = backgroundDefaultColor;
            var backgroundDarkColor = backgroundLightColor * 0.8f;
            backgroundDarkColor.a = 1;

            position.height = TotalHeight;

            var fieldRect = position;
            fieldRect.height = LineHeight;

            for (int i = 0; i < _logList.Count; i++)
            {
                // draw background
                var backgroundRect = fieldRect;
                backgroundRect.xMin += 1;
                backgroundRect.xMax -= 1;
                if (_logList.Count == i + 1)
                {
                    backgroundRect.yMax -= 1;
                }

                // apply background color
                EditorGUI.DrawRect(backgroundRect, i % 2 == 0 ? backgroundDarkColor : backgroundLightColor);
                onDrawElementCallback?.Invoke(GetContentRect(fieldRect), _logList[i]);

                fieldRect.y += LineHeight;
            }
        }

        private Rect GetContentRect(Rect rect)
        {
            rect.height = ContentHeight;
            rect.y += (LineHeight - ContentHeight) / 2;
            rect.xMin += 4;
            rect.xMax -= 24;
            return rect;
        }
    }
}