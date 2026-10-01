# Family Planning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make product family the main capacity-planning level while preserving part numbers as drill-down detail.

**Architecture:** Keep the current SQL schema for this phase. Import family-level rates/test-times by expanding them to all part numbers in the family, and change the planning matrix service to aggregate part demand/rates into family rows with child part rows.

**Tech Stack:** .NET 10, Blazor Server, EF Core, SQL Server, xUnit.

**Spec:** Conversation-approved design: family is the calculation level; part numbers remain imported for traceability; matrix shows family rows expandable to part numbers.

## Global Constraints

- Do not remove part-number catalog data.
- Family rates apply to every part number in the same `ProductFamily`.
- Keep part-number override path possible by allowing part-specific rates.
- Matrix rows must be family summaries with child part rows.
- Use TDD and run full build/test before completion.

---

### Task 1: Import family-level rates and test times

**Files:**
- Modify: `ProductionCapacity.Data/ExcelImport/ExcelImportModels.cs`
- Modify: `ProductionCapacity.Data/ExcelImport/CsvCapacityImportParser.cs`
- Modify: `ProductionCapacity.Data/ExcelImport/ExcelCapacityImportService.cs`
- Modify: `import-templates/operation_rates.csv`
- Modify: `import-templates/test_times.csv`
- Test: `ProductionCapacity.Data.Tests/CsvCapacityImportParserTests.cs`
- Test: `ProductionCapacity.Data.Tests/ParityDataServicesTests.cs`

**Interfaces:**
- `ExcelOperationRateImportRow` gains nullable `ProductFamily`; empty `PartNumber` means family rule.
- `ExcelTestTimeImportRow` gains nullable `ProductFamily`; empty `PartNumber` means family rule.
- Import service expands family rows to all matching `PartNumberEntity.ProductFamily`.

### Task 2: Group planning matrix by family

**Files:**
- Modify: `ProductionCapacity.Data/PlanningMatrix.cs`
- Modify: `ProductionCapacity.Data/PlanningMatrixService.cs`
- Test: `ProductionCapacity.Data.Tests/PlanningMatrixServiceTests.cs`

**Interfaces:**
- `PlanningMatrixRow` represents a family summary.
- `PlanningMatrixRow.ChildRows` contains the part-number detail rows.
- Operation summaries use family aggregate load.

### Task 3: Add expand/collapse in the matrix UI

**Files:**
- Modify: `ProductionCapacity/Components/Pages/PlanningMatrix.razor`
- Modify: `ProductionCapacity/Components/Pages/CapacityDashboard.razor`

**Interfaces:**
- Matrix page renders family rows first and expands to part-number rows.
- Dashboard drill-down can show family and part detail without losing current summaries.

### Task 4: Verify

**Files:**
- No code changes.

**Checks:**
- `dotnet build ProductionCapacity.slnx`
- `dotnet test ProductionCapacity.slnx`
- Start app and request `/matriz` and `/dashboard`.
