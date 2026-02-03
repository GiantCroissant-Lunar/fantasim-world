[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('new', 'backfill', 'done')]
    [string]$Mode,

    [string]$Feature,
    [string]$CliArgs,

    [Parameter(Mandatory = $true)]
    [string]$RepoName,

    [Parameter(Mandatory = $true)]
    [string]$WorktreeBase
)

$ErrorActionPreference = 'Stop'

function Resolve-FeatureName {
    $name = $CliArgs
    if ([string]::IsNullOrWhiteSpace($name)) {
        $name = $Feature
    }
    return $name
}

function Get-HighestNumberFromSpecs {
    param([Parameter(Mandatory = $true)][string]$SpecsDir)

    $highest = 0
    if (Test-Path -LiteralPath $SpecsDir) {
        Get-ChildItem -Path $SpecsDir -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            if ($_.Name -match '^(\d+)') {
                $n = [int]$matches[1]
                if ($n -gt $highest) { $highest = $n }
            }
        }
    }

    return $highest
}

function Get-HighestNumberFromBranches {
    $highest = 0

    try {
        $branches = git branch -a 2>$null
        if ($LASTEXITCODE -eq 0) {
            foreach ($b in $branches) {
                $clean = $b.Trim() -replace '^\*?\s+', '' -replace '^remotes/[^/]+/', ''
                if ($clean -match '^(\d+)-') {
                    $n = [int]$matches[1]
                    if ($n -gt $highest) { $highest = $n }
                }
            }
        }
    } catch {
    }

    return $highest
}

function ConvertTo-CleanBranchName {
    param([Parameter(Mandatory = $true)][string]$Name)

    return $Name.ToLower() -replace '[^a-z0-9]', '-' -replace '-{2,}', '-' -replace '^-', '' -replace '-$', ''
}

function Initialize-OpenCodeGenerated {
    param(
        [Parameter(Mandatory = $true)][string]$WorktreePath
    )

    # Generate .opencode from canonical .agent sources.
    Push-Location $WorktreePath
    try {
        python 'scripts/sync_opencode.py' | Out-Host
    } finally {
        Pop-Location
    }
}

$root = (Get-Location).Path
$specsDir = Join-Path $root 'specs'
New-Item -ItemType Directory -Force -Path $specsDir | Out-Null

switch ($Mode) {
    'new' {
        $rawFeature = Resolve-FeatureName
        if ([string]::IsNullOrWhiteSpace($rawFeature)) {
            throw 'Feature name required. Usage: task spec:new FEATURE=<feature-name>'
        }

        $suffix = ConvertTo-CleanBranchName -Name $rawFeature

        try { git fetch --all --prune 2>$null | Out-Null } catch { }

        $highestSpec = Get-HighestNumberFromSpecs -SpecsDir $specsDir
        $highestBranch = Get-HighestNumberFromBranches
        $next = ([Math]::Max($highestSpec, $highestBranch)) + 1

        $branch = ('{0:000}-{1}' -f $next, $suffix)
        $worktreePath = Join-Path $WorktreeBase ($RepoName + '--' + $branch)

        git worktree add $worktreePath -b $branch

        $specDir = Join-Path $worktreePath ('specs/' + $branch)
        New-Item -ItemType Directory -Force -Path $specDir | Out-Null

        if (Test-Path -LiteralPath '.specify') {
            $dst = Join-Path $worktreePath '.specify'
            if (-not (Test-Path -LiteralPath $dst)) {
                Copy-Item -Recurse -Force -LiteralPath '.specify' -Destination $dst
            }
        }

        if (Test-Path -LiteralPath 'opencode.json') {
            $dst = Join-Path $worktreePath 'opencode.json'
            if (-not (Test-Path -LiteralPath $dst)) {
                Copy-Item -Force -LiteralPath 'opencode.json' -Destination $dst
            }
        }

        Initialize-OpenCodeGenerated -WorktreePath $worktreePath

        Write-Host ''
        Write-Host 'Worktree created successfully!'
        Write-Host ('Created branch: ' + $branch)
        Write-Host ('Worktree path: ' + $worktreePath)
        Write-Host ''
        Write-Host 'Feature branch:'
        Write-Host ('  ' + $branch)
        Write-Host ''
        Write-Host 'Next steps:'
        Write-Host ('  cd ' + $worktreePath)
        Write-Host ('  task spec:specify FEATURE=' + $branch)
        Write-Host ''
    }
    'backfill' {
        $name = Resolve-FeatureName
        if ([string]::IsNullOrWhiteSpace($name)) {
            throw 'Feature name required. Usage: task spec:backfill FEATURE=<feature-name>'
        }

        $worktreePath = Join-Path $WorktreeBase ($RepoName + '--' + $name)
        if (-not (Test-Path -LiteralPath $worktreePath)) {
            throw ('Worktree not found: {0}' -f $worktreePath)
        }

        if (Test-Path -LiteralPath '.specify') {
            $dst = Join-Path $worktreePath '.specify'
            if (-not (Test-Path -LiteralPath $dst)) {
                Copy-Item -Recurse -Force -LiteralPath '.specify' -Destination $dst
            }
        }

        if (Test-Path -LiteralPath 'opencode.json') {
            $dst = Join-Path $worktreePath 'opencode.json'
            if (-not (Test-Path -LiteralPath $dst)) {
                Copy-Item -Force -LiteralPath 'opencode.json' -Destination $dst
            }
        }

        Initialize-OpenCodeGenerated -WorktreePath $worktreePath

        Write-Host ('Backfill complete: ' + $worktreePath)
    }
    'done' {
        $name = Resolve-FeatureName
        if ([string]::IsNullOrWhiteSpace($name)) {
            throw 'Feature name required. Usage: task spec:done FEATURE=<feature-name>'
        }

        $worktreePath = Join-Path $WorktreeBase ($RepoName + '--' + $name)
        if (-not (Test-Path -LiteralPath $worktreePath)) {
            throw ('Worktree not found: {0}' -f $worktreePath)
        }

        git worktree remove $worktreePath

        Write-Host ''
        Write-Host ('Worktree removed: ' + $worktreePath)
        Write-Host ''
        Write-Host 'To also delete the remote branch:'
        Write-Host ('  git push origin --delete ' + $name)
        Write-Host ''
    }
}
