using System.Collections;
using System.Numerics;
using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.HostCopy827Names;

namespace Erlang.Tests;

internal static class HostCopy827Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly MethodInfo ReadStorage = typeof(ProcessRuntime).Assembly.GetType(StorageType)!
        .GetMethod(Storage, BindingFlags.Static | BindingFlags.NonPublic)!;

    internal static async Task Run()
    {
        foreach (bool privateProfile in new[] { false, true })
        {
            await using ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create(privateProfile);
            ModuleDefinition definition = new Parser(Source).ParseModule();
            await using PreparedSourceModule819 prepared = definition.PrepareSourceModule819(runtime.Modules);
            await runtime.InitializeModulesAsync(_ => prepared.Register());
            await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
            await using var sender = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext receiving = await receiver.Ready;
            await sender.InvokeAsync(async context =>
            {
                _ = await runtime.Modules.Call(
                    context,
                    HostCopy827Names.Module,
                    Function,
                    receiver.Process.Pid
                );

                return true;
            });
            await receiver.InvokeAsync(async context =>
            {
                var entries = (IEnumerable)typeof(Mailbox).GetField(Messages, Members)!.GetValue(context.Mailbox)!;
                Term queued = entries.Cast<object>().Select(entry => (Term)entry.GetType().GetProperty(Value, Members)!.GetValue(entry)!).Single();
                Require(ReadStorage.Invoke(null, [queued]) is not null);
                Term? copied = await context.Mailbox.ReceiveAsync<Term>(value => value, TimeSpan.FromSeconds(10));
                Require(copied is not null);
                var pending = new Stack<Term>();
                pending.Push(copied!);
                while (pending.TryPop(out Term? value))
                {
                    Require(ReadStorage.Invoke(null, [value]) is null);
                    if (value is TupleTerm tuple)
                        foreach (Term child in tuple.Items) pending.Push(child);
                    else if (value is Cons list)
                    {
                        pending.Push(list.Head);
                        pending.Push(list.Tail);
                    }
                }
                Term expected = Term.Tuple(
                    Term.A(Tag),
                    Term.List(Term.I(SmallInteger), new Integer(BigInteger.One << IntegerShift)),
                    new BitString([BitsByte], BitsLength)
                );
                Require(copied!.Equals(expected));

                return true;
            });
            GC.KeepAlive(receiving);
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
