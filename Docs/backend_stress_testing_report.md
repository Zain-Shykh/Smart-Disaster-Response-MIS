# Backend Stress Testing Report

## Objective
Document high-volume and concurrency validation strategy for the Smart Disaster Response MIS backend, with focus on transactional correctness, approval gating, and conflict handling.

## Scope
Primary stress targets:
- Emergency report ingestion (high-frequency inserts)
- Resource allocation lifecycle updates
- Team assignment lifecycle updates
- Expense and approval workflow updates
- Concurrency-sensitive update endpoints returning conflict responses

## Critical Endpoints
- POST /api/EmergencyReport
- POST /api/ResourceLogistics/allocations
- PATCH /api/ResourceLogistics/allocations/{allocationId}/status
- POST /api/RescueTeam/{teamId}/assignments
- PATCH /api/RescueTeam/{teamId}/assignments/{assignmentId}/status
- POST /api/DonationFinance/expenses
- PATCH /api/DonationFinance/expenses/{expenseId}/payment-status
- PATCH /api/ApprovalWorkflow/requests/{requestId}/decision
- PUT /api/DisasterEvent/{id}

## Test Dimensions
1. Throughput and latency
- Requests per second
- p50, p95, p99 response time
- Endpoint-level error rate

2. Correctness under load
- No negative inventory values
- No over-capacity hospital bed allocation
- Approval-gated transitions blocked before approval
- Concurrency conflicts reported as HTTP 409 when stale version token is used

3. Stability
- Sustained load behavior
- Spike load behavior
- Recovery behavior after spike

## Recommended Tooling
- k6 or JMeter for HTTP load generation
- SQL Server query monitoring for DB pressure and lock behavior
- Application logs for error distribution and conflict telemetry

## Scenario Matrix

### Scenario A: Emergency Report Burst
- Endpoint: POST /api/EmergencyReport
- Concurrency: 50 virtual users
- Duration: 5 minutes
- Payload: mixed severity and locations
- Success criteria:
  - Error rate < 1%
  - p95 latency within acceptable SLA
  - Report counts remain consistent

### Scenario B: Allocation and Approval Contention
- Endpoints:
  - POST /api/ResourceLogistics/allocations
  - PATCH /api/ResourceLogistics/allocations/{id}/status
  - PATCH /api/ApprovalWorkflow/requests/{id}/decision
- Concurrency: 25 virtual users
- Duration: 10 minutes
- Success criteria:
  - No invalid status transitions bypass approval
  - Inventory constraints preserved
  - Conflict and validation responses are deterministic

### Scenario C: Assignment Workflow Contention
- Endpoints:
  - POST /api/RescueTeam/{teamId}/assignments
  - PATCH /api/RescueTeam/{teamId}/assignments/{assignmentId}/status
- Concurrency: 25 virtual users
- Duration: 10 minutes
- Success criteria:
  - No transition beyond Assigned before approval
  - Team assignment records remain consistent

### Scenario D: Financial Workflow Contention
- Endpoints:
  - POST /api/DonationFinance/expenses
  - PATCH /api/DonationFinance/expenses/{expenseId}/payment-status
  - PATCH /api/ApprovalWorkflow/requests/{requestId}/decision
- Concurrency: 20 virtual users
- Duration: 10 minutes
- Success criteria:
  - No paid/completed status without approved request
  - Expense and approval linkage remains consistent

### Scenario E: Version Token Conflict Handling
- Endpoints:
  - PUT /api/DisasterEvent/{id}
  - PATCH /api/ApprovalWorkflow/requests/{id}/decision
- Concurrency: paired stale-token updates
- Success criteria:
  - Stale request receives HTTP 409
  - Response includes current version token for retry

## Data Integrity Assertions
Post-run checks should verify:
- Inventory.Quantity >= 0 for all rows
- Hospital.AvailableBeds between 0 and TotalBeds
- ApprovalRequest linkage integrity for Allocation/Assignment/Expense targets
- No orphan records from multi-step transactional flows

## Current Verification Baseline
- Integration suite status: 54/54 passing
- Covered behaviors include:
  - Approval gating enforcement
  - Optimistic concurrency conflicts
  - Transaction-scope atomicity in multi-step flows
  - Rescue recommendation logic
  - Hospital load-balancing fallback and escalation

## Execution Notes
This repository currently records stress testing as a documented and repeatable procedure. Full production-scale load execution should be run against SQL Server-backed deployment with environment-appropriate datasets.

## Result Template
Use this table when running stress jobs:

| Scenario | Users | Duration | p95 Latency (ms) | Error Rate (%) | Key Integrity Checks |
|---|---:|---|---:|---:|---|
| A | 50 | 5m | TBD | TBD | TBD |
| B | 25 | 10m | TBD | TBD | TBD |
| C | 25 | 10m | TBD | TBD | TBD |
| D | 20 | 10m | TBD | TBD | TBD |
| E | paired | scripted | TBD | TBD | TBD |
