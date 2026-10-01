# SMT Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the SMT flow safer and easier for production planners by adding scenario selection, an SMT readiness checklist, and distribution validation before users trust capacity results.

**Architecture:** Keep the existing layered pattern: domain/query logic in `ProductionCapacity.Data`, UI composition in Blazor pages/components, and tests in `ProductionCapacity.Data.Tests`. Reuse existing SMT services and table-query style instead of introducing a new framework or client-side state system.

**Tech Stack:** .NET 10, Blazor Server Interactive, EF Core SQL Server/InMemory tests, xUnit.

**Spec:** Conversation requirement from September 24, 2026: next recommended phase after SMT catalog work is global scenario selection, a preparation checklist, and validation that SMT line distribution is complete before planning.

## Global Constraints

- Do not start localhost or a dev server during implementation.
- Keep SMT focus; do not expand BE/PCBA/racks/chambers/chasis in this phase.
- Use existing database tables when possible.
- Do not remove imported part-number data; SMT planning remains family-first but part numbers may remain in the model.
- Follow current patterns: service classes in `ProductionCapacity.Data`, Razor pages in `ProductionCapacity/Components/Pages`, lightweight markup tests for page affordances.
- Run `dotnet build .\ProductionCapacity\ProductionCapacity.csproj` and `dotnet test .\ProductionCapacity.slnx` before completion.

## Review Focus

- Scenario with no SMT data: pages should show actionable empty-state messages rather than exceptions.
- Multiple scenarios in SQL: changing selected scenario should reload capacity, diagnostics, and readiness results for the selected scenario only.
- Distribution totals under 100% or over 100%: checklist/diagnostics should mark them as blocking or warning with family, month/week, and operation context.
- Scenario with targets missing: readiness should show targets as optional comparison data, not block capacity calculation.
- Import-created scenario names: selector should not assume `Base FMD SMT` exists.

---

## File Structure

- Modify `ProductionCapacity.Data/SmtDiagnosticsService.cs`: add distribution-total validation and expose readiness-related issue categories through existing diagnostics.
- Create `ProductionCapacity.Data/SmtReadinessService.cs`: produces one readiness snapshot per scenario with required SMT data categories and clear status.
- Modify `ProductionCapacity.Data/SmtAdministrationService.cs`: reuse existing scenario lookup data if needed by UI selectors.
- Create `ProductionCapacity/Components/SmtScenarioSelector.razor`: shared scenario dropdown component for SMT pages.
- Modify `ProductionCapacity/Program.cs`: register new readiness service if constructor injection requires it.
- Modify `ProductionCapacity/Components/Pages/SmtCapacity.razor`: replace hardcoded scenario with selector and reload report/parity on change.
- Modify `ProductionCapacity/Components/Pages/SmtDiagnostics.razor`: replace hardcoded scenario with selector and show readiness panel.
- Test `ProductionCapacity.Data.Tests/SmtReadinessServiceTests.cs`: readiness checklist behavior.
- Modify `ProductionCapacity.Data.Tests/SmtDiagnosticsServiceTests.cs`: distribution-total validation.
- Modify markup tests: assert scenario selector/readiness UI exists.

---

### Task 1: Scenario Options and Shared Selector

**Files:**
- Create: `ProductionCapacity.Data/SmtScenarioOptionsService.cs`
- Create: `ProductionCapacity/Components/SmtScenarioSelector.razor`
- Modify: `ProductionCapacity/Program.cs`
- Test: `ProductionCapacity.Data.Tests/SmtScenarioOptionsServiceTests.cs`

**Interfaces:**
- Produces: `SmtScenarioOptionsService.GetOptionsAsync(CancellationToken) : Task<IReadOnlyList<SmtScenarioOption>>`
- Produces: `SmtScenarioOption(int ScenarioId, string ScenarioName, bool IsBaseline)`
- Produces: `SmtScenarioSelector` parameters: `IReadOnlyList<SmtScenarioOption> Options`, `string SelectedScenarioName`, `EventCallback<string> SelectedScenarioNameChanged`

- [ ] **Step 1: Write failing service test**

```csharp
[Fact]
public async Task ListsBaselineScenarioFirstThenByName()
{
    var factory = CreateFactory();
    await using (var db = await factory.CreateDbContextAsync())
    {
        db.Scenarios.AddRange(
            new Scenario { ScenarioId = 1, ScenarioName = "What If", IsBaseline = false },
            new Scenario { ScenarioId = 2, ScenarioName = "Base FMD SMT", IsBaseline = true },
            new Scenario { ScenarioId = 3, ScenarioName = "Ajuste SMT", IsBaseline = false });
        await db.SaveChangesAsync();
    }

    var service = new SmtScenarioOptionsService(factory);
    var result = await service.GetOptionsAsync();

    Assert.Collection(
        result,
        first => Assert.Equal("Base FMD SMT", first.ScenarioName),
        second => Assert.Equal("Ajuste SMT", second.ScenarioName),
        third => Assert.Equal("What If", third.ScenarioName));
}
```

- [ ] **Step 2: Run failing test**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtScenarioOptionsServiceTests"`

Expected: compile failure because `SmtScenarioOptionsService` does not exist.

- [ ] **Step 3: Implement service**

```csharp
using Microsoft.EntityFrameworkCore;

namespace ProductionCapacity.Data;

public sealed record SmtScenarioOption(int ScenarioId, string ScenarioName, bool IsBaseline);

public sealed class SmtScenarioOptionsService(IDbContextFactory<PlanningDbContext> dbFactory)
{
    public async Task<IReadOnlyList<SmtScenarioOption>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Scenarios
            .AsNoTracking()
            .OrderByDescending(item => item.IsBaseline)
            .ThenBy(item => item.ScenarioName)
            .Select(item => new SmtScenarioOption(item.ScenarioId, item.ScenarioName, item.IsBaseline))
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Register service**

In `ProductionCapacity/Program.cs`, add:

```csharp
builder.Services.AddScoped<SmtScenarioOptionsService>();
```

- [ ] **Step 5: Create selector component**

```razor
@using ProductionCapacity.Data

<label class="scenario-selector">
    <span>Escenario</span>
    <select class="form-control" value="@SelectedScenarioName" @onchange="OnScenarioChanged">
        @foreach (var option in Options)
        {
            <option value="@option.ScenarioName">@option.ScenarioName</option>
        }
    </select>
</label>

@code {
    [Parameter] public IReadOnlyList<SmtScenarioOption> Options { get; set; } = [];
    [Parameter] public string SelectedScenarioName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SelectedScenarioNameChanged { get; set; }

    private async Task OnScenarioChanged(ChangeEventArgs args)
    {
        var value = Convert.ToString(args.Value) ?? string.Empty;
        await SelectedScenarioNameChanged.InvokeAsync(value);
    }
}
```

- [ ] **Step 6: Verify**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtScenarioOptionsServiceTests"`

Expected: PASS.

---

### Task 2: Wire Scenario Selector into Capacity SMT

**Files:**
- Modify: `ProductionCapacity/Components/Pages/SmtCapacity.razor`
- Test: `ProductionCapacity.Data.Tests/SmtCapacityMarkupTests.cs`

**Interfaces:**
- Consumes: `SmtScenarioOptionsService.GetOptionsAsync`
- Consumes: `SmtScenarioSelector`
- Produces: `LoadScenarioAsync(string scenarioName) : Task`

- [ ] **Step 1: Write failing markup test**

Add assertions to `SmtCapacityMarkupTests`:

```csharp
Assert.Contains("@inject SmtScenarioOptionsService", markup);
Assert.Contains("<SmtScenarioSelector", markup);
Assert.Contains("LoadScenarioAsync", markup);
```

- [ ] **Step 2: Run failing test**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtCapacityMarkupTests"`

Expected: FAIL because selector is not wired.

- [ ] **Step 3: Inject and load scenarios**

In `SmtCapacity.razor`, add:

```razor
@inject SmtScenarioOptionsService ScenarioOptionsService
```

Add state:

```csharp
private IReadOnlyList<SmtScenarioOption> ScenarioOptions { get; set; } = [];
```

Replace `OnInitializedAsync` body with:

```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        ScenarioOptions = await ScenarioOptionsService.GetOptionsAsync();
        ScenarioName = ScenarioOptions.FirstOrDefault()?.ScenarioName ?? ScenarioName;
        await LoadScenarioAsync(ScenarioName);
    }
    catch (Exception ex)
    {
        LoadError = $"No pude cargar la capacidad SMT. Verifica que el script 005 este aplicado y que existan CSV SMT importados: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}
```

Add method:

```csharp
private async Task LoadScenarioAsync(string scenarioName)
{
    IsLoading = true;
    LoadError = null;
    ParityError = null;
    ScenarioName = scenarioName;
    Report = await ReportService.GetReportAsync(ScenarioName);
    try
    {
        Parity = await ParityService.CompareAsync(ScenarioName);
    }
    catch (Exception ex)
    {
        Parity = null;
        ParityError = $"No pude comparar contra targets de Capacity SMT. Aplica el script 006 y carga targets: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}
```

- [ ] **Step 4: Render selector**

In the header action area, add:

```razor
<SmtScenarioSelector Options="ScenarioOptions"
                     SelectedScenarioName="ScenarioName"
                     SelectedScenarioNameChanged="LoadScenarioAsync" />
```

- [ ] **Step 5: Verify**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtCapacityMarkupTests"
dotnet build .\ProductionCapacity\ProductionCapacity.csproj
```

Expected: PASS and build clean.

---

### Task 3: Wire Scenario Selector into Diagnostics SMT

**Files:**
- Modify: `ProductionCapacity/Components/Pages/SmtDiagnostics.razor`
- Test: `ProductionCapacity.Data.Tests/SmtDiagnosticsMarkupTests.cs`

**Interfaces:**
- Consumes: `SmtScenarioOptionsService.GetOptionsAsync`
- Consumes: `SmtScenarioSelector`
- Produces: `LoadScenarioAsync(string scenarioName) : Task`

- [ ] **Step 1: Write failing markup test**

Add assertions:

```csharp
Assert.Contains("@inject SmtScenarioOptionsService", markup);
Assert.Contains("<SmtScenarioSelector", markup);
Assert.Contains("LoadScenarioAsync", markup);
```

- [ ] **Step 2: Run failing test**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtDiagnosticsMarkupTests"`

Expected: FAIL.

- [ ] **Step 3: Inject scenarios and add state**

In `SmtDiagnostics.razor`:

```razor
@inject SmtScenarioOptionsService ScenarioOptionsService
```

```csharp
private IReadOnlyList<SmtScenarioOption> ScenarioOptions { get; set; } = [];
```

- [ ] **Step 4: Replace load flow**

```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        ScenarioOptions = await ScenarioOptionsService.GetOptionsAsync();
        ScenarioName = ScenarioOptions.FirstOrDefault()?.ScenarioName ?? ScenarioName;
        await LoadScenarioAsync(ScenarioName);
    }
    catch (Exception ex)
    {
        LoadError = $"No pude cargar el diagnostico SMT. Verifica que ya exista el escenario base y los scripts SMT: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}

private async Task LoadScenarioAsync(string scenarioName)
{
    IsLoading = true;
    LoadError = null;
    ScenarioName = scenarioName;
    Snapshot = await DiagnosticsService.GetSnapshotAsync(ScenarioName);
    IsLoading = false;
}
```

- [ ] **Step 5: Render selector**

```razor
<SmtScenarioSelector Options="ScenarioOptions"
                     SelectedScenarioName="ScenarioName"
                     SelectedScenarioNameChanged="LoadScenarioAsync" />
```

