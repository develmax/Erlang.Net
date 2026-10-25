using AllocationDiagnostic;
using Erlang;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;
if (args.Length != AllocationDiagnosticNames.ArgumentCount)
    throw new ArgumentException(AllocationDiagnosticNames.Usage);
if (File.Exists(args[0]))
    throw new IOException(AllocationDiagnosticNames.FreshOutput);
var rows = new List<AllocationDiagnosticSample>();
Exception? failure = null;
await using (var runtime = new ProcessRuntime())
{
    var actor = runtime.Spawn(async context =>
   {
       try
       {
           var binary = new BitString(new byte[AllocationDiagnosticParameters.BinaryBytes]);
           await runtime.Modules.Call(
               context,
               AllocationDiagnosticNames.Module,
               AllocationDiagnosticNames.Function,
               binary,
               Term.I(AllocationDiagnosticParameters.FullRange)
           );
           int thread = Environment.CurrentManagedThreadId;
           long before = GC.GetAllocatedBytesForCurrentThread();
           ValueTask<Term> pending = runtime.Modules.Call(
               context,
               AllocationDiagnosticNames.Module,
               AllocationDiagnosticNames.Function,
               binary,
               Term.I(AllocationDiagnosticParameters.FullRange)
           );
           bool sync = pending.IsCompletedSuccessfully;
           Term result = pending.GetAwaiter().GetResult();
           long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
           rows.Add(new(
               AllocationDiagnosticNames.ExactWindow,
               0,
               allocated,
               thread,
               Environment.CurrentManagedThreadId,
               sync,
               result.ToString(),
               null
           ));
           var algorithm = typeof(ProcessRuntime).Assembly.GetType(AllocationDiagnosticNames.AlgorithmType, true)!.GetMethod(AllocationDiagnosticNames.AlgorithmMethod)!.CreateDelegate<Func<Term, uint>>();
           var field = typeof(ModuleRegistry).GetField(AllocationDiagnosticNames.RegisteredFunctions, BindingFlags.Instance | BindingFlags.NonPublic)!;
           var functions = (IDictionary<(string, string, int), ErlangFunction>)field.GetValue(runtime.Modules)!;
           var body = functions[(AllocationDiagnosticNames.Module, AllocationDiagnosticNames.Function, AllocationDiagnosticParameters.FunctionArity)];
           Term[] args = [binary, Term.I(AllocationDiagnosticParameters.FullRange)];
           foreach (string kind in new[] { AllocationDiagnosticNames.RegistryWindow, AllocationDiagnosticNames.PrebuiltWindow, AllocationDiagnosticNames.BodyWindow, AllocationDiagnosticNames.DirectWindow, AllocationDiagnosticNames.ArgumentsWindow })
           {
               for (int i = 0; i < AllocationDiagnosticParameters.Samples; i++)
               {
                   thread = Environment.CurrentManagedThreadId;
                   before = GC.GetAllocatedBytesForCurrentThread();
                   uint value = 0;
                   bool synchronous = true;
                   if (kind == AllocationDiagnosticNames.DirectWindow)
                   {
                       value = algorithm(binary);
                   }
                   else if (kind == AllocationDiagnosticNames.ArgumentsWindow)
                   {
                       Term[] created = [binary, Term.I(AllocationDiagnosticParameters.FullRange)];
                       value = (uint)created.Length;
                       GC.KeepAlive(created);
                   }
                   else
                   {
                       ValueTask<Term> call = kind == AllocationDiagnosticNames.BodyWindow ? body(context, args) : kind == AllocationDiagnosticNames.PrebuiltWindow ? runtime.Modules.Call(
                           context,
                           AllocationDiagnosticNames.Module,
                           AllocationDiagnosticNames.Function,
                           args
                       ) : runtime.Modules.Call(
                           context,
                           AllocationDiagnosticNames.Module,
                           AllocationDiagnosticNames.Function,
                           binary,
                           Term.I(AllocationDiagnosticParameters.FullRange)
                       );
                       synchronous = call.IsCompletedSuccessfully;
                       result = call.GetAwaiter().GetResult();
                       value = (uint)((Integer)result).Value;
                   }
                   allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                   rows.Add(new(
                       kind,
                       i,
                       allocated,
                       thread,
                       Environment.CurrentManagedThreadId,
                       synchronous,
                       null,
                       value
                   ));
               }
           }
       }
       catch (Exception ex)
       {
           failure = ex;
       }

       return Nil.Value;
   });
    await actor.Completion;
}
bool windowsValid = rows.Count == AllocationDiagnosticParameters.ExpectedRows &&
    rows.All(row => row.Synchronous && row.StartingThread == row.EndingThread);
var report = new
{
    Status = failure is not null ? AllocationDiagnosticNames.Failed : windowsValid ? AllocationDiagnosticNames.Completed : AllocationDiagnosticNames.InvalidWindows,
    WindowsValid = windowsValid,
    Rows = rows,
    Failure = failure?.ToString(),
    Framework = RuntimeInformation.FrameworkDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    OS = RuntimeInformation.OSDescription,
    Environment = new[]
    {
        AllocationDiagnosticNames.ProcessorCountVariable,
        AllocationDiagnosticNames.TieredCompilationVariable,
        AllocationDiagnosticNames.TieredPgoVariable,
        AllocationDiagnosticNames.LegacyTieredCompilationVariable,
        AllocationDiagnosticNames.LegacyTieredPgoVariable
    }.ToDictionary(name => name, System.Environment.GetEnvironmentVariable),
    Assemblies = new[] { typeof(ProcessRuntime).Assembly, typeof(Term).Assembly }.Select(assembly => new
    {
        assembly.Location,
        assembly.ManifestModule.ModuleVersionId,
        Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
    }),
    UnchangedBudget = AllocationDiagnosticParameters.BudgetBytes,
    BinaryBytes = AllocationDiagnosticParameters.BinaryBytes,
    SourceOrBudgetChanged = false,
    NativeExecuted = false,
    BudgetExceeded = rows.Any(row => row.Kind == AllocationDiagnosticNames.ExactWindow && row.Bytes > AllocationDiagnosticParameters.BudgetBytes),
    CauseProven = false
};
await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(report.Status + AllocationDiagnosticNames.RowSummary + rows.Count);
if (failure is not null || !windowsValid)
    return 1;

return 0;
