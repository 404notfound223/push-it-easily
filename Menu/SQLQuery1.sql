-- =========================
-- USERS
-- =========================
CREATE TABLE [dbo].[Users] (
    [Id]       NVARCHAR(5)   NOT NULL,
    [Name]     NVARCHAR(50)  NOT NULL,
    [Password] NVARCHAR(50)  NOT NULL,
    [Email]    NVARCHAR(50)  NOT NULL,   -- expanded size
    [Role]     NVARCHAR(20)  NOT NULL,   -- expanded size
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id])
);

-- =========================
-- ORDERS
-- =========================
CREATE TABLE [dbo].[Orders] (
    [OrderId]     NVARCHAR(10)   NOT NULL,
    [UserId]      NVARCHAR(5)    NOT NULL,
    [OrderDate]   DATETIME       NOT NULL DEFAULT GETDATE(),
    [TotalAmount] DECIMAL(10, 2) NOT NULL,
    [Status]      NVARCHAR(20)   NOT NULL,  -- e.g., Pending, Completed, Cancelled
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([OrderId]),
    CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);

-- =========================
-- ORDER DETAILS
-- =========================
CREATE TABLE [dbo].[OrderDetails] (
    [OrderDetailId] NVARCHAR(10)   NOT NULL,
    [OrderId]       NVARCHAR(10)   NOT NULL,
    [ProductId]     NVARCHAR(450)    NOT NULL,
    [Quantity]      INT            NOT NULL,
    [UnitPrice]     DECIMAL(10, 2) NOT NULL,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED ([OrderDetailId]),
    CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([OrderId]),
    CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id])
);