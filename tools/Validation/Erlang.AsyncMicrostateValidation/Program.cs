using System.Security.Cryptography;
using System.Reflection;
using System.Text.Json;
using Erlang.Validation.AsyncMicrostates;

if (args.Length != AsyncObservationProgramNames.ArgumentCount)
{
    throw new ArgumentException(AsyncObservationProgramNames.UsageMessage);
}

using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(args[AsyncObservationProgramNames.InputsArgumentIndex]));
JsonElement input = document.RootElement.Clone();
var cases = new List<object>();
var failures = new List<Exception>();
var unavailable = input.GetProperty(AsyncObservationProgramNames.UnavailableCasesProperty).EnumerateObject().ToDictionary(
    row => row.Name,
    row => row.Value.GetString()!,
    StringComparer.Ordinal
);

void Verify()
{
    string configuration = input.GetProperty(AsyncObservationProgramNames.ConfigurationProperty).GetString()!;
    if (!Path.GetFullPath(Assembly.GetExecutingAssembly().Location).Equals(
        Path.GetFullPath(input.GetProperty(AsyncObservationProgramNames.ObserverAssemblyPathProperty).GetString()!),
        StringComparison.OrdinalIgnoreCase
    ))
    {
        throw new InvalidDataException(AsyncObservationProgramNames.ExecutablePathFailureMessage);
    }

    foreach (Assembly assembly in new[]
    {
        typeof(ProcessRuntime).Assembly,
        typeof(Term).Assembly,
        typeof(ExternalTermFormat).Assembly,
        Assembly.Load(AsyncObservationProgramNames.StdlibAssemblyName),
        Assembly.GetExecutingAssembly()
    })
    {
        if (assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration != configuration)
        {
            throw new InvalidDataException(AsyncObservationProgramNames.ConfigurationFailureMessage);
        }
    }

    foreach (JsonElement row in input.GetProperty(AsyncObservationProgramNames.GuardsProperty).EnumerateArray())
    {
        if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(row.GetProperty(AsyncObservationProgramNames.PathProperty).GetString()!)))
            != row.GetProperty(AsyncObservationProgramNames.Sha256Property).GetString())
        {
            throw new InvalidDataException(AsyncObservationProgramNames.InputChangedPrefix + row.GetProperty(AsyncObservationProgramNames.PathProperty).GetString());
        }
    }

    foreach (JsonElement row in input.GetProperty(AsyncObservationProgramNames.AssemblyGuardsProperty).EnumerateArray())
    {
        string actual = Path.Combine(AppContext.BaseDirectory, row.GetProperty(AsyncObservationProgramNames.AssemblyFileNameProperty).GetString()!);
        if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actual))) != row.GetProperty(AsyncObservationProgramNames.Sha256Property).GetString())
        {
            throw new InvalidDataException(AsyncObservationProgramNames.AssemblyHashFailureMessage);
        }
    }
}

async Task Case(
    string name,
    Func<Task<object>> body
)
{
    if (unavailable.TryGetValue(name, out string? reason))
    {
        var dependency = new InvalidOperationException(AsyncObservationProgramNames.DependencyFailurePrefix + name + AsyncObservationProgramNames.DependencyReasonSeparator + reason);
        cases.Add(new
        {
            Name = name,
            Passed = false,
            SkippedDependency = true,
            Dependency = reason
        });
        failures.Add(dependency);
        Console.WriteLine(AsyncObservationProgramNames.SkippedCasePrefix + name);

        return;
    }

    Exception? primary = null, post = null;
    object? result = null;
    Console.WriteLine(AsyncObservationProgramNames.CaseStartPrefix + name);
    try
    {
        Verify();
        result = await body();
    }
    catch (Exception error)
    {
        primary = error;
    }
    try
    {
        Verify();
    }
    catch (Exception error)
    {
        post = error;
    }

    cases.Add(new
    {
        Name = name,
        Passed = primary is null && post is null,
        Result = result,
        Primary = primary?.ToString(),
        InputFailure = post?.ToString()
    });
    if (primary is not null)
    {
        failures.Add(primary);
    }
    if (post is not null)
    {
        failures.Add(post);
    }
    Console.WriteLine(AsyncObservationProgramNames.CaseDonePrefix + name);
}

