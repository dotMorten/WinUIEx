$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '..\review-issue.ps1'
$sourceDirectory = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$sourceCommit = & git -C $sourceDirectory rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Tests require a repository checkout.' }
$testDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "issue-review-tests-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $testDirectory | Out-Null
$variables = @('REVIEW_DIRECTORY', 'COPILOT_GITHUB_TOKEN', 'WINUIEX_SOURCE', 'GITHUB_OUTPUT', 'GITHUB_STEP_SUMMARY')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
if (Test-Path Function:\copilot) { throw 'Run these tests in a fresh PowerShell process.' }

function global:copilot {
    $global:reviewTestState.calls += ,@($args)
    $global:LASTEXITCODE = 0
    if ($args -contains '--deny-tool=shell') {
        ConvertTo-Json -InputObject $global:reviewTestState.classification |
            Set-Content (Join-Path $env:REVIEW_DIRECTORY 'classification.json')
        return
    }
    $output = Join-Path $env:REVIEW_DIRECTORY 'output'
    $repro = Join-Path $env:REVIEW_DIRECTORY 'reproducer'
    New-Item -ItemType Directory $repro | Out-Null
    @{
        status = $global:reviewTestState.status
        winuiex_version = '2.10.0'
        reproduction_evidence = 'Observed failure in the final minimized repro.'
        regression = @{ status = 'unknown'; baseline_version = $null; evidence = 'No compatible baseline.' }
        unpublished_fix = @{ status = 'unknown'; commit = $global:reviewTestState.commit; evidence = 'HEAD test blocked.' }
    } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'result.json')
    Set-Content (Join-Path $output 'investigation.md') '**Outcome:** Test investigation.'
    Set-Content (Join-Path $repro 'README.md') 'Build and run instructions.'
    Set-Content (Join-Path $repro 'Repro.csproj') '<Project><ItemGroup><PackageReference Include="WinUIEx" Version="2.10.0" /></ItemGroup></Project>'
    Set-Content (Join-Path $repro 'MainWindow.xaml') '<Window />'
    foreach ($directory in @('bin', 'obj', '.git', '.vs', 'node_modules')) {
        $excluded = Join-Path $repro $directory
        New-Item -ItemType Directory $excluded | Out-Null
        Set-Content (Join-Path $excluded 'must-not-ship.txt') 'Generated content.'
    }
    foreach ($file in @('bad.dll', 'bad.exe', 'bad.pdb', 'bad.pfx', '.env', 'Repro.csproj.user')) {
        Set-Content (Join-Path $repro $file) 'Must not ship.'
    }
}

function global:Invoke-RestMethod {
    $global:reviewTestState.nugetCalls++
    @{ versions = @('2.9.0', '2.10.0', '3.0.0-preview.1') }
}

