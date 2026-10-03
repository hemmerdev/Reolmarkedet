# Reolmarkedet

## Local database setup

1. Open and execute `Reolmarkedet.Data/Database/001_CreateInitialSchema.sql` in SQL Server Management Studio. It creates `ReolmarkedetDB` if it does not already exist.
2. Open and execute `Reolmarkedet.Data/Database/002_SeedInitialData.sql` in SQL Server Management Studio. It adds two shelf types and 80 shelves.
3. From the repository root, run:

   ```powershell
   Copy-Item Reolmarkedet.WPF/appsettings.example.json Reolmarkedet.WPF/appsettings.json
   ```

4. Open `Reolmarkedet.WPF/appsettings.json`.
5. Change `Server` if your SQL Server instance is not `localhost`.
6. In Visual Studio, right click `Reolmarkedet.WPF` and select **Set as Startup Project**. 
7. Start the application.

The real `appsettings.json` is excluded from Git because connection settings may differ between developers.

The initial schema script is intended for a new database. Do not rerun it when the tables already exist.

## Admin mode

In this prototype, the admin mode is accessible by logging in with the password `admin`.

## Optional demo data

To populate a local database with fictional tenants, rentals, items, and sales, open and execute `Reolmarkedet.Data/Database/SeedRealisticDemoData.sql` in SQL Server Management Studio after steps 1 and 2 above. Its dates are relative to the day you run it.

If demo data already exists, run `Reolmarkedet.Data/Database/RemoveRealisticDemoData.sql` before seeding. That removal script also deletes any records you manually added under the demo tenants; it keeps the shelves and shelf types.

## Reset development data

To remove all local test data while keeping the database schema, close the application and execute:

`Reolmarkedet.Data/Database/ResetDevelopmentData.sql`

This permanently deletes all tenants, shelves, rentals, items, sales, and shelf types. Execute `Reolmarkedet.Data/Database/002_SeedInitialData.sql` afterward to restore the two standard shelf types and 80 initial shelves.
