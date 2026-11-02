using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.IntrinsicHalt811Names;

namespace Erlang.Tests;

public static class IntrinsicHalt811Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static async Task ActualHaltRetiresIntrinsic()
    {
        ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? frame = null, holder = null, reader = null;
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            object domain = Get(runtime, Domain);
            TupleTerm canonical = (TupleTerm)Get(domain, Value);
            object cells = Field(domain, Cells);
            actor = new(runtime);
            await actor.InvokeAsync(async context =>
            {
                await context.ExecutionCheckpoint();
                frame = (IDisposable)Call(Get(context, Heap), Enter)!;
                Term actual = (Term)Call(
                    frame,
                    TupleConstructor,
                    Call(frame, Reserve)!,
                    Array.Empty<Term>()
                )!;
                Require(ReferenceEquals(actual, canonical));
                object authority = ((IEnumerable)Get(frame, Authorities)).Cast<object>().Single();
                holder = (IDisposable)Call(authority, Retain)!;

                return Nil.Value;
            });
            reader = (IDisposable)Call(domain, Read, Get(canonical, Storage))!;
            frame!.Dispose();
            frame = null;
            await actor.DisposeAsync();
            actor = null;
            RuntimeHaltResult result = await runtime.RequestImmediateHalt(Status).WaitAsync(Deadline);
            Require(result.Status == Status);
            var elapsed = Stopwatch.StartNew();
            while (Snapshot(domain).Open)
            {
                if (runtime.HaltQuiescence.IsCompleted)
                {
                    await runtime.HaltQuiescence;
                    throw new InvalidOperationException(Failure);
                }
                Require(elapsed.Elapsed < Deadline);
                await Task.Delay(1);
            }
            Require(!runtime.HaltQuiescence.IsCompleted && !(bool)Get(cells, Disposed));
            Require(Snapshot(domain) == (false, 1, 1));
            holder!.Dispose();
            holder = null;
            Require(Snapshot(domain) == (false, 0, 1));
            Require(!runtime.HaltQuiescence.IsCompleted && !(bool)Get(cells, Disposed));
            reader.Dispose();
            reader = null;
            await runtime.HaltQuiescence.WaitAsync(Deadline);
            Require((bool)Get(cells, Disposed) && (await runtime.HaltCompletion).Status == Status);
            bool rejected = false;
            try
            {
                _ = canonical.Items.Count;
            }
            catch (ObjectDisposedException)
            {
                rejected = true;
            }
            Require(rejected);
            // These assertions precede DisposeAsync: the real halt path, not a
            // later host disposal, must have reclaimed the native allocation.
        }
        finally
        {
            holder?.Dispose();
            reader?.Dispose();
            frame?.Dispose();
            if (actor is not null) await actor.DisposeAsync();
            await runtime.DisposeAsync();
        }
    }

    private static (bool Open, int Holders, int Readers) Snapshot(object domain)
    {
        lock (Field(domain, Gate))
            return ((bool)Field(domain, Open), (int)Get(Field(domain, Holders), Count), (int)Field(domain, Readers));
    }

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static object? Call(object value, string name, params object[] arguments)
    {
        try
        {
            return value.GetType().GetMethods(Members).Single(method => method.Name == name && method.GetParameters().Length == arguments.Length).Invoke(value, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
