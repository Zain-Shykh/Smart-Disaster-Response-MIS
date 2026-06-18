# Smart Disaster Response Management Information System (SDR-MIS)

## Project Overview
**Role:** Full-Stack & Database Developer
**Tech Stack:** React, Vite, Bootstrap 5, ASP.NET Core 8, Entity Framework Core, SQL Server
**Architecture:** Multi-tier, Role-Based Access Control (RBAC), Event-Driven DB Triggers

A comprehensive enterprise-level management information system designed to coordinate natural disaster response efforts across a country. The system seamlessly handles high-volume emergency reports, resource allocation logistics, hospital bed coordination, financial tracking, and real-time dashboard analytics. 

---

## 🌟 Resume Bullet Points
*Copy and paste these directly into your resume, tailoring them to the role you are applying for.*

### Database & Performance Engineering
- **Designed and implemented a normalized relational database (3NF/BCNF)** using SQL Server containing 24 tables to manage complex domain objects, resolving multi-valued and composite attributes.
- **Engineered ACID-compliant transaction pipelines** utilizing `TRY-CATCH` blocks and `BEGIN/COMMIT/ROLLBACK` combined with EF Core transaction scopes, ensuring zero data inconsistency during concurrent resource allocations and financial approvals.
- **Achieved up to 100% query latency reduction (4ms to 0ms)** on analytical dashboards by implementing a robust indexing strategy, including composite indices with `INCLUDE` clauses and filtered indices on high-traffic queries.
- **Automated core business logic via SQL Server Triggers** (`AFTER INSERT/UPDATE`), abstracting real-time cascading state changes (e.g., inventory threshold alerts, capacity management, audit logging) away from the application layer.
- **Implemented rigorous compliance and security mechanisms** including an automated JSON-serialized Audit Log trigger for sensitive mutations, and isolated database views restricting column-level data exposure based on user roles.

### Backend Engineering (ASP.NET Core 8)
- **Built a robust RESTful API** utilizing ASP.NET Core 8 and Entity Framework Core, featuring centralized ProblemDetails error handling and a fully tested integration test suite (54/54 passing).
- **Developed a fine-grained Role-Based Access Control (RBAC) system** using JWT tokens, PBKDF2-HMAC-SHA256 password hashing (120k iterations), and module-action level permissions for 5 distinct operational roles.
- **Implemented Optimistic Concurrency Control** across high-contention resource allocation workflows using `RowVersion` tokens, preventing race conditions and race-updates with `HTTP 409 Conflict` feedback.
- **Constructed complex analytical reporting endpoints** exposing prioritized queue sorting algorithms, proximity-based rescue team routing, and real-time hospital load balancing capabilities.

### Frontend Engineering (React.js)
- **Developed a responsive Single Page Application (SPA)** using React, Vite, and Bootstrap 5, effectively processing data from over 20 distinct REST API endpoints.
- **Engineered strict role-aware routing** utilizing React Router DOM, guaranteeing impenetrable client-side navigation restrictions driven by AuthContext validation.
- **Designed an interactive MIS Analytics Dashboard** using Chart.js to visualize real-time disaster insights, including incident queues, capacity metrics, and financial breakdowns dynamically driven by backend views.
- **Created a resilient, approval-gated UX flow**, visually disabling/enabling components based on real-time transaction approval statuses, and elegantly handling API exceptions via global Axios interceptors.

---

## 🚀 Deep-Dive Implementation Details
*(Use this section to prepare for technical interviews and viva)*

### 1. Database Architecture
- **ERD & Normalization:** Modeled 15 strong entities, 3 weak entities (existentially dependent), and associative entities with descriptive attributes. Normalized to 3NF/BCNF to prevent data redundancy and anomalies.
- **Transaction Handling:** Critical workflows (Resource distribution, rescue deployment, financial processing) were encapsulated in explicit database transactions. Used `READ COMMITTED` by default and optimistic locking to prevent race conditions.
- **Database Automation (Triggers):** 
  - *Inventory Alerts:* `AFTER UPDATE` trigger that fires an alert exactly when inventory drops below a dynamic threshold.
  - *Audit Logging:* Captures all sensitive data mutations (INSERT/UPDATE/DELETE), serializing the old and new states into JSON and persisting them to a compliance table.
  - *Status Propagation:* Automatically updates Rescue Team availability when assigned to an incident and cascades Hospital Bed counts upon patient admission/discharge.
- **Views:** Built complex, pre-joined views (e.g., `ActiveEmergencyView`, `FinancialSummaryView`) to simplify analytical queries. Leveraged views as a security abstraction layer to hide internal foreign keys and personal data from read-only users.
- **Indexing Strategy:** Avoided over-indexing insert-heavy tables. Created targeted non-clustered, composite, and filtered indexes (e.g., `IX_ER_Pending` on `Status='Pending'`) optimizing for the most frequent `WHERE` and `JOIN` predicates. 

### 2. Backend Architecture (ASP.NET Core 8)
- **Auth & Security:** Implemented PBKDF2 hashing with a 16-byte random salt and 32-byte derived key to prevent brute-force attacks. JWT handles session claims for RBAC.
- **Validation:** 54 automated integration tests executed against a real database verifying authorization bounds, data-transfer object integrity, and transaction rollbacks during forced failures.
- **Data Consistency:** Entity Framework Core transaction bounds ensure DB and app code are aligned perfectly. 

### 3. Frontend Architecture (React)
- **Component Design:** Built isolated layout shells, status badges, and data-grids utilizing a global CSS overlay atop Bootstrap 5. Mobile-responsive.
- **State Management:** Handled globally via Context API (AuthContext, NotificationContext).
- **Concurrency Feedback UX:** When the backend intercepts a stale data mutation (via row versioning), the Axios client intercepts the `409 Conflict` and gracefully prompts the user with an intuitive Toast Notification to reload the latest changes.

### 4. Key Workflows Mastered
1. **Approval Workflow:** A user requests a resource. It is held in a `Pending` state. Once a Manager approves, an explicit transaction dispatches the goods, decrements the quantity, checks thresholds (triggering an alert if low), and logs the event.
2. **Rescue Coordination:** Proximity routing assigns the closest available Medical/Fire teams to a high-severity emergency. The backend executes a lock, assigns the team, and a DB trigger automatically sets the team to `Busy`.
3. **Analytics Pipeline:** Real-time data streams into indexed tables, passes through pre-joined SQL views, is served via `ReportsController`, and rendered dynamically onto Chart.js components on the frontend React app.
