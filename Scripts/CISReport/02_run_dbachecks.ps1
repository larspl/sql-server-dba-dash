<#
.SYNOPSIS
    Runs dbachecks CIS compliance checks per-server against DBA Dash instances
    and stores results in the DBA Dash repository after each server completes.
    LP: 2026-02 DBADash Team

.DESCRIPTION
    This script runs on the DBA Dash server. For each instance it:
    1. Runs dbachecks (Invoke-DbcCheck) against a single instance
    2. Converts results via Convert-DbcResult (native dbachecks pipeline)
    3. Writes results to UserData.dbachecks immediately
    4. Logs success or failure before moving to the next server

    Error handling is per-server: if one instance fails, the error is logged
    and processing continues with the remaining instances.

    A log file is created in the same folder as this script.

    Schedule via SQL Agent (CmdExec step) or Windows Task Scheduler.

.PARAMETER DbaDash
    SQL Server instance hosting the DBA Dash repository. Alias: RepositoryServer

.PARAMETER RepositoryDatabase
    DBA Dash repository database name. Default: DBADashCentral

.PARAMETER Check
    dbachecks check tags to run. Default: CIS
    Other options: CISOSLevel1, CISInstanceLevel1, CISDatabaseLevel1, or any valid tag.

.PARAMETER ConfigPath
    Path to dbachecks config JSON. If provided, imported before running checks.
    Create one with: Export-DbcConfig -Path "C:\DBADash\cis-config.json"

.PARAMETER Label
    Label for this run. Default: CIS-yyyyMMdd

.PARAMETER ExcludeCheck
    Checks to exclude. Pass as comma-separated list.

.EXAMPLE
    .\02_run_dbachecks.ps1 -DbaDash "DBADashServer"

.EXAMPLE
    .\02_run_dbachecks.ps1 -DbaDash "DBADashServer" -Verbose

.EXAMPLE
    .\02_run_dbachecks.ps1 -DbaDash "DBADashServer" -ConfigPath "C:\DBADash\cis-config.json"

.EXAMPLE
    # Backward-compatible: -RepositoryServer still works
    .\02_run_dbachecks.ps1 -RepositoryServer "DBADashServer"

.EXAMPLE
    # SQL Agent CmdExec step:
    powershell.exe -ExecutionPolicy Bypass -File "C:\DBADash\Scripts\02_run_dbachecks.ps1" -DbaDash "localhost"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [Alias('RepositoryServer')]
    [string]$DbaDash,

    [string]$RepositoryDatabase = "DBADashCentral",

    [string[]]$Check = @("CIS"),

    [string]$ConfigPath,

    [string]$Label = "CIS-$(Get-Date -Format 'yyyyMMdd')",

    [string[]]$ExcludeCheck
)

$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────────────────────────
# Log file - saved in the same folder as this script
# ─────────────────────────────────────────────────────────────────

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
$logFile = Join-Path $scriptDir "CISReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -Path $logFile -Value "$ts [$Level] $Message"
}

Write-Log "Session started - DbaDash=$DbaDash Database=$RepositoryDatabase Label=$Label"
Write-Log "Log file: $logFile"
Write-Host "Log file: $logFile" -ForegroundColor Gray

