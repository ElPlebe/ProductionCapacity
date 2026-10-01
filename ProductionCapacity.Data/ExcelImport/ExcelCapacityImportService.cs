using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;
using ProductionCapacity.Engine;

namespace ProductionCapacity.Data.ExcelImport;

public sealed class ExcelCapacityImportService(PlanningDbContext db)
{
    public async Task<ExcelCapacityImportPreview> PreviewAsync(
        ExcelCapacityImportPackage package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        var warnings = package.SourceWarnings
            .Where(warning => !string.IsNullOrWhiteSpace(warning))
            .Select(warning => warning.Trim())
            .ToList();

        if (package.OperationRates.Any(row => row.StandardTimeSeconds <= 0))
        {
            warnings.Add("Hay rates con tiempo estandar menor o igual a cero.");
        }

        if (package.SmtFamilyRates.Any(row => row.UnitsPerHour <= 0))
        {
            warnings.Add("Hay UPH de SMT menor o igual a cero; revisar antes de importar.");
        }

        if (package.SmtLineFamilyRates.Any(row => row.UnitsPerHour <= 0))
        {
            warnings.Add("Hay UPH por linea SMT menor o igual a cero; revisar antes de importar.");
        }

        if (package.WeeklyDemands.Any(row => row.DemandUnits < 0))
        {
            warnings.Add("Hay demandas semanales negativas; revisar antes de importar.");
        }

        if (package.SourceWarnings.Any(warning => warning.Contains("#REF!", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Se detectaron formulas rotas con #REF!; revisar antes de confiar en esos datos.");
        }

        var smtImpact = await BuildSmtImpactAsync(package, cancellationToken);
        warnings.AddRange(await ValidateSmtImportAsync(package, cancellationToken));

        return new ExcelCapacityImportPreview(
            package.Scenarios.Count,
            package.ProductionWeeks.Count,
            package.PartNumbers.Count,
            package.WeeklyDemands.Count,
            package.Operations.Count,
            package.OperationRates.Count,
            package.OperationCapacities.Count,
            package.CrossReferences.Count,
            package.TestTimes.Count,
            package.ResourceTypes.Count,
            package.ResourceConfigurations.Count,
            package.ResourceInventories.Count,
            package.SmtFamilyVolumes.Count,
            package.SmtFamilyRates.Count,
            package.SmtLineFamilyRates.Count,
            package.SmtLineAssignments.Count,
            package.SmtCapacityTargets.Count,
            warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            smtImpact);
    }

    public async Task<ExcelCapacityImportPreview> ApplyAsync(
        ExcelCapacityImportPackage package,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(package, cancellationToken);

        await UpsertScenariosAsync(package.Scenarios, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await UpsertProductionWeeksAsync(package.ProductionWeeks, cancellationToken);
        await UpsertPartNumbersAsync(package.PartNumbers, cancellationToken);
        await UpsertOperationsAsync(package.Operations, cancellationToken);
        await UpsertSmtFamiliesAsync(package, cancellationToken);
        await UpsertSmtSubFamiliesAsync(package.PartNumbers, cancellationToken);
        await UpsertResourceTypesAsync(package.ResourceTypes, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await UpsertWeeklyDemandAsync(package.WeeklyDemands, cancellationToken);
        await UpsertSmtFamilyVolumesAsync(package.SmtFamilyVolumes, cancellationToken);
        await UpsertSmtFamilyRatesAsync(package.SmtFamilyRates, cancellationToken);
        await UpsertSmtLineFamilyRatesAsync(package.SmtLineFamilyRates, cancellationToken);
        await UpsertSmtLineAssignmentsAsync(package.SmtLineAssignments, cancellationToken);
        await UpsertSmtCapacityTargetsAsync(package.SmtCapacityTargets, cancellationToken);
        await UpsertOperationCapacitiesAsync(package.OperationCapacities, cancellationToken);
        await UpsertOperationRatesAsync(package.OperationRates, cancellationToken);
        await UpsertCrossReferencesAsync(package.CrossReferences, cancellationToken);
        await UpsertTestTimesAsync(package.TestTimes, cancellationToken);
        await UpsertResourceConfigurationsAsync(package.ResourceConfigurations, cancellationToken);
        await UpsertResourceInventoriesAsync(package.ResourceInventories, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return preview;
    }

    private async Task UpsertScenariosAsync(IEnumerable<ExcelScenarioImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var name = Required(row.ScenarioName);
            var now = DateTime.UtcNow;
            var scenario = await db.Scenarios.SingleOrDefaultAsync(item => item.ScenarioName == name, cancellationToken);
            if (scenario is null)
            {
                scenario = new Scenario
                {
                    ScenarioName = name,
                    CreatedAtUtc = now
                };
                db.Scenarios.Add(scenario);
            }

            scenario.Description = EmptyToNull(row.Description);
            scenario.IsBaseline = row.IsBaseline;
            scenario.UpdatedAtUtc = now;
        }
    }

    private async Task UpsertProductionWeeksAsync(IEnumerable<ExcelProductionWeekImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            if (scenario is null)
            {
                continue;
            }

            var weekName = Required(row.WeekName);
            var week = await db.ProductionWeeks.SingleOrDefaultAsync(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.WeekName == weekName,
                cancellationToken);
            if (week is null)
            {
                week = new ProductionWeekEntity
                {
                    ScenarioId = scenario.ScenarioId,
                    WeekName = weekName
                };
                db.ProductionWeeks.Add(week);
            }

            week.WeekStartDate = row.WeekStartDate;
            week.WorkDays = row.WorkDays;
            week.HoursPerShift = row.HoursPerShift;
            week.ShiftsPerDay = row.ShiftsPerDay <= 0 ? 1 : row.ShiftsPerDay;
            week.HolidayDays = row.HolidayDays;
            week.OvertimeHours = row.OvertimeHours;
            week.PlannedDowntimeHours = row.PlannedDowntimeHours;
        }
    }

    private async Task UpsertPartNumbersAsync(IEnumerable<ExcelPartNumberImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var partNumber = Required(row.PartNumber);
            var part = await db.PartNumbers.SingleOrDefaultAsync(item => item.PartNumber == partNumber, cancellationToken);
            if (part is null)
            {
                part = new PartNumberEntity { PartNumber = partNumber };
                db.PartNumbers.Add(part);
            }

            part.Description = Required(row.Description);
            part.BusinessUnit = EmptyToNull(row.BusinessUnit);
            part.ProductFamily = NormalizeKey(row.ProductFamily);
            part.SubFamily = NormalizeKey(row.SubFamily);
            part.DefaultYield = row.DefaultYield <= 0 ? 1m : row.DefaultYield;
            part.IsActive = true;
        }
    }

    private async Task UpsertWeeklyDemandAsync(IEnumerable<ExcelWeeklyDemandImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            var week = scenario is null ? null : await FindWeekAsync(scenario.ScenarioId, row.WeekName, cancellationToken);
            if (scenario is null || week is null)
            {
                continue;
            }

            var parts = await FindPartsForDemandAsync(row, cancellationToken);
            if (parts.Count == 0)
            {
                continue;
            }

            var demandPerPart = row.DemandUnits / parts.Count;
            foreach (var part in parts)
            {
                var demand = await db.WeeklyDemand.SingleOrDefaultAsync(item =>
                    item.ScenarioId == scenario.ScenarioId &&
                    item.ProductionWeekId == week.ProductionWeekId &&
                    item.PartNumberId == part.PartNumberId,
                    cancellationToken);
                if (demand is null)
                {
                    demand = new WeeklyDemandEntity
                    {
                        ScenarioId = scenario.ScenarioId,
                        ProductionWeekId = week.ProductionWeekId,
                        PartNumberId = part.PartNumberId
                    };
                    db.WeeklyDemand.Add(demand);
                }

                demand.DemandUnits = demandPerPart;
            }
        }
    }

    private async Task UpsertOperationsAsync(IEnumerable<ExcelOperationImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var code = Required(row.OperationCode).ToUpperInvariant();
            var operation = await db.Operations.SingleOrDefaultAsync(item => item.OperationCode == code, cancellationToken);
            if (operation is null)
            {
                operation = new PlanningOperation { OperationCode = code };
                db.Operations.Add(operation);
            }

            operation.OperationName = Required(row.OperationName);
            operation.OperationFamily = EmptyToNull(row.OperationFamily);
            operation.SequenceNumber = row.SequenceNumber <= 0 ? 1 : row.SequenceNumber;
            operation.IsActive = true;
        }
    }

