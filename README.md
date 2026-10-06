# Reolmarkedet

Reolmarkedet is a school project developed as part of a Danish Datamatiker education. It is a desktop application for managing a physical market where private sellers rent shelves and sell goods through the store.

The application supports the central workflows from tenant and rental management to item registration, sales, returns, and monthly settlements. It is built with C#, WPF, and MSSQL, following the MVVM architecture.

The user interface is in Danish.

## Features

- Create, update, deactivate, and reactivate tenants.
- Manage shelves, shelf types, and shelf locations.
- View shelf availability and scheduled rental endings.
- Create rentals, prevent overlapping rental periods, and manage termination.
- Register items with automatically generated unique barcodes.
- Register sales through a checkout basket, including sale prices, payment methods, and notes.
- Register returns, making the returned item available for sale again.
- Calculate monthly settlements with sales, 10% commission, rental charges, and the resulting payout or amount owed.
- View a dashboard with operational information and additional financial figures in admin mode.

Data is stored in MSSQL and loaded again when the application starts.

## Project structure

| Project | Responsibility |
|---|---|
| `Reolmarkedet.Core` | Domain models, business services, repository interfaces, and calculation results. |
| `Reolmarkedet.Data` | ADO.NET repositories and database setup scripts. |
| `Reolmarkedet.WPF` | Views, ViewModels, commands, navigation, and application startup. |
| `ReolMarkedet.Tests` | Automated tests for business rules and ViewModel behaviour using MSTest and fake repositories. |

Database access uses `Microsoft.Data.SqlClient` and parameterised SQL queries.

## Prerequisites

- Windows.
- .NET 10 SDK.
- Visual Studio with .NET 10 support and the **.NET desktop development** workload, if using Visual Studio.
- A running SQL Server instance.
- SQL Server Management Studio (SSMS) for executing the database scripts.

The default connection configuration uses Windows authentication.

## Setup and startup

### 1. Get the project

Clone or download the repository.

If using Visual Studio, open `Reolmarkedet.slnx`.

### 2. Create the database

Connect to your SQL Server instance in SSMS and execute these scripts in order:

1. `Reolmarkedet.Data/Database/001_CreateInitialSchema.sql`
2. `Reolmarkedet.Data/Database/002_SeedInitialData.sql`

The first script creates `ReolmarkedetDB` if it does not already exist and creates the required tables and constraints.

The second script adds two standard shelf types and 80 shelves.

The initial schema script is intended for a fresh database. Do not rerun it when the tables already exist.

### 3. Configure the connection

From the repository root, run:

```powershell
Copy-Item Reolmarkedet.WPF/appsettings.example.json Reolmarkedet.WPF/appsettings.json
```

Open `Reolmarkedet.WPF/appsettings.json`. The default configuration is:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ReolmarkedetDB;Integrated Security=True;TrustServerCertificate=True"
  }
}
```

Change `Server` to match your SQL Server instance.

The real `appsettings.json` is excluded from Git because connection settings may differ between developers.

## Optional demo data

After completing the database setup, you can populate the database with fictional tenants, rentals, items, and sales.

Close the application and execute:

```text
Reolmarkedet.Data/Database/SeedRealisticDemoData.sql
```

The demo dates are relative to the day the script is executed.

If demo data already exists, execute the following script before seeding again:

```text
Reolmarkedet.Data/Database/RemoveRealisticDemoData.sql
```

This removes the demo tenants and their associated rentals, items, and sales. It also removes any records you manually added under those demo tenants. Shelves and shelf types are preserved.

Restart the application after changing the database.

### 4. Start the application

In Visual Studio:

1. Right-click `Reolmarkedet.WPF`.
2. Select **Set as Startup Project**.
3. Start the application.

Alternatively, run this command from the repository root:

```powershell
dotnet run --project Reolmarkedet.WPF/Reolmarkedet.WPF.csproj
```

The application requires the configured database to be available at startup.

## Staff and admin modes

The application starts in staff mode, where the daily operational workflows are available.

To enter admin mode, use **Administrator login** in the sidebar with the password:

```text
admin
```

Admin mode enables monthly settlements and the dashboard's monthly sales figures.

Use **Log ud** to return to staff mode. Closing and reopening the application also returns it to staff mode.

The shared password is a simplified access mechanism for the school prototype.

## Automated tests

From the repository root, run:

```powershell
dotnet test ReolMarkedet.Tests/ReolMarkedet.Tests.csproj
```

The tests cover business rules and ViewModel behaviour, including rental availability, pricing, termination, sales, returns, and settlement calculations.

They use fake repositories and do not require a running SQL Server database. They do not test the concrete SQL repositories or replace manual testing of database persistence and the user interface.

## Reset development data

To remove all local development data while preserving the database schema, close the application and execute:

```text
Reolmarkedet.Data/Database/ResetDevelopmentData.sql
```

**This permanently deletes all tenants, shelves, rentals, items, sales, and shelf types.**

Afterward, execute:

```text
Reolmarkedet.Data/Database/002_SeedInitialData.sql
```

This restores the two standard shelf types and 80 initial shelves.

## Prototype limitations

- **User accounts:** Admin access uses one shared password. Individual accounts, roles, and a record of who made changes are potential improvements.
- **Historical settlements:** Settlements are calculated from current data rather than stored as finalised records. A later return can change a previous month's result when recalculated. Corrections to actual payments are handled manually.
- **Settlement export:** Settlements can be viewed in the application but cannot currently be exported to PDF or CSV.
- **Operations involving multiple records:** Multi-item sales and multi-shelf rental creation save one record at a time. If a later save fails, earlier successful saves remain stored.
- **Payment processing:** Payment methods are recorded, but payments and bank transfers are handled outside the application.

## Created by 

-  **Andreas Hemmer**
-  **Anika Fuglsang**
-  **Emmerence Steffensen**
-  **Kristoffer Hov**
-  **Østen Boa**