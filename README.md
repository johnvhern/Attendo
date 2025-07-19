# 🎯 GCT QR Attendance System

![.NET](https://img.shields.io/badge/.NET_Framework-4.x-blue?logo=.net&logoColor=white)
![C#](https://img.shields.io/badge/C%23-Programming-blue.svg?logo=csharp)
![SQL Server](https://img.shields.io/badge/MSSQL-Database-red?logo=microsoftsqlserver)
![WinForms](https://img.shields.io/badge/WinForms-UI-green)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

The **GCT QR Attendance System** is a secure, offline desktop tool for tracking attendance using QR codes and barcode scanners. Designed for educational events or academic sessions, it enables easy session creation, QR-based attendance logging, ID generation, and reporting—all backed by a local MSSQL database and protected with bcrypt password hashing.

---

## 🧠 Key Features

- ✅ **User Management Console** (C# CLI tool)
  - Master user setup (cannot be deleted or reset by others)
  - Bcrypt password hashing for secure login
- ✅ **QR Attendance Dashboard** (WinForms)
  - Real-time scanning using barcode scanner or keyboard
  - Automatic late detection and duplicate prevention
  - Live feedback: sounds and UI changes for scanned results
- ✅ **Student Management**
  - Manual registration + photo upload
  - Auto-generated QR codes for each student (PNG format)
- ✅ **Session Management**
  - Add/edit/delete event sessions
  - Define session start and cutoff time (for late tagging)
- ✅ **Attendance Logging**
  - Tracks scanned time and marks IN or LATE
  - Displays last 6 scanned students with photo/info
- ✅ **Report Export**
  - Filter by session/date/section
  - Export PDF reports (via iText7)
- ✅ **Security**
  - Passwords hashed via BCrypt.Net
  - Admin actions restricted to master user
  - Offline storage — no external network dependencies

---

## 🧰 Tech Stack

| Component    | Technology          |
|--------------|---------------------|
| Frontend     | WinForms (.NET Framework) |
| Backend      | MSSQL Server (local) |
| Auth/Hashing | BCrypt.Net          |
| QR Generator | QRCoder             |
| PDF Export   | iText7              |
| Console Tool | C# Console App      |

---

## 📦 Installation Guide

### Prerequisites
- Visual Studio (with .NET Desktop Development)
- Microsoft SQL Server Express (`localhost\SQLEXPRESS`)
- Git

### Steps

1. Clone the repository**
   ```bash
   git clone https://github.com/johnvhern/Attendo.git
2. Set up the database
   - Create a new DB in SQL Server (e.g., GCTAttendance)
   - Run the provided schema (see Database/) to create tables:
     tblUsers, tblStudents, tblSessions, tblAttendance
3. Configure App.config
   ```bash
   <connectionStrings>
   <add name="GCTAttendance"
       connectionString="Data Source=localhost\SQLEXPRESS;Initial Catalog=GCTAttendance;Integrated Security=True"
       providerName="System.Data.SqlClient" />
   </connectionStrings>
4. Run the Console Tool
   - Register your first user (automatically becomes Master)
   - Path: /ConsoleApp/UserManager.exe
   - Launch the WinForms Application
   - Press F5 in Visual Studio or run the .exe
   - Login using your credentials
   - Begin adding students, generating IDs, and creating sessions

---

## 🗃️ Database Overview

| Table           | Purpose                              |
| --------------- | ------------------------------------ |
| `tblUsers`      | Stores users & hashed passwords      |
| `tblStudents`   | Student records + photo path & QR ID |
| `tblSessions`   | Event/session metadata               |
| `tblAttendance` | Logged scans (with IN/LATE tags)     |

---

## 📷 Screenshots
<img width="1919" height="1079" alt="Screenshot 2025-05-15 192002" src="https://github.com/user-attachments/assets/4d4fc9ed-b54c-4c8c-9ab5-2287fc72f8c5" />
<img width="1919" height="1078" alt="Screenshot 2025-05-15 193147" src="https://github.com/user-attachments/assets/962f883f-daf1-4be0-b5c9-d4d9ff9a1261" />

---

## 🔐 Security Features
- Passwords hashed with BCrypt.Net
- No plaintext credentials stored
- Master user is protected against deletion/modification
- All attendance logs and student info stored locally in MSSQL

---

## 📜 License
This project is licensed under the MIT License.
