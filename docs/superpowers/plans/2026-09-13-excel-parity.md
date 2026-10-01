# Excel Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the .NET/SQL Server production planning system into numeric and operational parity with the Excel capacity model.

**Architecture:** Add a parity layer before expanding broad UX. The system should first reproduce the Excel planning matrix, then persist the missing calculation inputs, then expose production-planner screens and import flows.

**Tech Stack:** .NET 10, Blazor, SQL Server, Entity Framework Core, xUnit.

**Spec:** `C:\Users\carl0\source\repos\ProductionCapacity\docs\excel-parity-audit.md`

## Global Constraints

- Keep UI labels aligned with the user's preference: use `Yield` and `Factor de utilizacion` or `Factor de eficiencia`; avoid user-facing `OEE`.
- Maintain SQL Server as the source of truth.
- Preserve the current MVP while extending it; do not remove the existing capacity calculator until the parity calculator is validated.
- Add tests before changing calculation behavior.
- Validate every formula change against sampled rows from the Excel workbook.

---

## Priority Backlog

### P0 - Create Excel parity test fixture - Implemented

**Why:** Before changing formulas, lock a small set of expected Excel results. This prevents guessing.

**Files:**
- Create: `ProductionCapacity.Engine.Tests/ExcelParity/ExcelParityFixture.cs`
- Create: `ProductionCapacity.Engine.Tests/ExcelParity/ExcelParityCalculatorTests.cs`
- Create: `docs/parity-samples/excel-parity-samples.md`

**Interfaces:**
- Produces: `ExcelParityCase`, with part number, operation, week, demand, expected load hours, expected available hours, expected remaining hours.

- [ ] Extract 10 to 20 representative examples from Excel: ASSY, PACK, chamber, eORT, rack-constrained product, Longball, Mobius/Quake and NGFW.
- [ ] Write failing tests using fixed expected values.
- [ ] Run `dotnet test` and confirm tests fail until the parity engine exists.
- [ ] Document source sheet/range for every sample.

### P1 - Add operation-based planning model - Implemented

**Why:** Excel calculates by operation columns, not only by product route rows.

**Files:**
- Modify: `database/003_excel_parity_schema.sql`
- Modify: `ProductionCapacity.Data/Entities/PlanningEntities.cs`
- Modify: `ProductionCapacity.Data/PlanningDbContext.cs`

**Interfaces:**
- Produces entities for `PlanningOperation`, `OperationWeeklyCapacity`, and operation-level results.

- [ ] Add `planning.Operations` with operation code, name, family and active flag.
- [ ] Add `planning.OperationWeeklyCapacities` with scenario, week, operation, hours per shift, shifts per day, work days, qty equipment, overtime and downtime.
- [ ] Add `planning.OperationCapacityResults` for matrix-style results by scenario, week, operation and part number.
- [ ] Add EF entities and DbSet mappings.
- [ ] Add database constraints for positive hours, positive equipment quantity and unique scenario-week-operation capacity rows.
- [ ] Add tests that insert/query operation weekly capacity.

### P2 - Implement Excel-equivalent availability formula - Implemented

**Why:** Excel uses `days * shifts * hours * qty equipment`; current system lacks operation equipment quantity.

**Files:**
- Modify: `ProductionCapacity.Engine/CapacityCalculator.cs`
- Create: `ProductionCapacity.Engine/OperationCapacityCalculator.cs`
- Create: `ProductionCapacity.Engine.Tests/OperationCapacityCalculatorTests.cs`

**Interfaces:**
- Produces: `OperationWeekCapacity.AvailableHours`.

- [ ] Add a value object for operation weekly capacity.
- [ ] Implement `AvailableHours = (workDays - holidayDays) * hoursPerShift * shiftsPerDay * equipmentQuantity + overtimeHours - downtimeHours`.
- [ ] Add tests matching Excel-style examples, including quantity 1 and quantity 2 equipment.
- [ ] Keep the old week-level calculator intact for existing pages.

### P3 - Add rate model: UPH, yield, utilization and factor efficiency - Implemented

**Why:** Excel uses `UPH+OEE` as the denominator for load hours. The app needs the same concept, even if the UI names it differently.

**Files:**
- Modify: `database/003_excel_parity_schema.sql`
- Modify: `ProductionCapacity.Data/Entities/PlanningEntities.cs`
- Modify: `ProductionCapacity.Engine/OperationCapacityCalculator.cs`
- Create: `ProductionCapacity.Engine.Tests/OperationRateTests.cs`

**Interfaces:**
- Produces: `OperationRate.EffectiveUnitsPerHour`.

- [ ] Add `planning.OperationRates` with part number, operation, standard time seconds, base UPH, yield, utilization factor, efficiency factor, max cap and fixed headcount.
- [ ] Implement `baseUph = 3600 / standardTimeSeconds`.
- [ ] Implement `effectiveUph = baseUph * yield * utilizationFactor * efficiencyFactor`.
- [ ] Add a stored field or calculated output equivalent to Excel `UPH+OEE`.
- [ ] Add tests proving `loadHours = demand / effectiveUph`.
- [ ] Decide migration rule: existing `DefaultUtilizationFactor` remains utilization, new `EfficiencyFactor` maps to Excel OEE.

### P4 - Rebuild PLAN UABU-style matrix - Implemented

**Why:** This is the core planner view in Excel and the best way to verify parity.

**Files:**
- Create: `ProductionCapacity.Data/PlanningMatrixService.cs`
- Create: `ProductionCapacity/Components/Pages/PlanningMatrix.razor`
- Modify: `ProductionCapacity/Components/Layout/NavMenu.razor`
- Create: `ProductionCapacity.Data.Tests/PlanningMatrixServiceTests.cs`

**Interfaces:**
- Produces: matrix rows by part number and columns by operation with demand, load hours, available hours and remaining hours.

- [ ] Query demand by scenario/week/part.
- [ ] Join operation rates by part and operation.
- [ ] Join operation weekly capacity by scenario/week/operation.
- [ ] Calculate load hours per part-operation.
- [ ] Calculate operation totals and remaining hours.
- [ ] Build a Blazor matrix page with operation columns and overloaded indicators.
- [ ] Add tests for row totals and operation totals.

### P5 - Integrate PartCrossReferences into EF and catalogs - Implemented

**Why:** Excel maps TAN PN, SKU name and 73-level PN. Without this, imports and formulas can attach to the wrong product.

**Files:**
- Modify: `ProductionCapacity.Data/Entities/PlanningEntities.cs`
- Modify: `ProductionCapacity.Data/PlanningDbContext.cs`
- Modify: `ProductionCapacity.Data/CatalogAdministrationService.cs`
- Modify: `ProductionCapacity/Components/Pages/Catalogs.razor`

**Interfaces:**
- Produces: crossreference CRUD for part numbers.

- [ ] Add `PartCrossReference` entity for existing SQL table.
- [ ] Add DbSet and unique mapping by reference type/value.
- [ ] Add catalog UI section for crossreferences.
- [ ] Add service methods to create/update references.
- [ ] Add validation to prevent duplicate external references.

### P6 - Add test-time derived standard time - Implemented

**Why:** Excel derives standard time from test time, handling, qty cell and qty equipment.

**Files:**
- Modify: `database/003_excel_parity_schema.sql`
- Create: `ProductionCapacity.Engine/TestTimeCalculator.cs`
- Create: `ProductionCapacity.Engine.Tests/TestTimeCalculatorTests.cs`
- Modify: `ProductionCapacity/Components/Pages/Catalogs.razor`

**Interfaces:**
- Produces: `CalculatedStandardTimeSeconds`.

- [ ] Add `planning.TestTimeDefinitions`.
- [ ] Implement `standardTimeSeconds = ((testTimeMinutes + handlingMinutes) / quantityCell / quantityEquipment) * 60`.
- [ ] Add tests for Longball, Mobius/Quake and NGFW examples.
- [ ] Add UI inputs for test time, handling, qty cell and qty equipment.
- [ ] Allow manual override when the business has an approved standard time.

### P7 - Model chambers and chasis as capacity resources - Implemented

**Why:** Excel has chamber/chasis constraints separate from generic racks.

