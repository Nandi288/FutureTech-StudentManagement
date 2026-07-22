# Student Management System

A full-stack student management platform built and deployed on Microsoft Azure, 
enabling secure student record management with role-based access.

## What it solves
Educational institutions need a secure, centralized way to manage student records 
with proper access control. This system allows authorized staff to manage student 
data through role-based permissions while keeping sensitive files (like profile 
images) securely stored and access-limited.

## Tech Stack
- C#, ASP.NET Core MVC, .NET 8
- Microsoft Azure, Azure Cosmos DB, Azure Blob Storage
- OAuth 2.0 (GitHub & Google authentication)
- Role-Based Access Control (RBAC)

## Key Features
- OAuth 2.0 authentication via GitHub and Google
- Role-Based Access Control restricting actions by user role
- Full CRUD operations for student record management
- Profile pictures stored in Azure Blob Storage, secured with time-limited 
  Shared Access Signature (SAS) tokens
- Student text data stored in Azure Cosmos DB

## My Role
Collaborated within a team to design and build the system's cloud architecture, 
focusing on secure authentication flow and Azure storage integration.

## Setup
1. Clone the repo
2. Configure Azure Cosmos DB and Blob Storage connection strings in `appsettings.json`
3. Set up OAuth credentials for GitHub/Google in the Azure portal
4. Run EF Core migrations: `dotnet ef database update`
5. Run the project: `dotnet run`

## Note
This project used the Azure Student free tier, which has since expired, so it's 
not currently live/running. The code and architecture remain fully functional 
and demonstrate the implementation.
