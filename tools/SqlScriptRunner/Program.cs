using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

const string DefaultConnectionString =
    "Server=localhost;Database=ProductionCapacity;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

var root = FindRepositoryRoot(AppContext.BaseDirectory);
var arguments = args.ToList();
var connectionString = DefaultConnectionString;
var scriptPaths = new List<string>();
var queryMode = arguments.Remove("--query");

if (arguments.Count > 0 && !arguments[0].EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !queryMode)
{
    connectionString = arguments[0];
    arguments.RemoveAt(0);
}
else if (arguments.Count > 1 && queryMode)
{
    connectionString = arguments[0];
    arguments.RemoveAt(0);
}

if (queryMode)
{
    await using var queryConnection = new SqlConnection(connectionString);
    await queryConnection.OpenAsync();
    var query = arguments.Count == 0 ? throw new ArgumentException("Debes enviar una consulta SQL.") : string.Join(' ', arguments);
    await PrintQueryAsync(queryConnection, query);
    return;
}

if (arguments.Count == 0)
{
    scriptPaths.Add(Path.Combine(root, "database", "003_add_excel_parity_operations.sql"));
    scriptPaths.Add(Path.Combine(root, "database", "004_add_excel_import_resources.sql"));
    scriptPaths.Add(Path.Combine(root, "database", "005_add_smt_capacity.sql"));
    scriptPaths.Add(Path.Combine(root, "database", "006_add_smt_capacity_targets.sql"));
    scriptPaths.Add(Path.Combine(root, "database", "007_add_smt_line_family_rates.sql"));
}
else
{
    scriptPaths.AddRange(arguments.Select(path => Path.GetFullPath(path, root)));
}

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

foreach (var scriptPath in scriptPaths)
{
    if (!File.Exists(scriptPath))
    {
        throw new FileNotFoundException($"No se encontro el script SQL: {scriptPath}", scriptPath);
    }

    Console.WriteLine($"Aplicando {Path.GetFileName(scriptPath)}...");
    var script = await File.ReadAllTextAsync(scriptPath);
    var batchNumber = 0;

    foreach (var batch in SplitBatches(script))
    {
        if (string.IsNullOrWhiteSpace(batch))
        {
            continue;
        }

        batchNumber++;
        await using var command = connection.CreateCommand();
        command.CommandText = batch;
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    Console.WriteLine($"  {batchNumber} batches ejecutados.");
}

Console.WriteLine("Scripts aplicados.");

static IEnumerable<string> SplitBatches(string script) =>
    Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

static string FindRepositoryRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProductionCapacity.slnx")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static async Task PrintQueryAsync(SqlConnection connection, string query)
{
    await using var command = connection.CreateCommand();
    command.CommandText = query;
    command.CommandTimeout = 120;

    await using var reader = await command.ExecuteReaderAsync();
    do
    {
        var headers = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToArray();
        Console.WriteLine(string.Join('\t', headers));

        while (await reader.ReadAsync())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? "NULL" : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
            Console.WriteLine(string.Join('\t', values));
        }
    }
    while (await reader.NextResultAsync());
}
