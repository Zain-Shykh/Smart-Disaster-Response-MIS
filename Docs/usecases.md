# Smart Disaster Response MIS Use Cases

This document lists the main system use cases derived from the project statement and relational schema. The use cases are organized by system role and include the core workflows supported by the database design, approval process, audit logging, and role-based access control.

## 1. Administrator

The Administrator has the broadest access and is responsible for managing users, roles, permissions, and system oversight.

### Use Cases
- Create, update, deactivate, and view user accounts.
- Assign one or more roles to a user.
- Define and update roles.
- Define permissions and map permissions to roles.
- Review audit logs for user activity and data changes.
- Monitor approval history across all workflow types.
- View system-wide reports and dashboards.
- Manage reference data used across the system, such as resource types, disaster categories, and status settings.
- Oversee data security settings and access control rules.
- Investigate unusual or inconsistent records through audit and log data.

## 2. Emergency Operator

The Emergency Operator handles incoming disaster reports and coordinates initial incident handling.

### Use Cases
- Register a new emergency report from mobile, helpline, or monitoring source.
- Search and view reported incidents by location, type, severity, and status.
- Link an emergency report to an active disaster event.
- Update incident status as the response progresses.
- Prioritize incidents based on severity and urgency.
- Request rescue deployment for an incident.
- Request hospital admission for affected patients when needed.
- View rescue team availability and location.
- View incident response time and resolution time metrics.
- Track all reports submitted by a citizen or for a specific event.

## 3. Field Officer

The Field Officer works on-site and supports operational response activities in the disaster area.

### Use Cases
- View assigned emergency reports and active disaster events.
- Update field status for a response assignment.
- Record progress of a rescue operation.
- Mark an assignment as en route, on site, or completed.
- Record observations, notes, and outcome details for team activities.
- Request additional resources from warehouses when field stock is insufficient.
- Report consumption or dispatch of allocated resources.
- Coordinate with hospitals for patient transfer or evacuation.
- Update completion details for rescue and relief work.
- Review historical activities for assigned teams.

## 4. Warehouse Manager

The Warehouse Manager controls inventory and supports resource allocation workflows.

### Use Cases
- Create and maintain warehouse records.
- Maintain resource master data and inventory records.
- View stock levels by warehouse and resource type.
- Update inventory quantities after dispatch, consumption, or replenishment.
- Monitor low-stock and out-of-stock alerts.
- Review pending resource allocation requests.
- Approve or reject resource distribution requests when authorized.
- Dispatch approved resources to a disaster event.
- Confirm resource consumption after delivery or use.
- Prevent inventory from dropping below zero or exceeding capacity.
- View warehouse utilization and stock movement history.

## 5. Finance Officer

The Finance Officer manages donations, expenses, and financial approvals related to disaster response.

### Use Cases
- Record incoming donations from individuals or organizations.
- Confirm, reject, or review donation records.
- Track donations by disaster event.
- Create and maintain expense records for procurement, operations, medical, and logistics categories.
- Review financial transactions linked to a disaster event.
- Request approval for financial expenditures.
- Approve or reject financial requests when authorized.
- Monitor budget-related summaries and financial reports.
- Review payment status for expenses.
- Audit financial activity and ensure transaction traceability.

## 6. Shared Operational Use Cases

These use cases are common across multiple roles and represent core system workflows.

### Use Cases
- Authenticate into the system with secure login credentials.
- View role-specific dashboards and filtered data.
- Search disaster events, reports, teams, resources, hospitals, donors, and transactions.
- Generate MIS reports and analytics dashboards.
- Review approval requests and approval history.
- View audit logs for traceability and compliance.
- Use system views for restricted or simplified data access.
- Perform CRUD operations only where permissions allow.
- Handle concurrent updates to reports, assignments, inventory, and financial data.

## 7. Approval Workflow Use Cases

The system uses an approval-based process for sensitive or high-impact actions.

