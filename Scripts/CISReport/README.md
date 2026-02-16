# CIS Benchmark Report for DBA Dash (via dbachecks)

Runs [dbachecks](https://dbachecks.readthedocs.io/) CIS compliance checks **remotely** 
from the DBA Dash server against all monitored instances.
 Nothing is deployed to target instances. 
 Checks are **configurable** via dbachecks config -- no static SQL.

 LP: 2026-02 DBADash Team

## Architecture

```
DBA Dash Server                          DBA Dash Repository DB
(where DBADash Service runs)             (UserData schema - persistent)
=================================        ==============================

02_run_dbachecks.ps1                     UserData.dbachecks
  │                                        (365-day retention)
  │  Per-server processing:                    │
  │  FOR EACH instance:                        │
  │    Invoke-DbcCheck -Check CIS              │
  │    Convert-DbcResult ──────────────>       │
  │    Write-DbaDataTable (immediate)          │
  │    Log success/failure                     │
  │  NEXT instance                             │
  │                                            ▼
  │                                      UserReport.CISReport
  │                                      UserReport.CISReportHistory
  scheduled via                          UserReport.CISReportSummary
  SQL Agent / Task Scheduler             UserReport.CISReportFailed
                                               │
                                               ▼
                                         DBA Dash GUI > Custom Reports
```

**How it works:**

1. PowerShell script reads active instances from the DBA Dash repository
2. Loops through each instance one at a time
3. Runs `Invoke-DbcCheck` (dbachecks) remotely against the current instance
4. Results converted via `Convert-DbcResult` (native dbachecks pipeline)
5. Stored in `UserData.dbachecks` using `Write-DbaDataTable` **immediately** -- before moving to the next server
6. If a server fails, the error is logged and processing continues with the next server
7. Session summary printed at the end with per-server status table

**Why dbachecks instead of static SQL?**
- Maintained by the community -- checks get updated
- Configurable via `Set-DbcConfig` or JSON config files
- Covers CIS OS, Instance, and Database level checks
- No need to write or maintain T-SQL check logic

## Prerequisites

On the **DBA Dash server**, install PowerShell modules:

```powershell
Install-Module dbachecks -Scope CurrentUser
Install-Module dbatools  -Scope CurrentUser
```

The service account running the script needs:
- Read access to the DBA Dash repository
- `sysadmin` (or equivalent read permissions) on all monitored instances
- INSERT permission on `UserData.dbachecks` (granted by the setup script)

## Deployment

### Step 1: Set Up the Repository (one-time)

```bash
sqlcmd -S "YourDBADashServer" -d "DBADash" -i 01_deploy_repository.sql
```

This creates:
- `UserData.dbachecks` -- persistent results table (365-day retention)
- `UserReport.CISReport` -- current compliance view
- `UserReport.CISReportHistory` -- historical trends
- `UserReport.CISReportSummary` -- pass/fail dashboard
- `UserReport.CISReportFailed` -- failed checks only

### Step 2: Run CIS Checks

```powershell
# Using the -DbaDash parameter (primary)
.\02_run_dbachecks.ps1 -DbaDash "YourDBADashServer"

# With verbose output for troubleshooting
.\02_run_dbachecks.ps1 -DbaDash "YourDBADashServer" -Verbose

# Backward-compatible: -RepositoryServer still works
.\02_run_dbachecks.ps1 -RepositoryServer "YourDBADashServer"
```

### Step 3: Schedule

**SQL Agent Job (recommended):**

1. Create a new SQL Agent job on the DBA Dash server
2. Add a CmdExec step:
   ```
   powershell.exe -ExecutionPolicy Bypass -File "C:\DBADash\Scripts\CISReport\02_run_dbachecks.ps1" -DbaDash "localhost"
   ```
3. Schedule: Weekly (CIS settings don't change often)

## Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-DbaDash` | Yes | -- | DBA Dash repository SQL instance. Alias: `-RepositoryServer` |
| `-RepositoryDatabase` | No | `DBADash` | Repository database name |
| `-Check` | No | `CIS` | dbachecks check tags to run |
| `-ConfigPath` | No | -- | Path to dbachecks config JSON |
| `-Label` | No | `CIS-yyyyMMdd` | Label for this run |
| `-ExcludeCheck` | No | -- | Checks to exclude |
| `-Verbose` | No | -- | Show detailed error diagnostics per failed server |

## Error Handling

The script processes each server individually. If a server fails, it does **not** stop the run:

- A short warning is printed to the console for the failed server
- Detailed diagnostics (exception type, stack trace, inner exception) are written to the **verbose** stream -- use `-Verbose` to see them
- Results from all successful servers are already saved (each is committed immediately after its checks complete)
- A session summary table is printed at the end showing the status of every server

**Example output with a failed server:**

```
[1/3] Processing: Server1\SQL2019
  Passed: 42  Failed: 5  Rows saved: 47  (01:23)

[2/3] Processing: Server2\SQL2022
WARNING:   FAILED: Server2\SQL2022 - A network-related error occurred

[3/3] Processing: Server3\SQL2017
  Passed: 38  Failed: 12  Rows saved: 50  (01:45)

========================================================
  Session Complete
========================================================
  Total instances : 3
  Succeeded       : 2
  Failed          : 1
  Total rows saved: 97
  Duration        : 04:15
  Label           : CIS-20260216

Instance         Status  Passed Failed Rows Duration
--------         ------  ------ ------ ---- --------
Server1\SQL2019  Success     42      5   47 01:23
Server2\SQL2022  Failed       0      0    0 00:07
Server3\SQL2017  Success     38     12   50 01:45

Failed instances (re-run with -Verbose for details):
  - Server2\SQL2022: A network-related error occurred
```

## Configuring Checks

dbachecks checks are fully configurable. This is the main advantage over static SQL.

### View available CIS checks

```powershell
Get-DbcCheck -Tag CIS
```

### Configure thresholds

```powershell
# Example: customize policies
Set-DbcConfig -Name policy.security.xpcmdshelldisabled -Value $true
Set-DbcConfig -Name policy.security.clrenabled -Value $false
Set-DbcConfig -Name policy.errorlog.logcount -Value 12

# Skip specific checks
Set-DbcConfig -Name skip.security.sadisabled -Value $true
Set-DbcConfig -Name skip.security.nonstandardport -Value $true
```

### Save/Load config

```powershell
# Save current config to a file
Export-DbcConfig -Path "C:\DBADash\cis-config.json"

# Use saved config when running checks
.\02_run_dbachecks.ps1 -DbaDash "DBADashServer" -ConfigPath "C:\DBADash\cis-config.json"
```

### Run specific check groups

```powershell
# Only instance-level CIS checks
.\02_run_dbachecks.ps1 -DbaDash "DBADashServer" -Check "CISInstanceLevel1"

# Exclude specific checks
.\02_run_dbachecks.ps1 -DbaDash "DBADashServer" -ExcludeCheck "NonStandardPort","DatabaseMailEnabled"
```

## Files

| File | Where to Run | Purpose |
|------|-------------|---------|
| `01_deploy_repository.sql` | DBA Dash repository DB (one-time) | Creates UserData table + UserReport procs |
| `02_run_dbachecks.ps1` | DBA Dash server (scheduled) | Runs dbachecks per-server, stores results in repository |

## DBA Dash GUI

After running checks, four reports appear under **Custom Reports**:

| Report | Description |
|--------|-------------|
| **CISReport** | Latest compliance status per instance. Failures sorted first. |
| **CISReportSummary** | Dashboard: pass/fail/skip counts and pass % per instance. Worst compliance first. |
| **CISReportFailed** | Only failed checks with failure details. |
| **CISReportHistory** | Trends over time. Use date picker to select range. |

All reports support the DBA Dash instance tree filter (`@InstanceIDs`) and the date picker (`@FromDate`/`@ToDate`).

## Data Retention

Results are stored in `UserData.dbachecks` with 365-day default retention.

Change retention via DBA Dash GUI (**Options > Data Retention**) or SQL:

```sql
EXEC dbo.DataRetention_Upd
    @SchemaName = 'UserData',
    @TableName = 'dbachecks',
    @RetentionDays = 730;  -- 2 years
```

Manual cleanup:

```sql
EXEC UserData.dbachecks_Cleanup @RetentionDays = 90;
```

## Troubleshooting

**"dbachecks module not installed":**
```powershell
Install-Module dbachecks -Scope CurrentUser -Force
Install-Module dbatools -Scope CurrentUser -Force
```

**No data in Custom Reports:**
- Run `02_run_dbachecks.ps1` manually and check console output
- Verify data: `SELECT TOP 10 * FROM UserData.dbachecks ORDER BY [Date] DESC`
- Check instance name matching: `SELECT DISTINCT Instance FROM UserData.dbachecks`

**Instance names don't match DBA Dash:**
The reports join `UserData.dbachecks.Instance` with `dbo.Instances.ConnectionID`. If dbachecks uses a different name format (e.g., FQDN vs short name), you may need to adjust the join in the UserReport procs.

**Connection failures on specific servers:**
- Re-run with `-Verbose` to see full exception details per failed server
- Ensure the service account has SQL access to all monitored instances
- Firewall: SQL port (1433) must be open from DBA Dash server to all instances
- WinRM: OS-level checks (`CISOSLevel1`) require WinRM access
- Results from other servers are not affected -- only the failing server is skipped
