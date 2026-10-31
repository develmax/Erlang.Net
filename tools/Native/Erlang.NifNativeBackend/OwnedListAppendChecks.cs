using System.Reflection;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

public static class OwnedListAppendChecks
{
    public static async Task PhysicalPrefixAndOpaqueReplay()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(OwnedListAppendCaseNames.Source, OwnedListAppendCaseNames.File);
            await using var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            bool opaqueReplay = false;
            int observed = 0;
            runtime.Modules.RegisterCanonicalRead507(
                OwnedListAppendCaseNames.Probe,
                OwnedListAppendCaseNames.Check,
                4,
                (context, arguments) =>
                {
                    var left = (Cons)arguments[0];
                    var result = (Cons)arguments[2];
                    Require(MailboxBackingProbe.Storage(result) is { } storage
                        && ReferenceEquals(storage.Owner, context.TermHeap506));
                    if (!opaqueReplay)
                    {
                        Require(ReferenceEquals(left, arguments[3])
                            && !ReferenceEquals(left, result)
                            && ReferenceEquals(left.Head, result.Head)
                            && ReferenceEquals(((Cons)left.Tail).Head, ((Cons)result.Tail).Head)
                            && ReferenceEquals(((Cons)result.Tail).Tail, arguments[1]));
                    }
                    _ = context.TermHeap506.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(result.Head is TupleTerm { Items.Count: 2 } head
                        && head.Items[0] is ReferenceTerm
                        && head.Items[1] is BitString bits && bits.BitLength == 24);
                    observed++;

                    return ValueTask.FromResult<Term>(Term.A(OwnedListAppendCaseNames.Ok));
                }
            );
            Term first = await Execute(runtime);
            Require(first is TupleTerm { Items.Count: 5 } firstTuple
                && firstTuple.Items[1].Equals(Term.A(OwnedListAppendCaseNames.Tail)));
            // Replaying the exact original delegate via public registration
            // must remain opaque: borrowed native arguments cannot escape.
            var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
                .GetField(OwnedListAppendCaseNames.Functions, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
            ErlangFunction original = functions[(ErlangModuleNames.Module, ErlangOperators.Append, 2)];
            opaqueReplay = true;
            runtime.Modules.Register(
                OwnedListAppendCaseNames.ReplayModule,
                OwnedListAppendCaseNames.ReplayFunction,
                2,
                (context, arguments) =>
                {
                    foreach (Term argument in arguments)
                        Require(MailboxBackingProbe.Storage(argument) is null);

                    return original(context, arguments);
                }
            );
            string replaySource = OwnedListAppendCaseNames.Source.Replace(
                string.Format(OwnedListAppendCaseNames.ModuleDeclarationFormat, OwnedListAppendCaseNames.Module),
                string.Format(OwnedListAppendCaseNames.ModuleDeclarationFormat, OwnedListAppendCaseNames.ReplayCaller),
                StringComparison.Ordinal
            ).Replace(
                OwnedListAppendCaseNames.CanonicalAppendCall,
                string.Format(
                    OwnedListAppendCaseNames.QualifiedCallFormat,
                    OwnedListAppendCaseNames.ReplayModule,
                    OwnedListAppendCaseNames.ReplayFunction
                ),
                StringComparison.Ordinal
            );
            using var replay = GeneratedModuleCompiler.Compile(replaySource, OwnedListAppendCaseNames.File);
            replay.Definition.Register(runtime.Modules);
            Term second = await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context => await runtime.Modules.Call(context, OwnedListAppendCaseNames.ReplayCaller, OwnedListAppendCaseNames.Run)
            );
            Require(second is TupleTerm { Items.Count: 5 } && observed == 2);
            // Ignore only the intentionally fresh reference identity; compare
            // complete append values and error outcomes after the first field.
            var a = (TupleTerm)first;
            var b = (TupleTerm)second;
            Require(a.Items.Skip(1).SequenceEqual(b.Items.Skip(1)));
        }
    }

    private static async Task<Term> Execute(ProcessRuntime runtime)
        => await CpuStatisticsAccess.Actor<Term>(runtime,
            async context => await runtime.Modules.Call(context, OwnedListAppendCaseNames.Module, OwnedListAppendCaseNames.Run));

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(OwnedListAppendCaseNames.Failure);
    }
}
