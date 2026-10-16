# Changelog

## [Unreleased]

- Added a server-hosted client list (`clients.yml` at FILELIST_URL). An eqgame.exe the built-in hashes don't know is looked up there and patched from the listed folder, e.g. `live/`, so new live builds need no patcher rebuild. RoF2 and ToB detection is unchanged.
- Added The Outer Brood (ToB) client support, pinned to build Sep 11 2025 11:54:10 (eqgame.exe MD5 23DD81D7FAB1808DB93A9F6EC9BEA89C). ToB players get `filelist_tob.yml`; RoF2 is unchanged.
- An unrecognized 64-bit eqgame.exe now gets a message to use the supported ToB build instead of the report-a-client prompt.
- CI builds `filelist_tob.yml` from the `tob/` folder (override the file host with the `TOB_STORAGE_URL` repo variable).
- Fixed the `PATCHER_URL` repo variable being ignored in CI.

## [1.0.4] 2023-02-11

- Introduced new CICD-friendly pipeline
- Added self updating support (yay)
- Added HTTPS support
- Fixed background threading and context cancellation, so it runs smoother (and cancels smoother)
- Added progress in the taskbar icon
- Added option to point to your self built hosting inside the CICD
- Added a pipeline that makes it easy to pull down upstream changes without impacting your version 
- Fix virustotal from falsely identifying as virus