    private async Task UpsertSmtFamiliesAsync(ExcelCapacityImportPackage package, CancellationToken cancellationToken)
    {
        var families = BuildSmtFamilyImports(package);
        foreach (var familyImport in families)
        {
            var family = await db.SmtFamilies.SingleOrDefaultAsync(item =>
                item.ProductFamily.ToUpper() == familyImport.ProductFamily.ToUpper(),
                cancellationToken);
            var now = DateTime.UtcNow;
            if (family is null)
            {
                family = new SmtFamilyEntity
                {
                    ProductFamily = familyImport.ProductFamily,
                    CreatedAtUtc = now
                };
                db.SmtFamilies.Add(family);
            }
            var actionType = family.SmtFamilyId == 0 ? "Creacion" : "Actualizacion";
            var oldValue = family.SmtFamilyId == 0 ? null : SummarizeSmtFamily(family);

            family.BusinessUnit = EmptyToNull(familyImport.BusinessUnit);
            family.Notes = "Importado/actualizado desde paquete SMT.";
            family.IsActive = true;
            family.UpdatedAtUtc = now;
            AddSmtAuditLog(
                "Familia SMT",
                family.ProductFamily,
                actionType,
                "Registro",
                oldValue,
                SummarizeSmtFamily(family),
                "Familia detectada en paquete SMT.");
        }
    }

