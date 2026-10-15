# UniGitHistoryViewer

[日本語](README.ja.md)

UniGitHistoryViewer is a Unity Editor extension that shows the Git history of the file or folder you select in the Project window.
You can check who changed an asset, when, and why without switching to a separate Git client.

![Git History window](https://raw.githubusercontent.com/baobao/UniGitHistoryViewer/master/Docs/images/screenshot-main.png)

- Works inside the Unity Editor only. Nothing is added to your builds.
- Reads history with the Git installed on your computer. Git is not bundled.
- Read-only. The tool never commits, checks out or changes your repository.
- Free and open source (MIT License).

## Requirements

- Unity 2022.3 or later
- Git installed on your computer (<https://git-scm.com/>)
- A Unity project, or a local package, that is inside a Git repository

## Installation

Install the package in one of the following ways. Use only one of them in a project, because they share the same package name (`info.shibuya24.uni-git-history-viewer`).

### Unity Asset Store

1. Get the package on the Unity Asset Store.
2. In Unity, open **Window > Package Manager**.
3. Select **My Assets**, find **UniGitHistoryViewer** and click **Install**.

### Git URL

1. Open **Window > Package Manager**.
2. Click **+** and choose **Install package from git URL...**.
3. Enter the following URL:

```
https://github.com/baobao/UniGitHistoryViewer.git?path=Assets/UniGitHistoryViewer
```

You can also add the following line to `dependencies` in `Packages/manifest.json`:

```json
"info.shibuya24.uni-git-history-viewer": "https://github.com/baobao/UniGitHistoryViewer.git?path=Assets/UniGitHistoryViewer"
```

## Usage

### Open the window

- Right-click a file or folder in the Project window and choose **Show Git History**, or
- choose **Tools > UniGitHistoryViewer** from the menu bar.

The window is titled **Git History**. The toolbar shows the target asset, the number of loaded commits, a search field, the **Max** field and a refresh button.

### Follow the selection

While the window is open, it shows the history of the asset you select in the Project window.
Selecting a GameObject in the Hierarchy does not change the target.

### Lock the window

Click the lock icon on the window tab (or choose **Lock** from the tab menu) to keep showing the current asset while you select other assets.
Click it again to unlock. The window then shows the asset currently selected.

Click the asset name in the toolbar to highlight the asset in the Project window.

### Check a commit

The commit list has the **Message**, **Author**, **Date** and **Commit** (short hash) columns.
You can resize the columns, and hide **Author**, **Date** or **Commit** from the right-click menu of the column header.
Use the arrow keys to move between commits.

Select a commit to see its details in the lower pane:

- Subject and the full commit message body
- Author name and email address
- Date and time, with relative time such as "3 days ago"
- Full commit hash (a merge commit is labeled **Merge**)
- Changed files

Drag the border between the list and the details to change their sizes.

### Search commits

Type in the search field on the toolbar. The list keeps commits whose subject, body or author name contains the text, or whose hash starts with the text. The search is not case-sensitive.

### Copy the commit hash

- Click **Copy** next to the hash in the details, or
- press **Ctrl+C** (**Cmd+C** on macOS) while the commit list has focus, or
- right-click a commit and choose **Copy Commit Hash**, **Copy Short Hash** or **Copy Message**.

### Changed files

The details list the files changed by the selected commit with a status badge:

| Badge | Meaning |
|---|---|
| A | Added |
| M | Modified |
| D | Deleted |
| R | Renamed (shown as `old path → new path`) |
| C | Copied |

The selected asset (or the files inside the selected folder) is highlighted.
Click a file to highlight it in the Project window. Files that no longer exist or are outside the Unity project are shown in gray and cannot be clicked.

For a merge commit, the files are compared with the first parent.

### History before a rename

When you select a single file, the history continues past renames (`git log --follow`), so commits made under the old file name are also listed.
Folders do not follow renames.

### Number of commits

**Max** sets how many commits are loaded (default 100, up to 10000). Press Enter to apply. The refresh button reloads the history.

### Local packages

Assets in local packages (installed with a `file:` path) and embedded packages can be selected in the same way, as long as the package folder is inside a Git repository.
Packages installed from a registry or a Git URL are stored in `Library/PackageCache`, which is not part of your repository, so they have no history.

## How it works

The window runs the following read-only Git commands on a background thread, so the Editor does not freeze while history is loading:

- `git rev-parse --show-toplevel --show-prefix`
- `git log` (with `--follow` for a single file)
- `git diff-tree --name-status`

Git is looked up in the absolute paths listed in the `PATH` environment variable. On macOS the usual install locations (`/opt/homebrew/bin`, `/usr/local/bin`, `/usr/bin`) are also checked, and on Windows `C:\Program Files\Git\cmd`.
The tool does not access the network and does not send any data.

## Troubleshooting

| Message or symptom | What to do |
|---|---|
| "Git was not found on this computer." | Install Git and restart Unity. If Git is installed in another location, add it to `PATH` and restart Unity Hub and Unity. |
| "The selected asset is not inside a Git repository." | The project or package folder is not under Git. Run `git init` or open a project inside a repository. |
| "No commits found for this path." | The asset has never been committed, or it is ignored by `.gitignore`. Commit the asset to see its history. |
| A package asset has no history | Packages in `Library/PackageCache` (registry or Git URL installs) are not part of your repository. |
| The history of a folder stops at a rename | Rename tracking works for single files only. Select a file inside the folder. |
| Loading takes a long time | Reduce **Max**. Following renames on a long history can be slow in large repositories. |
| Commit messages or paths are garbled | Make sure your commits are stored in UTF-8. The tool asks Git for UTF-8 output. |

## Third-party software

This package contains no third-party code or assets. Git is not included; it uses the Git already installed on your computer.

## License

[MIT License](LICENSE)

## Author

ohbashunsuke — <https://ohbashunsuke.com/>