**Files:**
- Modify: `database/003_excel_parity_schema.sql`
- Modify: `ProductionCapacity.Data/Entities/PlanningEntities.cs`
- Modify: `ProductionCapacity.Engine/OperationCapacityCalculator.cs`
- Create: `ProductionCapacity.Engine.Tests/ResourceConstraintTests.cs`

**Interfaces:**
- Produces: resource constraints for rack, chamber, chasis, accessory and golden resources.

- [ ] Add `planning.ResourceTypes` with type values: Machine, Rack, Chamber, Chasis, Accessory, Golden.
- [ ] Add `planning.ResourceConfigurations` linking resource type to operation, product/family and units per resource.
- [ ] Add `planning.ResourceInventories` by scenario/week/resource.
- [ ] Generalize current rack constraint into a reusable resource constraint.
- [ ] Keep rack UI compatible by mapping existing racks into resource type `Rack`.
- [ ] Add tests for missing chamber/chasis capacity.

### P8 - Add multi-week planning horizon - Implemented

**Why:** Excel works across many weeks; current service calculates the first scenario week.

**Files:**
- Modify: `ProductionCapacity.Data/CapacityPlanService.cs`
- Modify: `ProductionCapacity.Data/PlanningMatrixService.cs`
- Modify: `ProductionCapacity/Components/Pages/Home.razor`
- Create: `ProductionCapacity.Data.Tests/MultiWeekPlanningTests.cs`

**Interfaces:**
- Produces: scenario results for all selected weeks.

- [ ] Replace first-week-only logic with a selected week or full horizon mode.
- [ ] Add week selector in UI.
- [ ] Add calculate-all-weeks service method.
- [ ] Add test proving two weeks with different holidays produce different capacity.

### P9 - Add Excel import pipeline - Implemented

**Why:** Manual catalog entry will be too slow and error-prone for the size of this model.

**Files:**
- Create: `ProductionCapacity.Data/ExcelImport/ExcelCapacityImportService.cs`
- Create: `ProductionCapacity.Data/ExcelImport/ExcelImportModels.cs`
- Create: `ProductionCapacity.Data.Tests/ExcelImportTests.cs`
- Create: `ProductionCapacity/Components/Pages/ImportExcel.razor`

**Interfaces:**
- Produces: import preview and persisted catalog/demand/rate updates.

- [ ] Import operations from `PLAN UABU`.
- [ ] Import backend operation rates.
- [ ] Import test time definitions.
- [ ] Import crossreferences.
- [ ] Import resource inventory/configuration for racks, chambers and chasis.
- [ ] Add preview before saving.
- [ ] Add import log with warnings for broken Excel references like `#REF!`.

### P10 - Add planner dashboards and drill-down - Implemented

**Why:** After numeric parity, planners need visibility and action items.

**Files:**
- Create: `ProductionCapacity/Components/Pages/CapacityDashboard.razor`
- Modify: `ProductionCapacity/Components/Pages/PlanningMatrix.razor`
- Modify: `ProductionCapacity.Data/PlanningMatrixService.cs`

**Interfaces:**
- Produces: visual summary of overloaded operations, missing resources and load contributors.

- [ ] Add operation summary cards: available hours, load hours, remaining hours and load percent.
- [ ] Add filters by week, family, operation and machine/resource.
- [ ] Add drill-down from overloaded operation to contributing products.
- [ ] Add missing resources list: racks/chambers/chasis/accessories required vs available.

## Recommended Execution Order

1. P0: Excel parity fixture.
2. P1: Operation-based planning tables.
3. P2: Excel-equivalent available hours.
4. P3: Effective UPH and factor efficiency.
5. P4: PLAN UABU-style matrix.
6. P5: Part crossreferences.
7. P6: Test-time derived standard time.
8. P7: Chambers/chasis/resources.
9. P8: Multi-week horizon.
10. P9: Excel import.
11. P10: Dashboards and drill-down.

## Self-Review

- Spec coverage: All gaps identified in `excel-parity-audit.md` have a matching backlog item.
- Placeholder scan: No task depends on undefined future work; each priority has concrete files and outputs.
- Type consistency: The plan introduces operation capacity, operation rates, planning matrix, crossreferences, derived test times and generalized resources in dependency order.
