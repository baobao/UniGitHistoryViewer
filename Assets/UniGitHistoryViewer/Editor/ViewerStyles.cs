using UnityEditor;
using UnityEngine;

namespace info.shibuya24
{
    /// <summary>
    /// Colors and GUIStyles shared by the viewer. Every color has a Light and a Dark skin variant
    /// because hard-coded text colors become unreadable on one of the two skins.
    /// </summary>
    internal static class ViewerStyles
    {
        private static bool _hasCache;
        private static bool _cachedProSkin;
        private static GUIStyle _secondaryLabel;
        private static GUIStyle _subject;
        private static GUIStyle _body;
        private static GUIStyle _emptyState;
        private static GUIStyle _badge;
        private static GUIStyle _sectionHeader;
        private static GUIStyle _filePath;
        private static GUIStyle _targetFilePath;
        private static GUIStyle _toolbarTarget;

        private static bool IsPro => EditorGUIUtility.isProSkin;

        public static Color SecondaryTextColor => IsPro ? new Color(0.62f, 0.62f, 0.62f) : new Color(0.36f, 0.36f, 0.36f);
        public static Color SplitterColor => IsPro ? new Color(0.10f, 0.10f, 0.10f) : new Color(0.60f, 0.60f, 0.60f);
        public static Color TargetFileHighlightColor => IsPro ? new Color(0.30f, 0.50f, 0.85f, 0.22f) : new Color(0.25f, 0.50f, 0.95f, 0.15f);
        public static Color BodyBackgroundColor => IsPro ? new Color(0f, 0f, 0f, 0.15f) : new Color(1f, 1f, 1f, 0.45f);

        public static GUIStyle SecondaryLabel { get { EnsureStyles(); return _secondaryLabel; } }
        public static GUIStyle Subject { get { EnsureStyles(); return _subject; } }
        public static GUIStyle Body { get { EnsureStyles(); return _body; } }
        public static GUIStyle EmptyState { get { EnsureStyles(); return _emptyState; } }
        public static GUIStyle Badge { get { EnsureStyles(); return _badge; } }
        public static GUIStyle SectionHeader { get { EnsureStyles(); return _sectionHeader; } }
        public static GUIStyle FilePath { get { EnsureStyles(); return _filePath; } }
        public static GUIStyle TargetFilePath { get { EnsureStyles(); return _targetFilePath; } }
        public static GUIStyle ToolbarTarget { get { EnsureStyles(); return _toolbarTarget; } }

        /// <summary>
        /// Gives every author a stable color so commits by the same person can be spotted at a glance.
        /// string.GetHashCode is avoided because it is not guaranteed to be stable across runtimes.
        /// </summary>
        public static Color AuthorColor(string authorName)
        {
            uint hash = 2166136261;
            foreach (char c in authorName)
            {
                hash = (hash ^ c) * 16777619;
            }

            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, IsPro ? 0.50f : 0.60f, IsPro ? 0.90f : 0.75f);
        }

        public static Color StatusColor(char status)
        {
            switch (status)
            {
                case 'A': return new Color(0.29f, 0.69f, 0.31f);
                case 'M': return new Color(0.89f, 0.60f, 0.13f);
                case 'D': return new Color(0.86f, 0.30f, 0.27f);
                case 'R':
                case 'C': return new Color(0.58f, 0.44f, 0.86f);
                default: return new Color(0.50f, 0.50f, 0.50f);
            }
        }

        public static void DrawDot(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, rect.width * 0.5f);
        }

        public static void DrawBadge(Rect rect, string text, Color color)
        {
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 3f);
            GUI.Label(rect, text, Badge);
        }

        private static void EnsureStyles()
        {
            if (_hasCache && _cachedProSkin == IsPro)
            {
                return;
            }

            _hasCache = true;
            _cachedProSkin = IsPro;

            _secondaryLabel = new GUIStyle(EditorStyles.label);
            _secondaryLabel.normal.textColor = SecondaryTextColor;

            _subject = new GUIStyle(EditorStyles.boldLabel) {fontSize = 14, wordWrap = true};

            _body = new GUIStyle(EditorStyles.label) {wordWrap = true, padding = new RectOffset(8, 8, 6, 6)};

            _emptyState = new GUIStyle(EditorStyles.label) {alignment = TextAnchor.MiddleCenter, wordWrap = true};
            _emptyState.normal.textColor = SecondaryTextColor;

            _badge = new GUIStyle(EditorStyles.miniBoldLabel) {alignment = TextAnchor.MiddleCenter, padding = new RectOffset()};
            _badge.normal.textColor = Color.white;

            _sectionHeader = new GUIStyle(EditorStyles.miniBoldLabel);
            _sectionHeader.normal.textColor = SecondaryTextColor;

            _filePath = new GUIStyle(EditorStyles.label) {clipping = TextClipping.Clip};
            _targetFilePath = new GUIStyle(EditorStyles.boldLabel) {clipping = TextClipping.Clip};

            _toolbarTarget = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                imagePosition = ImagePosition.ImageLeft,
            };
        }
    }
}
