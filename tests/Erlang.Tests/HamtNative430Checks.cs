using System.Numerics;

namespace Erlang.Tests;

internal static class HamtNative430Checks
{
    private static void Require(bool value) => HamtNative430Access.Require(value);

    private static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapHamtMap430.Register(runtime, registry);
                RuntimeBootstrapHamtMap430.Register(runtime, registry);
            });

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    internal static async Task Graph()
    {
        await using var runtime = await Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        MapTerm map = new(Enumerable.Range(1, 256).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.Tuple(Term.I(index), Term.A("payload")))));
        Require((await HamtNative430Access.Type(actor, map)).Equals(Term.A(HamtNative430Names.Head)));
        Term[] children = Cons.Items(await HamtNative430Access.Call(
            actor,
            HamtNative430Names.Internal,
            HamtNative430Names.Children,
            map
        )).ToArray();
        Term[] repeated = Cons.Items(await HamtNative430Access.Call(
            actor,
            HamtNative430Names.Internal,
            HamtNative430Names.Children,
            map
        )).ToArray();
        Require(children.Length == repeated.Length);
        for (int index = 0; index < children.Length; index++)
            Require((await HamtNative430Access.Call(
                actor,
                HamtNative430Names.Debug,
                HamtNative430Names.Same,
                children[index],
                repeated[index]
            )).Equals(Term.A("true")));
        var pending = new Stack<Term>(children);
        int leaves = 0, interiors = 0;
        while (pending.TryPop(out Term? child))
        {
            Term type = await HamtNative430Access.Type(actor, child);
            if (type.Equals(Term.A(HamtNative430Names.Node)))
            {
                Require((await HamtNative430Access.Call(
                    actor,
                    HamtNative430Names.Runtime,
                    HamtNative430Names.IsMap,
                    child
                )).Equals(Term.A("true")));
                foreach (Term item in Cons.Items(await HamtNative430Access.Call(
                    actor,
                    HamtNative430Names.Internal,
                    HamtNative430Names.Children,
                    child
                ))) pending.Push(item);
                interiors++;
            }
            else if (type.Equals(Term.A(HamtNative430Names.Collision)))
                foreach (Term item in ((TupleTerm)child).Items) pending.Push(item);
            else
            {
                Require(type.Equals(Term.A(HamtNative430Names.Leaf)) && child is Cons);
                var pair = (Cons)child;
                Require(map.Get(pair.Head).Equals(pair.Tail));
                leaves++;
            }
        }
        Require(leaves == 256 && interiors > 0);
        Term info = await HamtNative430Access.Call(
            actor,
            HamtNative430Names.Debug,
            HamtNative430Names.Info,
            map
        );
        TupleTerm metric = (TupleTerm)Cons.Items(info).Single(value => value is TupleTerm tuple && tuple.Items[0].Equals(Term.A("leafs")));
        Require(metric.Items[1].Equals(Term.I(leaves)));
        Require(MapIteration.Entries(map, runtime).Count == leaves);
    }

    internal static async Task Types()
    {
        await using var runtime = await Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        BigInteger bound = BigInteger.One << 59;
        var values = new (Term Value, string Type)[]
        {
            (Term.A("a"),"atom"),(Nil.Value,"nil"),(Term.List(Term.I(1)),"list"),
            (new Cons(Term.I(1),Term.I(2)),"list"),(Term.Tuple(),"tuple"),
            (new FloatTerm(1.25),"hfloat"),(new Integer(-bound),"fixnum"),(new Integer(bound-1),"fixnum"),
            (new Integer(-bound-1),"bignum"),(new Integer(bound),"bignum"),
            (new MapTerm([]),"flatmap"),(context.Self,"pid"),(runtime.MakeReference(),"reference"),
            (new Pid("external430@host",7,9),"external_pid"),
            (new PortTerm("external430@host",7,9),"external_port"),
            (new ReferenceTerm("external430@host",7,9),"external_reference"),
            (new BitString(new byte[16]),HamtNative430Names.HeapBinary),
            (new BitString(new byte[128]),HamtNative430Names.SubBinary),
            // The public slice constructor copies a small result into heap storage.
            (new BitString(new byte[128]).SliceBits(8,16),HamtNative430Names.HeapBinary),
            (new BitString(new byte[128]).SliceBits(8,520),HamtNative430Names.SubBinary)
        };
        foreach (var item in values)
            Require((await HamtNative430Access.Type(actor, item.Value)).Equals(Term.A(item.Type)));
        MapTerm small = new([]);
        await HamtNative430Access.Bad(
            actor,
            HamtNative430Names.Debug,
            HamtNative430Names.Info,
            small,
            Term.A("badarg")
        );
        await HamtNative430Access.Bad(
            actor,
            HamtNative430Names.Internal,
            HamtNative430Names.Children,
            small,
            Term.A("badarg")
        );
        foreach (string function in new[] { HamtNative430Names.Info, HamtNative430Names.Children })
            await HamtNative430Access.Bad(
                actor,
                function == HamtNative430Names.Info ? HamtNative430Names.Debug : HamtNative430Names.Internal,
                function,
                Term.I(42),
                Term.Tuple(Term.A("badmap"), Term.I(42))
            );
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        bool rejected = false;
        try
        {
            await HamtNative430Access.Type(actor, (await other.Ready).Self);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
    }
}
