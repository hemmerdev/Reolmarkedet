# Reolmarkedet

## Local database setup

1. Open `Reolmarkedet.Data/Database/001_CreateInitialSchema.sql` in SQL Server Management Studio.
2. Execute the complete script. It creates `ReolmarkedetDB` if it does not already exist.
3. From the repository root, run:

   ```powershell
   Copy-Item Reolmarkedet.WPF/appsettings.example.json Reolmarkedet.WPF/appsettings.json
   ```

4. Open `Reolmarkedet.WPF/appsettings.json`.
5. Change `Server` if your SQL Server instance is not `localhost`.
6. Start the application.

The real `appsettings.json` is excluded from Git because connection settings may differ between developers.

The initial schema script is intended for a new database. Do not rerun it when the tables already exist.

## Reset development data

To remove all local test data while keeping the database schema, close the application and execute:

`Reolmarkedet.Data/Database/ResetDevelopmentData.sql`

This permanently deletes all tenants, shelves, rentals, items, and sales from the local database. It then restores the two standard shelf types.