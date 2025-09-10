ALTER TABLE dbo.[Orders]
ADD CONSTRAINT [FK_Orders_User_UserId]
FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id]);
