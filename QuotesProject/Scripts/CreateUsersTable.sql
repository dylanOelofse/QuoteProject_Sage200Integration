-- =============================================================
-- Creates the Users table and seeds one admin and one regular user.
-- IsAdmin: 1 = admin, 0 = regular user.
-- Passwords: both users log in with the password 1234, but the column
-- stores a salted PBKDF2 hash (ASP.NET Core PasswordHasher format),
-- never the password itself. Note the two hashes differ even though
-- the password is the same — that is the random salt at work.
-- Table is named "Users" (plural) because "User" is a reserved
-- keyword in SQL Server and would need [brackets] everywhere.
-- Safe to re-run: existing users get their password/role updated.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users
    (
        UserId      INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        Username    NVARCHAR(50)        NOT NULL UNIQUE,
        Password    NVARCHAR(200)       NOT NULL,   -- sized for a hash, not a password
        IsAdmin     BIT                 NOT NULL DEFAULT 0
    );
END
GO

-- admin / 1234
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Password, IsAdmin)
    VALUES ('admin', 'AQAAAAIAAYagAAAAEGwpdM5tU03RlDrkHbjr8ekmXADFlkOJa8DKDC/8Vde1iKZAA/DfWTAx+9gJDRviTA==', 1);
END
ELSE
BEGIN
    UPDATE Users
    SET Password = 'AQAAAAIAAYagAAAAEGwpdM5tU03RlDrkHbjr8ekmXADFlkOJa8DKDC/8Vde1iKZAA/DfWTAx+9gJDRviTA==',
        IsAdmin = 1
    WHERE Username = 'admin';
END
GO

-- user / 1234
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'user')
BEGIN
    INSERT INTO Users (Username, Password, IsAdmin)
    VALUES ('user', 'AQAAAAIAAYagAAAAENDIEUlHLKbTKS//pgahVOwUjhJGSZTWaIPfk/9HillwoUVMhUfmZxRenU9K346+7A==', 0);
END
ELSE
BEGIN
    UPDATE Users
    SET Password = 'AQAAAAIAAYagAAAAENDIEUlHLKbTKS//pgahVOwUjhJGSZTWaIPfk/9HillwoUVMhUfmZxRenU9K346+7A==',
        IsAdmin = 0
    WHERE Username = 'user';
END
GO
