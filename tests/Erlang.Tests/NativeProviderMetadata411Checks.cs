using Erlang;
using static Erlang.Tests.NativeProviderMetadata411Names;

namespace Erlang.Tests;

internal static class NativeProviderMetadata411Checks
{
    private static string[] Modules => [NativeProvider408Names.Ets, NativeProvider408Names.Module];

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime();
        var before = runtime.Modules.Exports.ToHashSet();
        NativeProvider408Access.Require(runtime.NativePrimitives.Bindings.Count == 0
            && !before.Any(value => Modules.Contains(value.Module)));
        await NativeProvider408Access.Enable(runtime);
        NativeProvider408Access.Both(runtime);
        NativeProvider408Access.Require(runtime.Modules.Exports.Count == before.Count + ProviderExports
            && runtime.NativePrimitives.Bindings.Count == NativeCount
            && !runtime.NativePrimitives.Bindings.Any(value => value.Function == Function));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string module in Modules)
            {
                Term all = await runtime.Modules.Call(context, module, Function);
                NativeProvider408Access.Require(ReferenceEquals(all, runtime.Modules.ModuleInformation(module)));
                foreach (string key in new[] { ModuleKey, ExportsKey, Md5Key, CompileKey })
                {
                    Term actual = await runtime.Modules.Call(context, module, Function, Term.A(key));
                    NativeProvider408Access.Require(actual.Equals(runtime.Modules.ModuleInformation(module, key)));
                }
                Term identity = await runtime.Modules.Call(context, module, Function, Term.A(ModuleKey));
                NativeProvider408Access.Require(identity.Equals(Term.A(module)));
                var exports = Cons.Items(runtime.Modules.ModuleInformation(module, ExportsKey))
                    .Cast<TupleTerm>().Select(value => (((Atom)value.Items[0]).Name, (int)((Integer)value.Items[1]).Value)).ToHashSet();
                NativeProvider408Access.Require(exports.SetEquals(runtime.Modules.Exports.Where(value => value.Module == module)
                    .Select(value => (value.Function, value.Arity))));
                for (int arity = 0; arity <= 1; arity++)
                    NativeProvider408Access.Require(runtime.Modules.SourceOwner(module, Function, arity) is null
                        && !(bool)BeamBinaryHostAccess.Call(runtime.Modules, NativeMethod, module, Function, arity));
            }
            return true;
        });
    }

    internal static async Task Errors()
    {
        await using var runtime = new ProcessRuntime();
        await NativeProvider408Access.Enable(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string module in Modules)
                foreach (Term key in new Term[] { Term.A(UnknownKey), Term.I(1), Nil.Value })
                {
                    bool refused = false;
                    try { await runtime.Modules.Call(context, module, Function, key); }
                    catch (ErlangException error)
                    {
                        refused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))
                            && error.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } frame }
                            && frame.Items[0].Equals(Term.A(NativeProvider408Names.RuntimeModule))
                            && frame.Items[1].Equals(Term.A(BifFunction))
                            && frame.Items[2].Equals(Term.List(Term.A(module), key));
                    }
                    NativeProvider408Access.Require(refused);
                }
            return true;
        });
        await using var foreign = new ProcessRuntime();
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        await foreignActor.InvokeAsync(async context =>
        {
            bool refused = false;
            try { await runtime.Modules.Call(context, NativeProvider408Names.Ets, Function); }
            catch (ArgumentException) { refused = true; }
            NativeProvider408Access.Require(refused && foreign.NativePrimitives.Bindings.Count == 0);
            return true;
        });
    }

    internal static async Task Ownership()
    {
        await using (var runtime = new ProcessRuntime())
        {
            await NativeProvider408Access.Enable(runtime);
            var before = runtime.Modules.Exports.ToHashSet();
            Term fingerprint = runtime.Modules.ModuleInformation(NativeProvider408Names.Ets, Md5Key);
            await NativeProvider408Access.Enable(runtime);
            NativeProvider408Access.Require(before.SetEquals(runtime.Modules.Exports)
                && fingerprint.Equals(runtime.Modules.ModuleInformation(NativeProvider408Names.Ets, Md5Key)));
            await runtime.InitializeModulesAsync(registry => registry.Register(NativeProvider408Names.Ets, OtherFunction, 0,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)));
            bool refused = false;
            try { await NativeProvider408Access.Enable(runtime); }
            catch (InvalidOperationException) { refused = true; }
            NativeProvider408Access.Require(refused && runtime.NativePrimitives.Bindings.Count == NativeCount);
        }
        await using (var occupied = new ProcessRuntime())
        {
            var owner = new object();
            await occupied.InitializeModulesAsync(registry => registry.RegisterSource(NativeProvider408Names.Ets, Function, 0,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value), owner));
            var before = occupied.Modules.Exports.ToHashSet();
            bool refused = false;
            try { await NativeProvider408Access.Enable(occupied); }
            catch (InvalidOperationException) { refused = true; }
            NativeProvider408Access.Require(refused && before.SetEquals(occupied.Modules.Exports)
                && occupied.NativePrimitives.Bindings.Count == 0
                && ReferenceEquals(owner, occupied.Modules.SourceOwner(NativeProvider408Names.Ets, Function, 0))
                && !occupied.Modules.Exports.Any(value => value.Module == NativeProvider408Names.Module));
        }
    }
}
