# Employee Management System

## Features

# Employee Management System

## Project Goal

Build a modern Employee Management System for small and medium-sized organizations.

Technology Stack:

* React
* .NET 9 Web API
* PostgreSQL
* Azure
* Docker

Applies to: Web Portal and Mobile Application. Requirements below should be implemented once and shared across both clients — no duplicate modules.

---

# Phase 1 (MVP)

## Authentication & Authorization

### Features

* Login
* Logout
* Forgot Password
* Role-Based Access Control

### Roles

* Admin
* HR
* Manager
* Employee

---

## Employee Management

### Features

* Create Employee
* Update Employee
* Delete Employee
* View Employee Details



### Employee Information

* Personal Details
* Contact Details
* Emergency Contact
* Address
* Profile Photo
* Documents
* Department
* Designation
* Manager
* Location
* Join Date
* Status

---

## Department Management

### Features

* Departments
* Teams
* Designations
* Reporting Hierarchy
* Office Locations

---

## Attendance Management

### Features

* Check-In
* Check-Out
* Daily Attendance
* Attendance History
* Late Arrival Tracking
* Early Leave Tracking
* Manual Attendance Correction
* Shift Attendance

### GPS & Location Tracking (Planned Enhancement)

Employees shall be able to punch in/out from Web or Mobile, with GPS location captured on every punch.

Each attendance record shall additionally store:

* Latitude
* Longitude
* Address (reverse-geocoded)
* Device Information
* IP Address
* Remarks

Business rules:

* Punch In should normally originate from company premises. Office Geofencing: each office location may configure a Latitude/Longitude/radius; when configured, Punch In is rejected outside that radius. Geofencing is opt-in per office — an office with no radius configured is never restricted.
* Punch Out may occur outside the office when business requires it (client visit, field work, travel). Punch Out location must always be recorded regardless of where it happens.
* Location data shall be visible to Admin on attendance records.

---

## Leave Management

### Features

* Apply Leave
* Approve Leave
* Reject Leave
* Leave Balance Tracking
* Holiday Calendar
* Leave History

### Leave Types

* Casual Leave
* Sick Leave
* Earned Leave
* Unpaid Leave
* Work From Home

---

## Dashboard

### Metrics

* Total Employees
* Active Employees
* Inactive Employees
* Attendance Summary
* Leave Summary
* Department Summary

---

# Phase 2

## Payroll Management

* Salary Structure
* Allowances
* Deductions
* Payslips
* Bonus
* Overtime

### Reimbursement Integration (Planned Enhancement)

Approved reimbursements (see Expense Management, Phase 3) shall automatically be included during payroll processing.

* Approved reimbursement amount shall appear on the payslip under a distinct "Reimbursement" line.
* Payroll shall process an approved reimbursement exactly once; a "Payroll Processed" flag and "Payroll Date" prevent reprocessing.
* Payroll shall never include Draft, Submitted, Under Review, Rejected, or Changes-Requested reimbursements — only Approved.

Payslip shall contain:

* Basic Salary
* Allowances
* Deductions
* Approved Reimbursements
* Net Salary

---

## Task Management

### Overview

Admin assigns work items to employees. Tasks may involve visiting a client outside the office, and link to the Client Master (see new module below) for site/contact details.

### Actors

* Admin
* Employee

### Admin

Admin shall be able to:

* Create Task
* Assign Task
* Edit Task
* Cancel Task
* Reassign Task
* Track Task
* View Progress

### Employee

Employee shall be able to:

* View assigned tasks
* Accept task
* Reject task (optional)
* Start task
* Update progress
* Upload photos
* Add notes
* Mark task completed
* View linked client details
* Open client location in Maps

### Task Information

Each task shall contain:

* Task Number
* Task Title
* Description
* Client (link to Client Master)
* Client Name
* Client Address
* GPS Coordinates
* Assigned Employee
* Assigned Date
* Due Date
* Priority
* Status
* Notes
* Attachments

### Priority

* Low
* Medium
* High
* Critical

### Status

* Assigned
* Accepted
* In Progress
* On Hold
* Completed
* Cancelled

### Business Rules

* Only Admin can assign tasks.
* Task history shall be maintained.
* Every status change shall be audited.
* Employees can only update tasks assigned to them.
* Completed tasks become read-only.

### Due Dates

* Due Date is tracked per task and surfaced in task lists/notifications.

---

