IF SCHEMA_ID(N'planning') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA planning');
END;
GO

IF OBJECT_ID(N'planning.SmtAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtAuditLogs
    (
        SmtAuditLogId bigint IDENTITY(1,1) NOT NULL,
        EntityType     nvarchar(80)  NOT NULL,
        EntityKey      nvarchar(180) NOT NULL,
        ActionType     nvarchar(40)  NOT NULL,
        FieldName      nvarchar(80)  NULL,
        OldValue       nvarchar(800) NULL,
        NewValue       nvarchar(800) NULL,
        ChangedBy      nvarchar(80)  NOT NULL,
        Notes          nvarchar(400) NULL,
        CreatedAtUtc   datetime2(0)  NOT NULL CONSTRAINT DF_SmtAuditLogs_CreatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_SmtAuditLogs PRIMARY KEY CLUSTERED (SmtAuditLogId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SmtAuditLogs_CreatedAtUtc'
      AND object_id = OBJECT_ID(N'planning.SmtAuditLogs')
)
BEGIN
    CREATE INDEX IX_SmtAuditLogs_CreatedAtUtc
        ON planning.SmtAuditLogs (CreatedAtUtc DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SmtAuditLogs_EntityType_EntityKey'
      AND object_id = OBJECT_ID(N'planning.SmtAuditLogs')
)
BEGIN
    CREATE INDEX IX_SmtAuditLogs_EntityType_EntityKey
        ON planning.SmtAuditLogs (EntityType, EntityKey);
END;
GO