function Assert([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}

function New-Fixture([string] $Name, [bool] $IsBug, [string] $Status = 'reproduced') {
    $env:REVIEW_DIRECTORY = Join-Path $testDirectory $Name
    New-Item -ItemType Directory $env:REVIEW_DIRECTORY | Out-Null
    $env:COPILOT_GITHUB_TOKEN = 'mock-token-not-a-credential'
    $env:WINUIEX_SOURCE = $sourceDirectory
    $env:GITHUB_OUTPUT = Join-Path $env:REVIEW_DIRECTORY 'github-output.txt'
    $env:GITHUB_STEP_SUMMARY = Join-Path $env:REVIEW_DIRECTORY 'github-summary.txt'
    Set-Content (Join-Path $env:REVIEW_DIRECTORY 'issue.json') '{"number":7,"title":"Test issue","body":"Test description"}'
    $global:reviewTestState = @{
        classification = @{ is_bug = $IsBug; reason = 'Test classification.' }
        status = $Status; commit = $sourceCommit; nugetCalls = 0; calls = @()
    }
}

try {
    New-Fixture 'non-bug' $false
    & $scriptPath -Stage Classify
    Assert ((Get-Content $env:GITHUB_OUTPUT -Raw).Trim() -eq 'is_bug=false') 'Non-bug gating failed.'
    Assert ($global:reviewTestState.calls[0] -contains '--deny-tool=shell') 'Classification must not execute shell commands.'
    Assert ($global:reviewTestState.calls[0] -notcontains '--allow-all-paths') 'Classification must retain scoped filesystem access.'
    Assert ($global:reviewTestState.nugetCalls -eq 0) 'Non-bug classification fetched dependencies.'
    Assert (-not (Test-Path (Join-Path $env:REVIEW_DIRECTORY 'reproducer'))) 'Non-bug classification created a repro.'
    $rejected = $false
    try { & $scriptPath -Stage Investigate } catch {
        $rejected = $_.Exception.Message -match 'positive bug classification'
    }
    Assert $rejected 'Investigation accepted a non-bug issue.'

    New-Fixture 'blocked' $true 'blocked'
    & $scriptPath -Stage Classify
    & $scriptPath -Stage Investigate
    Assert (-not (Test-Path (Join-Path $env:REVIEW_DIRECTORY 'output\reproducer.zip'))) 'Blocked investigations must not publish repro ZIPs.'
    $arguments = $global:reviewTestState.calls[1]
    Assert ($arguments -contains '--agent=winui:winui-dev') 'Investigation did not select the installed WinUI agent.'
    Assert ($arguments -contains "--add-dir=$sourceDirectory") 'Source checkout access was not granted.'
    Assert ($arguments -contains '--allow-all-tools') 'Investigation must permit build and run tools without interactive approval.'
    Assert ($arguments -contains '--allow-all-paths') 'Investigation must permit installed toolchain and dependency paths outside the workspace.'
    Assert ($arguments -contains '--deny-tool=shell(git push)') 'Investigation must retain the git push restriction.'
    Assert ($arguments -contains '--deny-tool=shell(gh)') 'Investigation must retain the GitHub CLI restriction.'
    Assert ($arguments -notcontains '--allow-all') 'Investigation must not indiscriminately grant all permission categories.'
    Assert ($arguments -contains '--secret-env-vars=COPILOT_GITHUB_TOKEN,GITHUB_TOKEN,GH_TOKEN') 'Agent subprocess secrets were not stripped.'
    $prompt = $arguments[[Array]::IndexOf($arguments, '--prompt') + 1]
    Assert ($prompt -notmatch '\{\{WINUIEX_') 'Prompt contains unresolved source/version placeholders.'
    Assert ($prompt -match 'WinUIEx\s+2\.10\.0') 'Latest stable version was not selected numerically.'

    New-Fixture 'reproduced' $true
    & $scriptPath -Stage Classify
    & $scriptPath -Stage Investigate
    $archive = [System.IO.Compression.ZipFile]::OpenRead((Join-Path $env:REVIEW_DIRECTORY 'output\reproducer.zip'))
    try {
        $entries = @($archive.Entries | ForEach-Object FullName | Sort-Object)
        $expected = @('reproducer/MainWindow.xaml', 'reproducer/README.md', 'reproducer/Repro.csproj') | Sort-Object
        Assert (($entries -join '|') -eq ($expected -join '|')) "ZIP contains unexpected files: $($entries -join ', ')"
    }
    finally { $archive.Dispose() }

    New-Fixture 'invalid-classification' $true
    $global:reviewTestState.classification.is_bug = 'true'
    $rejected = $false
    try { & $scriptPath -Stage Classify } catch {
        $rejected = $_.Exception.Message -match 'valid bug classification'
    }
    Assert $rejected 'String booleans must not pass classification validation.'
    Write-Host 'Passed: non-bug gating, blocked outcomes, WinUI agent configuration, version/prompt resolution, source-only packaging, classification validation.'
}
finally {
    Remove-Item Function:\copilot, Function:\Invoke-RestMethod
    Remove-Variable reviewTestState -Scope Global
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
    Remove-Item -LiteralPath $testDirectory -Recurse -Force
}