## Client Master (New Module — Supports Task Management)

### Overview

Maintains all client information used across the system. Shared by Task Management now; reserved for future CRM features.

### Actors

* Admin (full management)
* Employee (read-only, scoped to assigned clients/tasks)

### Admin

Admin shall be able to:

* Create Client
* Edit Client
* Delete Client (soft delete)
* Activate Client
* Deactivate Client
* Archive Client
* Restore Client
* Search Client
* Filter Client
* View Client
* Maintain Client Information

### Employee

Employee shall be able to:

* View clients assigned to them (via tasks)
* View client information associated with assigned tasks

Employees cannot edit client information.

### Client Details

Each client shall contain:

* Client ID
* Client Name
* Company Name
* Contact Person
* Mobile Number
* Alternate Mobile
* Email
* GST Number (Optional)
* Address Line 1
* Address Line 2
* City
* State
* Country
* Postal Code
* Latitude
* Longitude
* Notes
* Active Status
* Created Date
* Updated Date

### Business Rules

* Client names should be unique.
* Inactive clients cannot receive new tasks.
* Deleted clients should be soft deleted (never hard deleted).
* Existing tasks retain client history even if the client is later deactivated/archived.

---

## Document Management

* Offer Letters
* NDA Documents
* Appraisal Documents

---

## Announcements

* Company Announcements
* Notifications
* Email Alerts

---

# Phase 3

## Recruitment & Onboarding

* Candidate Management
* Interview Scheduling
* Offer Generation
* Joining Checklist

## Asset Management

* Laptop Allocation
* Mobile Allocation
* Asset Return Tracking

## Performance Management

* Goals
* KPI Tracking
* Performance Reviews
* Promotions

## Expense Management (Employee Reimbursement Management)

### Overview

Employees submit reimbursement (expense) claims. Once an Admin approves a claim, the amount is automatically included in the employee's salary during payroll processing (see Payroll Management → Reimbursement Integration, Phase 2).

### Actors

* Employee
* Admin
* Payroll System

### Employee

Employee shall be able to:

* Create a reimbursement request
* Save reimbursement as Draft
* Submit reimbursement
* Edit reimbursement before approval
* Delete Draft reimbursement
* Upload one or more supporting documents
* View reimbursement history
* Track reimbursement status
* View approval remarks

### Admin

Admin shall be able to:

* View all reimbursement requests
* Filter reimbursements
* Search reimbursements
* Review reimbursement details
* View uploaded documents
* Approve reimbursement
* Reject reimbursement
* Request changes
* Add remarks/comments
* Export reimbursement reports

### Payroll

When a reimbursement is approved:

* The approved amount becomes eligible for the next payroll run.
* The approved amount appears under the employee's salary as "Reimbursement".
* Paid reimbursements shall not be processed again.

### Reimbursement Information

Each reimbursement shall contain:

* Reimbursement Number
* Employee
* Expense Title
* Expense Category
* Expense Date
* Amount
* Currency
* Description
* Notes
* Attachment(s)
* Status
* Submitted Date
* Approved Date
* Approved By
* Payroll Processed Flag
* Payroll Date

### Mileage Reimbursement

An employee may submit a mileage-based claim instead of a flat amount by supplying a distance (km) instead of an amount. The reimbursement amount is then calculated automatically as distance × the configured per-km mileage rate, rather than entered by the employee — preventing self-reported inflation of the rate. The rate applied is recorded on the claim itself so a later rate change never retroactively changes an already-submitted claim.

### Attachment Support

Allowed types: PDF, JPG, JPEG, PNG. Multiple attachments per reimbursement are supported.

### Status Workflow

```
Draft
  ↓
Submitted
  ↓
Under Review
  ↓
Approved | Rejected | Changes Requested
  ↓
Paid (after payroll, from Approved only)
```

### Business Rules

* Employee cannot approve own reimbursement.
* Rejected reimbursement cannot be processed in payroll.
* Approved reimbursement becomes read-only.
* Payroll can process an approved reimbursement only once.
* Every approval action must be audited.

## Internal Messaging

* Employee Messaging
* Manager Messaging

## Notifications

- Email Notifications
- In-App Notifications
- Mobile Push Notifications
- Leave Approval Notifications
- Attendance Alerts / Exceptions
- Task Assigned / Updated / Completed
- Reimbursement Submitted / Approved / Rejected / Changes Requested / Paid (employee); New Reimbursement Submitted (admin)

