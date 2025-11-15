SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

------------------------------------------------------
-- Create database
------------------------------------------------------
CREATE DATABASE ABCRetailDB;
GO
USE ABCRetailDB;
 GO


------------------------------------------------------
--    TABLE: USERS
------------------------------------------------------
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'Customer',
    Email NVARCHAR(255),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO


------------------------------------------------------
--    TABLE: CUSTOMERS
------------------------------------------------------
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
CREATE TABLE dbo.Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Surname NVARCHAR(100) NOT NULL,
    Username NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255),
    Phone NVARCHAR(50),
    PasswordHash NVARCHAR(255),
    Role NVARCHAR(50) DEFAULT 'Customer',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_Customers_Username ON dbo.Customers(Username);
GO


------------------------------------------------------
--    TABLE: PRODUCTS
------------------------------------------------------
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
CREATE TABLE dbo.Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    SKU NVARCHAR(50),
    ProductName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Manufacturer NVARCHAR(200),
    Price DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    StockAvailable INT NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(1000),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO


------------------------------------------------------
--    TABLE: ORDERS
------------------------------------------------------
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
CREATE TABLE dbo.Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    OrderDateUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Submitted',
    TotalAmount DECIMAL(12,2) NOT NULL DEFAULT 0.00,

    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customers(CustomerId) ON DELETE CASCADE
);
GO


------------------------------------------------------
--    TABLE: ORDER ITEMS
------------------------------------------------------
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
CREATE TABLE dbo.OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NULL,
    ProductLegacyId NVARCHAR(50),
    ProductName NVARCHAR(200),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL,

    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,

    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products(ProductId) ON DELETE SET NULL
);
GO


------------------------------------------------------
--    TABLE: CART
------------------------------------------------------
IF OBJECT_ID('dbo.Cart', 'U') IS NOT NULL DROP TABLE dbo.Cart;
CREATE TABLE dbo.Cart (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerUsername NVARCHAR(100) NOT NULL,
    ProductLegacyId NVARCHAR(50) NOT NULL,
    ProductId INT NULL,
    Quantity INT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Cart_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products(ProductId) ON DELETE SET NULL
);
GO


------------------------------------------------------
--    TABLE: UPLOADED DOCUMENTS
------------------------------------------------------
IF OBJECT_ID('dbo.UploadedDocuments', 'U') IS NOT NULL DROP TABLE dbo.UploadedDocuments;
CREATE TABLE dbo.UploadedDocuments (
    Id NVARCHAR(100) PRIMARY KEY,
    FileName NVARCHAR(255) NOT NULL,
    OrderLegacyId NVARCHAR(50),
    OrderId INT NULL,
    CustomerName NVARCHAR(200),
    UploadedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    BlobUrl NVARCHAR(2000),
    FileSize BIGINT NOT NULL,

    CONSTRAINT FK_UploadedDocs_Orders FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders(OrderId) ON DELETE SET NULL
);
GO

------------------------------------------------------
--    INSERT SAMPLE DATA
------------------------------------------------------
PRINT 'Inserting 4 sample entries per category...';


------------------------------------------------------
-- CUSTOMERS
------------------------------------------------------
INSERT INTO dbo.Customers (Name, Surname, Username, Email, Phone, PasswordHash, Role)
VALUES
('Jason','Smith','jsmith87','jsmith87@gmail.com','', '<HASH>', 'Customer'),
('Amanda','Andrews','mandy.andrews','mandy.andrews@outlook.com','', '<HASH>', 'Customer'),
('Liam','Brown','liam.brown23','liam.brown23@yahoo.com','', '<HASH>', 'Customer'),
('Emily','Clark','emclark91','emclark91@gmail.com','', '<HASH>', 'Customer');


------------------------------------------------------
-- PRODUCTS
------------------------------------------------------
INSERT INTO dbo.Products (SKU, ProductName, Description, Manufacturer, Price, StockAvailable)
VALUES
('PLANT-01', 'Green Fake Plant',
 'Realistic artificial green plant in ceramic pot — perfect for desks and living rooms.',
 'DecorCo', 25.00, 45),

('PLANT-02', 'White Cherry Blossom Fake Plant',
 'Elegant faux cherry blossom with white flowers and flexible stems.',
 'DecorCo', 30.00, 32),

('THROW-01', 'White Fluffy Throw',
 'Cozy white faux fur throw blanket — adds texture and warmth.',
 'ComfortHome', 40.00, 20),

('MUG-01', 'Chunky Mugs',
 'Thick ceramic mugs with rustic glaze — ideal for coffee or tea lovers.',
 'CeramiWorks', 15.00, 40);


------------------------------------------------------
-- ORDERS
------------------------------------------------------
INSERT INTO dbo.Orders (CustomerId, Status, TotalAmount)
VALUES
(1, 'Processing', 25.00),
(2, 'Completed', 40.00),
(3, 'Processing', 30.00),
(4, 'Shipped', 55.00);


------------------------------------------------------
-- ORDER ITEMS
------------------------------------------------------
INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, Quantity, UnitPrice)
VALUES
(1, 1, 'Green Fake Plant', 1, 25.00),
(2, 3, 'White Fluffy Throw', 1, 40.00),
(3, 2, 'White Cherry Blossom Fake Plant', 1, 30.00),
(4, 4, 'Chunky Mugs', 1, 15.00);


------------------------------------------------------
-- CART
------------------------------------------------------
INSERT INTO dbo.Cart (CustomerUsername, ProductLegacyId, ProductId, Quantity)
VALUES
('jsmith87', '1', 1, 2),
('mandy.andrews', '2', 2, 1),
('liam.brown23', '3', 3, 1),
('emclark91', '4', 4, 3);


------------------------------------------------------
-- UPLOADED DOCUMENTS 
------------------------------------------------------
INSERT INTO dbo.UploadedDocuments (Id, FileName, OrderId, CustomerName, BlobUrl, UploadedAt, FileSize)
VALUES
(NEWID(), 'order_1_receipt.pdf', 1, 'Jason Smith', 'https://example.blob.core.windows.net/uploads/order_1.pdf', SYSUTCDATETIME(), 20480),

(NEWID(), 'order_2_receipt.pdf', 2, 'Amanda Andrews', 'https://example.blob.core.windows.net/uploads/order_2.pdf', SYSUTCDATETIME(), 19800),

(NEWID(), 'order_3_receipt.pdf', 3, 'Liam Brown', 'https://example.blob.core.windows.net/uploads/order_3.pdf', SYSUTCDATETIME(), 22100),

(NEWID(), 'order_4_receipt.pdf', 4, 'Emily Clark', 'https://example.blob.core.windows.net/uploads/order_4.pdf', SYSUTCDATETIME(), 18750);


PRINT 'All 4-sample data inserted successfully.';
GO
