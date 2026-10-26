using System.Text;

namespace Erlang.Tests;

internal static class ReviewFollowup498Checks
{
    internal static async Task ParserHostArguments()
    {
        await using var runtime = new ProcessRuntime();
        string parser = ReviewFollowup498Names.ListToInteger;
        Term[] input = [];
        bool suspend = false;
        runtime.Modules.Register(
            ReviewFollowup498Names.Handler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            async (context, _) =>
            {
                if (suspend)
                    await Task.Yield();

                return await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    parser,
                    input
                );
            }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessErrorHandlerHostNames.ProcessFlag,
                Term.A(ProcessErrorHandlerHostNames.Flag),
                Term.A(ReviewFollowup498Names.Handler)
            );
            Term payload = Term.Tuple(Term.A(ReviewFollowup498Names.Payload), Term.I(42));
            Term saved = Term.List(
                Term.A(ReviewFollowup498Names.MissingModule),
                Term.A(ReviewFollowup498Names.MissingFunction),
                Term.List(payload)
            );
            foreach (bool asyncHandler in new[] { false, true })
                foreach (int depth in new[] { ReviewFollowup498Names.MinimumDepth, ReviewFollowup498Names.ShortDepth, ReviewFollowup498Names.FullDepth })
                    foreach (bool binary in new[] { false, true })
                        foreach (bool explicitBase in new[] { false, true })
                            foreach (string text in new[] { ReviewFollowup498Names.ShortInvalid, ReviewFollowup498Names.LongInvalid })
                            {
                                suspend = asyncHandler;
                                parser = binary ? ReviewFollowup498Names.BinaryToInteger : ReviewFollowup498Names.ListToInteger;
                                Term value = binary ? new BitString(Encoding.UTF8.GetBytes(text)) : Term.String(text);
                                input = explicitBase ? [value, Term.I(ReviewFollowup498Names.DefaultBase)] : [value];
                                await runtime.Modules.Call(
                                    context,
                                    ErlangModuleNames.Module,
                                    ReviewFollowup498Names.SystemFlag,
                                    Term.A(ReviewFollowup498Names.BacktraceDepth),
                                    Term.I(depth)
                                );
                                bool caught = false;
                                try
                                {
                                    await runtime.Modules.Call(
                                        context,
                                        ReviewFollowup498Names.MissingModule,
                                        ReviewFollowup498Names.MissingFunction,
                                        payload
                                    );
                                }
                                catch (ErlangException error)
                                {
                                    caught = true;
                                    Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                                    var frames = Cons.Items(error.StackTraceTerm).Cast<TupleTerm>().ToArray();
                                    Require(frames.Length == Math.Min(2, depth));
                                    Require(frames[0].Items[0].Equals(Term.A(ErlangModuleNames.Module))
                                        && frames[0].Items[1].Equals(Term.A(parser))
                                        && frames[0].Items[2].Equals(Cons.From(input)));
                                    if (frames.Length == 1)
                                        continue;
                                    Require(frames[1].Equals(Term.Tuple(
                                        Term.A(ReviewFollowup498Names.Handler),
                                        Term.A(ProcessErrorHandlerHostNames.Undefined),
                                        saved,
                                        Nil.Value
                                    )));
                                }
                                Require(caught);
                            }

            return Nil.Value;
        });
    }

    internal static async Task ImportedFlatMaps()
    {
        await using var source = new ProcessRuntime();
        await using var target = new ProcessRuntime();
        await target.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(target, registry));
        string firstName = ReviewFollowup498Names.FirstAtom + Guid.NewGuid();
        string secondName = ReviewFollowup498Names.SecondAtom + Guid.NewGuid();
        MapTerm imported;
        TupleTerm shared;
        using (source.EnterAtomScope())
        {
            Term first = Term.A(firstName);
            Term second = Term.A(secondName);
            shared = Term.Tuple(Term.I(42));
            imported = new MapTerm([new(first, shared), new(second, shared)]);
        }
        var hostMap = new MapTerm([new(Term.A(firstName), shared), new(Term.A(secondName), shared)]);
        using (target.EnterAtomScope())
        {
            Term.A(secondName);
            Term.A(firstName);
        }
        await using var actor = new RuntimeNodeFoundationHostActor(target);
        await actor.InvokeAsync(async context =>
        {
            foreach (MapTerm current in new[] { imported, hostMap })
            {
                var expected = new MapTerm(current.Entries);
                Require(!DebugSharing425Access.Keys(current).Equals(DebugSharing425Access.Keys(expected)));
                foreach (string flag in new[] { DebugSharing425Names.FalseAtom, DebugSharing425Names.TrueAtom })
                {
                    var copied = (MapTerm)await DebugSharing425Access.Call(
                        context,
                        DebugSharing425Names.Copy,
                        current,
                        Term.A(flag)
                    );
                    Require(copied.Equals(current) && !ReferenceEquals(copied, current));
                    Require(DebugSharing425Access.Keys(copied).Equals(DebugSharing425Access.Keys(expected)));
                    Require(ReferenceEquals(copied.Get(Term.A(firstName)), copied.Get(Term.A(secondName)))
                        && !ReferenceEquals(copied.Get(Term.A(firstName)), shared));
                    var importedGraph = Term.Tuple(current, current, DebugSharing425Access.Keys(current));
                    var importedCopy = (TupleTerm)await DebugSharing425Access.Call(
                        context,
                        DebugSharing425Names.Copy,
                        importedGraph,
                        Term.A(flag)
                    );
                    Require(importedCopy.Equals(importedGraph)
                        && ReferenceEquals(importedCopy.Items[0], importedCopy.Items[1])
                        && importedCopy.Items[2].Equals(DebugSharing425Access.Keys(current)));
                    Require(!ReferenceEquals(DebugSharing425Access.Keys((MapTerm)importedCopy.Items[0]), importedCopy.Items[2]));
                    var graph = Term.Tuple(copied, DebugSharing425Access.Keys(copied));
                    var again = (TupleTerm)await DebugSharing425Access.Call(
                        context,
                        DebugSharing425Names.Copy,
                        graph,
                        Term.A(flag)
                    );
                    Require(ReferenceEquals(DebugSharing425Access.Keys((MapTerm)again.Items[0]), again.Items[1]));
                }
            }

            return Nil.Value;
        });
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ReviewFollowup498Names.Failure);
    }
}