### Use Cases
- Submit a resource distribution request.
- Submit a rescue deployment request.
- Submit a financial approval request.
- Review a pending request.
- Approve a request.
- Reject a request.
- Escalate a request when needed.
- Store approval decisions in approval history.
- Execute the linked operational action only after approval.

## 8. Audit and Compliance Use Cases

The system must keep a full record of important operations for monitoring and compliance.

### Use Cases
- Log user actions for create, update, and delete operations.
- Track changes to emergency reports, assignments, inventory, donations, and expenses.
- Record old and new values for sensitive updates.
- Store timestamped audit entries with user identity.
- Review historical changes for investigations or compliance checks.

## 9. Emergency Reporting Use Cases

These use cases describe the citizen-facing reporting flow that feeds the operational system.

### Use Cases
- Submit a new emergency report.
- Provide incident location, disaster type, severity, and description.
- Track the status of a submitted report.
- View incident response progress.
- Report multiple incidents from the same citizen over time.

## 10. Hospital Coordination Use Cases

These use cases support coordination with hospitals during disaster response.

### Use Cases
- View hospital records and bed availability.
- Assign patients to hospitals based on capacity and specialization.
- Admit a patient linked to an emergency report.
- Update admission status and discharge status.
- Monitor occupancy rate and hospital load.
- Review hospital specializations for routing critical cases.

## 11. Rescue Team Coordination Use Cases

These use cases cover rescue team management and deployment.

### Use Cases
- Register and maintain rescue team records.
- Update team location and availability status.
- Assign a rescue team to an emergency report.
- Track assignment status from assigned to completed.
- Record team activity history.
- View team specialization and capacity.
- Match teams to incidents based on team type and severity.

## 12. Resource and Inventory Use Cases

These use cases cover warehouse stock management and disaster resource allocation.

### Use Cases
- Register resources such as food, water, medicine, and shelter supplies.
- Maintain warehouse inventory by resource and warehouse.
- Submit a resource allocation request for a disaster event.
- Approve, reject, dispatch, and consume allocated resources.
- Track dispatched versus consumed quantities.
- Trigger low-stock alerts when inventory drops below threshold.
- Prevent invalid inventory updates through validation rules.

## 13. Donation and Expense Use Cases

These use cases cover disaster-related financial intake and spending.

### Use Cases
- Register donor information.
- Capture donor phone numbers and contact details.
- Record donations linked to a disaster event.
- Track donation confirmation status.
- Create expense entries for disaster response activities.
- Link expenses to approval records where required.
- Review financial summaries by event and category.

## 14. Reporting and Analytics Use Cases

These use cases support management reporting and dashboard analysis.

### Use Cases
- View incident counts by location, disaster type, and severity.
- View resource utilization by warehouse and resource type.
- View rescue response time and completion time statistics.
- View hospital occupancy and patient flow summaries.
- View donation and expense summaries by event.
- View approval workflow status and turnaround metrics.
- Drill down into operational data using filters and role-based views.

## 15. External Actors Related to the System

These are not internal user roles, but they interact with the system or provide data to it.

### Citizen
- Submit emergency reports.
- Provide contact and location details for incidents.
- Check report status when supported by the interface.

### Donor
- Provide donor details.
- Make donations through supported payment methods.
- Receive confirmation or receipt information.

### Hospital Staff
- Share bed availability and patient admission updates.
- Support emergency patient coordination.

### Monitoring Systems / Helplines
- Feed emergency incidents into the system.
- Provide real-time incident sources for response operators.

## 16. Summary of Core Role-to-Use-Case Mapping

- Administrator: identity, access, audit, and system governance.
- Emergency Operator: report intake, incident tracking, and dispatch coordination.
- Field Officer: on-ground response execution and status updates.
- Warehouse Manager: inventory control and resource distribution.
- Finance Officer: donations, expenses, approvals, and financial reporting.

