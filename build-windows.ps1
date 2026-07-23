$ErrorActionPreference = "Stop"

$projectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe"
$logPath = Join-Path $projectPath "unity-build.log"

if (-not (Test-Path -LiteralPath $unityPath)) {
    throw "Unity executable not found: $unityPath"
}

$arguments = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $projectPath,
    "-executeMethod", "BackgammonSceneBuilder.BuildWindowsPlayer",
    "-logFile", $logPath
)

$process = Start-Process -FilePath $unityPath -ArgumentList $arguments -NoNewWindow -Wait -PassThru
if ($process.ExitCode -ne 0) {
    throw "Unity build failed with exit code $($process.ExitCode). See: $logPath"
}

$exePath = Join-Path $projectPath "Builds\ErkekTavlasiUnity.exe"
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Build completed but exe was not found: $exePath"
}

Write-Host "Build succeeded: $exePath"
