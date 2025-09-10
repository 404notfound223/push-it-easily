-- Drop the existing constraint
ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_Users];

-- Recreate it with the name EF Core expects
ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Users_UserId] 
FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE;