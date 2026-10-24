using Erlang;
using static Erlang.Tests.PrimTtyNative412Names;

namespace Erlang.Tests;

internal static class PrimTtyNative412Checks
{
    private static IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Table(object owner)
        => (IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>>)BeamBinaryHostAccess.Call(owner, Bindings);

    private static ErlangFunction Body(object owner, string name)
        => Table(owner).Single(entry => entry.Key.Name == name).Value;

    internal static async Task Pure()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = (IAsyncDisposable)BeamBinaryHostAccess.New(Owner, runtime);
        var table = Table(owner);
        BeamBinaryHostAccess.Require(table.Count == 20 && table.Select(entry => entry.Key).Distinct().Count() == 20);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term CallExpected(bool value) => Term.A(value ? True : False);
            foreach (int value in new[] { 0, 31, 32, 65, 126, 127 })
            {
                Term actual = await Body(owner, Isprint)(context, [Term.I(value)]);
                BeamBinaryHostAccess.Require(actual.Equals(CallExpected(value >= 32 && value < 127)));
            }
            BeamBinaryHostAccess.Require((await Body(owner, SizeofWchar)(context, [])).Equals(Term.I(2)));
            BeamBinaryHostAccess.Require((await Body(owner, Wcswidth)(context, [Term.List(Term.I(1), Term.I(2), Term.I(3))]))
                .Equals(Term.Tuple(Term.A(Ok), Term.I(1))));
            BeamBinaryHostAccess.Require((await Body(owner, Wcwidth)(context, [Term.I(65)]))
                .Equals(Term.Tuple(Term.A(Error), Term.A(NoSupport))));
            foreach (string name in Termcap)
            {
                int arity = table.Single(entry => entry.Key.Name == name).Key.Arity;
                Term actual = await Body(owner, name)(context, Enumerable.Repeat<Term>(Nil.Value, arity).ToArray());
                BeamBinaryHostAccess.Require(actual.Equals(Term.Tuple(Term.A(Error), Term.A(NoSupport))));
            }

            return true;
        });
    }

    internal static async Task Resource()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = (IAsyncDisposable)BeamBinaryHostAccess.New(Owner, runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term reference = await actor.InvokeAsync(async context =>
        {
            Term value = await Body(owner, Create)(context, [Term.A(Stdout)]);
            BeamBinaryHostAccess.Require(value is TupleTerm { Items.Count: 2 } tuple
                && tuple.Items[0].Equals(Term.A(Ok)) && tuple.Items[1] is ResourceReferenceTerm);
            Term resource = ((TupleTerm)value).Items[1];
            BeamBinaryHostAccess.Require((await Body(owner, SetLocale)(context, [resource])).Equals(Term.A(True)));
            var disabled = new MapTerm(new Dictionary<Term, Term> { [Term.A(Input)] = Term.A(Disabled) });
            var raw = new MapTerm(new Dictionary<Term, Term> { [Term.A(Input)] = Term.A(Raw) });
            BeamBinaryHostAccess.Require((await Body(owner, Init)(context, [resource, disabled])).Equals(Term.A(Ok)));
            BeamBinaryHostAccess.Require((await Body(owner, Init)(context, [resource, raw]))
                .Equals(Term.Tuple(Term.A(Error), Term.A(NoSupport))));
            Term encoding = await Body(owner, Encoding)(context, [resource]);
            BeamBinaryHostAccess.Require(encoding.Equals(Term.A(Utf8)) || encoding.Equals(Term.Tuple(Term.A(Utf16), Term.A(Little))));

            return resource;
        });
        await using var foreign = new ProcessRuntime();
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        await foreignActor.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await Body(owner, Encoding)(context, [reference]);
            }
            catch (ArgumentException)
            {
                refused = true;
            }
            BeamBinaryHostAccess.Require(refused);

            return true;
        });
        await owner.DisposeAsync();
        await actor.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await Body(owner, Encoding)(context, [reference]);
            }
            catch (ObjectDisposedException)
            {
                refused = true;
            }
            BeamBinaryHostAccess.Require(refused);

            return true;
        });
    }

    internal static async Task Barrier()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        int finished = 0;
        Func<int> body = () =>
        {
            entered.Set();
            try
            {
                release.Wait();

                return 1;
            }
            finally
            {
                Interlocked.Exchange(ref finished, 1);
            }
        };
        var method = BeamBinaryHostAccess.Type(IoType).GetMethods(
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(value => value.Name == IoRun).MakeGenericMethod(typeof(int));
        var pending = (Task<int>)method.Invoke(null, [body, cancellation.Token])!;
        bool canceled = false;
        try
        {
            BeamBinaryHostAccess.Require(entered.Wait(TimeSpan.FromSeconds(DeadlineSeconds)));
            cancellation.Cancel();
            await Task.WhenAny(pending, Task.Delay(SettleMilliseconds));
            BeamBinaryHostAccess.Require(!pending.IsCompleted && Volatile.Read(ref finished) == 0);
        }
        finally
        {
            release.Set();
            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
        }
        BeamBinaryHostAccess.Require(canceled && Volatile.Read(ref finished) == 1);
    }

    internal static async Task BlockedRead()
    {
        using var pipe = new System.IO.Pipes.AnonymousPipeServerStream(
            System.IO.Pipes.PipeDirection.In, HandleInheritability.None);
        using var cancellation = new CancellationTokenSource();
        using var entered = new ManualResetEventSlim();
        int finished = 0;
        var read = BeamBinaryHostAccess.Type(NativeType).GetMethod(NativeRead,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        Func<int> body = () =>
        {
            byte[] buffer = new byte[1];
            object?[] arguments = [pipe.SafePipeHandle.DangerousGetHandle(), buffer, (uint)buffer.Length, (uint)0, (nint)0];
            entered.Set();
            try
            {
                return (bool)read.Invoke(null, arguments)! ? (int)(uint)arguments[3]! : -1;
            }
            finally
            {
                Interlocked.Exchange(ref finished, 1);
            }
        };
        var run = BeamBinaryHostAccess.Type(IoType).GetMethods(
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(value => value.Name == IoRun).MakeGenericMethod(typeof(int));
        var pending = (Task<int>)run.Invoke(null, [body, cancellation.Token])!;
        bool canceled = false;
        try
        {
            BeamBinaryHostAccess.Require(entered.Wait(TimeSpan.FromSeconds(DeadlineSeconds)));
            await Task.WhenAny(pending, Task.Delay(SettleMilliseconds));
            BeamBinaryHostAccess.Require(!pending.IsCompleted);
            cancellation.Cancel();
            Task settled = await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(DeadlineSeconds)));
            BeamBinaryHostAccess.Require(ReferenceEquals(settled, pending));
        }
        finally
        {
            cancellation.Cancel();
            // Release the genuine pipe if the assertion fails; never abandon the worker.
            pipe.DisposeLocalCopyOfClientHandle();
            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
        }
        BeamBinaryHostAccess.Require(canceled && Volatile.Read(ref finished) == 1);
    }

}
