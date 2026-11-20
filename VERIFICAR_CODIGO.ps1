param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'Verification'),
    [string]$UnityEditor = 'C:\Program Files\Unity 6000.0.37f1\Editor'
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$taskUnityData = Join-Path $UnityEditor 'Data'
$taskDotnet = Join-Path $taskUnityData 'NetCoreRuntime\dotnet.exe'
$taskCompiler = Join-Path $taskUnityData 'DotNetSdkRoslyn\csc.dll'
$taskNetReferences = @((Get-ChildItem -LiteralPath (Join-Path $taskUnityData 'NetStandard\ref\2.1.0') -Filter '*.dll').FullName)
$taskEngineReferences = @((Get-ChildItem -LiteralPath (Join-Path $taskUnityData 'Managed\UnityEngine') -Filter 'UnityEngine*.dll').FullName)
$taskEditorReferences = @((Get-ChildItem -LiteralPath (Join-Path $taskUnityData 'Managed\UnityEngine') -Filter 'UnityEditor*.dll').FullName)

function Invoke-PcFixCompile([string]$Name, [string[]]$Sources, [string[]]$References, [string]$Target = 'library', [string]$Symbols = '') {
    $taskOutputFile = Join-Path $OutputDirectory ($Name + '.dll')
    $taskResponseFile = Join-Path $OutputDirectory ($Name + '.rsp')
    $taskArguments = @('/nologo',('/target:'+$Target),'/langversion:9','/nostdlib+',('/out:"'+$taskOutputFile+'"'))
    if ($Symbols) { $taskArguments += '/define:'+$Symbols }
    $taskArguments += $References | ForEach-Object { '/reference:"'+$_+'"' }
    $taskArguments += $Sources | ForEach-Object { '"'+$_+'"' }
    $taskArguments | Set-Content -LiteralPath $taskResponseFile -Encoding UTF8
    & $taskDotnet $taskCompiler ('@'+$taskResponseFile)
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $Name" }
    Write-Output "PASS: $Name compiles against Unity 6000.0.37f1 assemblies"
}

$taskRuntimeSources = @((Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'PCRepairLab\Assets\Scripts') -Filter '*.cs').FullName)
$taskEditorSources = @((Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'PCRepairLab\Assets\Editor') -Filter '*.cs').FullName)
Invoke-PcFixCompile -Name 'GameRuntime' -Sources $taskRuntimeSources -References ($taskNetReferences + $taskEngineReferences)
Invoke-PcFixCompile -Name 'GameRuntimeEditor' -Sources $taskRuntimeSources -References ($taskNetReferences + $taskEngineReferences + $taskEditorReferences) -Symbols 'UNITY_EDITOR'
Invoke-PcFixCompile -Name 'GameEditor' -Sources $taskEditorSources -References ($taskNetReferences + $taskEngineReferences + $taskEditorReferences + (Join-Path $OutputDirectory 'GameRuntimeEditor.dll'))
Invoke-PcFixCompile -Name 'PartStateTests' -Sources @((Join-Path $PSScriptRoot 'PCRepairLab\Assets\Scripts\PartState.cs'),(Join-Path $PSScriptRoot 'PCRepairLab\Assets\Scripts\CampaignModel.cs'),(Join-Path $PSScriptRoot 'Tests\PartStateTests.cs')) -References $taskNetReferences -Target 'exe'
'{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.21"}}}' | Set-Content -LiteralPath (Join-Path $OutputDirectory 'PartStateTests.runtimeconfig.json') -Encoding UTF8
$taskTestOutput = & $taskDotnet (Join-Path $OutputDirectory 'PartStateTests.dll')
$taskTestExit = $LASTEXITCODE
$taskTestOutput | Set-Content -LiteralPath (Join-Path $OutputDirectory 'domain-tests.txt') -Encoding UTF8
$taskTestOutput | Write-Output
if ($taskTestExit -ne 0) { throw 'PartState domain tests failed' }
Invoke-PcFixCompile -Name 'CampaignTests' -Sources @((Join-Path $PSScriptRoot 'PCRepairLab\Assets\Scripts\PartState.cs'),(Join-Path $PSScriptRoot 'PCRepairLab\Assets\Scripts\CampaignModel.cs'),(Join-Path $PSScriptRoot 'Tests\CampaignTests.cs')) -References $taskNetReferences -Target 'exe'
Copy-Item -LiteralPath (Join-Path $OutputDirectory 'PartStateTests.runtimeconfig.json') -Destination (Join-Path $OutputDirectory 'CampaignTests.runtimeconfig.json') -Force
$taskCampaignOutput = & $taskDotnet (Join-Path $OutputDirectory 'CampaignTests.dll')
$taskCampaignExit = $LASTEXITCODE
$taskCampaignOutput | Set-Content -LiteralPath (Join-Path $OutputDirectory 'campaign-tests.txt') -Encoding UTF8
$taskCampaignOutput | Write-Output
if ($taskCampaignExit -ne 0) { throw 'Campaign integration tests failed' }
Write-Output 'Source compilation and domain tests passed. Unity scene, rendering and standalone build still require editor verification.'
