using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;
using Erlang.Compiler;
using Erlang.Differential;
namespace Erlang.NifNativeBackend;
internal static class NifOrdinaryLoaderDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(OperatingSystem.IsLinux() && IntPtr.Size == 8 && arguments.Length == 5);
        Require(Hash(arguments[0]) == arguments[1]);
        Require(Hash(Path.Combine(AppContext.BaseDirectory, NifOrdinaryLoaderNames.Bridge)) == arguments[4]);
        string output = Path.GetFullPath(arguments[3]);
        Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(arguments[0]));
        Require(compiled.Definition.Name == NifOrdinaryLoaderNames.Module);
        var runtime = new ProcessRuntime(NifOrdinaryLoaderNames.Node);
        ModuleCodeImage? owned = null;
        bool loaded = false;
        string? actualValue = null;
        try
        {
            compiled.Definition.Register(runtime.Modules);
            await NifNativeHarness.Context(runtime, async context =>
            {
                try
                {
                    Term result = await Execution.InvokeAsync(compiled.Definition, NifOrdinaryLoaderNames.Load, context, [Term.String(arguments[2])]);
                    File.WriteAllBytes(Path.Combine(output, NifOrdinaryLoaderNames.LoadTerm), ExternalTermFormat.Encode(result));
                    loaded = result.Equals(Term.A(NifOrdinaryLoaderNames.Ok));
                    if (loaded)
                    {
                        owned = runtime.Modules.CurrentCodeImage(NifOrdinaryLoaderNames.Module);
                        Require(owned is not null);
                        Term value = await Execution.InvokeAsync(compiled.Definition, NifOrdinaryLoaderNames.Probe, context, []);
                        File.WriteAllBytes(Path.Combine(output, NifOrdinaryLoaderNames.ReturnTerm), ExternalTermFormat.Encode(value));
                        Require(value is Integer);
                        actualValue = ((Integer)value).Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        object nativeDomain = typeof(ProcessRuntime).GetField(NifLinuxPthreadProbeNames.DomainField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(context.Runtime)
                            ?? throw new InvalidOperationException(NifOrdinaryLoaderNames.Failure);
                        string? witness = nativeDomain.GetType().GetProperty(NifLinuxPthreadProbeNames.WitnessProperty, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(nativeDomain) as string;
                        File.WriteAllText(Path.Combine(output, NifLinuxPthreadProbeNames.WitnessFile), witness ?? throw new InvalidOperationException(NifOrdinaryLoaderNames.Failure));
                    }
                }
                catch (ErlangException failure)
                {
                    File.WriteAllBytes(Path.Combine(output, NifOrdinaryLoaderNames.ErrorTerm), ExternalTermFormat.Encode(Term.Tuple(Term.A(failure.ExceptionClass), failure.Reason, failure.StackTraceTerm)));
                    throw;
                }
            });
        }
        finally
        {
            if (owned is not null)
                await owned.NifBindings.RetireAsync().WaitAsync(NifOrdinaryLoaderNames.Deadline);
            await runtime.DisposeAsync();
        }
        Require(Hash(arguments[0]) == arguments[1]);
        File.WriteAllText(Path.Combine(output, NifOrdinaryLoaderNames.Completion), JsonSerializer.Serialize(new
        {
            Status = NifOrdinaryLoaderNames.Acquired,
            Loaded = loaded,
            ActualValue = actualValue,
            RuntimeDisposalsCompleted = true,
            NativeImageRetirementCompleted = owned is not null,
            OriginalOtpComparisonExecuted = false
        }));
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NifOrdinaryLoaderNames.Failure);
    }
}