    private async Task UpsertSmtSubFamiliesAsync(IEnumerable<ExcelPartNumberImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var import in BuildSmtSubFamilyImports(rows))
        {
            var subFamily = await db.SubFamilies.SingleOrDefaultAsync(item =>
                item.ProductFamily == import.ProductFamily &&
                item.SubFamilyName == import.SubFamilyName,
                cancellationToken);
            var now = DateTime.UtcNow;
            if (subFamily is null)
            {
                subFamily = new SubFamilyEntity
                {
                    ProductFamily = import.ProductFamily,
                    SubFamilyName = import.SubFamilyName,
                    CreatedAtUtc = now
                };
                db.SubFamilies.Add(subFamily);
            }

            var actionType = subFamily.SubFamilyId == 0 ? "Creacion" : "Actualizacion";
            var oldValue = subFamily.SubFamilyId == 0 ? null : SummarizeSmtSubFamily(subFamily);
            subFamily.IsActive = true;
            subFamily.Notes = "Importado/actualizado desde numeros de parte SMT.";
            subFamily.UpdatedAtUtc = now;
            AddSmtAuditLog(
                "Subfamilia SMT",
                $"{subFamily.ProductFamily}|{subFamily.SubFamilyName}",
                actionType,
                "Registro",
                oldValue,
                SummarizeSmtSubFamily(subFamily),
                "Subfamilia detectada en numeros de parte importados.");
        }
    }


    private async Task UpsertOperationRatesAsync(IEnumerable<ExcelOperationRateImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (operation is null)
            {
                continue;
            }

            var parts = await FindPartsForRateAsync(row, cancellationToken);
            foreach (var part in parts)
            {
                var rate = await db.OperationRates.SingleOrDefaultAsync(item =>
                    item.PartNumberId == part.PartNumberId &&
                    item.OperationId == operation.OperationId,
                    cancellationToken);
                if (rate is null)
                {
                    rate = new OperationRateEntity
                    {
                        PartNumberId = part.PartNumberId,
                        OperationId = operation.OperationId
                    };
                    db.OperationRates.Add(rate);
                }

                rate.StandardTimeSeconds = row.StandardTimeSeconds;
                rate.YieldFactor = row.YieldFactor;
                rate.UtilizationFactor = row.UtilizationFactor;
                rate.EfficiencyFactor = row.EfficiencyFactor;
                rate.MaxCapacityUnits = row.MaxCapacityUnits;
                rate.FixedHeadcount = row.FixedHeadcount;
                rate.IsActive = true;
            }
        }
    }

    private async Task UpsertOperationCapacitiesAsync(IEnumerable<ExcelOperationCapacityImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            var week = scenario is null ? null : await FindWeekAsync(scenario.ScenarioId, row.WeekName, cancellationToken);
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (scenario is null || week is null || operation is null)
            {
                continue;
            }

            var capacity = await db.OperationWeeklyCapacities.SingleOrDefaultAsync(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                item.OperationId == operation.OperationId,
                cancellationToken);
            if (capacity is null)
            {
                capacity = new OperationWeeklyCapacityEntity
                {
                    ScenarioId = scenario.ScenarioId,
                    ProductionWeekId = week.ProductionWeekId,
                    OperationId = operation.OperationId
                };
                db.OperationWeeklyCapacities.Add(capacity);
            }

            capacity.WorkDays = row.WorkDays;
            capacity.HoursPerShift = row.HoursPerShift;
            capacity.ShiftsPerDay = row.ShiftsPerDay;
            capacity.EquipmentQuantity = row.EquipmentQuantity;
            capacity.HolidayDays = row.HolidayDays;
            capacity.OvertimeHours = row.OvertimeHours;
            capacity.PlannedDowntimeHours = row.PlannedDowntimeHours;
        }
    }

    private async Task UpsertSmtFamilyVolumesAsync(IEnumerable<ExcelSmtFamilyVolumeImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            var week = scenario is null ? null : await FindWeekAsync(scenario.ScenarioId, row.WeekName, cancellationToken);
            if (scenario is null || week is null)
            {
                continue;
            }

            var family = Required(row.ProductFamily);
            var volume = await db.SmtFamilyVolumes.SingleOrDefaultAsync(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                item.ProductFamily == family,
                cancellationToken);
            if (volume is null)
            {
                volume = new SmtFamilyVolumeEntity
                {
                    ScenarioId = scenario.ScenarioId,
                    ProductionWeekId = week.ProductionWeekId,
                    ProductFamily = family
                };
                db.SmtFamilyVolumes.Add(volume);
            }

            volume.BusinessUnit = EmptyToNull(row.BusinessUnit);
            volume.MonthName = EmptyToNull(row.MonthName);
            volume.MonthSequence = row.MonthSequence;
            volume.MonthlyMps = row.MonthlyMps;
            volume.WeeklyMps = row.WeeklyMps;
        }
    }

    private async Task UpsertSmtFamilyRatesAsync(IEnumerable<ExcelSmtFamilyRateImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (operation is null || row.UnitsPerHour <= 0)
            {
                continue;
            }

            var family = Required(row.ProductFamily);
            var smtRate = await db.SmtFamilyRates.SingleOrDefaultAsync(item =>
                item.OperationId == operation.OperationId &&
                item.ProductFamily == family,
                cancellationToken);
            if (smtRate is null)
            {
                smtRate = new SmtFamilyRateEntity
                {
                    OperationId = operation.OperationId,
                    ProductFamily = family
                };
                db.SmtFamilyRates.Add(smtRate);
            }
            var actionType = smtRate.SmtFamilyRateId == 0 ? "Creacion" : "Actualizacion";
            var oldValue = smtRate.SmtFamilyRateId == 0 ? null : SummarizeSmtFamilyRate(smtRate);

            smtRate.UnitsPerHour = row.UnitsPerHour;
            smtRate.ChangeoverCount = row.ChangeoverCount;
            smtRate.SourcePartNumber = EmptyToNull(row.SourcePartNumber);
            smtRate.BottleneckUnitsPerHour = row.BottleneckUnitsPerHour;
            smtRate.RouterUnitsPerHour = row.RouterUnitsPerHour;
            smtRate.WaveUnitsPerHour = row.WaveUnitsPerHour;
            smtRate.PressfitUnitsPerHour = row.PressfitUnitsPerHour;
            smtRate.IsActive = true;
            AddSmtAuditLog(
                "UPH Familia SMT",
                $"{family}|{operation.OperationCode}",
                actionType,
                "Registro",
                oldValue,
                SummarizeSmtFamilyRate(smtRate),
                "UPH importado desde paquete SMT.");

            var standardTimeSeconds = Math.Round(3600m / row.UnitsPerHour, 6, MidpointRounding.AwayFromZero);
            var parts = await FindPartsByFamilyAsync(family, cancellationToken);
            foreach (var part in parts)
            {
                var rate = await db.OperationRates.SingleOrDefaultAsync(item =>
                    item.PartNumberId == part.PartNumberId &&
                    item.OperationId == operation.OperationId,
                    cancellationToken);
                if (rate is null)
                {
                    rate = new OperationRateEntity
                    {
                        PartNumberId = part.PartNumberId,
                        OperationId = operation.OperationId
                    };
                    db.OperationRates.Add(rate);
                }

                rate.StandardTimeSeconds = standardTimeSeconds;
                rate.YieldFactor = 1m;
                rate.UtilizationFactor = 1m;
                rate.EfficiencyFactor = 1m;
                rate.IsActive = true;
            }
        }
    }

    private async Task UpsertSmtLineFamilyRatesAsync(IEnumerable<ExcelSmtLineFamilyRateImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (operation is null || row.UnitsPerHour <= 0)
            {
                continue;
            }

            var family = Required(row.ProductFamily);
            var lineCode = Required(row.LineCode);
            var smtRate = await db.SmtLineFamilyRates.SingleOrDefaultAsync(item =>
                item.OperationId == operation.OperationId &&
                item.ProductFamily == family &&
                item.LineCode == lineCode,
                cancellationToken);
            if (smtRate is null)
            {
                smtRate = new SmtLineFamilyRateEntity
                {
                    OperationId = operation.OperationId,
                    ProductFamily = family,
                    LineCode = lineCode
                };
                db.SmtLineFamilyRates.Add(smtRate);
            }
            var actionType = smtRate.SmtLineFamilyRateId == 0 ? "Creacion" : "Actualizacion";
            var oldValue = smtRate.SmtLineFamilyRateId == 0 ? null : SummarizeSmtLineFamilyRate(smtRate);

            smtRate.LineNumber = row.LineNumber;
            smtRate.UnitsPerHour = row.UnitsPerHour;
            smtRate.SourcePartNumber = EmptyToNull(row.SourcePartNumber);
            smtRate.BottleneckUnitsPerHour = row.BottleneckUnitsPerHour;
            smtRate.RouterUnitsPerHour = row.RouterUnitsPerHour;
            smtRate.WaveUnitsPerHour = row.WaveUnitsPerHour;
            smtRate.PressfitUnitsPerHour = row.PressfitUnitsPerHour;
            smtRate.IsActive = true;
            AddSmtAuditLog(
                "UPH Linea SMT",
                $"{family}|{operation.OperationCode}|{lineCode}",
                actionType,
                "Registro",
                oldValue,
                SummarizeSmtLineFamilyRate(smtRate),
                "UPH por linea importado desde paquete SMT.");
        }
    }

    private async Task UpsertSmtLineAssignmentsAsync(IEnumerable<ExcelSmtLineAssignmentImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            var week = scenario is null ? null : await FindWeekAsync(scenario.ScenarioId, row.WeekName, cancellationToken);
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (scenario is null || week is null || operation is null)
            {
                continue;
            }

            var family = Required(row.ProductFamily);
            var lineCode = Required(row.LineCode);
            var assignment = await db.SmtLineAssignments.SingleOrDefaultAsync(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                item.OperationId == operation.OperationId &&
                item.ProductFamily == family &&
                item.LineCode == lineCode,
                cancellationToken);
            if (assignment is null)
            {
                assignment = new SmtLineAssignmentEntity
                {
                    ScenarioId = scenario.ScenarioId,
                    ProductionWeekId = week.ProductionWeekId,
                    OperationId = operation.OperationId,
                    ProductFamily = family,
                    LineCode = lineCode
                };
                db.SmtLineAssignments.Add(assignment);
            }

            assignment.LineNumber = row.LineNumber;
            assignment.LineDistribution = row.LineDistribution;
            assignment.MonthName = EmptyToNull(row.MonthName);
            assignment.MonthSequence = row.MonthSequence;
            assignment.IsActive = true;
        }
    }

    private async Task UpsertSmtCapacityTargetsAsync(IEnumerable<ExcelSmtCapacityTargetImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            var week = scenario is null ? null : await FindWeekAsync(scenario.ScenarioId, row.WeekName, cancellationToken);
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (scenario is null || week is null || operation is null)
            {
                continue;
            }

            var family = Required(row.ProductFamily);
            var lineCode = Required(row.LineCode);
            var target = await db.SmtCapacityTargets.SingleOrDefaultAsync(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                item.OperationId == operation.OperationId &&
                item.ProductFamily == family &&
                item.LineCode == lineCode,
                cancellationToken);
            if (target is null)
            {
                target = new SmtCapacityTargetEntity
                {
                    ScenarioId = scenario.ScenarioId,
                    ProductionWeekId = week.ProductionWeekId,
                    OperationId = operation.OperationId,
                    ProductFamily = family,
                    LineCode = lineCode
                };
                db.SmtCapacityTargets.Add(target);
            }

            target.ExpectedAssignedDemandUnits = row.ExpectedAssignedDemandUnits;
            target.ExpectedMaxCapacityUnits = row.ExpectedMaxCapacityUnits;
            target.ExpectedUtilizationPercent = row.ExpectedUtilizationPercent;
            target.ExpectedDeltaCapacityUnits = row.ExpectedDeltaCapacityUnits;
            target.SourceReference = EmptyToNull(row.SourceReference);
            target.Notes = EmptyToNull(row.Notes);
        }
    }

    private async Task UpsertCrossReferencesAsync(IEnumerable<ExcelCrossReferenceImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var part = await FindPartAsync(row.PartNumber, cancellationToken);
            if (part is null)
            {
                continue;
            }

            var type = Required(row.ReferenceType).ToUpperInvariant();
            var value = Required(row.ReferenceValue);
            var crossReference = await db.PartCrossReferences.SingleOrDefaultAsync(item =>
                item.ReferenceType == type &&
                item.ReferenceValue == value,
                cancellationToken);
            if (crossReference is null)
            {
                crossReference = new PartCrossReference
                {
                    ReferenceType = type,
                    ReferenceValue = value
                };
                db.PartCrossReferences.Add(crossReference);
            }

            crossReference.PartNumberId = part.PartNumberId;
        }
    }

    private async Task UpsertTestTimesAsync(IEnumerable<ExcelTestTimeImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            if (operation is null)
            {
                continue;
            }

            var name = Required(row.TestName);
            var parts = await FindPartsForTestTimeAsync(row, cancellationToken);
            foreach (var part in parts)
            {
                var testTime = await db.TestTimeDefinitions.SingleOrDefaultAsync(item =>
                    item.PartNumberId == part.PartNumberId &&
                    item.OperationId == operation.OperationId &&
                    item.TestName == name,
                    cancellationToken);
                if (testTime is null)
                {
                    testTime = new TestTimeDefinitionEntity
                    {
                        PartNumberId = part.PartNumberId,
                        OperationId = operation.OperationId,
                        TestName = name
                    };
                    db.TestTimeDefinitions.Add(testTime);
                }

                testTime.TestTimeMinutes = row.TestTimeMinutes;
                testTime.HandlingMinutes = row.HandlingMinutes;
                testTime.QuantityCell = row.QuantityCell;
                testTime.QuantityEquipment = row.QuantityEquipment;
                testTime.ManualStandardTimeSeconds = row.ManualStandardTimeSeconds;
                testTime.CalculatedStandardTimeSeconds = TestTimeCalculator.CalculateStandardTimeSeconds(new TestTimeDefinition(
                    row.TestTimeMinutes,
                    row.HandlingMinutes,
                    row.QuantityCell,
                    row.QuantityEquipment,
                    row.ManualStandardTimeSeconds));
                testTime.IsActive = true;
            }
        }
    }

    private async Task UpsertResourceTypesAsync(IEnumerable<ExcelResourceTypeImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var code = Required(row.ResourceCode).ToUpperInvariant();
            var resource = await db.ResourceTypes.SingleOrDefaultAsync(item => item.ResourceCode == code, cancellationToken);
            if (resource is null)
            {
                resource = new ResourceTypeEntity { ResourceCode = code };
                db.ResourceTypes.Add(resource);
            }

            resource.ResourceName = Required(row.ResourceName);
            resource.ResourceKind = Required(row.ResourceKind);
            resource.IsActive = true;
        }
    }

    private async Task UpsertResourceConfigurationsAsync(IEnumerable<ExcelResourceConfigurationImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var resource = await FindResourceTypeAsync(row.ResourceCode, cancellationToken);
            var operation = await FindOperationAsync(row.OperationCode, cancellationToken);
            var part = string.IsNullOrWhiteSpace(row.PartNumber) ? null : await FindPartAsync(row.PartNumber, cancellationToken);
            if (resource is null || operation is null)
            {
                continue;
            }

            var configuration = await db.ResourceConfigurations.FirstOrDefaultAsync(item =>
                item.ResourceTypeId == resource.ResourceTypeId &&
                item.OperationId == operation.OperationId &&
                item.PartNumberId == (part == null ? null : part.PartNumberId) &&
                item.ProductFamily == row.ProductFamily,
                cancellationToken);
            if (configuration is null)
            {
                configuration = new ResourceConfigurationEntity
                {
                    ResourceTypeId = resource.ResourceTypeId,
                    OperationId = operation.OperationId,
                    PartNumberId = part?.PartNumberId,
                    ProductFamily = EmptyToNull(row.ProductFamily)
                };
                db.ResourceConfigurations.Add(configuration);
            }

            configuration.UnitsPerResource = row.UnitsPerResource;
            configuration.Notes = EmptyToNull(row.Notes);
            configuration.IsActive = true;
        }
    }

    private async Task UpsertResourceInventoriesAsync(IEnumerable<ExcelResourceInventoryImportRow> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var scenario = await FindScenarioAsync(row.ScenarioName, cancellationToken);
            var week = scenario is null ? null : await FindWeekAsync(scenario.ScenarioId, row.WeekName, cancellationToken);
            var resource = await FindResourceTypeAsync(row.ResourceCode, cancellationToken);
            if (scenario is null || week is null || resource is null)
            {
                continue;
            }

            var inventory = await db.ResourceInventories.SingleOrDefaultAsync(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                item.ResourceTypeId == resource.ResourceTypeId,
                cancellationToken);
            if (inventory is null)
            {
                inventory = new ResourceInventoryEntity
                {
                    ScenarioId = scenario.ScenarioId,
                    ProductionWeekId = week.ProductionWeekId,
                    ResourceTypeId = resource.ResourceTypeId
                };
                db.ResourceInventories.Add(inventory);
            }

            inventory.AvailableResourceCount = row.AvailableResourceCount;
            inventory.Notes = EmptyToNull(row.Notes);
        }
    }

    private async Task<IReadOnlyList<string>> ValidateSmtImportAsync(
        ExcelCapacityImportPackage package,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var smtOperationCodes = package.SmtFamilyRates
            .Select(row => row.OperationCode)
            .Concat(package.SmtLineFamilyRates.Select(row => row.OperationCode))
            .Concat(package.SmtLineAssignments.Select(row => row.OperationCode))
            .Concat(package.SmtCapacityTargets.Select(row => row.OperationCode))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (smtOperationCodes.Count > 0)
        {
            var packageOperations = package.Operations
                .Select(row => row.OperationCode.Trim())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var databaseOperations = await db.Operations
                .AsNoTracking()
                .Select(row => row.OperationCode)
                .ToListAsync(cancellationToken);
            var knownOperations = packageOperations
                .Concat(databaseOperations)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var operationCode in smtOperationCodes.Where(code => !knownOperations.Contains(code)))
            {
                warnings.Add($"SMT: la operacion SMT '{operationCode}' no existe en catalogo ni viene en operations.csv; la importacion omitira esos registros.");
            }
        }

        foreach (var assignment in package.SmtLineAssignments.Where(row => row.LineDistribution <= 0m))
        {
            warnings.Add($"SMT: la distribucion de linea para familia '{assignment.ProductFamily}' en {assignment.LineCode}/{assignment.WeekName} debe ser mayor a cero.");
        }

        foreach (var assignment in package.SmtLineAssignments.Where(row => row.LineDistribution > 1m))
        {
            warnings.Add($"SMT: la distribucion de linea para familia '{assignment.ProductFamily}' en {assignment.LineCode}/{assignment.WeekName} parece capturada como numero entero. Usa 100%, 50% o 0%, no 100.");
        }

        foreach (var part in package.PartNumbers.Where(row =>
            !string.IsNullOrWhiteSpace(row.ProductFamily) &&
            string.IsNullOrWhiteSpace(row.SubFamily)))
        {
            warnings.Add($"SMT: el numero de parte '{part.PartNumber}' de la familia '{part.ProductFamily}' no trae subfamilia; agregala en el archivo para mantener el catalogo SMT completo.");
        }

        var importedRateFamilies = package.SmtFamilyRates
            .Where(row => row.UnitsPerHour > 0m)
            .Select(row => row.ProductFamily)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var databaseRateFamilies = await db.SmtFamilyRates
            .AsNoTracking()
            .Where(row => row.IsActive && row.UnitsPerHour > 0m)
            .Select(row => row.ProductFamily)
            .ToListAsync(cancellationToken);
        var knownRateFamilies = importedRateFamilies
            .Concat(databaseRateFamilies)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var plannedFamilies = package.SmtFamilyVolumes
            .Select(row => row.ProductFamily)
            .Concat(package.SmtLineAssignments.Select(row => row.ProductFamily))
            .Concat(package.WeeklyDemands.Where(row => !string.IsNullOrWhiteSpace(row.ProductFamily)).Select(row => row.ProductFamily!))
            .Where(family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(family => family, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var family in plannedFamilies.Where(family => !knownRateFamilies.Contains(family)))
        {
            warnings.Add($"SMT: la familia SMT '{family}' tiene volumen/asignacion pero no tiene UPH en la importacion ni en catalogos.");
        }

        return warnings;
    }

    private async Task<ExcelSmtImportImpact> BuildSmtImpactAsync(
        ExcelCapacityImportPackage package,
        CancellationToken cancellationToken)
    {
        var familyImports = BuildSmtFamilyImports(package);
        var existingFamilies = await db.SmtFamilies
            .AsNoTracking()
            .Select(row => new ExcelSmtFamilyImpactItem(row.ProductFamily, row.BusinessUnit))
            .ToListAsync(cancellationToken);
        var existingFamilyNames = existingFamilies
            .Select(row => row.ProductFamily)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newFamilies = familyImports
            .Where(row => !existingFamilyNames.Contains(row.ProductFamily))
            .Select(row => new ExcelSmtFamilyImpactItem(row.ProductFamily, row.BusinessUnit))
            .OrderBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var updatedFamilies = familyImports
            .Where(row => existingFamilyNames.Contains(row.ProductFamily))
            .Select(row => new ExcelSmtFamilyImpactItem(row.ProductFamily, row.BusinessUnit))
            .OrderBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var replacements = await BuildSmtFamilyRateReplacementsAsync(package, cancellationToken);
        var incompleteItems = await BuildSmtIncompleteItemsAsync(package, cancellationToken);

        return new ExcelSmtImportImpact(newFamilies, updatedFamilies, replacements, incompleteItems);
    }

    private async Task<IReadOnlyList<ExcelSmtFamilyRateReplacement>> BuildSmtFamilyRateReplacementsAsync(
        ExcelCapacityImportPackage package,
        CancellationToken cancellationToken)
    {
        var operationIdsByCode = await db.Operations
            .AsNoTracking()
            .Select(row => new { row.OperationCode, row.OperationId })
            .ToDictionaryAsync(row => row.OperationCode, row => row.OperationId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var replacements = new List<ExcelSmtFamilyRateReplacement>();

        foreach (var importedRate in package.SmtFamilyRates.Where(row => row.UnitsPerHour > 0m))
        {
            if (!operationIdsByCode.TryGetValue(importedRate.OperationCode, out var operationId))
            {
                continue;
            }

            var productFamily = Required(importedRate.ProductFamily).ToUpperInvariant();
            var existingRate = await db.SmtFamilyRates
                .AsNoTracking()
                .SingleOrDefaultAsync(row =>
                    row.ProductFamily == productFamily &&
                    row.OperationId == operationId &&
                    row.IsActive,
                    cancellationToken);
            if (existingRate is null || existingRate.UnitsPerHour == importedRate.UnitsPerHour)
            {
                continue;
            }

            replacements.Add(new ExcelSmtFamilyRateReplacement(
                productFamily,
                importedRate.OperationCode.Trim().ToUpperInvariant(),
                existingRate.UnitsPerHour,
                importedRate.UnitsPerHour));
        }

        return replacements
            .OrderBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.OperationCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<ExcelSmtImportIssue>> BuildSmtIncompleteItemsAsync(
        ExcelCapacityImportPackage package,
        CancellationToken cancellationToken)
    {
        var issues = new List<ExcelSmtImportIssue>();
        var smtOperationCodes = package.SmtFamilyRates
            .Select(row => row.OperationCode)
            .Concat(package.SmtLineFamilyRates.Select(row => row.OperationCode))
            .Concat(package.SmtLineAssignments.Select(row => row.OperationCode))
            .Concat(package.SmtCapacityTargets.Select(row => row.OperationCode))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var packageOperations = package.Operations
            .Select(row => row.OperationCode.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var databaseOperations = await db.Operations
            .AsNoTracking()
            .Select(row => row.OperationCode)
            .ToListAsync(cancellationToken);
        var knownOperations = packageOperations
            .Concat(databaseOperations)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var operationCode in smtOperationCodes.Where(code => !knownOperations.Contains(code)))
        {
            issues.Add(new ExcelSmtImportIssue(
                "Operacion",
                "-",
                $"La operacion SMT '{operationCode}' no existe en catalogo ni viene en operations.csv."));
        }

        foreach (var assignment in package.SmtLineAssignments.Where(row => row.LineDistribution <= 0m))
        {
            issues.Add(new ExcelSmtImportIssue(
                "Distribucion",
                assignment.ProductFamily,
                $"La distribucion para {assignment.ProductFamily} en {assignment.LineCode}/{assignment.WeekName} debe ser mayor a cero."));
        }

        foreach (var assignment in package.SmtLineAssignments.Where(row => row.LineDistribution > 1m))
        {
            issues.Add(new ExcelSmtImportIssue(
                "Distribucion",
                assignment.ProductFamily,
                $"La distribucion para {assignment.ProductFamily} en {assignment.LineCode}/{assignment.WeekName} parece capturada como numero entero. Usa 100%, 50% o 0%, no 100."));
        }

        var importedRateFamilies = package.SmtFamilyRates
            .Where(row => row.UnitsPerHour > 0m)
            .Select(row => row.ProductFamily)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var databaseRateFamilies = await db.SmtFamilyRates
            .AsNoTracking()
            .Where(row => row.IsActive && row.UnitsPerHour > 0m)
            .Select(row => row.ProductFamily)
            .ToListAsync(cancellationToken);
        var knownRateFamilies = importedRateFamilies
            .Concat(databaseRateFamilies)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var plannedFamilies = package.SmtFamilyVolumes
            .Select(row => row.ProductFamily)
            .Concat(package.SmtLineAssignments.Select(row => row.ProductFamily))
            .Concat(package.WeeklyDemands.Where(row => !string.IsNullOrWhiteSpace(row.ProductFamily)).Select(row => row.ProductFamily!))
            .Where(family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(family => family, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var family in plannedFamilies.Where(family => !knownRateFamilies.Contains(family)))
        {
            issues.Add(new ExcelSmtImportIssue(
                "UPH",
                family,
                $"La familia SMT '{family}' tiene volumen/asignacion pero no tiene UPH en la importacion ni en catalogos."));
        }

        return issues;
    }

    private Task<Scenario?> FindScenarioAsync(string scenarioName, CancellationToken cancellationToken) =>
        db.Scenarios.SingleOrDefaultAsync(item => item.ScenarioName == Required(scenarioName), cancellationToken);

    private Task<ProductionWeekEntity?> FindWeekAsync(int scenarioId, string weekName, CancellationToken cancellationToken) =>
        db.ProductionWeeks.SingleOrDefaultAsync(item =>
            item.ScenarioId == scenarioId &&
            item.WeekName == Required(weekName),
            cancellationToken);

    private Task<PartNumberEntity?> FindPartAsync(string partNumber, CancellationToken cancellationToken) =>
        db.PartNumbers.SingleOrDefaultAsync(item => item.PartNumber == Required(partNumber), cancellationToken);

    private async Task<IReadOnlyList<PartNumberEntity>> FindPartsForDemandAsync(ExcelWeeklyDemandImportRow row, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(row.PartNumber))
        {
            var part = await FindPartAsync(row.PartNumber, cancellationToken);
            return part is null ? [] : [part];
        }

        return await FindPartsByFamilyAsync(row.ProductFamily, cancellationToken);
    }

    private async Task<IReadOnlyList<PartNumberEntity>> FindPartsForRateAsync(ExcelOperationRateImportRow row, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(row.PartNumber))
        {
            var part = await FindPartAsync(row.PartNumber, cancellationToken);
            return part is null ? [] : [part];
        }

        return await FindPartsByFamilyAsync(row.ProductFamily, cancellationToken);
    }

    private async Task<IReadOnlyList<PartNumberEntity>> FindPartsForTestTimeAsync(ExcelTestTimeImportRow row, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(row.PartNumber))
        {
            var part = await FindPartAsync(row.PartNumber, cancellationToken);
            return part is null ? [] : [part];
        }

        return await FindPartsByFamilyAsync(row.ProductFamily, cancellationToken);
    }

    private Task<List<PartNumberEntity>> FindPartsByFamilyAsync(string? productFamily, CancellationToken cancellationToken)
    {
        var family = Required(productFamily ?? string.Empty);
        return db.PartNumbers
            .Where(item => item.ProductFamily == family && item.IsActive)
            .OrderBy(item => item.PartNumber)
            .ToListAsync(cancellationToken);
    }

    private Task<PlanningOperation?> FindOperationAsync(string operationCode, CancellationToken cancellationToken)
    {
        var code = Required(operationCode).ToUpperInvariant();
        return db.Operations.SingleOrDefaultAsync(item => item.OperationCode == code, cancellationToken);
    }

    private Task<ResourceTypeEntity?> FindResourceTypeAsync(string resourceCode, CancellationToken cancellationToken)
    {
        var code = Required(resourceCode).ToUpperInvariant();
        return db.ResourceTypes.SingleOrDefaultAsync(item => item.ResourceCode == code, cancellationToken);
    }

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Required import value is missing.") : value.Trim();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private void AddSmtAuditLog(
        string entityType,
        string entityKey,
        string actionType,
        string fieldName,
        string? oldValue,
        string? newValue,
        string notes) =>
        db.SmtAuditLogs.Add(new SmtAuditLogEntity
        {
            EntityType = entityType,
            EntityKey = entityKey,
            ActionType = actionType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = "Importacion SMT",
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow
        });

    private static string SummarizeSmtFamily(SmtFamilyEntity entity) =>
        $"Familia {entity.ProductFamily}; BU {entity.BusinessUnit ?? "-"}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeSmtSubFamily(SubFamilyEntity entity) =>
        $"Familia {entity.ProductFamily}; Subfamilia {entity.SubFamilyName}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeSmtFamilyRate(SmtFamilyRateEntity entity) =>
        $"UPH {entity.UnitsPerHour:0.##}; CO {entity.ChangeoverCount:0.##}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeSmtLineFamilyRate(SmtLineFamilyRateEntity entity) =>
        $"Linea {entity.LineCode}; UPH {entity.UnitsPerHour:0.##}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static IReadOnlyList<SmtFamilyImport> BuildSmtFamilyImports(ExcelCapacityImportPackage package)
    {
        var families = new Dictionary<string, SmtFamilyImport>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in package.PartNumbers.Where(row => !string.IsNullOrWhiteSpace(row.ProductFamily)))
        {
            AddSmtFamily(families, row.ProductFamily!, row.BusinessUnit);
        }

        foreach (var row in package.SmtFamilyVolumes)
        {
            AddSmtFamily(families, row.ProductFamily, row.BusinessUnit);
        }

        foreach (var row in package.WeeklyDemands.Where(row => !string.IsNullOrWhiteSpace(row.ProductFamily)))
        {
            AddSmtFamily(families, row.ProductFamily!, null);
        }

        foreach (var row in package.SmtFamilyRates)
        {
            AddSmtFamily(families, row.ProductFamily, null);
        }

        foreach (var row in package.SmtLineFamilyRates)
        {
            AddSmtFamily(families, row.ProductFamily, null);
        }

        foreach (var row in package.SmtLineAssignments)
        {
            AddSmtFamily(families, row.ProductFamily, null);
        }

        foreach (var row in package.SmtCapacityTargets)
        {
            AddSmtFamily(families, row.ProductFamily, null);
        }

        return families.Values
            .OrderBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddSmtFamily(IDictionary<string, SmtFamilyImport> families, string productFamily, string? businessUnit)
    {
        productFamily = Required(productFamily);
        if (!families.TryGetValue(productFamily, out var existing))
        {
            families.Add(productFamily, new SmtFamilyImport(productFamily, EmptyToNull(businessUnit)));
            return;
        }

        if (string.IsNullOrWhiteSpace(existing.BusinessUnit) && !string.IsNullOrWhiteSpace(businessUnit))
        {
            families[existing.ProductFamily] = existing with { BusinessUnit = businessUnit.Trim() };
        }
    }

    private static IReadOnlyList<SmtSubFamilyImport> BuildSmtSubFamilyImports(IEnumerable<ExcelPartNumberImportRow> rows)
    {
        var subFamilies = new Dictionary<string, SmtSubFamilyImport>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Where(row =>
            !string.IsNullOrWhiteSpace(row.ProductFamily) &&
            !string.IsNullOrWhiteSpace(row.SubFamily)))
        {
            var productFamily = Required(row.ProductFamily!).ToUpperInvariant();
            var subFamilyName = Required(row.SubFamily!).ToUpperInvariant();
            subFamilies.TryAdd(
                $"{productFamily}|{subFamilyName}",
                new SmtSubFamilyImport(productFamily, subFamilyName));
        }

        return subFamilies.Values
            .OrderBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.SubFamilyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record SmtFamilyImport(string ProductFamily, string? BusinessUnit);
    private sealed record SmtSubFamilyImport(string ProductFamily, string SubFamilyName);
}
