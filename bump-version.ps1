# bump-version.ps1
$files = @(
    '.\SnakeBite\Properties\AssemblyInfo.cs',
    '.\makebite\Properties\AssemblyInfo.cs'
)

# Check files exist
foreach ($f in $files) {
    if (!(Test-Path $f)) {
        Write-Host "Error: File not found: $f" -ForegroundColor Red
        exit 1
    }
}

# Read current version
$content = Get-Content $files[0] -Raw
$match = [regex]::Match($content, 'AssemblyVersion\("([^"]+)"\)')

if (!$match.Success) {
    Write-Host "Error: Could not find AssemblyVersion in file." -ForegroundColor Red
    exit 1
}

$currentVer = $match.Groups[1].Value
Write-Host "Current Version: $currentVer" -ForegroundColor Cyan
Write-Host ""

$newVer = Read-Host "Enter new version (e.g. 0.2.0.0)"

if ([string]::IsNullOrWhiteSpace($newVer)) {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

# Update both files
foreach ($f in $files) {
    (Get-Content $f -Raw) -replace 'AssemblyVersion\("[^"]*"\)', "AssemblyVersion(`"$newVer`")" `
                         -replace 'AssemblyFileVersion\("[^"]*"\)', "AssemblyFileVersion(`"$newVer`")" |
        Set-Content $f -Encoding UTF8
}

Write-Host ""
Write-Host "Successfully updated version to $newVer in both projects." -ForegroundColor Green
Write-Host "Don't forget to rebuild the solution." -ForegroundColor Cyan