namespace Erlang.Tests;

internal static class BinarySearchResourceHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term retained = await actor.InvokeAsync(async context =>
        {
            BitString pattern = new([1, 2]);
            var compiled = (TupleTerm)await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.CompilePattern,
                pattern
            );
            var reference = (ResourceReferenceTerm)compiled.Items[1];
            byte[] encoded = ExternalTermFormat.Encode(compiled);
            Term restored = runtime.ImportExternalTerm(encoded).Term;
            BinarySearchMfaContracts.Require(restored.Equals(compiled) && restored is TupleTerm restoredPair
                && restoredPair.Items[1] is ResourceReferenceTerm restoredReference
                && restoredReference.ResourceKind == ReferenceResourceKind.BinarySearchPattern);
            Term found = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.Match,
                new BitString([0, 1, 2]),
                restored
            );
            BinarySearchMfaContracts.Require(found.Equals(Term.Tuple(Term.I(1), Term.I(2))));
            foreach (Term invalid in new Term[]
            {
                Term.Tuple(Term.A(BinarySearchHostNames.Multiple), reference),
                Term.Tuple(Term.A(BinarySearchHostNames.Invalid), reference),
                Term.Tuple(Term.A(BinarySearchHostNames.Single), new ReferenceTerm(reference.Node, reference.Words, reference.Creation)),
                Term.Tuple(Term.A(BinarySearchHostNames.Single), runtime.ReferenceResources.CreateBinary(new byte[] { 1, 2 })),
                Term.Tuple(Term.A(BinarySearchHostNames.Single), Term.I(1))
            })
                await BinarySearchMfaContracts.Invalid(context, BinaryModuleNames.Match, [new BitString([0, 1, 2]), invalid]);
            await BinarySearchMfaContracts.Invalid(context, BinaryModuleNames.CompilePattern, [compiled]);

            return compiled;
        });
        await using var foreign = new ProcessRuntime();
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        await foreignActor.InvokeAsync(async context =>
        {
            await BinarySearchMfaContracts.Invalid(context, BinaryModuleNames.Match, [new BitString([1, 2]), retained]);

            return Nil.Value;
        });
        await actor.DisposeAsync();
        var readers = Enumerable.Range(0, 8).Select(_ => new RuntimeNodeFoundationHostActor(runtime)).ToArray();
        try
        {
            var tasks = readers.Select(reader => reader.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Matches,
                    new BitString([1, 2, 1, 2]),
                    retained
                ))).ToArray();
            await Task.WhenAll(tasks);
            BinarySearchMfaContracts.Require(tasks.All(x => x.Result.Equals(Term.List(Term.Tuple(Term.I(0), Term.I(2)), Term.Tuple(Term.I(2), Term.I(2))))));
        }
        finally
        {
            foreach (var reader in readers)
                await reader.DisposeAsync();
        }
    }
}
