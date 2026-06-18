USE Final_DB;
GO

/* =====================================================
   PERFORMANCE BENCHMARK SCRIPT

   HOW TO USE:
   1) Run this once BEFORE executing indexes.sql and save output as WITHOUT_INDEX.
   2) Execute indexes.sql.
   3) Run this again and save output as WITH_INDEX.
   4) Compare BenchmarkResults output + STATISTICS IO/TIME between runs.

   This script also includes a write-overhead benchmark in a rollback transaction.
   ===================================================== */

SET NOCOUNT ON;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @RunLabel VARCHAR(30) = 'WITH_INDEX'; -- WITHOUT_INDEX or WITH_INDEX

IF OBJECT_ID('tempdb..#BenchmarkResults') IS NOT NULL DROP TABLE #BenchmarkResults;
CREATE TABLE #BenchmarkResults
(
    TestName VARCHAR(120) NOT NULL,
    Variant VARCHAR(40) NOT NULL,
    RunLabel VARCHAR(30) NOT NULL,
    DurationMs INT NOT NULL,
    RowsOrCount BIGINT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

DECLARE @t0 DATETIME2;
DECLARE @t1 DATETIME2;
DECLARE @count BIGINT;

/* -----------------------------
   READ BENCHMARK 1
   Incident queue by city/severity/status
   Base table query
   ----------------------------- */
SET @t0 = SYSDATETIME();
SELECT @count = COUNT_BIG(1)
FROM EmergencyReport er
LEFT JOIN DisasterEvent de ON de.EventID = er.EventID
WHERE er.City = 'City A'
  AND er.Status IN ('Pending', 'InProgress')
  AND er.SeverityLevel IN ('High', 'Critical');
SET @t1 = SYSDATETIME();

INSERT INTO #BenchmarkResults (TestName, Variant, RunLabel, DurationMs, RowsOrCount)
VALUES ('IncidentQueueByCitySeverity', 'BaseTable', @RunLabel, DATEDIFF(MILLISECOND, @t0, @t1), @count);

/* View query */
SET @t0 = SYSDATETIME();
SELECT @count = COUNT_BIG(1)
FROM dbo.vw_FieldOfficer_IncidentQueue v
WHERE v.City = 'City A'
  AND v.ReportStatus IN ('Pending', 'InProgress')
  AND v.SeverityLevel IN ('High', 'Critical');
SET @t1 = SYSDATETIME();

INSERT INTO #BenchmarkResults (TestName, Variant, RunLabel, DurationMs, RowsOrCount)
VALUES ('IncidentQueueByCitySeverity', 'View', @RunLabel, DATEDIFF(MILLISECOND, @t0, @t1), @count);

/* -----------------------------
   READ BENCHMARK 2
   Event financial summary
   Base table query
   ----------------------------- */
SET @t0 = SYSDATETIME();
SELECT @count = COUNT_BIG(1)
FROM
(
    SELECT
        de.EventID,
        SUM(CASE WHEN d.Status = 'Confirmed' THEN d.Amount ELSE 0 END) AS ConfirmedDonationTotal,
        SUM(CASE WHEN e.PaymentStatus IN ('Paid', 'Completed') THEN e.Amount ELSE 0 END) AS SettledExpenseTotal
    FROM DisasterEvent de
    LEFT JOIN Donation d ON d.EventID = de.EventID
    LEFT JOIN Expense e ON e.EventID = de.EventID
    GROUP BY de.EventID
) q;
SET @t1 = SYSDATETIME();

INSERT INTO #BenchmarkResults (TestName, Variant, RunLabel, DurationMs, RowsOrCount)
VALUES ('EventFinancialSummary', 'BaseTable', @RunLabel, DATEDIFF(MILLISECOND, @t0, @t1), @count);

/* View query */
SET @t0 = SYSDATETIME();
SELECT @count = COUNT_BIG(1)
FROM dbo.vw_FinanceOfficer_EventFinancialSummary;
SET @t1 = SYSDATETIME();

INSERT INTO #BenchmarkResults (TestName, Variant, RunLabel, DurationMs, RowsOrCount)
VALUES ('EventFinancialSummary', 'View', @RunLabel, DATEDIFF(MILLISECOND, @t0, @t1), @count);

/* -----------------------------
   READ BENCHMARK 3
   Allocation history by event and status
   ----------------------------- */
SET @t0 = SYSDATETIME();
SELECT @count = COUNT_BIG(1)
FROM ResourceAllocation ra
WHERE ra.EventID IN (SELECT TOP 5 EventID FROM DisasterEvent ORDER BY EventID DESC)
  AND ra.Status IN ('Pending', 'Approved', 'Dispatched');
SET @t1 = SYSDATETIME();

INSERT INTO #BenchmarkResults (TestName, Variant, RunLabel, DurationMs, RowsOrCount)
VALUES ('AllocationHistoryByEventStatus', 'BaseTable', @RunLabel, DATEDIFF(MILLISECOND, @t0, @t1), @count);

/* -----------------------------
   WRITE OVERHEAD BENCHMARK
   Uses rollback so database state is unchanged.
   ----------------------------- */
DECLARE @before BIGINT;
DECLARE @after BIGINT;

SELECT @before = COUNT_BIG(1) FROM AuditLog;

BEGIN TRANSACTION;
BEGIN TRY
    SET @t0 = SYSDATETIME();

    ;WITH n AS
    (
        SELECT TOP (1000)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS rn
        FROM sys.all_objects a
        CROSS JOIN sys.all_objects b
    )
    INSERT INTO AuditLog (UserID, [Action], TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
    SELECT
        NULL,
        'BENCH_INSERT',
        'Benchmark',
        CAST(rn AS VARCHAR(120)),
        NULL,
        'Payload',
        SYSUTCDATETIME(),
        '127.0.0.1'
    FROM n;

    SET @t1 = SYSDATETIME();

    SELECT @after = COUNT_BIG(1) FROM AuditLog;

    INSERT INTO #BenchmarkResults (TestName, Variant, RunLabel, DurationMs, RowsOrCount)
    VALUES ('WriteOverhead_AuditLogBatch1000', 'RollbackTxn', @RunLabel, DATEDIFF(MILLISECOND, @t0, @t1), (@after - @before));

    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT *
FROM #BenchmarkResults
ORDER BY TestName, Variant;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO
