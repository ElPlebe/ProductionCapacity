/*
    ProductionCapacity - Excel import, crossreferences, test times and physical resources

    Run this script after:
        001_create_planning_schema.sql
        002_add_rack_constraints.sql
        003_add_excel_parity_operations.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'planning.TestTimeDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE planning.TestTimeDefinitions
    (
        TestTimeDefinitionId          int IDENTITY(1,1) NOT NULL,
        PartNumberId                  int NOT NULL,
        OperationId                   int NOT NULL,
        TestName                      nvarchar(160) NOT NULL,
        TestTimeMinutes               decimal(18,6) NOT NULL,
        HandlingMinutes               decimal(18,6) NOT NULL,
        QuantityCell                  decimal(18,6) NOT NULL,
        QuantityEquipment             decimal(18,6) NOT NULL,
        ManualStandardTimeSeconds     decimal(18,6) NULL,
        CalculatedStandardTimeSeconds decimal(18,6) NOT NULL,
        IsActive                      bit NOT NULL CONSTRAINT DF_TestTimeDefinitions_IsActive DEFAULT (1),
        CreatedAtUtc                  datetime2(0) NOT NULL CONSTRAINT DF_TestTimeDefinitions_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc                  datetime2(0) NOT NULL CONSTRAINT DF_TestTimeDefinitions_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_TestTimeDefinitions PRIMARY KEY CLUSTERED (TestTimeDefinitionId),
        CONSTRAINT FK_TestTimeDefinitions_PartNumbers FOREIGN KEY (PartNumberId)
            REFERENCES planning.PartNumbers (PartNumberId),
        CONSTRAINT FK_TestTimeDefinitions_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_TestTimeDefinitions_Part_Operation_Test UNIQUE (PartNumberId, OperationId, TestName),
        CONSTRAINT CK_TestTimeDefinitions_TestTimeMinutes CHECK (TestTimeMinutes >= 0),
        CONSTRAINT CK_TestTimeDefinitions_HandlingMinutes CHECK (HandlingMinutes >= 0),
        CONSTRAINT CK_TestTimeDefinitions_QuantityCell CHECK (QuantityCell > 0),
        CONSTRAINT CK_TestTimeDefinitions_QuantityEquipment CHECK (QuantityEquipment > 0),
        CONSTRAINT CK_TestTimeDefinitions_ManualStandardTimeSeconds CHECK (ManualStandardTimeSeconds IS NULL OR ManualStandardTimeSeconds > 0),
        CONSTRAINT CK_TestTimeDefinitions_CalculatedStandardTimeSeconds CHECK (CalculatedStandardTimeSeconds > 0)
    );
END;
GO

IF OBJECT_ID(N'planning.ResourceTypes', N'U') IS NULL
BEGIN
    CREATE TABLE planning.ResourceTypes
    (
        ResourceTypeId int IDENTITY(1,1) NOT NULL,
        ResourceCode   nvarchar(60) NOT NULL,
        ResourceName   nvarchar(160) NOT NULL,
        ResourceKind   nvarchar(40) NOT NULL,
        IsActive       bit NOT NULL CONSTRAINT DF_ResourceTypes_IsActive DEFAULT (1),
        CreatedAtUtc   datetime2(0) NOT NULL CONSTRAINT DF_ResourceTypes_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc   datetime2(0) NOT NULL CONSTRAINT DF_ResourceTypes_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_ResourceTypes PRIMARY KEY CLUSTERED (ResourceTypeId),
        CONSTRAINT UQ_ResourceTypes_ResourceCode UNIQUE (ResourceCode),
        CONSTRAINT CK_ResourceTypes_ResourceKind CHECK (ResourceKind IN (N'Machine', N'Rack', N'Chamber', N'Chasis', N'Accessory', N'Golden'))
    );
END;
GO

IF OBJECT_ID(N'planning.ResourceConfigurations', N'U') IS NULL
BEGIN
    CREATE TABLE planning.ResourceConfigurations
    (
        ResourceConfigurationId int IDENTITY(1,1) NOT NULL,
        ResourceTypeId          int NOT NULL,
        OperationId             int NOT NULL,
        PartNumberId            int NULL,
        ProductFamily           nvarchar(120) NULL,
        UnitsPerResource        decimal(18,6) NOT NULL,
        IsActive                bit NOT NULL CONSTRAINT DF_ResourceConfigurations_IsActive DEFAULT (1),
        Notes                   nvarchar(400) NULL,
        CreatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ResourceConfigurations_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ResourceConfigurations_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_ResourceConfigurations PRIMARY KEY CLUSTERED (ResourceConfigurationId),
        CONSTRAINT FK_ResourceConfigurations_ResourceTypes FOREIGN KEY (ResourceTypeId)
            REFERENCES planning.ResourceTypes (ResourceTypeId),
        CONSTRAINT FK_ResourceConfigurations_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT FK_ResourceConfigurations_PartNumbers FOREIGN KEY (PartNumberId)
            REFERENCES planning.PartNumbers (PartNumberId),
        CONSTRAINT CK_ResourceConfigurations_UnitsPerResource CHECK (UnitsPerResource > 0)
    );
END;
GO

IF OBJECT_ID(N'planning.ResourceInventories', N'U') IS NULL
BEGIN
    CREATE TABLE planning.ResourceInventories
    (
        ResourceInventoryId     int IDENTITY(1,1) NOT NULL,
        ScenarioId              int NOT NULL,
        ProductionWeekId        int NOT NULL,
        ResourceTypeId          int NOT NULL,
        AvailableResourceCount  int NOT NULL,
        Notes                   nvarchar(400) NULL,
        CreatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ResourceInventories_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ResourceInventories_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_ResourceInventories PRIMARY KEY CLUSTERED (ResourceInventoryId),
        CONSTRAINT FK_ResourceInventories_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_ResourceInventories_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT FK_ResourceInventories_ResourceTypes FOREIGN KEY (ResourceTypeId)
            REFERENCES planning.ResourceTypes (ResourceTypeId),
        CONSTRAINT UQ_ResourceInventories_Scenario_Week_Resource UNIQUE (ScenarioId, ProductionWeekId, ResourceTypeId),
        CONSTRAINT CK_ResourceInventories_AvailableResourceCount CHECK (AvailableResourceCount >= 0)
    );
END;
GO

INSERT INTO planning.ResourceTypes (ResourceCode, ResourceName, ResourceKind)
SELECT N'RACK-GENERICO', N'Rack generico', N'Rack'
WHERE NOT EXISTS (SELECT 1 FROM planning.ResourceTypes WHERE ResourceCode = N'RACK-GENERICO');

INSERT INTO planning.ResourceTypes (ResourceCode, ResourceName, ResourceKind)
SELECT N'CHAMBER-GENERICO', N'Chamber generico', N'Chamber'
WHERE NOT EXISTS (SELECT 1 FROM planning.ResourceTypes WHERE ResourceCode = N'CHAMBER-GENERICO');

INSERT INTO planning.ResourceTypes (ResourceCode, ResourceName, ResourceKind)
SELECT N'CHASIS-GENERICO', N'Chasis generico', N'Chasis'
WHERE NOT EXISTS (SELECT 1 FROM planning.ResourceTypes WHERE ResourceCode = N'CHASIS-GENERICO');
GO
