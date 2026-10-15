#!/bin/sh
# Builds the Asset Store submission project from the committed package source.
# The GitHub package (Assets/UniGitHistoryViewer) stays the single source; this script only assembles a copy:
#   - Tests are left out because the Asset Store validator requires test-framework to be a declared dependency,
#     and users of the Asset Store version do not need the tests.
#   - README.md / README.ja.md are copied into Documentation~ as offline documentation.
#
# Usage: Tools/build-asset-store-package.sh [output-directory]
# Env:   UNITY_VERSION  Editor version written to the submission project (default: 2022.3.62f3)
set -eu

PACKAGE_NAME="info.shibuya24.uni-git-history-viewer"
PACKAGE_DIR="Assets/UniGitHistoryViewer"
UNITY_VERSION="${UNITY_VERSION:-2022.3.62f3}"

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUTPUT_DIR="${1:-$REPO_ROOT/Build/AssetStore}"
PROJECT_DIR="$OUTPUT_DIR/SubmissionProject"
EMBEDDED_DIR="$PROJECT_DIR/Packages/$PACKAGE_NAME"

cd "$REPO_ROOT"

if [ -n "$(git status --porcelain -- "$PACKAGE_DIR" README.md README.ja.md)" ]; then
    echo "error: commit the changes in $PACKAGE_DIR and the READMEs first. The build uses HEAD." >&2
    exit 1
fi

VERSION=$(git show "HEAD:$PACKAGE_DIR/package.json" | sed -n 's/^ *"version": *"\([^"]*\)".*/\1/p')

rm -rf "$EMBEDDED_DIR"
mkdir -p "$EMBEDDED_DIR" "$PROJECT_DIR/Assets" "$PROJECT_DIR/ProjectSettings"

git archive --format=tar "HEAD:$PACKAGE_DIR" | tar -x -C "$EMBEDDED_DIR"
rm -rf "$EMBEDDED_DIR/Tests" "$EMBEDDED_DIR/Tests.meta"

mkdir -p "$EMBEDDED_DIR/Documentation~"
git show HEAD:README.md > "$EMBEDDED_DIR/Documentation~/README.md"
git show HEAD:README.ja.md > "$EMBEDDED_DIR/Documentation~/README.ja.md"

if [ ! -f "$PROJECT_DIR/Packages/manifest.json" ]; then
    cat > "$PROJECT_DIR/Packages/manifest.json" <<'MANIFEST'
{
  "dependencies": {
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0"
  }
}
MANIFEST
fi

printf 'm_EditorVersion: %s\n' "$UNITY_VERSION" > "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt"

ARCHIVE="$OUTPUT_DIR/$PACKAGE_NAME-$VERSION.tgz"
tar -czf "$ARCHIVE" -C "$PROJECT_DIR/Packages" "$PACKAGE_NAME"

echo "Submission project: $PROJECT_DIR"
echo "Embedded package:   $EMBEDDED_DIR ($VERSION, from $(git rev-parse --short HEAD))"
echo "Archive:            $ARCHIVE"