---

# Multi-Tenancy & Super Admin Portal

This system is a multi-tenant SaaS platform: many companies (from 50 to 100+ employees each) share one deployment, each seeing only their own data, with a **Super Admin** tier above every tenant that can onboard, monitor, and suspend a company (immediately blocking login for every one of that company's admins and employees).

See database-design.md §24 and api-specification.md §27 for the schema and endpoint details.

## Phase 1 (Must Have — Delivered)

### Dashboard

* Cross-company counts: total / active / suspended / trial companies
* Total employees across every company
* Most recently registered companies

### Company Management

* List, search, and filter companies by status
* View a company's detail: employee count, admin list
* Create, update, soft-delete, and restore a company
* Activate / Suspend a company (the same switch — see below)
* Force-logout a company's users on demand, independent of suspension

### Company Registration

* Public, unauthenticated self-service registration URL — creates a company and its first Admin user atomically
* Gated by a platform-wide toggle (registration can be turned off entirely)
* New registrations require Super Admin approval by default (configurable) before the admin can log in

### Company Activation / Suspension

* Suspending a company immediately revokes every refresh token for its users and blocks all subsequent requests from their still-valid access tokens — not just future logins
* "Lock/Unlock Company" and "Activate/Suspend Company" are the same status switch, not two independent flags

### User Management (Company-Admin Actions)

* Super Admin can view a company's Admin users and issue a password reset for one, reusing the existing self-service forgot-password mechanism
* Day-to-day user/employee management within a company remains the existing tenant-scoped Users/Employees APIs — Super Admin does not manage individual employees

### Audit Logs

* Every company-management action (create, update, suspend, activate, approve, reject, force-logout) is written to the existing audit log
* A tenant Admin's audit log view is scoped to their own company; Super Admin has a separate cross-company view, optionally filterable to one company

### Platform Settings

* Two toggles: whether public registration is enabled at all, and whether new registrations require approval before activation

## Phase 2 (Nice to Have — Deferred)

* Subscription Management (plans, billing, trial expiry enforcement)
* Feature Management (per-company feature flags / module entitlements)
* Company branding/settings beyond name, timezone, currency, and logo
* Retrofitting tenant-scoping onto the remaining business-process entities not covered in Phase 1: Attendance records, Leave requests/balances, Payroll, Tasks, Reimbursements, Recruitment, Assets, Performance, Messaging, Notifications, Announcements, Clients (these are reached today only through an already-scoped Employee/Department/etc., so there is no direct cross-tenant data leak, but a query written directly against one of them would not itself be tenant-safe)

### Design Decisions

* Email/username uniqueness is global across the whole platform (one email = one account = one company), not per-company — no login-flow changes needed as a result
* New registrations require Super Admin approval by default; a Super Admin can flip this off later via Platform Settings
* The first-ever Super Admin is bootstrapped from configuration on startup, not created via any API

---

# Non-Functional Requirements

## Security

* JWT Authentication
* Refresh Tokens
* MFA Support
* Role-Based Authorization
* Audit Logs

### Role Capabilities (incl. Planned Modules)

Employee — can: submit reimbursement, punch in/out, view own attendance, view assigned clients, view assigned tasks, complete assigned tasks.
Employee — cannot: approve reimbursements, maintain client master, assign tasks.

Admin — can: manage reimbursements (approve/reject), manage clients, assign tasks, view attendance (incl. location), manage payroll integration, generate reports.

### Audit Trail

The system shall record, for auditable entities (including Reimbursements, Clients, Tasks, and Attendance):

* Created By / Created Date
* Updated By / Updated Date
* Deleted By / Deleted Date
* Approval History
* Status Changes
* Location Changes

## Performance

* Support 10,000+ Employees
* Response Time < 2 Seconds

## Usability

* Responsive UI
* Mobile Friendly

## Reporting

* Excel Export
* PDF Export

---

# Nice To Have

* Dark Mode
* Multi-Language Support
* QR Attendance
* Biometric Integration (incl. Attendance Face Recognition)
* Slack Integration
* Teams Integration
* ERP Integration (incl. Payroll ERP sync)
* Route Optimization
* Expense Policy Validation
* OCR Receipt Scanning
* Digital Approval Workflow
* Electronic Signature
* Offline Mobile Support
* Client Visit History
* Task Calendar Integration
* Google Maps Integration
