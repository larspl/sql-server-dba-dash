/*
    DBA Dash Repository - dbachecks CIS Report Setup
    =================================================
    Deploy this ONCE to your DBA Dash REPOSITORY database.

    LP: 2026-02 DBADash Team

    Creates:
      - UserData.dbachecks       : Persistent storage for dbachecks results
      - UserReport.CISReport*    : Custom Report views in the DBA Dash GUI

    Nothing is deployed to monitored instances.
    dbachecks runs remotely from the DBA Dash server.

    Usage:
      sqlcmd -S "YourDBADashServer" -d "DBADash" -i 01_deploy_repository.sql
*/

USE [DBADash]; -- << Change to your DBA Dash repository database name
GO

/* ================================================================
   1. Persistent data table in UserData schema
   Schema matches Convert-DbcResult output from dbachecks.
   Data here is NOT touched by DBA Dash collection or overwrite.
   ================================================================ */

IF OBJECT_ID('UserData.dbachecks') IS NOT NULL
    DROP TABLE UserData.dbachecks;
GO

CREATE TABLE UserData.dbachecks (
    ID              INT             IDENTITY(1,1) NOT NULL,
    [Date]          DATETIME2(3)    NOT NULL,
    Label           NVARCHAR(256)   NULL,
    [Describe]      NVARCHAR(512)   NOT NULL,
    Context         NVARCHAR(512)   NULL,
    Name            NVARCHAR(512)   NOT NULL,
    [Database]      NVARCHAR(256)   NULL,
    ComputerName    NVARCHAR(256)   NULL,
    Instance        NVARCHAR(256)   NULL,
    Result          NVARCHAR(50)    NOT NULL,
    FailureMessage  NVARCHAR(MAX)   NULL,
    CONSTRAINT PK_UserData_dbachecks PRIMARY KEY CLUSTERED (ID)
);
GO

-- Index for fast lookups by instance and date
CREATE NONCLUSTERED INDEX IX_dbachecks_Instance_Date
    ON UserData.dbachecks (Instance, [Date] DESC)
    INCLUDE (Result, [Describe], Name);
GO

-- Index for retention cleanup (date-based)
CREATE NONCLUSTERED INDEX IX_dbachecks_Date
    ON UserData.dbachecks ([Date]);
GO

-- Register retention: PurgeData will manage cleanup
EXEC dbo.DataRetention_Upd
    @SchemaName    = 'UserData',
    @TableName     = 'dbachecks',
    @RetentionDays = 365,
    @Validate      = 0;
GO

/* ================================================================
   2. Custom Report: Current CIS Status (latest run per instance)
   ================================================================ */

IF OBJECT_ID('UserReport.CISReport') IS NOT NULL
    DROP PROCEDURE UserReport.CISReport;
GO

CREATE PROCEDURE UserReport.CISReport(
    @InstanceIDs IDs READONLY
)
AS
SET NOCOUNT ON;

-- Find latest run per instance
;WITH LatestRun AS (
    SELECT Instance, MAX([Date]) AS LastRun
    FROM UserData.dbachecks
    WHERE Label LIKE '%CIS%' OR [Describe] LIKE '%CIS%'
       OR Label IS NULL  -- include untagged runs
    GROUP BY Instance
)
SELECT
    ISNULL(I.InstanceDisplayName, d.Instance)   AS [Instance],
    d.[Describe]                                 AS [Check Group],
    d.Name                                       AS [Check],
    d.[Database]                                 AS [Database],
    d.Result,
    d.FailureMessage                             AS [Failure Details],
    d.[Date]                                     AS [Checked At]
FROM UserData.dbachecks d
JOIN LatestRun lr ON d.Instance = lr.Instance AND d.[Date] = lr.LastRun
LEFT JOIN dbo.Instances I ON I.ConnectionID = d.Instance AND I.IsActive = 1
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
ORDER BY
    CASE d.Result WHEN 'Failed' THEN 1 WHEN 'Skipped' THEN 2 ELSE 3 END,
    ISNULL(I.InstanceDisplayName, d.Instance),
    d.[Describe],
    d.Name;
GO

/* ================================================================
   3. Custom Report: CIS History (trend over time)
   ================================================================ */

IF OBJECT_ID('UserReport.CISReportHistory') IS NOT NULL
    DROP PROCEDURE UserReport.CISReportHistory;
GO

CREATE PROCEDURE UserReport.CISReportHistory(
    @InstanceIDs IDs READONLY,
    @FromDate    DATETIME2(3) = NULL,
    @ToDate      DATETIME2(3) = NULL
)
AS
SET NOCOUNT ON;

IF @ToDate IS NULL SET @ToDate = SYSUTCDATETIME();
IF @FromDate IS NULL SET @FromDate = DATEADD(d, -90, SYSUTCDATETIME());

SELECT
    ISNULL(I.InstanceDisplayName, d.Instance)   AS [Instance],
    d.[Describe]                                 AS [Check Group],
    d.Name                                       AS [Check],
    d.[Database]                                 AS [Database],
    d.Result,
    d.FailureMessage                             AS [Failure Details],
    d.Label,
    d.[Date]                                     AS [Checked At]
FROM UserData.dbachecks d
LEFT JOIN dbo.Instances I ON I.ConnectionID = d.Instance AND I.IsActive = 1
WHERE d.[Date] >= @FromDate
AND d.[Date] < @ToDate
AND EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
ORDER BY d.[Date] DESC, ISNULL(I.InstanceDisplayName, d.Instance), d.[Describe];
GO