- [ ] **Step 6: Verify**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtDiagnosticsMarkupTests"
dotnet build .\ProductionCapacity\ProductionCapacity.csproj
```

Expected: PASS and build clean.

---

### Task 4: Readiness Checklist Service

**Files:**
- Create: `ProductionCapacity.Data/SmtReadinessService.cs`
- Test: `ProductionCapacity.Data.Tests/SmtReadinessServiceTests.cs`

**Interfaces:**
- Produces: `SmtReadinessService.GetSnapshotAsync(string scenarioName, CancellationToken) : Task<SmtReadinessSnapshot>`
- Produces records: `SmtReadinessSnapshot`, `SmtReadinessItem`, `SmtReadinessStatus`

- [ ] **Step 1: Write failing test for complete data**

```csharp
[Fact]
public async Task ReportsReadyWhenRequiredSmtInputsExist()
{
    var factory = CreateFactory();
    await SeedCompleteSmtScenarioAsync(factory);

    var service = new SmtReadinessService(factory);
    var snapshot = await service.GetSnapshotAsync("Base FMD SMT");

    Assert.True(snapshot.IsReadyForCapacity);
    Assert.Contains(snapshot.Items, item => item.Key == "families" && item.Status == SmtReadinessStatus.Ok);
    Assert.Contains(snapshot.Items, item => item.Key == "volumes" && item.Status == SmtReadinessStatus.Ok);
    Assert.Contains(snapshot.Items, item => item.Key == "rates" && item.Status == SmtReadinessStatus.Ok);
    Assert.Contains(snapshot.Items, item => item.Key == "lineAssignments" && item.Status == SmtReadinessStatus.Ok);
    Assert.Contains(snapshot.Items, item => item.Key == "calendar" && item.Status == SmtReadinessStatus.Ok);
}
```

- [ ] **Step 2: Write failing test for missing data**

```csharp
[Fact]
public async Task ReportsBlockedWhenRequiredInputsAreMissing()
{
    var factory = CreateFactory();
    await using (var db = await factory.CreateDbContextAsync())
    {
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base FMD SMT", IsBaseline = true });
        await db.SaveChangesAsync();
    }

    var service = new SmtReadinessService(factory);
    var snapshot = await service.GetSnapshotAsync("Base FMD SMT");

    Assert.False(snapshot.IsReadyForCapacity);
    Assert.Contains(snapshot.Items, item => item.Key == "volumes" && item.Status == SmtReadinessStatus.Blocked);
    Assert.Contains(snapshot.Items, item => item.Key == "rates" && item.Status == SmtReadinessStatus.Blocked);
}
```

- [ ] **Step 3: Run failing tests**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtReadinessServiceTests"`

Expected: compile failure because service does not exist.

- [ ] **Step 4: Implement records and service**

```csharp
public enum SmtReadinessStatus
{
    Ok,
    Warning,
    Blocked
}

public sealed record SmtReadinessItem(
    string Key,
    string Title,
    SmtReadinessStatus Status,
    int Count,
    string Message,
    string RecommendedAction);

public sealed record SmtReadinessSnapshot(
    string ScenarioName,
    IReadOnlyList<SmtReadinessItem> Items)
{
    public bool IsReadyForCapacity => Items.All(item => item.Status != SmtReadinessStatus.Blocked);
    public int BlockedCount => Items.Count(item => item.Status == SmtReadinessStatus.Blocked);
    public int WarningCount => Items.Count(item => item.Status == SmtReadinessStatus.Warning);
}
```

Implement `GetSnapshotAsync` to count:

```csharp
families = db.SmtFamilies.CountAsync(IsActive)
volumes = db.SmtFamilyVolumes joined to scenario
rates = db.SmtFamilyRates.CountAsync(IsActive)
lineRates = db.SmtLineFamilyRates.CountAsync(IsActive) warning-only optional
lineAssignments = db.SmtLineAssignments joined to scenario, active
calendar = db.OperationWeeklyCapacities joined to scenario and SMT operation
targets = db.SmtCapacityTargets joined to scenario, warning-only optional
```

Required blocked categories:

```text
families, volumes, rates, lineAssignments, calendar
```

Warning-only categories:

```text
lineRates, targets
```

- [ ] **Step 5: Register service**

In `ProductionCapacity/Program.cs`:

```csharp
builder.Services.AddScoped<SmtReadinessService>();
```

- [ ] **Step 6: Verify**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtReadinessServiceTests"`

Expected: PASS.

---

### Task 5: Readiness Panel in Diagnostics

**Files:**
- Modify: `ProductionCapacity/Components/Pages/SmtDiagnostics.razor`
- Test: `ProductionCapacity.Data.Tests/SmtDiagnosticsMarkupTests.cs`

**Interfaces:**
- Consumes: `SmtReadinessService.GetSnapshotAsync`
- Produces UI section title: `Checklist SMT`

- [ ] **Step 1: Write failing markup test**

```csharp
Assert.Contains("@inject SmtReadinessService", markup);
Assert.Contains("Checklist SMT", markup);
Assert.Contains("Readiness", markup);
```

- [ ] **Step 2: Run failing test**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtDiagnosticsMarkupTests"`

Expected: FAIL.

