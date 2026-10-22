using System.Buffers.Binary;
using System.Numerics;

namespace Erlang;

internal static class DebugDirtyOperations424
{
    internal static Term Execute(ProcessRuntime runtime, ProcessContext context, DebugDirtyStep424 step)
    {
        string? operation = (step.Operation as Atom)?.Name;
        switch (operation)
        {
            case DebugDirtyNames424.Scheduler:
                if (step.Argument is not Atom { Name: DebugDirtyNames424.Type }) throw BadArgument();

                return Term.A(DirtyNativeThread419.Read(context) switch
                {
                    DirtyNativeKind419.Cpu => DebugDirtyNames424.Cpu,
                    DirtyNativeKind419.Io => DebugDirtyNames424.Io,
                    _ => RuntimeActorExecutionTurn.IsOwnedBy(context) ? DebugDirtyNames424.Normal : DebugDirtyNames424.Error
                });
            case DebugDirtyNames424.Error:
                throw new ErlangException(step.Argument is Atom reason && reason.Name is
                    DebugDirtyNames424.NotSupported or ErlangErrorReasons.UndefinedFunction or ErlangErrorReasons.BadArithmetic
                    or ErlangErrorReasons.NoProcess or ErlangErrorReasons.SystemLimit ? reason.Name : ErlangErrorReasons.BadArgument);
            case DebugDirtyNames424.Copy:
                return Copy(runtime, step.Argument);
            case DebugDirtyNames424.Send:
                runtime.DebugDirtySend424(context, step.Argument, DebugDirtyNames424.Ok);

                return Term.A(DebugDirtyNames424.Ok);
            case DebugDirtyNames424.Wait:
                DebugDirtyWait424.Wait(Signed(step.Argument), step.Kind == DirtyNativeKind419.Cpu);

                return Term.A(DebugDirtyNames424.Ok);
            case DebugDirtyNames424.Reschedule:
                long count = Signed(step.Argument);
                if (count < 0) throw BadArgument();
                if (count == 0) return Term.A(DebugDirtyNames424.Ok);
                DirtyNativeKind419 next = count % 4 == 0 ? step.Kind switch
                {
                    DirtyNativeKind419.Cpu => DirtyNativeKind419.Io,
                    DirtyNativeKind419.Io => DirtyNativeKind419.Normal,
                    _ => DirtyNativeKind419.Cpu
                } : step.Kind;
                // The pin casts n-2 to Uint before make_integer. In particular,
                // odd n=1 schedules UINT64_MAX, then term_to_Sint rejects it.
                step.Next = new(next, step.Operation, new Integer(new BigInteger(unchecked((ulong)(count - 2)))));

                return Nil.Value;
            case DebugDirtyNames424.ReadyWaitDone:
                runtime.DebugDirtySend424(context, step.Argument, DebugDirtyNames424.Ready);
                DebugDirtyWait424.Wait(DebugDirtyNames424.ReadyMilliseconds, false);
                runtime.DebugDirtySend424(context, step.Argument, DebugDirtyNames424.Done);

                return Term.A(DebugDirtyNames424.Ok);
            case DebugDirtyNames424.AliveWait:
            case DebugDirtyNames424.AliveWaitOnly:
                if (runtime.DebugDirtyExiting424(context)) throw BadArgument();
                runtime.DebugDirtySend424(context, step.Argument, DebugDirtyNames424.Alive);
                while (!runtime.DebugDirtyExiting424(context))
                {
                    if (step.Kind == DirtyNativeKind419.Io) DebugDirtyWait424.Wait(DebugDirtyNames424.ExitPollMilliseconds, false);
                    else Thread.Yield();
                }
                if (operation == DebugDirtyNames424.AliveWaitOnly) return Term.A(DebugDirtyNames424.Ok);
                DebugDirtyWait424.Wait(DebugDirtyNames424.PostExitMilliseconds, false);
                // Allocation is deliberately after process death, while its
                // admitted physical dirty invocation still owns the shadow.
                byte[] digits = new byte[DebugDirtyNames424.PostExitWords * sizeof(ulong)];
                for (int index = 0; index < DebugDirtyNames424.PostExitWords; index++)
                    BinaryPrimitives.WriteUInt64LittleEndian(digits.AsSpan(index * sizeof(ulong)), DebugDirtyNames424.PostExitDigit);

                return Term.Tuple(Term.A(DebugDirtyNames424.Ok), new Integer(new BigInteger(digits, isUnsigned: true, isBigEndian: false)));
            default:
                throw BadArgument();
        }
    }

    private static long Signed(Term argument)
        => argument is Integer integer && integer.Value >= long.MinValue && integer.Value <= long.MaxValue
            ? (long)integer.Value : throw BadArgument();

    private static ErlangException BadArgument() => new(ErlangErrorReasons.BadArgument);

    private static Term Copy(ProcessRuntime runtime, Term argument)
    {
        Term result = Nil.Value;
        for (int index = 0; index < DebugDirtyNames424.Copies; index++)
            result = new Cons(DebugDirtyTermCopy424.Copy(runtime, argument), result);

        return result;
    }

}
