# Changelog

## [2.0.0] - 2026-10-15

### Added
- Detail pane with the full message, author, date, hash and changed files of the selected commit
- Click a changed file to highlight it in the Project window
- Search field to filter commits by message, author or hash
- The window follows the Project selection; lock it from the tab's lock icon
- `Assets/Show Git History` context menu
- Rename tracking for files (`git log --follow`)
- Support for assets in local (`file:`) packages

### Changed
- Commit list rebuilt with TreeView: resizable columns, zebra rows and keyboard navigation
- Colors adapt to both Light and Dark editor skins
- Minimum Unity version is now 2019.4

### Fixed
- Commit messages containing the word "commit" or blank lines broke parsing
- Possible deadlock when git wrote a large output
- Garbled non-ASCII commit messages and paths

## [1.0.0] - 2020-07-16

- Initial release
