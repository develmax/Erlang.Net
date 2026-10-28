using System.Numerics;
using System.Reflection;

namespace Erlang.Tests;

internal static class SpawnResource540Checks
{
    private static Term Option(string name, Term value) => Term.Tuple(Term.A(name), value);

    private static IEnumerable<Term[]> InvalidCases()
    {
        foreach (string name in new[] { SpawnResource540Names.MinimumHeap, SpawnResource540Names.MinimumBinaryHeap, SpawnResource540Names.MaximumHeap })
        {
            yield return [Option(name, Term.I(-1))];
            yield return [Option(name, Term.A(SpawnResource540Names.Bad))];
            yield return [Option(name, new Integer(BigInteger.One << 70))];
            yield return [Option(name, Term.I(-1)), Option(name, Term.I(0))];
        }
        yield return [Option(SpawnResource540Names.MaximumHeap, new MapTerm([]))];
        yield return [Option(
            SpawnResource540Names.MaximumHeap,
            new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(SpawnResource540Names.Size)] = Term.I(0),
            [Term.A(SpawnResource540Names.Kill)] = Term.I(1)
        })
        )];
        yield return [Option(SpawnResource540Names.MinimumHeap, Term.I(234)), Option(SpawnResource540Names.MaximumHeap, Term.I(233))];
    }

    private static object Analyze(Term[] options, ulong? actualDefault = null)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(SpawnResource540Names.ValidatorType, true)!;

        return type.GetMethod(SpawnResource540Names.Analyze, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [options, 64, actualDefault])!;
    }

    private static bool Flag(object result, string name)
        => (bool)result.GetType().GetProperty(name)!.GetValue(result)!;

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(SpawnResource540Names.Failure);
    }

    internal static Task Validation()
    {
        foreach (Term[] options in InvalidCases())
            Check(Flag(Analyze(options), SpawnResource540Names.Invalid));
        object lowerDefault = Analyze([Option(SpawnResource540Names.MaximumHeap, Term.I(200))], 233);
        Check(Flag(lowerDefault, SpawnResource540Names.Invalid));
        object unboundDefault = Analyze([Option(SpawnResource540Names.MaximumHeap, Term.I(200))]);
        Check(!Flag(unboundDefault, SpawnResource540Names.Invalid));
        Check(Flag(unboundDefault, SpawnResource540Names.PendingDefault));
        object valid = Analyze([Option(
            SpawnResource540Names.MaximumHeap,
            new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(SpawnResource540Names.Size)] = Term.I(0),
            [Term.A(SpawnResource540Names.Kill)] = Term.A(ErlangBooleanAtoms.False),
            [Term.A(SpawnResource540Names.Bad)] = Term.I(-9)
        })
        )]);
        Check(!Flag(valid, SpawnResource540Names.Invalid));
        Check(Flag(valid, SpawnResource540Names.HasResource));

        return Task.CompletedTask;
    }

    internal static async Task InternalErrors()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction body = InternalSpawnRequest429HostAccess.Body(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (Term[] options in InvalidCases())
            {
                var reference = (ReferenceTerm)await body(context, InternalSpawnRequest429HostAccess.Arguments(Cons.From(options)));
                Term reply = await InternalSpawnRequest429HostAccess.ErrorReply(context, reference);
                Check(reply.Equals(Term.Tuple(
                    Term.A(InternalSpawnRequest429HostNames.Reply),
                    reference,
                    Term.A(InternalSpawnRequest429HostNames.Error),
                    Term.A(InternalSpawnRequest429HostNames.BadOptions)
                )));
            }
            Term validHeap = Option(SpawnResource540Names.MinimumHeap, Term.I(233));
            Term invalidReply = Option(ProcessSpawnRequestNames.Reply, Term.A(SpawnResource540Names.Bad));
            Term defaultTag = Term.A(InternalSpawnRequest429HostNames.Reply);
            Term customTag = Term.A(SpawnResource540Names.MixedTag);
            Term tagged = Option(ProcessSpawnRequestNames.ReplyTag, customTag);
            foreach ((Term[] mixed, Term expectedTag) in new[]
            {
                (new[] { validHeap, invalidReply }, defaultTag),
                (new[] { invalidReply, validHeap }, defaultTag),
                (new[] { invalidReply, validHeap, tagged }, customTag),
                (new[] { tagged, validHeap, invalidReply }, customTag)
            })
            {
                var reference = (ReferenceTerm)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessSpawnRequestNames.Function,
                    Term.A(ErlangModuleNames.Module),
                    Term.A(ErlangModuleNames.Self),
                    Nil.Value,
                    Cons.From(mixed)
                );
                Term reply = await InternalSpawnRequest429HostAccess.ErrorReply(context, reference);
                Check(reply.Equals(Term.Tuple(
                    expectedTag,
                    reference,
                    Term.A(InternalSpawnRequest429HostNames.Error),
                    Term.A(InternalSpawnRequest429HostNames.BadOptions)
                )));
            }
            bool refused = false;
            try
            {
                await body(context, InternalSpawnRequest429HostAccess.Arguments(Term.List(Option(SpawnResource540Names.MinimumHeap, Term.I(0)))));
            }
            catch (NotSupportedException)
            {
                refused = true;
            }
            Check(refused);

            return true;
        });
    }

    internal static async Task PublicMetadata()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (Term[] options in InvalidCases())
            {
                bool caught = false;
                try
                {
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessSpawnOptionNames.Function,
                        Term.A(InternalSpawnRequest429HostNames.TargetModule),
                        Term.A(InternalSpawnRequest429HostNames.Target),
                        Nil.Value,
                        Cons.From(options)
                    );
                }
                catch (ErlangException error)
                {
                    caught = true;
                    Check(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                    Check(error.ExplicitStackTrace);
                    Check(error.StackTraceTerm is Cons);
                    var top = (TupleTerm)((Cons)error.StackTraceTerm).Head;
                    Check(top.Items[0].Equals(Term.A(ErlangModuleNames.Module)));
                    Check(top.Items[1].Equals(Term.A(ProcessSpawnOptionNames.Function)));
                    Check(top.Items[2].Equals(Term.List(
                        Term.A(InternalSpawnRequest429HostNames.TargetModule),
                        Term.A(InternalSpawnRequest429HostNames.Target),
                        Nil.Value,
                        Cons.From(options)
                    )));
                    bool badOptionCause = Cons.Items(top.Items[3]).Any(item =>
                        item is TupleTerm { Items: [Atom { Name: SpawnResource540Names.ErrorInfo }, MapTerm fields] }
                        && fields.TryGet(Term.A(ProcessSpawnOptionNames.Cause), out Term? cause)
                        && cause!.Equals(Term.A(ProcessSpawnOptionNames.BadOption)));
                    Check(badOptionCause);
                }
                Check(caught);
            }

            return true;
        });
    }
}
