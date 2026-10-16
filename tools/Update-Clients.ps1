<#
.SYNOPSIS
Adds a new eqgame.exe build to clients.yml and prints the updated file.

.DESCRIPTION
Hashes eqgame.exe, adds the MD5 to the hashes of the clients.yml entry for -Folder (default live),
and with -Supported also makes it the build the server supports, so players on older listed builds
are updated through LaunchPad the next time they run the patcher.
Prints the new clients.yml, or writes it to -OutFile. Upload the result to the patch root.

.EXAMPLE
.\Update-Clients.ps1 -Exe "C:\EQ Ascendant EverquestLive"
Adds the build without supporting it yet (players on it are recognised, nobody is updated).

.EXAMPLE
.\Update-Clients.ps1 -Exe "C:\EQ Ascendant EverquestLive" -Supported -OutFile .\clients.yml
Adds the build, makes it the supported one and saves the file.
#>
param(
    # eqgame.exe, or the client folder that holds it
    [Parameter(Mandatory = $true)][string]$Exe,
    # Current clients.yml: a local path or a URL
    [string]$ClientsFile = "http://downloads.ascendanteq.com/ascendantpatch/clients.yml",
    # The entry to update, matched by its folder
    [string]$Folder = "live",
    # Also make this build the supported one
    [switch]$Supported,
    # Write the new file here instead of printing it
    [string]$OutFile
)

$ErrorActionPreference = "Stop"

if (Test-Path -LiteralPath $Exe -PathType Container) { $Exe = Join-Path $Exe "eqgame.exe" }
$exeItem = Get-Item -LiteralPath $Exe
$hash = (Get-FileHash -LiteralPath $exeItem.FullName -Algorithm MD5).Hash.ToUpper()
$date = $exeItem.LastWriteTime.ToString("yyyy-MM-dd")

if ($ClientsFile -match '^https?://') {
    $text = (Invoke-WebRequest -Uri $ClientsFile -UseBasicParsing).Content
    if ($text -is [byte[]]) { $text = [System.Text.Encoding]::UTF8.GetString($text) }
} else {
    $text = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $ClientsFile))
}
$lines = New-Object System.Collections.Generic.List[string]
$lines.AddRange([string[]]($text -split "\r?\n"))
while ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -eq "") { $lines.RemoveAt($lines.Count - 1) }

function Get-Indent([string]$line) { return $line.Length - $line.TrimStart().Length }

# Find the entry whose folder matches
$folderIdx = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "^\s*(-\s+)?folder:\s*$([regex]::Escape($Folder))\s*(#.*)?$") { $folderIdx = $i; break }
}
if ($folderIdx -lt 0) { throw "No entry with 'folder: $Folder' in $ClientsFile" }

$start = $folderIdx
while ($start -ge 0 -and $lines[$start] -notmatch '^\s*-\s') { $start-- }
if ($start -lt 0) { throw "Couldn't find the start of the '$Folder' entry" }
$entryIndent = Get-Indent $lines[$start]
$end = $start + 1
while ($end -lt $lines.Count -and ($lines[$end].Trim() -eq "" -or (Get-Indent $lines[$end]) -gt $entryIndent)) { $end++ }

$fieldIndent = " " * ((Get-Indent $lines[$folderIdx]) + $(if ($lines[$folderIdx].TrimStart().StartsWith("-")) { 2 } else { 0 }))

# Add the hash
$hashesIdx = -1
for ($i = $start; $i -lt $end; $i++) { if ($lines[$i] -match '^\s*(-\s+)?hashes:\s*$') { $hashesIdx = $i; break } }
$changes = @()
$alreadyListed = $false
if ($hashesIdx -lt 0) {
    $lines.Insert($end, "${fieldIndent}hashes:")
    $lines.Insert($end + 1, "$fieldIndent  - $hash # eqgame.exe dated $date")
    $end += 2
    $changes += "added hashes with $hash"
} else {
    $last = $hashesIdx
    $itemPrefix = "$fieldIndent  - "
    for ($i = $hashesIdx + 1; $i -lt $end; $i++) {
        if ($lines[$i] -match '^(\s*-\s*)([0-9A-Fa-f]{32})') {
            $last = $i
            $itemPrefix = $Matches[1]
            if ($Matches[2].ToUpper() -eq $hash) { $alreadyListed = $true }
        } elseif ($lines[$i].Trim() -ne "" -and -not $lines[$i].Trim().StartsWith("#")) { break }
    }
    if ($alreadyListed) {
        $changes += "$hash was already listed"
    } else {
        $lines.Insert($last + 1, "$itemPrefix$hash # eqgame.exe dated $date")
        $end++
        $changes += "added $hash"
    }
}

# Set the supported build
if ($Supported) {
    $supportedIdx = -1
    for ($i = $start; $i -lt $end; $i++) { if ($lines[$i] -match '^\s*supported:') { $supportedIdx = $i; break } }
    $supportedLine = "${fieldIndent}supported: $hash # eqgame.exe dated $date"
    if ($supportedIdx -ge 0) { $lines[$supportedIdx] = $supportedLine } else { $lines.Insert($folderIdx + 1, $supportedLine) }
    $changes += "supported is now $hash"
}

$result = ($lines -join "`r`n") + "`r`n"
Write-Host "eqgame.exe: $($exeItem.FullName)"
Write-Host "MD5:        $hash (dated $date)"
Write-Host "Changes:    $($changes -join '; ')"
if ($OutFile) {
    [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($OutFile), $result)
    Write-Host "Wrote $OutFile"
} else {
    Write-Host ""
    Write-Output $result
}
