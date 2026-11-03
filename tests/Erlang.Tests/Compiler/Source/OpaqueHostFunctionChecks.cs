using Erlang.Differential;

namespace Erlang.Tests;

internal static class OpaqueHostFunctionChecks
{
    internal static Task Metadata() => Run(false);

    internal static Task Provider() => Run(true);

    private static async Task Run(bool provider)
    {
        using var artifact = GeneratedModuleCompiler.Compile(OpaqueHostFunctionNames.Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create(false);
        artifact.Definition.Register(runtime.Modules);
        int invoked = 0, metadataCalls = 0;
        Term captured = Term.Tuple(Term.A(OpaqueHostFunctionNames.State), Term.I(OpaqueHostFunctionNames.Result));
        var identity = new LocalFunctionIdentity(
            Term.A(OpaqueHostFunctionNames.HostModule),
            null,
            OpaqueHostFunctionNames.Index,
            OpaqueHostFunctionNames.OldUnique,
            new BitString(new byte[OpaqueHostFunctionNames.UniqueBytes])
        );

        ValueTask<Term> Invoke(ITermExecutionContext _, IReadOnlyList<Term> arguments)
        {
            invoked++;
            Require(arguments.Count == 1 && arguments[0].Equals(Term.I(OpaqueHostFunctionNames.Operand)));

            return ValueTask.FromResult(((TupleTerm)captured).Items[1]);
        }

        LocalFunctionIdentity Identify()
        {
            metadataCalls++;

            return identity;
        }
        FunctionTerm callback = provider
            ? new FunctionTerm(
                Identify,
                1,
                new[] { captured },
                Invoke
            )
            : new FunctionTerm(
                identity,
                1,
                new[] { captured },
                Invoke
            );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            OpaqueHostFunctionNames.Guest,
            OpaqueHostFunctionNames.Entry,
            callback
        ));
        Require(result.Equals(Term.I(OpaqueHostFunctionNames.Result)) && invoked == 1 && metadataCalls == (provider ? 1 : 0));
        Require(callback.Environment.Count == 1 && ReferenceEquals(callback.Environment[0], captured));
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(OpaqueHostFunctionNames.Failure);
    }
}