- [ ] **Step 3: Inject readiness service and state**

```razor
@inject SmtReadinessService ReadinessService
```

```csharp
private SmtReadinessSnapshot? Readiness { get; set; }
```

- [ ] **Step 4: Load readiness with diagnostics**

Inside `LoadScenarioAsync`:

```csharp
Snapshot = await DiagnosticsService.GetSnapshotAsync(ScenarioName);
Readiness = await ReadinessService.GetSnapshotAsync(ScenarioName);
```

- [ ] **Step 5: Render readiness panel before current Semaforo**

```razor
@if (Readiness is not null)
{
    <section class="panel">
        <div class="panel-title">
            <h2>Checklist SMT</h2>
            <span>@(Readiness.IsReadyForCapacity ? "listo para calcular" : $"{Readiness.BlockedCount} bloqueo(s)")</span>
        </div>
        <div class="summary-grid operation-summary-grid">
            @foreach (var item in Readiness.Items)
            {
                <div class="metric-card @GetReadinessCardClass(item.Status)">
                    <span>@item.Title</span>
                    <strong>@FormatReadinessStatus(item.Status)</strong>
                    <small>@item.Message</small>
                </div>
            }
        </div>
    </section>
}
```

Add helpers:

```csharp
private static string GetReadinessCardClass(SmtReadinessStatus status) =>
    status switch
    {
        SmtReadinessStatus.Blocked => "danger",
        SmtReadinessStatus.Warning => "warning",
        _ => string.Empty
    };

private static string FormatReadinessStatus(SmtReadinessStatus status) =>
    status switch
    {
        SmtReadinessStatus.Blocked => "Falta",
        SmtReadinessStatus.Warning => "Revision",
        _ => "OK"
    };
```

- [ ] **Step 6: Verify**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtDiagnosticsMarkupTests|FullyQualifiedName~SmtReadinessServiceTests"
dotnet build .\ProductionCapacity\ProductionCapacity.csproj
```

Expected: PASS and build clean.

---

### Task 6: Distribution Total Validation

**Files:**
- Modify: `ProductionCapacity.Data/SmtDiagnosticsService.cs`
- Test: `ProductionCapacity.Data.Tests/SmtDiagnosticsServiceTests.cs`

**Interfaces:**
- Produces diagnostic issue category `Distribucion total`
- Rule: group active `SmtLineAssignments` by scenario, production week, operation, product family. Sum `LineDistribution`. If sum is not `1.0`, emit warning or critical.

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task FlagsLineDistributionTotalsThatDoNotEqualOneHundredPercent()
{
    var factory = CreateFactory();
    await SeedScenarioOperationAndWeekAsync(factory);
    await using (var db = await factory.CreateDbContextAsync())
    {
        db.SmtLineAssignments.AddRange(
            new SmtLineAssignmentEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                ProductFamily = "AZTECA",
                LineCode = "SMT 1",
                LineNumber = 1,
                LineDistribution = 0.60m,
                MonthName = "M1",
                MonthSequence = 1,
                IsActive = true
            },
            new SmtLineAssignmentEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                ProductFamily = "AZTECA",
                LineCode = "SMT 2",
                LineNumber = 2,
                LineDistribution = 0.20m,
                MonthName = "M1",
                MonthSequence = 1,
                IsActive = true
            });
        await db.SaveChangesAsync();
    }

    var service = new SmtDiagnosticsService(factory);
    var snapshot = await service.GetSnapshotAsync("Base FMD SMT");

    Assert.Contains(snapshot.Issues, issue =>
        issue.Category == "Distribucion total" &&
        issue.EntityKey.Contains("AZTECA", StringComparison.OrdinalIgnoreCase) &&
        issue.Message.Contains("80", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 2: Run failing test**

Run: `dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtDiagnosticsServiceTests.FlagsLineDistributionTotalsThatDoNotEqualOneHundredPercent"`

Expected: FAIL because rule is missing.

- [ ] **Step 3: Implement query**

Inside diagnostics service, after loading assignments, group active rows:

```csharp
var distributionTotals = assignments
    .Where(item => item.IsActive)
    .GroupBy(item => new { item.ProductionWeekId, item.OperationId, item.ProductFamily })
    .Select(group => new
    {
        group.Key.ProductionWeekId,
        group.Key.OperationId,
        group.Key.ProductFamily,
        Total = group.Sum(item => item.LineDistribution),
        Lines = group.Select(item => item.LineCode).OrderBy(item => item).ToList()
    })
    .ToList();
```

