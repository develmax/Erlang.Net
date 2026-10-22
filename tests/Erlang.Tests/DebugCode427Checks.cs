using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class DebugCode427Checks
{

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugCode427Names.Failure);
    }

    internal static Task Owner()
    {
        Type owner = typeof(ProcessRuntime).Assembly.GetType(DebugCode427Names.ProfileOwner, true)!;
        PropertyInfo selected = owner.GetProperty(DebugCode427Names.ProfileProperty, DebugCode427Names.Members)!;
        object profile = selected.GetValue(null) ?? throw new InvalidOperationException(DebugCode427Names.Failure);
        Require(ReferenceEquals(profile, selected.GetValue(null)));

        object Read(string name) => profile.GetType().GetProperty(name, DebugCode427Names.Members)!.GetValue(profile)!;
        var entry = (MethodInfo)Read(DebugCode427Names.EntryProperty);
        var machine = (MethodInfo)Read(DebugCode427Names.MachineBodyProperty);
        Require(Read(DebugCode427Names.OwnerProperty).Equals(owner) && entry.DeclaringType == owner && entry.Name == DebugCode427Names.EntryMethod
            && machine.DeclaringType == entry.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType);
        Require(Read(DebugCode427Names.ModuleIdentityProperty).Equals(entry.Module.ModuleVersionId) && entry.Module == machine.Module
            && entry.Module.Assembly == typeof(ProcessRuntime).Assembly);
        Require(Read(DebugCode427Names.EntryHashProperty).Equals(Convert.ToHexString(SHA256.HashData(entry.GetMethodBody()!.GetILAsByteArray()!)))
            && Read(DebugCode427Names.MachineHashProperty).Equals(Convert.ToHexString(SHA256.HashData(machine.GetMethodBody()!.GetILAsByteArray()!))));
        Require(machine.DeclaringType!.GetFields(DebugCode427Names.Members).Any(field => field.FieldType.FullName == DebugCode427Names.RegisterState));

        return Task.CompletedTask;
    }

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDebugCode427.Register(runtime, registry);
            RuntimeBootstrapDebugCode427.Register(runtime, registry);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var function = runtime.Functions.External(runtime, new(Term.A(DebugCode427Names.RuntimeModule), Term.A(DebugCode427Names.RuntimeSelf), 0));
            Term[] values = [Term.A(DebugCode427Names.InvalidValue), Nil.Value, Term.I(0), Term.I(-1), new Integer(System.Numerics.BigInteger.One << 130),
                new FloatTerm(1.25), new BitString(new byte[] { 1 }), Term.Tuple(), new MapTerm([]), context.Self,
                runtime.MakeReference(), function, Term.Tuple(Term.A(DebugCode427Names.RuntimeModule), Term.A(DebugCode427Names.RuntimeSelf), Term.I(0)),
                Term.Tuple(Term.A(DebugCode427Names.RuntimeModule), Term.A(DebugCode427Names.RuntimeSelf), Term.I(-1)), new Cons(Term.A(DebugCode427Names.PairKey), Term.A(DebugCode427Names.PairValue))];
            foreach (Term value in values)
                Require((await runtime.Modules.Call(
                    context,
                    DebugCode427Names.Module,
                    DebugCode427Names.Disassemble,
                    value
                )).Equals(Term.A(DebugCode427Names.FalseAtom)));
            Require((await runtime.Modules.Call(context, DebugCode427Names.Module, DebugCode427Names.Size)).Equals(Term.I(0)));

            return true;
        });
        Type native = typeof(ProcessRuntime).Assembly.GetType(DebugCode427Names.CaptureOwner, true)!;
        object?[] arguments = [runtime, DebugCode427Names.Module, DebugCode427Names.Disassemble, 1, null];
        Require((bool)native.GetMethod(DebugCode427Names.Capture, DebugCode427Names.Members)!.Invoke(null, arguments)!);
        var body = (ErlangFunction)arguments[4]!;
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        bool refused = false;
        try
        {
            await other.InvokeAsync(async context => await body(context, [Term.I(0)]));
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
    }
}