/* ================================================================
   4. Custom Report: CIS Summary Dashboard (pass/fail per instance)
   ================================================================ */

IF OBJECT_ID('UserReport.CISReportSummary') IS NOT NULL
    DROP PROCEDURE UserReport.CISReportSummary;
GO

CREATE PROCEDURE UserReport.CISReportSummary(
    @InstanceIDs IDs READONLY
)
AS
SET NOCOUNT ON;

;WITH LatestRun AS (
    SELECT Instance, MAX([Date]) AS LastRun
    FROM UserData.dbachecks
    GROUP BY Instance
)
SELECT
    ISNULL(I.InstanceDisplayName, d.Instance)                       AS [Instance],
    COUNT(*)                                                        AS [Total Checks],
    SUM(CASE WHEN d.Result = 'Passed'  THEN 1 ELSE 0 END)         AS [Passed],
    SUM(CASE WHEN d.Result = 'Failed'  THEN 1 ELSE 0 END)         AS [Failed],
    SUM(CASE WHEN d.Result = 'Skipped' THEN 1 ELSE 0 END)         AS [Skipped],
    CAST(ROUND(
        100.0 * SUM(CASE WHEN d.Result = 'Passed' THEN 1 ELSE 0 END)
        / NULLIF(SUM(CASE WHEN d.Result IN('Passed','Failed') THEN 1 ELSE 0 END), 0)
    , 1) AS DECIMAL(5,1))                                          AS [Pass %],
    lr.LastRun                                                      AS [Last Checked]
FROM UserData.dbachecks d
JOIN LatestRun lr ON d.Instance = lr.Instance AND d.[Date] = lr.LastRun
LEFT JOIN dbo.Instances I ON I.ConnectionID = d.Instance AND I.IsActive = 1
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
GROUP BY ISNULL(I.InstanceDisplayName, d.Instance), lr.LastRun
ORDER BY [Pass %] ASC, [Instance];
GO

/* ================================================================
   5. Custom Report: Failed Checks Detail
   ================================================================ */

IF OBJECT_ID('UserReport.CISReportFailed') IS NOT NULL
    DROP PROCEDURE UserReport.CISReportFailed;
GO

CREATE PROCEDURE UserReport.CISReportFailed(
    @InstanceIDs IDs READONLY
)
AS
SET NOCOUNT ON;

;WITH LatestRun AS (
    SELECT Instance, MAX([Date]) AS LastRun
    FROM UserData.dbachecks
    GROUP BY Instance
)
SELECT
    ISNULL(I.InstanceDisplayName, d.Instance)   AS [Instance],
    d.[Describe]                                 AS [Check Group],
    d.Name                                       AS [Check],
    d.[Database]                                 AS [Database],
    d.FailureMessage                             AS [Details],
    d.[Date]                                     AS [Checked At]
FROM UserData.dbachecks d
JOIN LatestRun lr ON d.Instance = lr.Instance AND d.[Date] = lr.LastRun
LEFT JOIN dbo.Instances I ON I.ConnectionID = d.Instance AND I.IsActive = 1
WHERE d.Result = 'Failed'
AND EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
ORDER BY ISNULL(I.InstanceDisplayName, d.Instance), d.[Describe], d.Name;
GO

/* ================================================================
   6. Cleanup proc: remove old runs (called by retention or manually)
   ================================================================ */

IF OBJECT_ID('UserData.dbachecks_Cleanup') IS NOT NULL
    DROP PROCEDURE UserData.dbachecks_Cleanup;
GO

CREATE PROCEDURE UserData.dbachecks_Cleanup(
    @RetentionDays INT = NULL
)
AS
SET NOCOUNT ON;

IF @RetentionDays IS NULL
    SELECT @RetentionDays = RetentionDays
    FROM dbo.DataRetention
    WHERE SchemaName = 'UserData' AND TableName = 'dbachecks';

IF @RetentionDays IS NULL SET @RetentionDays = 365;

DELETE FROM UserData.dbachecks
WHERE [Date] < DATEADD(d, -@RetentionDays, SYSUTCDATETIME());

PRINT CONCAT('Cleaned up dbachecks data older than ', @RetentionDays, ' days. Rows deleted: ', @@ROWCOUNT);
GO

/* ================================================================
   7. Grant permissions
   ================================================================ */

GRANT SELECT, INSERT, DELETE ON UserData.dbachecks TO App;
GRANT EXECUTE ON UserData.dbachecks_Cleanup TO App;
GRANT SELECT ON UserData.dbachecks TO RunUserReports;
GRANT EXECUTE ON UserReport.CISReport        TO RunUserReports;
GRANT EXECUTE ON UserReport.CISReportHistory TO RunUserReports;
GRANT EXECUTE ON UserReport.CISReportSummary TO RunUserReports;
GRANT EXECUTE ON UserReport.CISReportFailed  TO RunUserReports;
GO

PRINT '';
PRINT '=== dbachecks CIS Report repository setup complete ===';
PRINT '';
PRINT 'Objects created:';
PRINT '  UserData.dbachecks              - Persistent results table (365-day retention)';
PRINT '  UserData.dbachecks_Cleanup      - Manual cleanup proc';
PRINT '  UserReport.CISReport            - Current compliance (Custom Report)';
PRINT '  UserReport.CISReportHistory     - Compliance history (Custom Report)';
PRINT '  UserReport.CISReportSummary     - Dashboard summary (Custom Report)';
PRINT '  UserReport.CISReportFailed      - Failed checks only (Custom Report)';
PRINT '';
PRINT 'Next: Run 02_run_dbachecks.ps1 from the DBA Dash server.';
GO
