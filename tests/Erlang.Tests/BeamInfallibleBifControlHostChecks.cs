using Erlang;
using static Erlang.Tests.BeamInfallibleBifControlNames;

namespace Erlang.Tests;

internal static class BeamInfallibleBifControlHostChecks
{
    internal static async Task CanonicalOpcodeAndRemoteOverride()
    {
        foreach ((string function, int arity) in new[] { (Get, 1), (Min, 2), (Max, 2) })
        {
            await using var runtime = new ProcessRuntime();
            runtime.Modules.DeleteModule(ErlangModuleNames.Module);
            int overrides = 0;
            runtime.Modules.Register(
                ErlangModuleNames.Module,
                function,
                arity,
                (_, _) =>
            {
                overrides++;
                throw new ErlangException(Term.A(OverrideFailure));
            }
            );
            BeamModule opcode = BeamModule.Prepare(BeamInfallibleBifControlBytes.Image(function, arity));
            BeamModule remote = BeamModule.Prepare(BeamInfallibleBifControlBytes.Remote(function, arity));
            Require(opcode.Supports(BeamOpcodeEdgeNames.Run, arity) && remote.Supports(BeamOpcodeEdgeNames.Run, arity));
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term first = Term.I(1), second = new FloatTerm(1.0), stored = Term.Tuple(Term.A(OverrideFailure), Term.I(394));
                context.Dictionary[first] = stored;
                Term[] arguments = arity == 1 ? [first] : [first, second];
                Term expected = arity == 1 ? stored : first;
                Term result = await opcode.Invoke(context, BeamOpcodeEdgeNames.Run, arguments);
                Require(result.Equals(expected) && ReferenceEquals(result, expected) && overrides == 0);
                if (arity == 1)
                    Require((await opcode.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(2))).Equals(Term.A(ProcessDictionaryAtoms.Undefined)));
                bool failed = false;
                try
                {
                    _ = await remote.Invoke(context, BeamOpcodeEdgeNames.Run, arguments);
                }
                catch (ErlangException error)
                {
                    failed = error.Reason.Equals(Term.A(OverrideFailure));
                }
                Require(failed && overrides == 1);
                failed = false;
                try
                {
                    _ = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        function,
                        arguments
                    );
                }
                catch (ErlangException error)
                {
                    failed = error.Reason.Equals(Term.A(OverrideFailure));
                }
                Require(failed && overrides == 2 && context.Mailbox.Count == 0);

                return true;
            });
        }
    }

    internal static Task FallibleGcAndOpaqueRefusals()
    {
        foreach (byte[] bytes in new[]{
            BeamInfallibleBifControlBytes.Image(Length,1),
            BeamInfallibleBifControlBytes.Image(TupleSize,1),
            BeamInfallibleBifControlBytes.Image(Get,1,gc:true),
            BeamInfallibleBifControlBytes.Image(Get,1,opaque:true),
            BeamInfallibleBifControlBytes.Image(Get,1,missingArgument:true)})
        {
            bool rejected = false;
            try
            {
                rejected = !BeamModule.Prepare(bytes).Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                rejected = true;
            }
            Require(rejected);
        }

        return Task.CompletedTask;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
