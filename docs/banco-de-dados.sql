-- =====================================================================================
-- Atividade 5 - Script do banco de dados (SQL Server)
-- Gerado a partir das migrations do Entity Framework Core:
--     dotnet ef migrations script --idempotent
--
-- OPCIONAL: a API já cria/atualiza o banco automaticamente ao iniciar (Database.Migrate).
-- Use este script somente se preferir criar o banco pelo SSMS / Azure Data Studio.
-- O usuário administrador NÃO é inserido aqui: ele é criado pela API na primeira execução,
-- porque a senha precisa ser gravada como hash (PBKDF2), nunca em texto puro.
-- =====================================================================================
IF DB_ID(N'Atividade5ApiDb') IS NULL
    CREATE DATABASE [Atividade5ApiDb];
GO

USE [Atividade5ApiDb];
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112012936_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Produtos] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Descricao] nvarchar(500) NULL,
        [Preco] decimal(18,2) NOT NULL,
        [Estoque] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [AtualizadoEm] datetime2 NULL,
        CONSTRAINT [PK_Produtos] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112012936_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [SenhaHash] nvarchar(256) NOT NULL,
        [Perfil] nvarchar(30) NOT NULL DEFAULT N'Usuario',
        [CriadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112012936_CriacaoInicial'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AtualizadoEm', N'CriadoEm', N'Descricao', N'Estoque', N'Nome', N'Preco') AND [object_id] = OBJECT_ID(N'[Produtos]'))
        SET IDENTITY_INSERT [Produtos] ON;
    EXEC(N'INSERT INTO [Produtos] ([Id], [AtualizadoEm], [CriadoEm], [Descricao], [Estoque], [Nome], [Preco])
    VALUES (1, NULL, ''2026-01-01T00:00:00.0000000Z'', N''Notebook 15,6" 16 GB RAM'', 10, N''Notebook'', 4599.9),
    (2, NULL, ''2026-01-01T00:00:00.0000000Z'', N''Mouse óptico 1600 DPI'', 50, N''Mouse sem fio'', 89.9),
    (3, NULL, ''2026-01-01T00:00:00.0000000Z'', N''Teclado ABNT2 switch blue'', 25, N''Teclado mecânico'', 299.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AtualizadoEm', N'CriadoEm', N'Descricao', N'Estoque', N'Nome', N'Preco') AND [object_id] = OBJECT_ID(N'[Produtos]'))
        SET IDENTITY_INSERT [Produtos] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112012936_CriacaoInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112012936_CriacaoInicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112012936_CriacaoInicial', N'8.0.11');
END;
GO

COMMIT;
GO


-- Consultas úteis para conferir a segurança do armazenamento:
-- SELECT Id, Nome, Email, Perfil, SenhaHash FROM Usuarios;   -- repare que a senha está em hash
-- SELECT * FROM Produtos;
