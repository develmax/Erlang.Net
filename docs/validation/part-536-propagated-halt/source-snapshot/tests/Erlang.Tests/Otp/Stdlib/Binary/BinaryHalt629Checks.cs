using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.BinaryHalt629Names;

namespace Erlang.Tests;

internal static class BinaryHalt629Checks
{
    internal static async Task Run()
    {
        await Run(false);
        await Run(true);
    }

    private static Task Run(bool erlang) => BinaryArena598Fixture.Run(
        new BitString([First, Second]),
        async fixture =>
    {
        object frame = fixture.Frame();
        int slot = (int)Call(frame, BinaryArena598Names.Reserve)!;
        Call(
            frame,
            BinaryArena598Names.Set,
            slot,
            fixture.Reference
        );
        var local = (ReferenceTerm)Call(frame, BinaryArena598Names.Read, slot)!;
        RuntimeExternalTermReservation? host = fixture.Runtime.ReserveExternalTerm(local);
        IDisposable? reader = null;
        Task? halt = null;
        ProcessHandle? haltCaller = null;
        var failures = new List<Exception>();
        object owner = typeof(ProcessRuntime).GetField(Owner, BinaryArena598Names.Members)!.GetValue(fixture.Runtime)!;
        try
        {
            fixture.Release();
            Call(frame, BinaryArena598Names.Clear, slot);
            Call(
                fixture.HeapOwner,
                BinaryArena598Names.CollectGeneration,
                true,
                ushort.MaxValue
            );
            ((IDisposable)frame).Dispose();
            reader = (IDisposable)Call(fixture.BackingOwner, BinaryHalt629Names.Read)!;
            if (erlang)
            {
                haltCaller = fixture.Runtime.Spawn(context => fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    HaltLifecycleHostNames.Halt,
                    Term.I(0),
                    Term.List(Term.Tuple(Term.A(Flush), Term.A(ErlangBooleanAtoms.False)))
                ));
                halt = fixture.Runtime.HaltCompletion;
                await halt.WaitAsync(Deadline);
            }
            else
                halt = fixture.Runtime.RequestImmediateHalt(0);
            await halt.WaitAsync(Deadline);
            bool refused = false;
            try
            {
                ((IDisposable)Call(fixture.BackingOwner, BinaryHalt629Names.Read)!).Dispose();
            }
            catch (ObjectDisposedException)
            {
                refused = true;
            }
            Require(refused);
            Require(!(bool)Property(fixture.BackingOwner, BinaryArena598Names.Freed));
            Require((byte)Call(Property(fixture.BackingOwner, Arena), BinaryHalt629Names.Byte, 0)! == First);
            Require(!fixture.Runtime.HaltQuiescence.IsCompleted);
            Require(!fixture.Actor!.Process.Completion.IsCompleted);
            reader.Dispose();
            reader = null;
            await halt.WaitAsync(Deadline);
            await fixture.Runtime.HaltQuiescence.WaitAsync(Deadline);
            if (haltCaller is not null)
                Require((await haltCaller.Completion.WaitAsync(Deadline)).Equals(Term.A(ProcessExitReasons.Shutdown)));
            Require((bool)Property(fixture.BackingOwner, BinaryArena598Names.Freed));
            bool stale = false;
            try
            {
                _ = Call(Property(fixture.BackingOwner, Arena), BinaryHalt629Names.Byte, 0);
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
            Require(Call(fixture.Runtime.ReferenceResources, Acquire, host.Value) is null);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            try
            {
                reader?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                host?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            // Bounded cleanup also works on the deliberately missing-close RED.
            // It runs only AFTER the acceptance assertions above have executed.
            try
            {
                Task drain = (Task)Call(owner, Drain)!;
                await Task.WhenAll(drain, halt ?? Task.CompletedTask,
                    halt is null ? Task.CompletedTask : fixture.Runtime.HaltQuiescence).WaitAsync(Deadline);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        if (failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count != 0) throw new AggregateException(failures);
    }
    );

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
