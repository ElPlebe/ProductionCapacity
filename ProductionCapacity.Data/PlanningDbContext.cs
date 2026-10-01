using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options) : DbContext(options)
{
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ProductionWeekEntity> ProductionWeeks => Set<ProductionWeekEntity>();
    public DbSet<MachineEntity> Machines => Set<MachineEntity>();
    public DbSet<PartNumberEntity> PartNumbers => Set<PartNumberEntity>();
    public DbSet<SubFamilyEntity> SubFamilies => Set<SubFamilyEntity>();
    public DbSet<PartCrossReference> PartCrossReferences => Set<PartCrossReference>();
    public DbSet<ProcessRoute> ProcessRoutes => Set<ProcessRoute>();
    public DbSet<PlanningOperation> Operations => Set<PlanningOperation>();
    public DbSet<OperationWeeklyCapacityEntity> OperationWeeklyCapacities => Set<OperationWeeklyCapacityEntity>();
    public DbSet<OperationRateEntity> OperationRates => Set<OperationRateEntity>();
    public DbSet<OperationCapacityResultEntity> OperationCapacityResults => Set<OperationCapacityResultEntity>();
    public DbSet<TestTimeDefinitionEntity> TestTimeDefinitions => Set<TestTimeDefinitionEntity>();
    public DbSet<ResourceTypeEntity> ResourceTypes => Set<ResourceTypeEntity>();
    public DbSet<ResourceConfigurationEntity> ResourceConfigurations => Set<ResourceConfigurationEntity>();
    public DbSet<ResourceInventoryEntity> ResourceInventories => Set<ResourceInventoryEntity>();
    public DbSet<SmtFamilyEntity> SmtFamilies => Set<SmtFamilyEntity>();
    public DbSet<SmtFamilyVolumeEntity> SmtFamilyVolumes => Set<SmtFamilyVolumeEntity>();
    public DbSet<SmtFamilyRateEntity> SmtFamilyRates => Set<SmtFamilyRateEntity>();
    public DbSet<SmtAuditLogEntity> SmtAuditLogs => Set<SmtAuditLogEntity>();
    public DbSet<SmtLineFamilyRateEntity> SmtLineFamilyRates => Set<SmtLineFamilyRateEntity>();
    public DbSet<SmtLineAssignmentEntity> SmtLineAssignments => Set<SmtLineAssignmentEntity>();
    public DbSet<SmtCapacityTargetEntity> SmtCapacityTargets => Set<SmtCapacityTargetEntity>();
    public DbSet<WeeklyDemandEntity> WeeklyDemand => Set<WeeklyDemandEntity>();
    public DbSet<ScenarioPartOverride> ScenarioPartOverrides => Set<ScenarioPartOverride>();
    public DbSet<ScenarioMachineOverride> ScenarioMachineOverrides => Set<ScenarioMachineOverride>();
    public DbSet<ScenarioMachinePartOverride> ScenarioMachinePartOverrides => Set<ScenarioMachinePartOverride>();
    public DbSet<RackType> RackTypes => Set<RackType>();
    public DbSet<RackAccessory> RackAccessories => Set<RackAccessory>();
    public DbSet<RackConfiguration> RackConfigurations => Set<RackConfiguration>();
    public DbSet<ScenarioRackInventory> ScenarioRackInventories => Set<ScenarioRackInventory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("planning");
        ConfigureDecimalPrecision(modelBuilder);

        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.ToTable("Scenarios");
            entity.HasKey(item => item.ScenarioId);
            entity.Property(item => item.ScenarioName).HasMaxLength(120);
        });

        modelBuilder.Entity<ProductionWeekEntity>(entity =>
        {
            entity.ToTable("ProductionWeeks");
            entity.HasKey(item => item.ProductionWeekId);
            entity.Property(item => item.WeekName).HasMaxLength(30);
        });

        modelBuilder.Entity<MachineEntity>(entity =>
        {
            entity.ToTable("Machines");
            entity.HasKey(item => item.MachineId);
            entity.Property(item => item.MachineCode).HasMaxLength(60);
            entity.Property(item => item.MachineName).HasMaxLength(160);
        });

        modelBuilder.Entity<PartNumberEntity>(entity =>
        {
            entity.ToTable("PartNumbers");
            entity.HasKey(item => item.PartNumberId);
            entity.Property(item => item.PartNumber).HasMaxLength(80);
            entity.Property(item => item.Description).HasMaxLength(240);
            entity.Property(item => item.BusinessUnit).HasMaxLength(80);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.SubFamily).HasMaxLength(120);
            entity.HasIndex(item => new { item.ProductFamily, item.SubFamily });
        });

        modelBuilder.Entity<SubFamilyEntity>(entity =>
        {
            entity.ToTable("SubFamilies");
            entity.HasKey(item => item.SubFamilyId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.SubFamilyName).HasMaxLength(120);
            entity.Property(item => item.Notes).HasMaxLength(240);
            entity.HasIndex(item => new { item.ProductFamily, item.SubFamilyName }).IsUnique();
        });

        modelBuilder.Entity<PartCrossReference>(entity =>
        {
            entity.ToTable("PartCrossReferences");
            entity.HasKey(item => item.PartCrossReferenceId);
            entity.Property(item => item.ReferenceType).HasMaxLength(40);
            entity.Property(item => item.ReferenceValue).HasMaxLength(120);
            entity.HasIndex(item => new { item.ReferenceType, item.ReferenceValue }).IsUnique();
        });

        modelBuilder.Entity<ProcessRoute>(entity =>
        {
            entity.ToTable("ProcessRoutes");
            entity.HasKey(item => item.ProcessRouteId);
            entity.Property(item => item.OperationName).HasMaxLength(120);
        });

        modelBuilder.Entity<PlanningOperation>(entity =>
        {
            entity.ToTable("Operations");
            entity.HasKey(item => item.OperationId);
            entity.Property(item => item.OperationCode).HasMaxLength(60);
            entity.Property(item => item.OperationName).HasMaxLength(160);
            entity.Property(item => item.OperationFamily).HasMaxLength(120);
        });

        modelBuilder.Entity<OperationWeeklyCapacityEntity>(entity =>
        {
            entity.ToTable("OperationWeeklyCapacities");
            entity.HasKey(item => item.OperationWeeklyCapacityId);
        });

        modelBuilder.Entity<OperationRateEntity>(entity =>
        {
            entity.ToTable("OperationRates");
            entity.HasKey(item => item.OperationRateId);
        });

        modelBuilder.Entity<OperationCapacityResultEntity>(entity =>
        {
            entity.ToTable("OperationCapacityResults");
            entity.HasKey(item => item.OperationCapacityResultId);
        });

        modelBuilder.Entity<TestTimeDefinitionEntity>(entity =>
        {
            entity.ToTable("TestTimeDefinitions");
            entity.HasKey(item => item.TestTimeDefinitionId);
            entity.Property(item => item.TestName).HasMaxLength(160);
            entity.HasIndex(item => new { item.PartNumberId, item.OperationId, item.TestName }).IsUnique();
        });

        modelBuilder.Entity<ResourceTypeEntity>(entity =>
        {
            entity.ToTable("ResourceTypes");
            entity.HasKey(item => item.ResourceTypeId);
            entity.Property(item => item.ResourceCode).HasMaxLength(60);
            entity.Property(item => item.ResourceName).HasMaxLength(160);
            entity.Property(item => item.ResourceKind).HasMaxLength(40);
            entity.HasIndex(item => item.ResourceCode).IsUnique();
        });

        modelBuilder.Entity<ResourceConfigurationEntity>(entity =>
        {
            entity.ToTable("ResourceConfigurations");
            entity.HasKey(item => item.ResourceConfigurationId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.Notes).HasMaxLength(400);
        });

        modelBuilder.Entity<ResourceInventoryEntity>(entity =>
        {
            entity.ToTable("ResourceInventories");
            entity.HasKey(item => item.ResourceInventoryId);
            entity.Property(item => item.Notes).HasMaxLength(400);
            entity.HasIndex(item => new { item.ScenarioId, item.ProductionWeekId, item.ResourceTypeId }).IsUnique();
        });

        modelBuilder.Entity<SmtFamilyEntity>(entity =>
        {
            entity.ToTable("SmtFamilies");
            entity.HasKey(item => item.SmtFamilyId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.BusinessUnit).HasMaxLength(80);
            entity.Property(item => item.Notes).HasMaxLength(400);
            entity.HasIndex(item => item.ProductFamily).IsUnique();
        });

        modelBuilder.Entity<SmtFamilyVolumeEntity>(entity =>
        {
            entity.ToTable("SmtFamilyVolumes");
            entity.HasKey(item => item.SmtFamilyVolumeId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.BusinessUnit).HasMaxLength(80);
            entity.Property(item => item.MonthName).HasMaxLength(30);
            entity.HasIndex(item => new { item.ScenarioId, item.ProductionWeekId, item.ProductFamily }).IsUnique();
        });

        modelBuilder.Entity<SmtFamilyRateEntity>(entity =>
        {
            entity.ToTable("SmtFamilyRates");
            entity.HasKey(item => item.SmtFamilyRateId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.SourcePartNumber).HasMaxLength(80);
            entity.HasIndex(item => new { item.OperationId, item.ProductFamily }).IsUnique();
        });

        modelBuilder.Entity<SmtAuditLogEntity>(entity =>
        {
            entity.ToTable("SmtAuditLogs");
            entity.HasKey(item => item.SmtAuditLogId);
            entity.Property(item => item.EntityType).HasMaxLength(80);
            entity.Property(item => item.EntityKey).HasMaxLength(180);
            entity.Property(item => item.ActionType).HasMaxLength(40);
            entity.Property(item => item.FieldName).HasMaxLength(80);
            entity.Property(item => item.OldValue).HasMaxLength(800);
            entity.Property(item => item.NewValue).HasMaxLength(800);
            entity.Property(item => item.ChangedBy).HasMaxLength(80);
            entity.Property(item => item.Notes).HasMaxLength(400);
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasIndex(item => new { item.EntityType, item.EntityKey });
        });

        modelBuilder.Entity<SmtLineFamilyRateEntity>(entity =>
        {
            entity.ToTable("SmtLineFamilyRates");
            entity.HasKey(item => item.SmtLineFamilyRateId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.LineCode).HasMaxLength(60);
            entity.Property(item => item.SourcePartNumber).HasMaxLength(80);
            entity.HasIndex(item => new { item.OperationId, item.ProductFamily, item.LineCode }).IsUnique();
        });

        modelBuilder.Entity<SmtLineAssignmentEntity>(entity =>
        {
            entity.ToTable("SmtLineAssignments");
            entity.HasKey(item => item.SmtLineAssignmentId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.LineCode).HasMaxLength(60);
            entity.Property(item => item.MonthName).HasMaxLength(30);
            entity.HasIndex(item => new { item.ScenarioId, item.ProductionWeekId, item.OperationId, item.ProductFamily, item.LineCode }).IsUnique();
        });

        modelBuilder.Entity<SmtCapacityTargetEntity>(entity =>
        {
            entity.ToTable("SmtCapacityTargets");
            entity.HasKey(item => item.SmtCapacityTargetId);
            entity.Property(item => item.ProductFamily).HasMaxLength(120);
            entity.Property(item => item.LineCode).HasMaxLength(60);
            entity.Property(item => item.SourceReference).HasMaxLength(160);
            entity.Property(item => item.Notes).HasMaxLength(400);
            entity.HasIndex(item => new { item.ScenarioId, item.ProductionWeekId, item.OperationId, item.ProductFamily, item.LineCode }).IsUnique();
        });

        modelBuilder.Entity<WeeklyDemandEntity>(entity =>
        {
            entity.ToTable("WeeklyDemand");
            entity.HasKey(item => item.WeeklyDemandId);
        });

        modelBuilder.Entity<ScenarioPartOverride>(entity =>
        {
            entity.ToTable("ScenarioPartOverrides");
            entity.HasKey(item => item.ScenarioPartOverrideId);
        });

        modelBuilder.Entity<ScenarioMachineOverride>(entity =>
        {
            entity.ToTable("ScenarioMachineOverrides");
            entity.HasKey(item => item.ScenarioMachineOverrideId);
        });

        modelBuilder.Entity<ScenarioMachinePartOverride>(entity =>
        {
            entity.ToTable("ScenarioMachinePartOverrides");
            entity.HasKey(item => item.ScenarioMachinePartOverrideId);
        });

        modelBuilder.Entity<RackType>(entity =>
        {
            entity.ToTable("RackTypes");
            entity.HasKey(item => item.RackTypeId);
            entity.Property(item => item.RackCode).HasMaxLength(60);
            entity.Property(item => item.RackName).HasMaxLength(160);
        });

        modelBuilder.Entity<RackAccessory>(entity =>
        {
            entity.ToTable("RackAccessories");
            entity.HasKey(item => item.RackAccessoryId);
            entity.Property(item => item.AccessoryCode).HasMaxLength(60);
            entity.Property(item => item.AccessoryName).HasMaxLength(160);
        });

        modelBuilder.Entity<RackConfiguration>(entity =>
        {
            entity.ToTable("RackConfigurations");
            entity.HasKey(item => item.RackConfigurationId);
            entity.Property(item => item.Notes).HasMaxLength(400);
        });

        modelBuilder.Entity<ScenarioRackInventory>(entity =>
        {
            entity.ToTable("ScenarioRackInventories");
            entity.HasKey(item => item.ScenarioRackInventoryId);
            entity.Property(item => item.Notes).HasMaxLength(400);
        });
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        var decimalProperties = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?));

        foreach (var property in decimalProperties)
        {
            property.SetPrecision(18);
            property.SetScale(6);
        }
    }
}
