# 📖 Digital Grama Niladhari (GN) Service Management System
## Complete End-to-End User Manual

Welcome to the official User Manual for the **Digital Grama Niladhari (GN) Service Management System**. This system is a state-of-the-art web application designed to digitize local administration, service request handling, appointments, residency registry updates, secure communications, and document verification for Sri Lankan citizens and administrative officers.

---

## 📂 Table of Contents
1. [🌟 System Overview & Role Matrix](#-system-overview--role-matrix)
2. [🔑 Authentication & Account Recovery](#-authentication--account-recovery)
3. [👤 Citizen Portal Guide](#-citizen-portal-guide)
4. [🛡️ Grama Niladhari (GN) Officer Portal Guide](#-grama-niladhari-gn-officer-portal-guide)
5. [🏛️ Divisional Secretariat (DS) Officer Portal Guide](#-divisional-secretariat-ds-officer-portal-guide)
6. [🔍 Public Document Verification Portal](#-public-document-verification-portal)
7. [⚙️ Developer & System Administrator Setup](#-developer--system-administrator-setup)

---

## 🌟 System Overview & Role Matrix

The Digital GN System serves four distinct user types, each with custom layouts, dashboards, and access levels tailored to their responsibilities:

| User Role | Primary Target Users | Core Responsibilities & Available Features |
| :--- | :--- | :--- |
| **Citizen** | General Public / Residents | Request certificates, track applications, book appointments, chat with GN, update residence registry, and view announcements. |
| **GN Officer** | Local Grama Niladhari | Process & verify requests, issue certificates, set availability, manage appointments, update residence registry, publish division announcements, and chat with residents. |
| **DS Officer** | Divisional Secretariat | Monitor GN division performance, onboard/register new GN officers, assign/transfer GN divisions, and analyze divisional metrics. |
| **Administrator** | System IT Admins | Full database management, configure lookup tables, check security logs, and verify system integrity. |

---

## 🔑 Authentication & Account Recovery

All portals are secured with robust role-based JWT Bearer authentication. 

### A. Logging In
1. Navigate to the login portal at **`http://localhost:4200/auth`**.
2. Enter your **NIC (National Identity Card) Number** or **Username** (e.g., `citizen1` or `gn1`).
3. Enter your secure password (default seeded password for test users is **`Password123`**).
4. Click **Login**. You will be automatically redirected to your role-specific dashboard.

### B. Registering a New Citizen Account
*Citizens whose details are already registered in the Residence Registry can sign up:*
1. On the login page, click **Register / Sign Up**.
2. Provide your **NIC**, **Full Name**, **Date of Birth**, **Gender**, and contact details.
3. Choose a secure password and click **Register**.
4. Once registered, log in to access the citizen portal.

### C. Forgot & Reset Password Workflow
If a citizen or officer forgets their password, they can recover it using the secure OTP workflow:
1. Click **Forgot Password** on the login screen.
2. Enter your registered **NIC** and **Email Address**.
3. Click **Submit**. A verification request will be triggered.
4. For local testing, use the pre-configured recovery code: **`123456`**.
5. Enter the verification code, then input your **New Password** and confirm it.
6. Click **Reset Password**. You can now log in using your new credentials.

---

## 👤 Citizen Portal Guide

When logged in as a **Citizen**, your path is `/citizen`. Here is a detailed walkthrough of the citizen-focused modules.

### 1. Citizen Dashboard
The dashboard serves as a command center featuring:
* **Division Info**: Quick profile of your assigned Grama Niladhari division and the name of your active GN Officer.
* **Quick Stats**: Summary cards indicating your *Active Requests*, *Upcoming Appointments*, and *New Announcements*.
* **Pending Actions Alert**: Highlighting if changes are requested on a submitted application.

### 2. Requesting a New Service
To apply for certificates or register updates, go to **Service Requests** ➡️ **New Request**:
1. **Browse Service Directory**: Services are categorized under five headings:
   * **Registry Updates**: Newborn registration, Marriage Split, Migrate Family Member, Family Head changes.
   * **Document Services**: Character certificates, Residency certificates, Income certificates.
   * **Welfare & Assistance**: Samurdhi, senior citizen allowances, disaster relief.
   * **Complaints & Grievances**: Noise complaints, boundary disputes, civic issues.
   * **General Requests**: Custom administrative letters.
2. **Select Service & View Guidelines**: Click on your desired service category. Read the system-displayed dynamic guidelines detailing mandatory uploads (e.g., *"Upload Child's Birth Certificate and parents' NICs"*).
3. **Fill the Dynamic Form**: The system generates specific input fields depending on the service category chosen. Fill in all details.
4. **Upload Attachments**: Drag and drop or browse to upload proof documents (PDFs, JPGs).
5. **Submit**: Click **Submit Application**. A unique Request ID (e.g., `REQ-2026-XXXX`) will be generated.

### 3. Tracking Service Requests
* Go to the **Service Requests** page to view a table of all your applications.
* Filter requests by **Status** (*Pending, In Progress, Approved, Rejected*) or search by date.
* Click **View Details** on any request to view:
  * **Interactive Progress Timeline**: Current step of review (e.g., *Submitted ➡️ GN Verifying ➡️ DS Reviewing ➡️ Approved*).
  * **Timeline logs**: Timestamps of actions.
  * **Remarks & Messages**: Direct notes or change requests added by the GN Officer.
  * **Download Issued Certificate**: If the status is `Approved` or `Verified`, you will see a **Download Verified PDF** button to retrieve your cryptographically stamped digital certificate.

### 4. Booking Appointments with the GN Officer
When you need to visit the GN office physically for document checks or inquiries, use the **Appointments** module:
1. Click **Book Appointment**.
2. Select the **Date** (only dates marked as available by the GN Officer will be selectable).
3. Select an available **Time Slot**.
4. Choose the **Service Type** (e.g., *Document Verification, Land Dispute consultation*).
5. Add a brief description of the purpose of your visit.
6. Click **Schedule Appointment**.
7. View your upcoming appointments in the calendar/table list. You can click **Cancel** or request to **Reschedule** if your availability changes.

### 5. Residence Registry (Household Card)
View your official digital family registration details:
* Inspect your **Family Card Number** (e.g., `FC-COL-9901`).
* View the designated **Head of Household** and their details.
* See the complete table of registered household members including their relationship to the head, NIC, marital status, and occupation.
* If any details are incorrect, use the **Request Update** link to launch a registry correction service request.

### 6. Secure Document Vault
Your personal repository of secure files:
* **Uploaded Documents**: View files you've uploaded during service requests. You can upload permanent files here (like a digital copy of your Birth Certificate) to quickly attach them to future requests.
* **Received Documents**: Look through all certificates officially issued to you by the government. Download copies anytime.

### 7. Direct Chat with your GN Officer
For instant communication regarding pending applications:
* Go to the **Messages** page.
* Type a message in the chat box to converse directly with your assigned Grama Niladhari officer.
* Attach files (like missing receipts or photos) directly within the chat screen.
* The messaging console maintains a permanent record of all communications to prevent disputes.

---

## 🛡️ Grama Niladhari (GN) Officer Portal Guide

When logged in as a **GN Officer**, your path is `/gn-officer`. Here are your operational modules.

### 1. Processing Citizen Service Requests
When citizens submit applications in your division, they appear in your **Requests** list:
1. **Filter & Sort**: Filter by *Pending, In Progress, Approved,* and *Rejected*, or search by citizen name/NIC.
2. **Review Details**: Click **View Details** on a request. You can:
   * Read the submitted form fields.
   * Preview uploaded attachments in the inline viewer.
   * Read or add **Internal Remarks** (visible only to officers) or **Citizen Messages** (shared with the citizen).
3. **Verify Details via Verification Editor**:
   * If details require registry cross-checking, open the **Verification Editor**.
   * Compare submitted applicant details against the active database records. Mark items as *"Verified"* or *"Mismatched"*.
4. **Action the Request**:
   * **Request Changes**: If an attachment is blurry or information is missing, select *Request Changes* and write a message specifying what the citizen must upload. The request status updates to *Changes Requested*, notifying the citizen.
   * **Approve / Generate Certificate**: If all checks pass, click **Generate Certificate** to launch the **Certificate Editor**.
   * **Reject**: If ineligible, input a mandatory rejection reason. The citizen is notified immediately.

### 2. Issuing Certificates (Certificate Editor)
When approving document requests:
1. The **Certificate Editor** opens showing official templates (Residency Letter, Character Certificate, etc.).
2. The template automatically pulls data fields from the citizen's profile and request details.
3. You can customize the text, add custom official remarks, and select the digital stamp.
4. Click **Publish Certificate**. The system:
   * Generates a secure PDF certificate.
   * Digitally signs the document.
   * Attaches a unique cryptographic verification token.
   * Places a clickable/scannable verification link on the certificate footer.
   * Deposits it in the Citizen's Vault.

### 3. Setting Availability & Managing Appointments
Manage your weekly calendar to control citizen visits:
* Navigate to **Availability**:
  * Set active working dates and specify start/end hours (e.g., *Mondays and Wednesdays, 9:00 AM - 1:00 PM*).
  * Add custom notes (e.g., *"Available for pension document signing only"*).
  * Save availability. Citizens will only be able to request appointments within these constraints.
* Navigate to **Appointments**:
  * View list of requested appointments.
  * Click **Approve** to confirm a booking. The citizen will be notified.
  * Click **Reschedule** or **Cancel** (providing a reason) if you will be out of the office.

### 4. Residence Registry Administration
* Look up any household in your division.
* Search by **Family Card Number**, citizen name, or NIC.
* View household compositions and verify relationships.
* Review registry change requests submitted by citizens (like adding a newborn or splitting a family card).

### 5. Managing Announcements
Keep your division informed of updates (e.g., mobile health clinics, vaccine distributions, dry ration distributions):
1. Navigate to **Announcements** ➡️ **Create New**.
2. Enter a Title, Category (Welfare, General, Urgent, Public Notice), and the body content.
3. Set the target audience (e.g., *All Citizens, Only Registered Voters*).
4. Save as a **Draft** or click **Publish**.
5. Once published, the announcement appears instantly on the dashboard of every citizen in your division.

---

## 🏛️ Divisional Secretariat (DS) Officer Portal Guide

When logged in as a **DS Officer**, your path is `/ds-officer`. Here are your monitoring and administrative modules.

### 1. Divisional Performance Monitoring
* **Dashboard Overview**: Check divisional highlights—total requests processed across all GN divisions, pending approvals, and average turnaround times.
* **GN Activity Monitor**: Drill down into individual GN divisions (e.g., *Fort GN Division, Pettah GN Division*) to inspect their request backlog, appointment volume, and citizen satisfaction ratings.

### 2. Onboarding & Registering GN Officers
To onboard a new GN officer:
1. Navigate to **GN Registration**.
2. Fill out the registration form:
   * Enter the officer's **NIC**, **Full Name**, **Email**, and **Phone**.
   * Assign them a temporary password.
   * Select the designated **GN Division ID** (e.g., `GN-COL-A`).
3. Click **Register GN Officer**. The system runs a secure atomic transaction to establish the account, hash credentials, and assign the officer to the division.

### 3. Managing GN Transfers
When a GN officer is transferred or leaves their post:
1. Navigate to **GN Transfers**.
2. Select the target **GN Officer** from the dropdown.
3. Select their **New Division** assignment.
4. Input the transfer order details and date of effect.
5. Click **Process Transfer**. The system updates the assignments and routes all future citizen requests for that division to the newly assigned officer.

---

## 🔍 Public Document Verification Portal

This feature provides external third parties (such as banks, employers, universities, and government agencies) with a way to check the legitimacy of issued documents without requiring access to the internal system.

### How Verification Works
1. Every official PDF certificate issued by a GN Officer contains a verification footer:
   > *"To verify the authenticity of this document, scan the QR code or visit http://localhost:4200/verify-document/[verification-token]"*
2. An external verifier opens the verification URL.
3. The system queries the secure cryptographic registry database.
4. The verifier sees one of four states:

#### A. Success (Valid & Verified)
* The page displays a green banner: **"Verification Success - This document is verified and officially authentic."**
* Details shown: Document Number, Document Type, Citizen Name, Masked NIC, Verification Date, Division, and Verifying Officer Name.
* A **Download Verified PDF** button is enabled to download the master copy from the secure server to compare it layout-by-layout with the copy provided.

#### B. Revoked Certificate
* The page displays a red banner: **"Verification Revoked"**.
* The warning notes: *"This document verification has been officially revoked and is no longer valid."*
* Details shown: Reason for revocation (e.g., *"Citizen relocated, details expired"*), date revoked, and original metadata.

#### C. Document Unavailable
* The page displays a yellow banner: **"Document Unavailable"**.
* Indicates that the certificate token exists but the document is still undergoing review or has been put on hold by the GN Officer.

#### D. Failed Verification (Invalid)
* The page displays an error icon: **"Verification Failed - The document token is invalid, not found, or has been tampered with."**
* Warning causes: The QR code was modified, the document is a counterfeit copy, or the record does not exist in the official state database.

---

## ⚙️ Developer & System Administrator Setup

For developers running or setting up the workspace locally:

### 1. Database Initialization
The backend relies on Microsoft SQL Server. Follow these steps to initialize:
1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to your database engine (e.g., `localhost\SQLEXPRESS`).
3. Open a New Query window and paste the contents of [GN_System_Sample_Data.sql](file:///c:/Users/hp/Documents/ACEDEMIC/SEMESTER%208/GN-SYSTEM/GN_System_Sample_Data.sql).
4. Run the query (**F5**). This seeds all standard divisions, test citizens, officers, and initial sample documents.

### 2. Local Startup Instructions
1. Navigate to the workspace root directory: `C:\Users\hp\Documents\ACEDEMIC\SEMESTER 8\GN-SYSTEM`
2. Run the start script:
   * **Double-click** `run-all.bat` to launch the backend and frontend in separate console windows.
   * *Alternatively*, run `run-all.ps1` in PowerShell.
3. The services run at:
   * **Backend REST API**: `http://localhost:5058` (Swagger at `http://localhost:5058/swagger`)
   * **Frontend Angular Web App**: `http://localhost:4200`

---
*End of User Manual. For further support, contact your Divisional Secretariat IT Administrator.*
