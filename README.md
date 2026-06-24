# Digital Grama Niladhari (GN) System - Backend & Database Setup Guide

Welcome to the **Digital Grama Niladhari Service Management System** workspace! This guide provides comprehensive, step-by-step instructions to initialize your database, seed secure test data, run the services locally, and execute authenticated API calls via Swagger.

---

## 👥 Standard Test Users & Credentials

All test users are seeded with the secure pre-hashed password: **`Password123`**

| Role | NIC / Username | Password | Email | Description |
| :--- | :--- | :--- | :--- | :--- |
| **Citizen** | `citizen1` | `Password123` | `citizen1@egn.gov.lk` | Saman Kumara (Requests certificates, chats) |
| **Citizen Spouse**| `citizen2` | `Password123` | `citizen2@egn.gov.lk` | Nimali Sandamali (Submits requests) |
| **GN Officer** | `gn1` | `Password123` | `gn1@egn.gov.lk` | Fort GN Area Officer (Approves/rejects requests) |
| **DS Officer** | `ds1` | `Password123` | `ds1@egn.gov.lk` | Divisional Secretariat Officer (Final approval) |
| **Administrator**| `admin1` | `Password123` | `admin@egn.gov.lk` | System Administrator (Full access) |

---

## 🗄️ 1. Database Setup Instructions (SSMS)

To initialize your SQL Server database and seed the required tables with valid test records, perform the following:

1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to your local SQL Server instance (e.g., `localhost\SQLEXPRESS` or `(local)`).
3. Open a **New Query** window.
4. Copy the entire contents of your [GN_System_Sample_Data.sql](file:///c:/Users/hp/Documents/ACEDEMIC/SEMESTER%208/GN-SYSTEM/GN_System_Sample_Data.sql) file.
5. Paste it into the SSMS query window and click **Execute** (or press **F5**).
   * *This will clean existing tables, seed complete test profiles, configure the circular family card relations, and prepare everything for instant verification.*

---

## 🚀 2. Run the Services Locally

1. Open your Windows File Explorer and navigate to the project root directory:
   `C:\Users\hp\Documents\ACEDEMIC\SEMESTER 8\GN-SYSTEM`
2. **Double-click** the [run-all.bat](file:///c:/Users/hp/Documents/ACEDEMIC/SEMESTER%208/GN-SYSTEM/run-all.bat) file.
3. Two dedicated terminal windows will open:
   * 🖥️ **Backend API Console**: Running on **`http://localhost:5058`**
   * 🖥️ **Frontend Angular Console**: Running on **`http://localhost:4200`**

---

## 🛡️ 3. Swagger Authentication & API Testing Instructions

We have configured Swagger with full **JWT Bearer padlock support** so you can easily test secure, role-restricted endpoints.

### Step A: Generate an Authentication Token
1. Open your browser and navigate to the Swagger UI:
   👉 **[http://localhost:5058/swagger](http://localhost:5058/swagger)**
2. Find the **`POST /api/Account/login`** endpoint.
3. Click **Try it out** and replace the request body with a test user's NIC (e.g., `citizen1` or `gn1`):
   ```json
   {
     "nic": "citizen1",
     "password": "Password123"
   }
   ```
4. Click **Execute**. The API will return a successful `200 OK` response with a JSON envelope containing the `token` string:
   ```json
   {
     "success": true,
     "message": "Login successful.",
     "data": {
       "nic": "citizen1",
       "role": "Citizen",
       "email": "citizen1@egn.gov.lk",
       "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c..."
     }
   }
   ```
5. **Copy** the value of the `"token"` field (do not include the outer quotes).

### Step B: Authorize Swagger UI
1. Scroll to the very top of the Swagger UI page and click the green **`Authorize`** button (or click any padlock icon next to secure routes).
2. In the text box, type:
   ```text
   Bearer YOUR_COPIED_TOKEN_HERE
   ```
   *(Ensure there is a single space between `Bearer` and your token string).*
3. Click **Authorize** and then **Close**. All secured padlock icons will now appear locked.

### Step C: Test Protected Endpoints
Now you can execute protected operations! For example:
- Try calling `GET /api/Announcement` to fetch seeded Fort announcements.
- Try calling `POST /api/Announcement` (creating an announcement). If you are authorized as `citizen1` (Citizen), Kestrel will return `403 Forbidden` (since only `GNOfficer` or `DSOfficer` can publish announcements). If you log in and authorize as `gn1` (GN Officer), the request will succeed and return the new announcement!

---

## 🔑 Forgot / Reset Password Verification Workflow

To test the account recovery process:
1. Trigger **`POST /api/Account/forgot-password`** in Swagger using NIC `citizen1` and Email `citizen1@egn.gov.lk`.
2. The response will return success and display verification details. For local development, the code is set to **`123456`**.
3. Trigger **`POST /api/Account/reset-password`** using NIC `citizen1`, Code `123456`, and a new password (e.g., `NewSecurePassword99`).
4. Upon execution, the password hash in the `accounts` database will be securely hashed using BCrypt. You can immediately log in again using your new password!

---

## 👥 4. Role-Based User Creation Testing (DS ➡️ GN ➡️ Citizen)

We have implemented atomic database transaction endpoints for secure role-based administrative user creation with active constraints:

### Scenario A: DS Officer creates a GN Officer
1. Log in as **`ds1`** (DS Officer) and copy the JWT token.
2. Authorize Swagger with the `ds1` token.
3. Call **`POST /api/Account/gn-officer`** with a new profile payload:
   ```json
   {
     "nic": "gn_new_99",
     "fullName": "Officer Samantha Perera",
     "gnDivisionId": "GN-COL-A",
     "email": "samantha@egn.gov.lk",
     "phone": "0771239999",
     "password": "Password99"
   }
   ```
4. Clicking execute will securely create the new `Account` (role `GNOfficer`, password securely BCrypt-hashed) and seed the linked `gn_officers` profile in a secure transaction.
   * *Note: Attempting to call this endpoint as `citizen1` or `gn1` will yield a `403 Forbidden` response.*

### Scenario B: GN Officer creates a Citizen
1. Log in as **`gn1`** (GN Officer) and copy the JWT token.
2. Authorize Swagger with the `gn1` token.
3. Call **`POST /api/Account/citizen`** with a new citizen payload:
   ```json
   {
     "nic": "citizen_new_88",
     "fullName": "Sunil Shantha",
     "dob": "1992-10-12",
     "gender": "Male",
     "maritalStatus": "Single",
     "religion": "Buddhist",
     "occupation": "Accountant",
     "familyCardNumber": "FC-COL-9901",
     "relationshipToHead": "Son",
     "username": "sunils",
     "email": "sunil@egn.gov.lk",
     "phone": "0771112222",
     "password": "Password88"
   }
   ```
4. Clicking execute will securely create the `Account`, the `Citizen_Master` profile, and the `EGN_Service_User` record atomically.
