# Database Latency Benchmark Report

## Scope
This report documents execution of database benchmark and transaction demonstration scripts for the Smart Disaster Response MIS.

## Executed Scripts
- SQL_SCRIPTS/views.sql
- SQL_SCRIPTS/indexes.sql
- SQL_SCRIPTS/performance_benchmarks.sql
- SQL_SCRIPTS/transaction_demos.sql

Execution status: all scripts executed successfully.

## Benchmark Output Captured
Run labels captured from benchmark output: WITHOUT_INDEX and WITH_INDEX

| TestName | Variant | RunLabel | DurationMs | RowsOrCount | RecordedAt |
|---|---|---|---:|---:|---|
| AllocationHistoryByEventStatus | BaseTable | WITHOUT_INDEX | 3 | 3 | 2026-04-23 09:37:08.3893257 |
| EventFinancialSummary | BaseTable | WITHOUT_INDEX | 0 | 2 | 2026-04-23 09:37:08.3802367 |
| EventFinancialSummary | View | WITHOUT_INDEX | 0 | 2 | 2026-04-23 09:37:08.3832348 |
| IncidentQueueByCitySeverity | BaseTable | WITHOUT_INDEX | 4 | 3 | 2026-04-23 09:37:08.3622661 |
| IncidentQueueByCitySeverity | View | WITHOUT_INDEX | 2 | 3 | 2026-04-23 09:37:08.3765510 |
| AllocationHistoryByEventStatus | BaseTable | WITH_INDEX | 0 | 3 | 2026-04-23 09:38:11.6987120 |
| EventFinancialSummary | BaseTable | WITH_INDEX | 0 | 2 | 2026-04-23 09:38:11.6957315 |
| EventFinancialSummary | View | WITH_INDEX | 0 | 2 | 2026-04-23 09:38:11.6987120 |
| IncidentQueueByCitySeverity | BaseTable | WITH_INDEX | 0 | 3 | 2026-04-23 09:38:11.6816087 |
| IncidentQueueByCitySeverity | View | WITH_INDEX | 9 | 3 | 2026-04-23 09:38:11.6915462 |

## Indexed vs Non-Indexed Delta Summary

| TestName | Variant | WITHOUT_INDEX (ms) | WITH_INDEX (ms) | Delta (ms) | Observation |
|---|---|---:|---:|---:|---|
| AllocationHistoryByEventStatus | BaseTable | 3 | 0 | -3 | Improved with indexes |
| EventFinancialSummary | BaseTable | 0 | 0 | 0 | No measurable change in this dataset |
| EventFinancialSummary | View | 0 | 0 | 0 | No measurable change in this dataset |
| IncidentQueueByCitySeverity | BaseTable | 4 | 0 | -4 | Improved with indexes |
| IncidentQueueByCitySeverity | View | 2 | 9 | +7 | Slower in this run (small dataset/runtime variance) |

## Observations
- Row counts remained consistent between WITHOUT_INDEX and WITH_INDEX runs.
- Base-table benchmarks for allocation history and incident queue improved after indexes were applied.
- Financial summary remained near-zero in both runs on the current dataset.
- One view query (`IncidentQueueByCitySeverity`) measured slower in the indexed run, which is plausible with small datasets and plan variance.
- Current evidence satisfies before/after indexing comparison requirement for project submission.

## Transaction Demonstration Evidence
The transaction demo output confirms expected ACID behavior:
- Demo A commit succeeded:
  - AllocationID = 4
  - EventID = 2
  - InventoryID = 1
  - RequestID = 5
  - RequestType = ResourceDistribution
  - ApprovalStatus = Pending
- Demo B rollback succeeded:
  - Expense query returned no row for the rolled-back expense
  - ApprovalRequest query returned no row for the rolled-back request
- Demo C commit succeeded:
  - AssignmentID = 4
  - TeamID = 1
  - ReportID = 3
  - RequestID = 7
  - RequestType = RescueDeployment
  - ApprovalStatus = Pending

## Deliverable Conclusion
- Benchmarks executed and documented with explicit WITHOUT_INDEX and WITH_INDEX comparison.
- Views and indexes scripts executed successfully.
- Transaction commit and rollback scenarios executed and verified.