For each total outside `0.999m..1.001m`, add:

```csharp
issues.Add(new SmtDiagnosticIssue(
    SmtDiagnosticSeverity.Critical,
    "Distribucion total",
    $"{productFamily}|{weekName}|{operationCode}",
    $"La distribucion de {productFamily} suma {total * 100m:0.##}% entre lineas {string.Join(", ", lines)}.",
    "Ajusta Distribucion por linea para que la familia sume 100% en ese mes/semana."));
```

- [ ] **Step 4: Verify**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtDiagnosticsServiceTests"
```

Expected: PASS.

---

### Task 7: Empty State and Action Text Polish

**Files:**
- Modify: `ProductionCapacity/Components/Pages/SmtCapacity.razor`
- Modify: `ProductionCapacity/Components/Pages/SmtDiagnostics.razor`
- Test: markup tests for capacity and diagnostics.

**Interfaces:**
- Produces empty-state copy that tells users where to fix data.

- [ ] **Step 1: Write failing markup assertions**

In `SmtCapacityMarkupTests`:

```csharp
Assert.Contains("Ve a Importar Excel o Admin SMT", markup);
```

In `SmtDiagnosticsMarkupTests`:

```csharp
Assert.Contains("Corrige los datos marcados antes de revisar Capacidad SMT", markup);
```

- [ ] **Step 2: Run failing tests**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtCapacityMarkupTests|FullyQualifiedName~SmtDiagnosticsMarkupTests"
```

Expected: FAIL.

- [ ] **Step 3: Update empty state in Capacity SMT**

Replace:

```razor
<div class="panel">No hay asignaciones SMT cargadas para este escenario.</div>
```

With:

```razor
<div class="panel">
    No hay asignaciones SMT cargadas para este escenario. Ve a Importar Excel o Admin SMT para cargar volumen, UPH, calendario y distribucion por linea.
</div>
```

- [ ] **Step 4: Update diagnostics guidance**

In diagnostics checklist section, add:

```razor
@if (Readiness is not null && !Readiness.IsReadyForCapacity)
{
    <div class="alert alert-warning">Corrige los datos marcados antes de revisar Capacidad SMT.</div>
}
```

- [ ] **Step 5: Verify**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~SmtCapacityMarkupTests|FullyQualifiedName~SmtDiagnosticsMarkupTests"
dotnet build .\ProductionCapacity\ProductionCapacity.csproj
```

Expected: PASS and build clean.

---

### Task 8: Final Verification

**Files:**
- No production files unless earlier tasks uncover compile issues.

**Interfaces:**
- Consumes all previous tasks.
- Produces verified implementation.

- [ ] **Step 1: Run focused SMT tests**

Run:

```powershell
dotnet test .\ProductionCapacity.Data.Tests\ProductionCapacity.Data.Tests.csproj --filter "FullyQualifiedName~Smt"
```

Expected: all SMT-related tests PASS.

- [ ] **Step 2: Build web app**

Run:

```powershell
dotnet build .\ProductionCapacity\ProductionCapacity.csproj
```

Expected: `0 Error(s)`.

- [ ] **Step 3: Run full suite**

Run:

```powershell
dotnet test .\ProductionCapacity.slnx
```

Expected: all tests PASS.

- [ ] **Step 4: Manual no-server check**

Do not start localhost. Report that no server was started and summarize changed flows.

---

## Self-Review

- Spec coverage: global scenario selection is covered by Tasks 1-3; readiness checklist by Tasks 4-5; distribution validation by Task 6; user guidance by Task 7.
- Placeholder scan: no TBD/TODO/implement-later placeholders remain.
- Type consistency: scenario selector uses `string ScenarioName` because existing services consume scenario names; readiness service uses records local to `ProductionCapacity.Data`.
- Review Focus coverage: no-data scenarios covered by Tasks 4 and 7; multiple scenarios covered by Tasks 1-3; bad distribution totals covered by Task 6; missing targets are warning-only in Task 4; missing `Base FMD SMT` avoided by selecting first available scenario in Tasks 2-3.
