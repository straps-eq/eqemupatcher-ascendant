# Changelog

## [Unreleased]

- The patcher sets the modified time of files listed in a server-hosted `<client>/filetimes.yml` on every start and patch run, e.g. the ToB achievement files that must match `Character:AchievementFileTime`.
- The Patch button no longer stays red after a patch finds every file already up to date; the filelist version is now saved in that case too.
- An `eqemupatcher.yml` the patcher can't read (e.g. one left by another server's patcher with `clientVersion: Quarm`) is now reset instead of crashing at startup with "Requested value ... was not found".
- `clients.yml` entries can name a `supported` build. Live clients on it have LaunchPad.exe disabled so they can't update past it; clients on an older listed build are updated through LaunchPad, which the patcher runs and waits on. `tools/Update-Clients.ps1` hashes a new eqgame.exe and updates `clients.yml`.
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