try {

# ─────────────────────────────────────────────────────────────────
# 0. Verify modules
# ─────────────────────────────────────────────────────────────────

foreach ($mod in @('dbachecks', 'dbatools')) {
    if (-not (Get-Module -ListAvailable -Name $mod)) {
        Write-Error "$mod module not installed. Run: Install-Module $mod -Scope CurrentUser"
        exit 1
    }
}

Import-Module dbachecks -ErrorAction Stop
Import-Module dbatools  -ErrorAction Stop

# ─────────────────────────────────────────────────────────────────
# 1. Load dbachecks config (if provided)
# ─────────────────────────────────────────────────────────────────

if ($ConfigPath) {
    if (-not (Test-Path $ConfigPath)) {
        Write-Error "Config file not found: $ConfigPath"
        exit 1
    }
    Write-Host "Loading dbachecks config: $ConfigPath" -ForegroundColor Cyan
    Import-DbcConfig -Path $ConfigPath
}

# ─────────────────────────────────────────────────────────────────
# 2. Get instances from DBA Dash repository
# ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Reading instances from $DbaDash\$RepositoryDatabase ..." -ForegroundColor Cyan

$instanceQuery = @"
SELECT I.ConnectionID
FROM dbo.Instances I
WHERE I.IsActive = 1
ORDER BY I.ConnectionID
"@

$instances = @(
    (Invoke-DbaQuery -SqlInstance $DbaDash -Database $RepositoryDatabase `
        -Query $instanceQuery -EnableException).ConnectionID
)

if ($instances.Count -eq 0) {
    Write-Log "No active instances found in repository." -Level WARN
    Write-Warning "No active instances found in repository."
    exit 0
}

Write-Log "Found $($instances.Count) instances: $($instances -join ', ')"
Write-Host "Found $($instances.Count) instances:" -ForegroundColor Green
$instances | ForEach-Object { Write-Host "  - $_" }

# ─────────────────────────────────────────────────────────────────
# 3. Run dbachecks per server
# ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Running dbachecks per server [Tags: $($Check -join ', ')] ..." -ForegroundColor Cyan

$sessionLog = [System.Collections.Generic.List[PSCustomObject]]::new()
$totalRows = 0
$serverNum = 0
$sessionStart = Get-Date

foreach ($instance in $instances) {
    $serverNum++
    $serverStart = Get-Date

    Write-Host ""
    Write-Host "[$serverNum/$($instances.Count)] Processing: $instance" -ForegroundColor White
    Write-Log "[$serverNum/$($instances.Count)] Starting: $instance"

    try {
        # Run dbachecks for this single instance
        $dbcParams = @{
            SqlInstance = $instance
            Check       = $Check
            PassThru    = $true
            Show        = 'Fails'
        }
        if ($ExcludeCheck) {
            $dbcParams.ExcludeCheck = $ExcludeCheck
        }

        $results = Invoke-DbcCheck @dbcParams

        if (-not $results) {
            throw "Invoke-DbcCheck returned null for $instance"
        }

        Write-Log "  Invoke-DbcCheck done: Total=$($results.TotalCount) Passed=$($results.PassedCount) Failed=$($results.FailedCount)"

        # Convert and count results
        $converted = $results | Convert-DbcResult -Label $Label
        $rowCount = ($converted | Measure-Object).Count

        Write-Host "  Checks: Total=$($results.TotalCount) Passed=$($results.PassedCount) Failed=$($results.FailedCount)  ConvertedRows=$rowCount" -ForegroundColor Gray
        Write-Log "  Converted: $rowCount rows"

        if ($rowCount -eq 0) {
            Write-Log "  WARNING: Convert-DbcResult produced 0 rows (TotalCount was $($results.TotalCount))" -Level WARN
            Write-Warning "  Convert-DbcResult produced 0 rows for $instance"
        }

        # Write to table immediately before moving to next server
        if ($rowCount -gt 0) {
            Write-Log "  Writing $rowCount rows to UserData.dbachecks ..."

            $converted |
                Select-Object Date, Label, Describe, Context, Name, Database, ComputerName, Instance, Result, FailureMessage |
                Write-DbaDataTable -SqlInstance $DbaDash `
                    -Database $RepositoryDatabase `
                    -Table "dbachecks" `
                    -Schema "UserData" `
                    -AutoCreateTable:$false `
                    -EnableException

            Write-Host "  Saved $rowCount rows" -ForegroundColor Green
            Write-Log "  Saved $rowCount rows OK"
        }

        $serverDuration = (Get-Date) - $serverStart
        $totalRows += $rowCount

        Write-Log "$instance - OK  Passed=$($results.PassedCount) Failed=$($results.FailedCount) Rows=$rowCount Duration=$($serverDuration.ToString('mm\:ss'))"

        $sessionLog.Add([PSCustomObject]@{
            Instance = $instance
            Status   = 'Success'
            Rows     = $rowCount
            Passed   = $results.PassedCount
            Failed   = $results.FailedCount
            Duration = $serverDuration.ToString('mm\:ss')
            Error    = $null
        })

        $color = if ($results.FailedCount -gt 0) { "Yellow" } else { "Green" }
        Write-Host "  Passed: $($results.PassedCount)  Failed: $($results.FailedCount)  Rows saved: $rowCount  ($($serverDuration.ToString('mm\:ss')))" -ForegroundColor $color
    }
    catch {
        $serverDuration = (Get-Date) - $serverStart
        $errMsg = $_.Exception.Message

        Write-Warning "  FAILED: $instance - $errMsg"
        Write-Log "$instance - FAILED: $errMsg" -Level ERROR
        Write-Log "  Exception Type : $($_.Exception.GetType().FullName)" -Level ERROR
        Write-Log "  Line           : $($_.InvocationInfo.ScriptLineNumber)" -Level ERROR
        Write-Log "  Command        : $($_.InvocationInfo.Line.Trim())" -Level ERROR
        if ($_.Exception.InnerException) {
            Write-Log "  Inner Exception: $($_.Exception.InnerException.Message)" -Level ERROR
        }
        Write-Log "  Stack Trace    : $($_.ScriptStackTrace)" -Level ERROR

        $sessionLog.Add([PSCustomObject]@{
            Instance = $instance
            Status   = 'Failed'
            Rows     = 0
            Passed   = 0
            Failed   = 0
            Duration = $serverDuration.ToString('mm\:ss')
            Error    = $errMsg
        })
    }
}

# ─────────────────────────────────────────────────────────────────
# 4. Session summary
# ─────────────────────────────────────────────────────────────────

$sessionDuration = (Get-Date) - $sessionStart
$successCount = @($sessionLog | Where-Object Status -eq 'Success').Count
$failCount = @($sessionLog | Where-Object Status -eq 'Failed').Count

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Session Complete" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Total instances : $($instances.Count)"
Write-Host "  Succeeded       : $successCount" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "  Failed          : $failCount" -ForegroundColor Red
}
Write-Host "  Total rows saved: $totalRows"
Write-Host "  Duration        : $($sessionDuration.ToString('mm\:ss'))"
Write-Host "  Label           : $Label"
Write-Host ""

$sessionLog | Format-Table Instance, Status, Passed, Failed, Rows, Duration -AutoSize

if ($failCount -gt 0) {
    Write-Host "Failed instances:" -ForegroundColor Yellow
    $sessionLog | Where-Object Status -eq 'Failed' | ForEach-Object {
        Write-Host "  - $($_.Instance): $($_.Error)" -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Log "Session complete - Succeeded=$successCount Failed=$failCount TotalRows=$totalRows Duration=$($sessionDuration.ToString('mm\:ss'))"

} catch {
    # Top-level catch: log any error that crashes the script outside per-server handling
    Write-Log "SCRIPT ERROR: $($_.Exception.Message)" -Level ERROR
    Write-Log "  Line: $($_.InvocationInfo.ScriptLineNumber) Command: $($_.InvocationInfo.Line.Trim())" -Level ERROR
    Write-Log "  Stack: $($_.ScriptStackTrace)" -Level ERROR
    throw
}

Write-Host "Log saved to: $logFile" -ForegroundColor Gray
Write-Host "View in DBA Dash GUI: Custom Reports > CISReport / CISReportSummary / CISReportFailed" -ForegroundColor Cyan
