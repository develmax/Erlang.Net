using System.Numerics;

namespace Erlang.Tests;

internal static class RuntimeNodeCreationHostChecks
{
    internal static async Task Run()
    {
        foreach (var (requested, expected) in RuntimeNodeCreationHostParameters.TruncationCases)
            await Admit(requested, expected, null);
        await Admit(
            1L << RuntimeNodeCreationHostParameters.WireCreationShift,
            RuntimeNodeCreationHostParameters.CollisionSuccessor,
            RuntimeNodeCreationHostParameters.MinimumCreation
        );
        await Admit(RuntimeNodeCreationHostParameters.MaximumSmallCreation, RuntimeNodeCreationHostParameters.MinimumCreation, uint.MaxValue);
    }

    private static async Task Admit(long requested, uint expected, uint? retainedCreation)
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
        {
            runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

            return true;
        });
        Term? retained = retainedCreation is uint creation
            ? runtime.ImportExternalTerm(ExternalTermFormat.Encode(new ReferenceTerm(RuntimeNodeCreationHostNames.Node, RuntimeNodeCreationHostParameters.RetainedReferenceWords, creation))).Term
            : null;
        await caller.InvokeAsync(async context =>
        {
            foreach (Term invalid in new Term[] { Term.I(-1), new Integer(BigInteger.One << RuntimeNodeCreationHostParameters.FirstNonSmallShift), new FloatTerm(0), Nil.Value })
            {
                bool rejected = false;
                try
                {
                    await Call(context, invalid);
                }
                catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    rejected = true;
                }
                Require(rejected && !runtime.IsDistributionAlive);
            }
            Require((await Call(context, Term.I(requested))).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(runtime.IsDistributionAlive && context.Self.Node == RuntimeNodeCreationHostNames.Node && context.Self.Creation == expected);
            Require((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeSetNodeHostNames.SystemInfo,
                Term.A(RuntimeSetNodeHostNames.Creation)
            )).Equals(Term.I(expected)));

            return true;
        });
        GC.KeepAlive(retained);
    }

    private static ValueTask<Term> Call(ProcessContext context, Term creation) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        RuntimeSetNodeHostNames.SetNode,
        Term.A(RuntimeNodeCreationHostNames.Node),
        creation
    );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(RuntimeNodeCreationHostNames.Failure);
    }
}
