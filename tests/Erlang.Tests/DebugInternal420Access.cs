using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Tests;

internal static class DebugInternal420Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugInternal420Names.Failure);
    }

    internal static byte[] Beam()
    {
        byte[] Read(string name)
        {
            using var input = typeof(DebugInternal420Access).Assembly.GetManifestResourceStream(DebugInternal420Names.Prefix + name)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugInternal420Names.Manifest))!;
        byte[] bytes = Read(DebugInternal420Names.Beam);
        Require(hashes.Count == 1 && hashes[DebugInternal420Names.Beam] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static ErlangFunction Capture(ProcessRuntime runtime, string function, int arity)
    {
        object?[] args = [runtime, DebugInternal420Names.Module, function, arity, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(DebugInternal420Names.Native, true)!.GetMethod(DebugInternal420Names.Capture, DebugInternal420Names.Members)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static async Task Initialize(ProcessRuntime runtime)
    {
        await runtime.InitializeModulesAsync(registry =>
        {
            Require(!registry.HasExport(DebugInternal420Names.Module, DebugInternal420Names.Get, 1));
            var image = registry.PrepareBeam(Beam(), new BeamDependencySelection([DebugInternal420Names.Runtime]));
            Require(image.NativePrimitiveExports.Count == 2 && image.NativePrimitiveExports.Contains(new BeamFunctionSignature(DebugInternal420Names.Get, 1)) && image.NativePrimitiveExports.Contains(new BeamFunctionSignature(DebugInternal420Names.Set, 2)));
            Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            var exports = registry.Exports.Where(e => e.Module == DebugInternal420Names.Module).ToArray();
            Require(exports.Length == 4);
            foreach (var e in exports) Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
        });
    }

    internal static ValueTask<Term> Get(ProcessContext context, Term key) => context.Runtime.Modules.Call(
        context,
        DebugInternal420Names.Module,
        DebugInternal420Names.Get,
        key
    );

    internal static ValueTask<Term> Set(ProcessContext context, Term key, Term value) => context.Runtime.Modules.Call(
        context,
        DebugInternal420Names.Module,
        DebugInternal420Names.Set,
        key,
        value
    );

    internal static ValueTask<Term> Enabled(ProcessContext context, bool value) => Set(context, Term.A(DebugInternal420Names.Available), Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));

    internal static async Task Error(ProcessContext context, Term query, string reason)
    {
        bool found = false;
        try
        {
            await Get(context, query);
        }
        catch (ErlangException error)
        {
            found = error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(reason)) && error.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } frame } && frame.Items[0].Equals(Term.A(DebugInternal420Names.Module)) && frame.Items[1].Equals(Term.A(DebugInternal420Names.Get)) && frame.Items[2].Equals(Term.List(query));
        }
        Require(found);
    }
}
