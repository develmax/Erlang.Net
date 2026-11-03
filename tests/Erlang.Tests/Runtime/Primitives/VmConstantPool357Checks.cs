using Erlang.Compiler;
using static Erlang.Tests.VmConstantPool357Names;

namespace Erlang.Tests;

internal static class VmConstantPool357Checks
{
    internal static async Task Run()
    {
        foreach (bool enabled in new[] { false, true })
        {
            await using var first = OwnedGuest506RuntimeFactory.Create(enabled);
            await using var second = OwnedGuest506RuntimeFactory.Create(enabled);
            await first.InitializeModulesAsync(registry => new Parser(Source).ParseModule().Register(registry));
            await second.InitializeModulesAsync(registry => new Parser(Source).ParseModule().Register(registry));
            await using var firstActor = new RuntimeNodeFoundationHostActor(first);
            await using var secondActor = new RuntimeNodeFoundationHostActor(second);
            Term[] one = await Load(firstActor);
            Term[] two = await Load(secondActor);
            Require(!ReferenceEquals(one[0], two[0]) && !ReferenceEquals(one[2], two[2]));
            Task[] oneRetirement = one.Select(RetirementOf).Distinct().ToArray();
            Task[] twoRetirement = two.Select(RetirementOf).Distinct().ToArray();
            Require(oneRetirement.Length == 3 && twoRetirement.Length == 3);
            await secondActor.DisposeAsync();
            await second.DisposeAsync();
            await Task.WhenAll(twoRetirement).WaitAsync(Deadline);
            Require(((BitString)one[0]).ToArray().AsSpan().SequenceEqual(new byte[] { 97, 98, 99 }));
            await firstActor.InvokeAsync(async context =>
            {
                var portable = (BitString)await first.Modules.Call(
                    context,
                    VmConstantPool357Names.Erlang,
                    AtomToBinary,
                    Term.A(Abc)
                );
                Require(portable.GetType().GetProperty(OwnedBinary, Members)!.GetValue(portable) is null);
                Require(portable.ToArray().AsSpan().SequenceEqual(new byte[] { 97, 98, 99 }));

                return Nil.Value;
            });
            await PublicBoundaryOverride(enabled);
            await firstActor.InvokeAsync(async context =>
            {
                _ = await first.Modules.Call(context, TestModule, Clear);

                return Nil.Value;
            });
            Require(oneRetirement.All(task => !task.IsCompleted));
            Require(((BitString)one[0]).BitLength == 24 && ((BitString)one[1]).BitLength == 0);
            Require(((MapTerm)one[2]).Entries.Count == 1);
            await firstActor.DisposeAsync();
            await first.DisposeAsync();
            await Task.WhenAll(oneRetirement).WaitAsync(Deadline);
            Stale(() => _ = ((BitString)one[0]).ToArray());
            Stale(() => _ = ((BitString)one[1]).BitLength);
            Stale(() => _ = ((MapTerm)one[2]).Entries[0].Key);
        }
    }

    private static async Task PublicBoundaryOverride(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        var hostBinary = new BitString(new byte[] { 97, 98, 99 });
        int calls = 0;
        await runtime.InitializeModulesAsync(_ => { });
        Require(Field(runtime, VmPool380) is null);
        await runtime.InitializeModulesAsync(registry =>
        {
            Require(registry.DeleteModule(VmConstantPool357Names.Erlang).Equals(Term.A(ErlangBooleanAtoms.True)));
            registry.Register(
                VmConstantPool357Names.Erlang,
                AtomToBinary,
                1,
                (context, arguments) =>
            {
                Interlocked.Increment(ref calls);
                Require(arguments[0] is BitString input && input.Equals(hostBinary));
                Require(arguments[0].GetType().GetProperty(OwnedBinary, Members)!.GetValue(arguments[0]) is null);
                object invocation = Property(context, CurrentOwned380);
                string kind = Property(invocation, BindingKind380).ToString()!;
                bool parentCanBorrow = (bool)Property(invocation, CanBorrow380);
                Require(kind == Opaque380);
                // Effective borrowing requires both the actual parent flag and
                // a reviewed binding kind. Opaque registration has no grant.
                Require(!(parentCanBorrow && kind != Opaque380));
                Console.WriteLine(
                    AuthorityDiagnostic380,
                    enabled,
                    kind,
                    parentCanBorrow
                );
                Require(Field(runtime, VmPool380) is null);

                return ValueTask.FromResult<Term>(hostBinary);
            }
            );
            new Parser(OverrideSource380).ParseModule().Register(registry);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term returned = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                TestModule,
                Override,
                hostBinary
            ));
        Require(calls == 1 && returned is BitString && returned.Equals(hostBinary));
        Require(returned.GetType().GetProperty(OwnedBinary, Members)!.GetValue(returned) is null);
        Require(Field(runtime, VmPool380) is null);
    }

    private static Task<Term[]> Load(RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync(async context =>
        {
            _ = await context.Runtime.Modules.Call(context, TestModule, Keep);
            object dictionary = context.Dictionary;
            Term[] values;
            lock (Field(dictionary, Gate))
            {
                var data = (Dictionary<Term, Term>)Field(dictionary, Data);
                values = new[] { Binary, Empty, Info, CrossInfo, Again }.Select(key => data[Term.A(key)]).ToArray();
            }
            Require(ReferenceEquals(values[0], values[4]) && ReferenceEquals(values[2], values[3]));
            Require(((BitString)values[0]).BitLength == 24 && ((BitString)values[1]).BitLength == 0);
            Require(((MapTerm)values[2]).Get(Term.A(MetadataModule)).Equals(Term.A(Formatter)));
            foreach (Term value in values) Require(!RetirementOf(value).IsCompleted);

            return values;
        });

    private static Task RetirementOf(Term value)
    {
        object storage = value is BitString
            ? Property(value, OwnedBinary) : Property(Property(value, OwnedMap), RootPayload);
        object domain = Property(Property(storage, Owner), Domain);

        return (Task)Property(domain, Retirement);
    }

    private static object Property(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object Field(object target, string name)
        => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static void Stale(Action read)
    {
        try
        {
            read();
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