await Case(
    AsyncObservationProgramNames.OriginalSingleWorkerCase,
    () => AsyncDriverPositiveChecks.Run(input, AsyncDriverPositiveNames.SingleAsyncWorkerCount)
);
await Case(
    AsyncObservationProgramNames.OriginalTwoWorkerCase,
    () => AsyncDriverPositiveChecks.Run(input, AsyncDriverPositiveNames.TwoAsyncWorkerCount)
);
await Case(
    AsyncObservationProgramNames.PublicCancelCase,
    () => RemoteMsaccNegativeChecks.Run(input, RemoteMsaccNegativeNames.CancelMode)
);
await Case(
    AsyncObservationProgramNames.PublicCloseCase,
    () => RemoteMsaccNegativeChecks.Run(input, RemoteMsaccNegativeNames.CloseMode)
);
await Case(
    AsyncObservationProgramNames.ComponentChildExitCase,
    () => AsyncCaptureFaultChecks.Run(input, AsyncCaptureFaultNames.ChildExitMode)
);
await Case(
    AsyncObservationProgramNames.ComponentWrongEpochCase,
    () => AsyncCaptureFaultChecks.Run(input, AsyncCaptureFaultNames.WrongEpochMode)
);
await Case(
    AsyncQueuedPortCloseNames.CaseName,
    () => AsyncQueuedPortCloseChecks.Run(input)
);
await Case(
    AsyncChildRollbackNames.CaseName,
    () => AsyncChildRollbackChecks.Run(input)
);
await Case(
    AsyncPendingRpcCaseNames1471.CaptureKillCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.CaptureKill)
);
await Case(
    AsyncPendingRpcCaseNames1471.CaptureCloseCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.CaptureClose)
);
await Case(
    AsyncPendingRpcCaseNames1471.ApplyKillCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.ApplyKill)
);
await Case(
    AsyncPendingRpcCaseNames1471.ApplyCloseCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.ApplyClose)
);
await Case(
    AsyncPendingRpcCaseNames1471.FinishKillCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.FinishKill)
);
await Case(
    AsyncPendingRpcCaseNames1471.FinishCloseCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.FinishClose)
);
await Case(
    AsyncPendingRpcCaseNames1471.ReleaseKillCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.ReleaseKill)
);
await Case(
    AsyncPendingRpcCaseNames1471.ReleaseCloseCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.ReleaseClose)
);
await Case(
    AsyncPendingRpcCaseNames1471.UnarmedCase,
    () => PendingRpcGateControls1465.Run(input, AsyncGateControlNames1465.Unarmed)
);
await Case(
    AsyncPendingRpcCaseNames1471.WrongOperationCase,
    () => PendingRpcGateControls1465.Run(input, AsyncGateControlNames1465.WrongOperation)
);
await Case(
    AsyncPendingRpcCaseNames1471.DisposeBeforeCase,
    () => PendingRpcGateControls1465.Run(input, AsyncGateControlNames1465.DisposeBefore)
);
await Case(
    AsyncPendingRpcCaseNames1471.DuplicateReleaseCase,
    () => PendingRpcGateControls1465.Run(input, AsyncGateControlNames1465.DuplicateRelease)
);
await Case(
    AsyncPendingRpcCaseNames1471.ChildDeathCase,
    () => PendingRpcGateControls1465.Run(input, AsyncGateControlNames1465.ChildDeath)
);
await Case(
    AsyncPendingRpcCaseNames1471.NoOpReleaseKillCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.ReleaseKill, noOp: true)
);
await Case(
    AsyncPendingRpcCaseNames1471.NoOpReleaseCloseCase,
    () => PendingRpcMatrixChecks1441.Run(input, AsyncPendingRpcNames1441.ReleaseClose, noOp: true)
);
File.WriteAllText(
    args[AsyncObservationProgramNames.ReportArgumentIndex],
    JsonSerializer.Serialize(
        new
        {
            Passed = failures.Count == 0,
            Configuration = input.GetProperty(AsyncObservationProgramNames.ConfigurationProperty).GetString(),
            Cases = cases,
            Errors = failures.Select(error => error.ToString()).ToArray(),
            Scope = AsyncObservationProgramNames.Scope
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
if (failures.Count != 0)
{
    throw new AggregateException(failures);
}
