IF SCHEMA_ID(N'planning') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA planning');
END;
GO

IF OBJECT_ID(N'planning.SmtFamilyVolumes', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtFamilyVolumes
    (
        SmtFamilyVolumeId  int IDENTITY(1,1) NOT NULL,
        ScenarioId         int NOT NULL,
        ProductionWeekId   int NOT NULL,
        ProductFamily      nvarchar(120) NOT NULL,
        BusinessUnit       nvarchar(80) NULL,
        MonthName          nvarchar(30) NULL,
        MonthSequence      int NOT NULL CONSTRAINT DF_SmtFamilyVolumes_MonthSequence DEFAULT (0),
        MonthlyMps         decimal(18,6) NOT NULL CONSTRAINT DF_SmtFamilyVolumes_MonthlyMps DEFAULT (0),
        WeeklyMps          decimal(18,6) NOT NULL CONSTRAINT DF_SmtFamilyVolumes_WeeklyMps DEFAULT (0),
        CONSTRAINT PK_SmtFamilyVolumes PRIMARY KEY CLUSTERED (SmtFamilyVolumeId),
        CONSTRAINT FK_SmtFamilyVolumes_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_SmtFamilyVolumes_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT UQ_SmtFamilyVolumes_Scenario_Week_Family UNIQUE (ScenarioId, ProductionWeekId, ProductFamily),
        CONSTRAINT CK_SmtFamilyVolumes_Mps CHECK (MonthlyMps >= 0 AND WeeklyMps >= 0)
    );
END;
GO

IF OBJECT_ID(N'planning.SmtFamilyRates', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtFamilyRates
    (
        SmtFamilyRateId       int IDENTITY(1,1) NOT NULL,
        OperationId           int NOT NULL,
        ProductFamily         nvarchar(120) NOT NULL,
        UnitsPerHour          decimal(18,6) NOT NULL,
        ChangeoverCount       decimal(18,6) NOT NULL CONSTRAINT DF_SmtFamilyRates_ChangeoverCount DEFAULT (0),
        SourcePartNumber      nvarchar(80) NULL,
        BottleneckUnitsPerHour decimal(18,6) NULL,
        RouterUnitsPerHour    decimal(18,6) NULL,
        WaveUnitsPerHour      decimal(18,6) NULL,
        PressfitUnitsPerHour  decimal(18,6) NULL,
        IsActive              bit NOT NULL CONSTRAINT DF_SmtFamilyRates_IsActive DEFAULT (1),
        CONSTRAINT PK_SmtFamilyRates PRIMARY KEY CLUSTERED (SmtFamilyRateId),
        CONSTRAINT FK_SmtFamilyRates_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_SmtFamilyRates_Operation_Family UNIQUE (OperationId, ProductFamily),
        CONSTRAINT CK_SmtFamilyRates_UnitsPerHour CHECK (UnitsPerHour > 0),
        CONSTRAINT CK_SmtFamilyRates_ChangeoverCount CHECK (ChangeoverCount >= 0)
    );
END;
GO

IF OBJECT_ID(N'planning.SmtLineAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtLineAssignments
    (
        SmtLineAssignmentId int IDENTITY(1,1) NOT NULL,
        ScenarioId          int NOT NULL,
        ProductionWeekId    int NOT NULL,
        OperationId         int NOT NULL,
        ProductFamily       nvarchar(120) NOT NULL,
        LineCode            nvarchar(60) NOT NULL,
        LineNumber          int NOT NULL CONSTRAINT DF_SmtLineAssignments_LineNumber DEFAULT (0),
        LineDistribution    decimal(18,6) NOT NULL,
        MonthName           nvarchar(30) NULL,
        MonthSequence       int NOT NULL CONSTRAINT DF_SmtLineAssignments_MonthSequence DEFAULT (0),
        IsActive            bit NOT NULL CONSTRAINT DF_SmtLineAssignments_IsActive DEFAULT (1),
        CONSTRAINT PK_SmtLineAssignments PRIMARY KEY CLUSTERED (SmtLineAssignmentId),
        CONSTRAINT FK_SmtLineAssignments_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_SmtLineAssignments_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT FK_SmtLineAssignments_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_SmtLineAssignments_Scenario_Week_Operation_Family_Line UNIQUE
            (ScenarioId, ProductionWeekId, OperationId, ProductFamily, LineCode),
        CONSTRAINT CK_SmtLineAssignments_LineDistribution CHECK (LineDistribution >= 0 AND LineDistribution <= 1)
    );
END;
GO
