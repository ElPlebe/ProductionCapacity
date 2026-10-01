using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data;

public sealed class SmtAdministrationService(IDbContextFactory<PlanningDbContext> dbFactory)
{
    public async Task<SmtAdministrationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var persistedFamilies = await db.SmtFamilies
            .AsNoTracking()
            .OrderBy(item => item.ProductFamily)
            .Select(item => new SmtFamilyCatalogItem
            {
                SmtFamilyId = item.SmtFamilyId,
                ProductFamily = item.ProductFamily,
                BusinessUnit = item.BusinessUnit,
                Notes = item.Notes,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var importedFamilyNames = await GetImportedFamilyNamesAsync(db, cancellationToken);
        foreach (var familyName in importedFamilyNames)
        {
            if (persistedFamilies.Any(item => string.Equals(item.ProductFamily, familyName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            persistedFamilies.Add(new SmtFamilyCatalogItem
            {
                ProductFamily = familyName,
                BusinessUnit = await FindBusinessUnitAsync(db, familyName, cancellationToken),
                IsActive = true
            });
        }

        var partCounts = await db.PartNumbers
            .AsNoTracking()
            .Where(item => item.ProductFamily != null && item.IsActive)
            .GroupBy(item => item.ProductFamily!)
            .Select(group => new { ProductFamily = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        foreach (var family in persistedFamilies)
        {
            family.PartNumberCount = partCounts
                .Where(item => string.Equals(item.ProductFamily, family.ProductFamily, StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Count);
        }

        var scenarios = await db.Scenarios
            .AsNoTracking()
            .OrderByDescending(item => item.IsBaseline)
            .ThenBy(item => item.ScenarioName)
            .Select(item => new SmtScenarioLookupItem
            {
                ScenarioId = item.ScenarioId,
                ScenarioName = item.ScenarioName
            })
            .ToListAsync(cancellationToken);

        var productionWeeks = await (
            from week in db.ProductionWeeks.AsNoTracking()
            join scenario in db.Scenarios.AsNoTracking() on week.ScenarioId equals scenario.ScenarioId
            orderby scenario.ScenarioName, week.ProductionWeekId
            select new SmtProductionWeekLookupItem
            {
                ProductionWeekId = week.ProductionWeekId,
                ScenarioId = week.ScenarioId,
                ScenarioName = scenario.ScenarioName,
                WeekName = week.WeekName
            })
            .ToListAsync(cancellationToken);

        var operations = await db.Operations
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SequenceNumber)
            .ThenBy(item => item.OperationCode)
            .Select(item => new SmtOperationLookupItem
            {
                OperationId = item.OperationId,
                OperationCode = item.OperationCode,
                OperationName = item.OperationName
            })
            .ToListAsync(cancellationToken);

        var familyRates = await (
            from rate in db.SmtFamilyRates.AsNoTracking()
            join operation in db.Operations.AsNoTracking() on rate.OperationId equals operation.OperationId
            orderby rate.ProductFamily, operation.OperationCode
            select new SmtFamilyRateCatalogItem
            {
                SmtFamilyRateId = rate.SmtFamilyRateId,
                ProductFamily = rate.ProductFamily,
                OperationId = rate.OperationId,
                OperationCode = operation.OperationCode,
                UnitsPerHour = rate.UnitsPerHour,
                ChangeoverCount = rate.ChangeoverCount,
                SourcePartNumber = rate.SourcePartNumber,
                BottleneckUnitsPerHour = rate.BottleneckUnitsPerHour,
                RouterUnitsPerHour = rate.RouterUnitsPerHour,
                WaveUnitsPerHour = rate.WaveUnitsPerHour,
                PressfitUnitsPerHour = rate.PressfitUnitsPerHour,
                IsActive = rate.IsActive
            })
            .ToListAsync(cancellationToken);

        var lineRates = await (
            from rate in db.SmtLineFamilyRates.AsNoTracking()
            join operation in db.Operations.AsNoTracking() on rate.OperationId equals operation.OperationId
            orderby rate.ProductFamily, rate.LineNumber, rate.LineCode
            select new SmtLineFamilyRateCatalogItem
            {
                SmtLineFamilyRateId = rate.SmtLineFamilyRateId,
                ProductFamily = rate.ProductFamily,
                OperationId = rate.OperationId,
                OperationCode = operation.OperationCode,
                LineCode = rate.LineCode,
                LineNumber = rate.LineNumber,
                UnitsPerHour = rate.UnitsPerHour,
                SourcePartNumber = rate.SourcePartNumber,
                BottleneckUnitsPerHour = rate.BottleneckUnitsPerHour,
                RouterUnitsPerHour = rate.RouterUnitsPerHour,
                WaveUnitsPerHour = rate.WaveUnitsPerHour,
                PressfitUnitsPerHour = rate.PressfitUnitsPerHour,
                IsActive = rate.IsActive
            })
            .ToListAsync(cancellationToken);

        var lineAssignments = await (
            from assignment in db.SmtLineAssignments.AsNoTracking()
            join scenario in db.Scenarios.AsNoTracking() on assignment.ScenarioId equals scenario.ScenarioId
            join week in db.ProductionWeeks.AsNoTracking() on assignment.ProductionWeekId equals week.ProductionWeekId
            join operation in db.Operations.AsNoTracking() on assignment.OperationId equals operation.OperationId
            orderby scenario.ScenarioName, week.ProductionWeekId, assignment.ProductFamily, assignment.LineNumber
            select new SmtLineAssignmentCatalogItem
            {
                SmtLineAssignmentId = assignment.SmtLineAssignmentId,
                ScenarioId = assignment.ScenarioId,
                ScenarioName = scenario.ScenarioName,
                ProductionWeekId = assignment.ProductionWeekId,
                WeekName = week.WeekName,
                OperationId = assignment.OperationId,
                OperationCode = operation.OperationCode,
                ProductFamily = assignment.ProductFamily,
                LineCode = assignment.LineCode,
                LineNumber = assignment.LineNumber,
                LineDistributionPercent = assignment.LineDistribution * 100m,
                MonthName = assignment.MonthName,
                MonthSequence = assignment.MonthSequence,
                IsActive = assignment.IsActive
            })
            .ToListAsync(cancellationToken);

        var operationCapacities = await (
            from capacity in db.OperationWeeklyCapacities.AsNoTracking()
            join scenario in db.Scenarios.AsNoTracking() on capacity.ScenarioId equals scenario.ScenarioId
            join week in db.ProductionWeeks.AsNoTracking() on capacity.ProductionWeekId equals week.ProductionWeekId
            join operation in db.Operations.AsNoTracking() on capacity.OperationId equals operation.OperationId
            orderby scenario.ScenarioName, week.ProductionWeekId, operation.OperationCode
            select new SmtOperationCapacityCatalogItem
            {
                OperationWeeklyCapacityId = capacity.OperationWeeklyCapacityId,
                ScenarioId = capacity.ScenarioId,
                ScenarioName = scenario.ScenarioName,
                ProductionWeekId = capacity.ProductionWeekId,
                WeekName = week.WeekName,
                OperationId = capacity.OperationId,
                OperationCode = operation.OperationCode,
                WorkDays = capacity.WorkDays,
                HoursPerShift = capacity.HoursPerShift,
                ShiftsPerDay = capacity.ShiftsPerDay,
                EquipmentQuantity = capacity.EquipmentQuantity,
                HolidayDays = capacity.HolidayDays,
                OvertimeHours = capacity.OvertimeHours,
                PlannedDowntimeHours = capacity.PlannedDowntimeHours
            })
            .ToListAsync(cancellationToken);

        var capacityTargets = await (
            from target in db.SmtCapacityTargets.AsNoTracking()
            join scenario in db.Scenarios.AsNoTracking() on target.ScenarioId equals scenario.ScenarioId
            join week in db.ProductionWeeks.AsNoTracking() on target.ProductionWeekId equals week.ProductionWeekId
            join operation in db.Operations.AsNoTracking() on target.OperationId equals operation.OperationId
            orderby scenario.ScenarioName, week.ProductionWeekId, target.LineCode, target.ProductFamily
            select new SmtCapacityTargetCatalogItem
            {
                SmtCapacityTargetId = target.SmtCapacityTargetId,
                ScenarioId = target.ScenarioId,
                ScenarioName = scenario.ScenarioName,
                ProductionWeekId = target.ProductionWeekId,
                WeekName = week.WeekName,
                OperationId = target.OperationId,
                OperationCode = operation.OperationCode,
                ProductFamily = target.ProductFamily,
                LineCode = target.LineCode,
                ExpectedAssignedDemandUnits = target.ExpectedAssignedDemandUnits,
                ExpectedMaxCapacityUnits = target.ExpectedMaxCapacityUnits,
                ExpectedUtilizationPercent = target.ExpectedUtilizationPercent,
                ExpectedDeltaCapacityUnits = target.ExpectedDeltaCapacityUnits,
                SourceReference = target.SourceReference,
                Notes = target.Notes
            })
            .ToListAsync(cancellationToken);

        var auditLogs = await db.SmtAuditLogs
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.SmtAuditLogId)
            .Take(100)
            .Select(item => new SmtAuditLogItem
            {
                SmtAuditLogId = item.SmtAuditLogId,
                EntityType = item.EntityType,
                EntityKey = item.EntityKey,
                ActionType = item.ActionType,
                FieldName = item.FieldName,
                OldValue = item.OldValue,
                NewValue = item.NewValue,
                ChangedBy = item.ChangedBy,
                Notes = item.Notes,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new SmtAdministrationSnapshot(
            persistedFamilies.OrderBy(item => item.ProductFamily).ToList(),
            scenarios,
            productionWeeks,
            operations,
            familyRates,
            lineRates,
            lineAssignments,
            operationCapacities,
            capacityTargets,
            auditLogs);
    }

    public async Task<int> SaveFamilyAsync(SmtFamilyCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var productFamily = Required(item.ProductFamily).ToUpperInvariant();
        var duplicate = await db.SmtFamilies.AnyAsync(family =>
            family.SmtFamilyId != item.SmtFamilyId &&
            family.ProductFamily == productFamily,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"La familia SMT ya existe: {productFamily}.");
        }

        var now = DateTime.UtcNow;
        var entity = item.SmtFamilyId == 0
            ? new SmtFamilyEntity { CreatedAtUtc = now }
            : await db.SmtFamilies.SingleAsync(family => family.SmtFamilyId == item.SmtFamilyId, cancellationToken);
        var actionType = entity.SmtFamilyId == 0 ? "Creacion" : "Actualizacion";
        var oldValue = entity.SmtFamilyId == 0 ? null : SummarizeFamily(entity);

        entity.ProductFamily = productFamily;
        entity.BusinessUnit = EmptyToNull(item.BusinessUnit);
        entity.Notes = EmptyToNull(item.Notes);
        entity.IsActive = item.IsActive;
        entity.UpdatedAtUtc = now;

        if (item.SmtFamilyId == 0)
        {
            db.SmtFamilies.Add(entity);
        }

        AddAuditLog(
            db,
            "Familia SMT",
            productFamily,
            actionType,
            "Registro",
            oldValue,
            SummarizeFamily(entity),
            "Admin SMT",
            "Cambio realizado desde Administracion SMT.");

        await db.SaveChangesAsync(cancellationToken);
        return entity.SmtFamilyId;
    }

    public async Task<int> SaveFamilyRateAsync(SmtFamilyRateCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ValidateRate(item.ProductFamily, item.OperationId, item.UnitsPerHour);
        var productFamily = Required(item.ProductFamily).ToUpperInvariant();
        var entity = item.SmtFamilyRateId == 0
            ? await db.SmtFamilyRates.SingleOrDefaultAsync(rate =>
                rate.ProductFamily == productFamily &&
                rate.OperationId == item.OperationId,
                cancellationToken) ?? new SmtFamilyRateEntity()
            : await db.SmtFamilyRates.SingleAsync(rate => rate.SmtFamilyRateId == item.SmtFamilyRateId, cancellationToken);

        var isNew = entity.SmtFamilyRateId == 0;
        var actionType = isNew ? "Creacion" : "Actualizacion";
        var oldValue = isNew ? null : SummarizeFamilyRate(entity);
        var operation = await db.Operations.AsNoTracking().SingleAsync(row => row.OperationId == item.OperationId, cancellationToken);
        entity.ProductFamily = productFamily;
        entity.OperationId = item.OperationId;
        entity.UnitsPerHour = item.UnitsPerHour;
        entity.ChangeoverCount = item.ChangeoverCount;
        entity.SourcePartNumber = EmptyToNull(item.SourcePartNumber);
        entity.BottleneckUnitsPerHour = item.BottleneckUnitsPerHour;
        entity.RouterUnitsPerHour = item.RouterUnitsPerHour;
        entity.WaveUnitsPerHour = item.WaveUnitsPerHour;
        entity.PressfitUnitsPerHour = item.PressfitUnitsPerHour;
        entity.IsActive = item.IsActive;

        if (isNew)
        {
            db.SmtFamilyRates.Add(entity);
        }

        AddAuditLog(
            db,
            "UPH Familia SMT",
            $"{productFamily}|{operation.OperationCode}",
            actionType,
            "Registro",
            oldValue,
            SummarizeFamilyRate(entity),
            "Admin SMT",
            "Cambio realizado desde Administracion SMT.");

        await db.SaveChangesAsync(cancellationToken);
        return entity.SmtFamilyRateId;
    }

    public async Task<int> SaveLineFamilyRateAsync(SmtLineFamilyRateCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ValidateRate(item.ProductFamily, item.OperationId, item.UnitsPerHour);
        var productFamily = Required(item.ProductFamily).ToUpperInvariant();
        var lineCode = Required(item.LineCode).ToUpperInvariant();
        var entity = item.SmtLineFamilyRateId == 0
            ? await db.SmtLineFamilyRates.SingleOrDefaultAsync(rate =>
                rate.ProductFamily == productFamily &&
                rate.OperationId == item.OperationId &&
                rate.LineCode == lineCode,
                cancellationToken) ?? new SmtLineFamilyRateEntity()
            : await db.SmtLineFamilyRates.SingleAsync(rate => rate.SmtLineFamilyRateId == item.SmtLineFamilyRateId, cancellationToken);

        var isNew = entity.SmtLineFamilyRateId == 0;
        var actionType = isNew ? "Creacion" : "Actualizacion";
        var oldValue = isNew ? null : SummarizeLineFamilyRate(entity);
        var operation = await db.Operations.AsNoTracking().SingleAsync(row => row.OperationId == item.OperationId, cancellationToken);
        entity.ProductFamily = productFamily;
        entity.OperationId = item.OperationId;
        entity.LineCode = lineCode;
        entity.LineNumber = item.LineNumber <= 0 ? 1 : item.LineNumber;
        entity.UnitsPerHour = item.UnitsPerHour;
        entity.SourcePartNumber = EmptyToNull(item.SourcePartNumber);
        entity.BottleneckUnitsPerHour = item.BottleneckUnitsPerHour;
        entity.RouterUnitsPerHour = item.RouterUnitsPerHour;
        entity.WaveUnitsPerHour = item.WaveUnitsPerHour;
        entity.PressfitUnitsPerHour = item.PressfitUnitsPerHour;
        entity.IsActive = item.IsActive;

        if (isNew)
        {
            db.SmtLineFamilyRates.Add(entity);
        }

        AddAuditLog(
            db,
            "UPH Linea SMT",
            $"{productFamily}|{operation.OperationCode}|{lineCode}",
            actionType,
            "Registro",
            oldValue,
            SummarizeLineFamilyRate(entity),
            "Admin SMT",
            "Cambio realizado desde Administracion SMT.");

        await db.SaveChangesAsync(cancellationToken);
        return entity.SmtLineFamilyRateId;
    }

    public async Task<int> SaveLineAssignmentAsync(SmtLineAssignmentCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ValidatePlanningReference(item.ScenarioId, item.ProductionWeekId, item.OperationId);
        var productFamily = Required(item.ProductFamily).ToUpperInvariant();
        var lineCode = Required(item.LineCode).ToUpperInvariant();
        var entity = item.SmtLineAssignmentId == 0
            ? await db.SmtLineAssignments.SingleOrDefaultAsync(assignment =>
                assignment.ScenarioId == item.ScenarioId &&
                assignment.ProductionWeekId == item.ProductionWeekId &&
                assignment.OperationId == item.OperationId &&
                assignment.ProductFamily == productFamily &&
                assignment.LineCode == lineCode,
                cancellationToken) ?? new SmtLineAssignmentEntity()
            : await db.SmtLineAssignments.SingleAsync(assignment => assignment.SmtLineAssignmentId == item.SmtLineAssignmentId, cancellationToken);

        if (item.LineDistributionPercent <= 0m)
        {
            throw new InvalidOperationException("La Distribucion debe ser mayor a cero.");
        }

        if (item.LineDistributionPercent > 100m)
        {
            throw new InvalidOperationException("La Distribucion no puede ser mayor a 100%.");
        }

        var isNew = entity.SmtLineAssignmentId == 0;
        var actionType = isNew ? "Creacion" : "Actualizacion";
        var oldValue = isNew ? null : SummarizeLineAssignment(entity);
        var operation = await db.Operations.AsNoTracking().SingleAsync(row => row.OperationId == item.OperationId, cancellationToken);
        var week = await db.ProductionWeeks.AsNoTracking().SingleAsync(row => row.ProductionWeekId == item.ProductionWeekId, cancellationToken);
        entity.ScenarioId = item.ScenarioId;
        entity.ProductionWeekId = item.ProductionWeekId;
        entity.OperationId = item.OperationId;
        entity.ProductFamily = productFamily;
        entity.LineCode = lineCode;
        entity.LineNumber = item.LineNumber <= 0 ? 1 : item.LineNumber;
        entity.LineDistribution = item.LineDistributionPercent / 100m;
        entity.MonthName = EmptyToNull(item.MonthName) ?? week.WeekName;
        entity.MonthSequence = item.MonthSequence <= 0 ? ParseMonthSequence(week.WeekName) : item.MonthSequence;
        entity.IsActive = item.IsActive;

        if (isNew)
        {
            db.SmtLineAssignments.Add(entity);
        }

        AddAuditLog(
            db,
            "Distribucion Linea SMT",
            $"{productFamily}|{operation.OperationCode}|{week.WeekName}|{lineCode}",
            actionType,
            "Registro",
            oldValue,
            SummarizeLineAssignment(entity),
            "Admin SMT",
            "Cambio realizado desde Administracion SMT.");

        await db.SaveChangesAsync(cancellationToken);
        return entity.SmtLineAssignmentId;
    }

    public async Task<int> SaveOperationCapacityAsync(SmtOperationCapacityCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ValidatePlanningReference(item.ScenarioId, item.ProductionWeekId, item.OperationId);
        if (item.WorkDays < 0m)
        {
            throw new InvalidOperationException("Dias por semana no puede ser negativo.");
        }

        if (item.HoursPerShift <= 0m)
        {
            throw new InvalidOperationException("Horas por turno debe ser mayor a cero.");
        }

        if (item.ShiftsPerDay <= 0)
        {
            throw new InvalidOperationException("Turnos por dia debe ser mayor a cero.");
        }

        var entity = item.OperationWeeklyCapacityId == 0
            ? await db.OperationWeeklyCapacities.SingleOrDefaultAsync(capacity =>
                capacity.ScenarioId == item.ScenarioId &&
                capacity.ProductionWeekId == item.ProductionWeekId &&
                capacity.OperationId == item.OperationId,
                cancellationToken) ?? new OperationWeeklyCapacityEntity()
            : await db.OperationWeeklyCapacities.SingleAsync(capacity => capacity.OperationWeeklyCapacityId == item.OperationWeeklyCapacityId, cancellationToken);

        var isNew = entity.OperationWeeklyCapacityId == 0;
        var actionType = isNew ? "Creacion" : "Actualizacion";
        var oldValue = isNew ? null : SummarizeOperationCapacity(entity);
        var operation = await db.Operations.AsNoTracking().SingleAsync(row => row.OperationId == item.OperationId, cancellationToken);
        var week = await db.ProductionWeeks.AsNoTracking().SingleAsync(row => row.ProductionWeekId == item.ProductionWeekId, cancellationToken);

        entity.ScenarioId = item.ScenarioId;
        entity.ProductionWeekId = item.ProductionWeekId;
        entity.OperationId = item.OperationId;
        entity.WorkDays = item.WorkDays;
        entity.HoursPerShift = item.HoursPerShift;
        entity.ShiftsPerDay = item.ShiftsPerDay;
        entity.EquipmentQuantity = item.EquipmentQuantity <= 0m ? 1m : item.EquipmentQuantity;
        entity.HolidayDays = Math.Max(0m, item.HolidayDays);
        entity.OvertimeHours = Math.Max(0m, item.OvertimeHours);
        entity.PlannedDowntimeHours = Math.Max(0m, item.PlannedDowntimeHours);

        if (isNew)
        {
            db.OperationWeeklyCapacities.Add(entity);
        }

        AddAuditLog(
            db,
            "Calendario SMT",
            $"{operation.OperationCode}|{week.WeekName}",
            actionType,
            "Registro",
            oldValue,
            SummarizeOperationCapacity(entity),
            "Admin SMT",
            "Cambio realizado desde Administracion SMT.");

        await db.SaveChangesAsync(cancellationToken);
        return entity.OperationWeeklyCapacityId;
    }

    public async Task<int> SaveCapacityTargetAsync(SmtCapacityTargetCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ValidatePlanningReference(item.ScenarioId, item.ProductionWeekId, item.OperationId);
        if (item.ExpectedUtilizationPercent < 0m)
        {
            throw new InvalidOperationException("La utilizacion esperada no puede ser negativa.");
        }

        var productFamily = Required(item.ProductFamily).ToUpperInvariant();
        var lineCode = Required(item.LineCode).ToUpperInvariant();
        var entity = item.SmtCapacityTargetId == 0
            ? await db.SmtCapacityTargets.SingleOrDefaultAsync(target =>
                target.ScenarioId == item.ScenarioId &&
                target.ProductionWeekId == item.ProductionWeekId &&
                target.OperationId == item.OperationId &&
                target.ProductFamily == productFamily &&
                target.LineCode == lineCode,
                cancellationToken) ?? new SmtCapacityTargetEntity()
            : await db.SmtCapacityTargets.SingleAsync(target => target.SmtCapacityTargetId == item.SmtCapacityTargetId, cancellationToken);

        var isNew = entity.SmtCapacityTargetId == 0;
        var actionType = isNew ? "Creacion" : "Actualizacion";
        var oldValue = isNew ? null : SummarizeCapacityTarget(entity);
        var operation = await db.Operations.AsNoTracking().SingleAsync(row => row.OperationId == item.OperationId, cancellationToken);
        var week = await db.ProductionWeeks.AsNoTracking().SingleAsync(row => row.ProductionWeekId == item.ProductionWeekId, cancellationToken);

        entity.ScenarioId = item.ScenarioId;
        entity.ProductionWeekId = item.ProductionWeekId;
        entity.OperationId = item.OperationId;
        entity.ProductFamily = productFamily;
        entity.LineCode = lineCode;
        entity.ExpectedAssignedDemandUnits = item.ExpectedAssignedDemandUnits;
        entity.ExpectedMaxCapacityUnits = item.ExpectedMaxCapacityUnits;
        entity.ExpectedUtilizationPercent = item.ExpectedUtilizationPercent;
        entity.ExpectedDeltaCapacityUnits = item.ExpectedDeltaCapacityUnits;
        entity.SourceReference = EmptyToNull(item.SourceReference);
        entity.Notes = EmptyToNull(item.Notes);

        if (isNew)
        {
            db.SmtCapacityTargets.Add(entity);
        }

        AddAuditLog(
            db,
            "Target Capacity SMT",
            $"{productFamily}|{operation.OperationCode}|{week.WeekName}|{lineCode}",
            actionType,
            "Registro",
            oldValue,
            SummarizeCapacityTarget(entity),
            "Admin SMT",
            "Cambio realizado desde Administracion SMT.");

        await db.SaveChangesAsync(cancellationToken);
        return entity.SmtCapacityTargetId;
    }

    private static async Task<IReadOnlyList<string>> GetImportedFamilyNamesAsync(PlanningDbContext db, CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        names.UnionWith(await db.PartNumbers.AsNoTracking()
            .Where(item => item.ProductFamily != null)
            .Select(item => item.ProductFamily!)
            .ToListAsync(cancellationToken));
        names.UnionWith(await db.SmtFamilyVolumes.AsNoTracking()
            .Select(item => item.ProductFamily)
            .ToListAsync(cancellationToken));
        names.UnionWith(await db.SmtFamilyRates.AsNoTracking()
            .Select(item => item.ProductFamily)
            .ToListAsync(cancellationToken));
        names.UnionWith(await db.SmtLineFamilyRates.AsNoTracking()
            .Select(item => item.ProductFamily)
            .ToListAsync(cancellationToken));
        names.UnionWith(await db.SmtLineAssignments.AsNoTracking()
            .Select(item => item.ProductFamily)
            .ToListAsync(cancellationToken));
        names.UnionWith(await db.SmtCapacityTargets.AsNoTracking()
            .Select(item => item.ProductFamily)
            .ToListAsync(cancellationToken));
        return names.Where(name => !string.IsNullOrWhiteSpace(name)).OrderBy(name => name).ToList();
    }

    private static async Task<string?> FindBusinessUnitAsync(PlanningDbContext db, string productFamily, CancellationToken cancellationToken)
    {
        var fromSmtVolume = await db.SmtFamilyVolumes
            .AsNoTracking()
            .Where(item => item.ProductFamily == productFamily && item.BusinessUnit != null)
            .Select(item => item.BusinessUnit)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromSmtVolume))
        {
            return fromSmtVolume;
        }

        return await db.PartNumbers
            .AsNoTracking()
            .Where(item => item.ProductFamily == productFamily && item.BusinessUnit != null)
            .Select(item => item.BusinessUnit)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ValidateRate(string productFamily, int operationId, decimal unitsPerHour)
    {
        _ = Required(productFamily);
        if (operationId <= 0)
        {
            throw new ArgumentException("La operacion SMT es requerida.");
        }

        if (unitsPerHour <= 0m)
        {
            throw new ArgumentException("El UPH debe ser mayor a cero.");
        }
    }

    private static void ValidatePlanningReference(int scenarioId, int productionWeekId, int operationId)
    {
        if (scenarioId <= 0)
        {
            throw new ArgumentException("El escenario es requerido.");
        }

        if (productionWeekId <= 0)
        {
            throw new ArgumentException("La semana/mes es requerida.");
        }

        if (operationId <= 0)
        {
            throw new ArgumentException("La operacion SMT es requerida.");
        }
    }

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("El campo es requerido.") : value.Trim();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddAuditLog(
        PlanningDbContext db,
        string entityType,
        string entityKey,
        string actionType,
        string fieldName,
        string? oldValue,
        string? newValue,
        string changedBy,
        string notes) =>
        db.SmtAuditLogs.Add(new SmtAuditLogEntity
        {
            EntityType = entityType,
            EntityKey = entityKey,
            ActionType = actionType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow
        });

    private static string SummarizeFamily(SmtFamilyEntity entity) =>
        $"Familia {entity.ProductFamily}; BU {entity.BusinessUnit ?? "-"}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeFamilyRate(SmtFamilyRateEntity entity) =>
        $"UPH {entity.UnitsPerHour:0.##}; CO {entity.ChangeoverCount:0.##}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeLineFamilyRate(SmtLineFamilyRateEntity entity) =>
        $"Linea {entity.LineCode}; UPH {entity.UnitsPerHour:0.##}; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeLineAssignment(SmtLineAssignmentEntity entity) =>
        $"Linea {entity.LineCode}; Distribucion {entity.LineDistribution * 100m:0.##}%; Estado {(entity.IsActive ? "Activo" : "Inactivo")}";

    private static string SummarizeOperationCapacity(OperationWeeklyCapacityEntity entity) =>
        $"Dias {entity.WorkDays:0.##}; Hrs/turno {entity.HoursPerShift:0.##}; Turnos {entity.ShiftsPerDay}; Festivos {entity.HolidayDays:0.##}; OT {entity.OvertimeHours:0.##}; Paro {entity.PlannedDowntimeHours:0.##}";

    private static string SummarizeCapacityTarget(SmtCapacityTargetEntity entity) =>
        $"MPS {entity.ExpectedAssignedDemandUnits:0.##}; Cap {entity.ExpectedMaxCapacityUnits:0.##}; Util {entity.ExpectedUtilizationPercent:0.##}%; Delta {entity.ExpectedDeltaCapacityUnits:0.##}";

    private static int ParseMonthSequence(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 1;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var sequence) && sequence > 0 ? sequence : 1;
    }
}

public sealed record SmtAdministrationSnapshot(
    IReadOnlyList<SmtFamilyCatalogItem> Families,
    IReadOnlyList<SmtScenarioLookupItem> Scenarios,
    IReadOnlyList<SmtProductionWeekLookupItem> ProductionWeeks,
    IReadOnlyList<SmtOperationLookupItem> Operations,
    IReadOnlyList<SmtFamilyRateCatalogItem> FamilyRates,
    IReadOnlyList<SmtLineFamilyRateCatalogItem> LineFamilyRates,
    IReadOnlyList<SmtLineAssignmentCatalogItem> LineAssignments,
    IReadOnlyList<SmtOperationCapacityCatalogItem> OperationCapacities,
    IReadOnlyList<SmtCapacityTargetCatalogItem> CapacityTargets,
    IReadOnlyList<SmtAuditLogItem> AuditLogs);

public sealed class SmtFamilyCatalogItem
{
    public int SmtFamilyId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string? BusinessUnit { get; set; }
    public string? Notes { get; set; }
    public int PartNumberCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtOperationLookupItem
{
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
}

public sealed class SmtScenarioLookupItem
{
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
}

public sealed class SmtProductionWeekLookupItem
{
    public int ProductionWeekId { get; set; }
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public string WeekName { get; set; } = string.Empty;
}

public sealed class SmtFamilyRateCatalogItem
{
    public int SmtFamilyRateId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public decimal UnitsPerHour { get; set; }
    public decimal ChangeoverCount { get; set; }
    public string? SourcePartNumber { get; set; }
    public decimal? BottleneckUnitsPerHour { get; set; }
    public decimal? RouterUnitsPerHour { get; set; }
    public decimal? WaveUnitsPerHour { get; set; }
    public decimal? PressfitUnitsPerHour { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtLineFamilyRateCatalogItem
{
    public int SmtLineFamilyRateId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public int LineNumber { get; set; } = 1;
    public decimal UnitsPerHour { get; set; }
    public string? SourcePartNumber { get; set; }
    public decimal? BottleneckUnitsPerHour { get; set; }
    public decimal? RouterUnitsPerHour { get; set; }
    public decimal? WaveUnitsPerHour { get; set; }
    public decimal? PressfitUnitsPerHour { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtLineAssignmentCatalogItem
{
    public int SmtLineAssignmentId { get; set; }
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public int ProductionWeekId { get; set; }
    public string WeekName { get; set; } = string.Empty;
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string ProductFamily { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public int LineNumber { get; set; } = 1;
    public decimal LineDistributionPercent { get; set; }
    public string? MonthName { get; set; }
    public int MonthSequence { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public sealed class SmtOperationCapacityCatalogItem
{
    public int OperationWeeklyCapacityId { get; set; }
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public int ProductionWeekId { get; set; }
    public string WeekName { get; set; } = string.Empty;
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public decimal WorkDays { get; set; }
    public decimal HoursPerShift { get; set; }
    public int ShiftsPerDay { get; set; } = 1;
    public decimal EquipmentQuantity { get; set; } = 1m;
    public decimal HolidayDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal PlannedDowntimeHours { get; set; }
}

public sealed class SmtCapacityTargetCatalogItem
{
    public int SmtCapacityTargetId { get; set; }
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public int ProductionWeekId { get; set; }
    public string WeekName { get; set; } = string.Empty;
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string ProductFamily { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public decimal ExpectedAssignedDemandUnits { get; set; }
    public decimal ExpectedMaxCapacityUnits { get; set; }
    public decimal ExpectedUtilizationPercent { get; set; }
    public decimal ExpectedDeltaCapacityUnits { get; set; }
    public string? SourceReference { get; set; }
    public string? Notes { get; set; }
}

public sealed class SmtAuditLogItem
{
    public long SmtAuditLogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
