namespace ProductionCapacity.Engine;

public sealed record PlanningResourceConstraint(
    string ResourceName,
    string ResourceKind,
    decimal UnitsPerResource,
    int AvailableResourceCount)
{
    public decimal ResourceCapacityUnits => UnitsPerResource * AvailableResourceCount;
}

public sealed record PlanningResourceConstraintResult(
    string ResourceName,
    string ResourceKind,
    int RequiredResourceCount,
    int AvailableResourceCount,
    int MissingResourceCount,
    decimal ResourceCapacityUnits,
    decimal ConstrainedCapacityUnits,
    bool IsConstrained);

public static class ResourceConstraintCalculator
{
    public static PlanningResourceConstraintResult Calculate(
        decimal demandUnits,
        decimal timeCapacityUnits,
        PlanningResourceConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        if (constraint.UnitsPerResource <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(constraint), "Units per resource must be greater than zero.");
        }

        if (constraint.AvailableResourceCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(constraint), "Available resource count cannot be negative.");
        }

        var required = demandUnits <= 0
            ? 0
            : (int)Math.Ceiling(demandUnits / constraint.UnitsPerResource);
        var missing = Math.Max(0, required - constraint.AvailableResourceCount);
        var constrainedCapacity = Math.Min(timeCapacityUnits, constraint.ResourceCapacityUnits);

        return new PlanningResourceConstraintResult(
            constraint.ResourceName,
            constraint.ResourceKind,
            required,
            constraint.AvailableResourceCount,
            missing,
            constraint.ResourceCapacityUnits,
            constrainedCapacity,
            constraint.ResourceCapacityUnits < timeCapacityUnits || missing > 0);
    }
}
