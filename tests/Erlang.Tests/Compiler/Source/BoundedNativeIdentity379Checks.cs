using Erlang.Compiler;
using static Erlang.Tests.BoundedNativeIdentity379Names;

namespace Erlang.Tests;

internal static class BoundedNativeIdentity379Checks
{
    internal static async Task Run(ModuleDefinition definition, Term publicOutcome)
    {
        // Validate actual detached public topology, including legitimate CLR map indexes.
        KnownHostGraph384Checks.RequireDetached(publicOutcome);
        var failures = new List<Exception>();
        foreach (bool enabled in new[] { false, true })
            try
            {
                await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
                await DebugSame420HostAccess.Initialize(runtime);
                await runtime.InitializeModulesAsync(registry =>
                {
                    definition.Register(registry);
                    new Parser(Source).ParseModule().Register(registry);
                });
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                await actor.InvokeAsync(async context =>
                {
                    Term actual = await runtime.Modules.Call(context, TestModule, BoundedNativeIdentity379Names.Run);
                    Require(actual.Equals(Term.Tuple(Term.A(ErlangBooleanAtoms.True), Term.A(ErlangBooleanAtoms.True))));
                    TupleTerm identity;
                    lock (Field(context.Dictionary, Gate))
                        identity = (TupleTerm)((Dictionary<Term, Term>)Field(context.Dictionary, Data))[Term.A(Key)];
                    Term[] payloads = identity.Items.ToArray();
                    object[] native = payloads.Select(value => value.GetType().GetProperty(Storage, Members)!.GetValue(value)!).ToArray();
                    Require(native.All(value => value is not null));
                    Require(ReferenceEquals(native[0], native[2]) && ReferenceEquals(native[1], native[2]));
                    Require(context.Dictionary.Remove(Term.A(Key)));

                    return Nil.Value;
                });
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
