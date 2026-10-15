using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

public static partial class SourceLoadNifHostChecks
{
    private static async Task ArgumentEvaluationAndGeneratedRoute(bool generated)
    {
        // This is the existing real CodeGeneration -> Roslyn -> loaded assembly
        // helper, not a hand-built ModuleDefinition standing in for compilation.
        using GeneratedModuleArtifact? artifact = generated
            ? GeneratedModuleCompiler.Compile(SourceLoadNifArgumentHostNames.Source)
            : null;
        ModuleDefinition module = artifact?.Definition
            ?? new Parser(SourceLoadNifArgumentHostNames.Source).ParseModule();
        if (generated)
            Require(artifact!.CSharpSha256.Length == SourceLoadNifArgumentHostNames.GeneratedHashLength);
        await using var runtime = new ProcessRuntime();
        module.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(module.Name)!;
        int handlerCalls = 0;
        object? escaped = null;
        Term order = Term.Tuple(
            Term.A(SourceLoadNifArgumentHostNames.First),
            Term.A(SourceLoadNifArgumentHostNames.Second)
        );
        Install(runtime.Modules, async (context, capability, arguments) =>
        {
            handlerCalls++;
            escaped = capability;
            Require(context.Dictionary[Term.A(SourceLoadNifArgumentHostNames.FirstEffect)].Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(context.Dictionary[Term.A(SourceLoadNifArgumentHostNames.Order)].Equals(order));
            Require(arguments.Length == SourceLoadNifHostNames.Arity
                && arguments[0].Equals(Term.I(SourceLoadNifHostNames.Value))
                && arguments[1].Equals(Term.I(SourceLoadNifHostNames.Marker)));
            SourceCodeEntry caller = Consume(capability, context);
            Require(ReferenceEquals(caller.Owner, module) && ReferenceEquals(caller.Image, image));
            Require(References(context, image) == SourceLoadNifHostNames.OwnedDirectReferences);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await NifAdmissionHostBridge.Dispose(reservation);

            return arguments[0];
        });
        await NifAdmissionHostBridge.Context(runtime, async context =>
        {
            // Registry invocation exercises the actual registered generated entry.
            // No test EnterSourceCode or direct EvaluateAsync supplies its identity.
            Term value = await runtime.Modules.Call(context, module.Name, SourceLoadNifArgumentHostNames.TwoEffects);
            Require(value.Equals(Term.I(SourceLoadNifHostNames.Value)));
            Require(context.Dictionary[Term.A(SourceLoadNifArgumentHostNames.Order)].Equals(order));
            Refused(escaped!, context);
            Require(References(context, image) == 0);
            value = await runtime.Modules.Call(context, module.Name, SourceLoadNifArgumentHostNames.NonTailEffects);
            Require(value.Equals(Term.Tuple(Term.I(SourceLoadNifHostNames.Value))));
            Refused(escaped!, context);
            Require(References(context, image) == 0);
            Require(handlerCalls == SourceLoadNifArgumentHostNames.SuccessfulHandlerCalls);
            bool failed = false;
            try
            {
                await runtime.Modules.Call(context, module.Name, SourceLoadNifArgumentHostNames.SecondFails);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A(SourceLoadNifArgumentHostNames.FailureReason)))
            {
                failed = true;
            }
            Require(failed && handlerCalls == SourceLoadNifArgumentHostNames.SuccessfulHandlerCalls);
            Require(context.Dictionary[Term.A(SourceLoadNifArgumentHostNames.FirstEffect)].Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(context.Dictionary[Term.A(SourceLoadNifArgumentHostNames.Order)].Equals(order));
            Require(References(context, image) == 0);
        });
    }
}
