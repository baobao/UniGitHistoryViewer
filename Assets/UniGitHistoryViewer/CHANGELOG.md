# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this package adheres to [Semantic Versioning](https://semver.org/).

## [2.0.0] - 2026-10-15

### Added
- Detail pane with the full message, author, date, hash and changed files of the selected commit
- Click a changed file to highlight it in the Project window
- Search field to filter commits by message, author or hash
- The window follows the Project window selection; lock it from the lock icon on the window tab
- `Assets/Show Git History` context menu in the Project window
- Rename tracking for single files (`git log --follow`)
- Support for assets in local (`file:`) and embedded packages
- Clear messages when Git is not installed or the asset is not inside a Git repository

### Changed
- Commit list rebuilt with TreeView: resizable columns, zebra rows and keyboard navigation
- Colors adapt to both the Light and the Dark Editor skin
- The window title is now "Git History"
- Minimum Unity version is now 2022.3

### Removed
- Public types `GitLogData` and `GistHistoryListView`

### Fixed
- Commit messages containing the word "commit" or blank lines broke parsing
- Possible deadlock when Git wrote a large output
- Garbled non-ASCII commit messages and paths

### Security
- The Git executable is resolved from absolute `PATH` entries only, so a `git` executable placed inside the project is never run

## [1.0.0] - 2020-07-16

### Added
- Initial release
