using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang;

internal static class PollHarnessCases1557
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(PollHarnessNames1557.Contract);
    }

    private static async Task<Exception> Failure(Task task, Func<Exception, bool> expected)
    {
        try
        {
            await task.WaitAsync(PollHarnessNames1557.Deadline);
        }
        catch (Exception error)
        {
            if (!expected(error)) throw new AggregateException(new InvalidOperationException(PollHarnessNames1557.Contract), error);

            return error;
        }
        throw new InvalidOperationException(PollHarnessNames1557.Contract);
    }

    private static async Task<string> KernelPoll(LinuxDescriptorPollService engine, int descriptors)
    {
        long end = Environment.TickCount64 + (long)PollHarnessNames1557.Deadline.TotalMilliseconds;
        string last = string.Empty;
        while (Environment.TickCount64 < end)
        {
            last = File.ReadAllText(PollHarnessNames1557.ProcTask + engine.ActualThreadId.ToString(CultureInfo.InvariantCulture)
                + PollHarnessNames1557.ProcSyscall).Trim();
            string[] fields = last.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length > PollHarnessNames1557.NfdsToken && fields[0] == PollHarnessNames1557.PollSyscallText)
            {
                string value = fields[PollHarnessNames1557.NfdsToken];
                if (value.StartsWith(PollHarnessNames1557.HexPrefix, StringComparison.Ordinal)) value = value[2..];
                if (ulong.TryParse(
                    value,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out ulong count
                )
                    && count == (ulong)descriptors) return last;
            }
            // Test-only observation polling of our exact owned OS thread;
            // this does not provide or settle descriptor readiness.
            await Task.Delay(PollHarnessNames1557.ProbeMilliseconds);
        }
        throw new TimeoutException(PollHarnessNames1557.Contract + Environment.NewLine + last);
    }

    internal static async Task<object> Run(string mode)
    {
        var fixture = new PollHarnessFixture1557();
        Exception? primary = null, cleanup = null;
        object? result = null;
        try
        {
            await fixture.Initialize(mode != PollHarnessNames1557.Unstarted, mode is PollHarnessNames1557.LostWake or PollHarnessNames1557.ConcurrentDispose);
            result = await Body(fixture, mode);
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            try
            {
                await fixture.DisposeAsync();
            }
            catch (Exception error)
            {
                cleanup = error;
            }
        }
        if (primary is not null && cleanup is not null) throw new AggregateException(primary, cleanup);
        if (primary is not null)
        {
            primary.Data[PollHarnessNames1557.CleanupSucceeded] = cleanup is null;
            primary.Data[PollHarnessNames1557.PhysicalEnded] = fixture.ExistingEngine?.PhysicalLifetimeEnded;
            primary.Data[PollHarnessNames1557.ThreadJoined] = fixture.ExistingEngine?.ActualThreadJoined;
            ExceptionDispatchInfo.Capture(primary).Throw();
        }
        if (cleanup is not null) ExceptionDispatchInfo.Capture(cleanup).Throw();
        Require(fixture.Engine.PhysicalLifetimeEnded);

        return new
        {
            Mode = mode,
            Evidence = result,
            PhysicalLifetimeEnded = fixture.Engine.PhysicalLifetimeEnded,
            ActualThreadJoined = fixture.Engine.ActualThreadJoined
        };
    }

    private static async Task<object> ConcurrentDispose(PollHarnessFixture1557 fixture)
    {
        var engine = fixture.Engine;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;

        object Read(string name) => typeof(LinuxDescriptorPollService).GetField(name, members)!.GetValue(engine)!;
        object gate = Read(PollHarnessNames1557.ServiceGate);
        var completion = (TaskCompletionSource)Read(PollHarnessNames1557.ShutdownCompletion);
        Task<object>? firstHost = null;
        Task? second = null;
        Task? firstInner = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            await fixture.Probe!.Reached.WaitAsync(PollHarnessNames1557.Deadline);
            // Owned host invocation: service Dispose itself now synchronously
            // joins, so it must not prevent this controller releasing the probe.
            firstHost = Task.Run(() => (object)engine.DisposeAsync().AsTask());
            long end = Environment.TickCount64 + (long)PollHarnessNames1557.Deadline.TotalMilliseconds;
            bool claimed = false;
            while (!claimed && Environment.TickCount64 < end)
            {
                lock (gate) claimed = (bool)Read(PollHarnessNames1557.ShutdownStarted);
                if (!claimed) await Task.Delay(PollHarnessNames1557.ProbeMilliseconds);
            }
            Require(claimed && !firstHost.IsCompleted);
            second = engine.DisposeAsync().AsTask();
            Require(ReferenceEquals(second, completion.Task) && !second.IsCompleted
                && !engine.PhysicalLifetimeEnded && !engine.ActualThreadJoined);
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            fixture.Probe!.Release();
            if (firstHost is not null)
            {
                try
                {
                    firstInner = (Task)await firstHost.WaitAsync(PollHarnessNames1557.Deadline);
                    await firstInner.WaitAsync(PollHarnessNames1557.Deadline);
                }
                catch (Exception error)
                {
                    cleanup.Add(error);
                }
            }
            if (second is not null)
            {
                try
                {
                    await second.WaitAsync(PollHarnessNames1557.Deadline);
                }
                catch (Exception error)
                {
                    cleanup.Add(error);
                }
            }
        }
        if (primary is not null) cleanup.Insert(0, primary);
        if (cleanup.Count != 0) throw new AggregateException(cleanup);
        Task third = engine.DisposeAsync().AsTask();
        Require(ReferenceEquals(firstInner, second) && ReferenceEquals(second, third)
            && ReferenceEquals(third, completion.Task) && third.IsCompletedSuccessfully
            && engine.PhysicalLifetimeEnded && engine.ActualThreadJoined);
        lock (gate)
        {
            Require((int)Read(PollHarnessNames1557.WakeRead) == PollHarnessNames1557.ClosedDescriptor
                && (int)Read(PollHarnessNames1557.WakeWrite) == PollHarnessNames1557.ClosedDescriptor);
        }

        return new
        {
            ActualPollEntryHeld = true,
            FirstInvocationClaimedAndBlocked = true,
            SecondSharedTaskPendingBeforeRelease = true,
            SameTaskAllThreeDisposals = true,
            ActualThreadJoined = true,
            BothOwnedWakeFieldsRetired = true
        };
    }

    private static async Task<object> Body(PollHarnessFixture1557 fixture, string mode)
    {
        var engine = fixture.Engine;
        if (mode == PollHarnessNames1557.ConcurrentDispose) return await ConcurrentDispose(fixture);
        if (mode == PollHarnessNames1557.Unstarted)
        {
            await engine.DisposeAsync();
            Require(engine.PhysicalLifetimeEnded && !engine.ActualThreadJoined);

            return new { NeverStarted = true };
        }
        if (mode == PollHarnessNames1557.Identity)
        {
            string syscall = await KernelPoll(engine, PollHarnessNames1557.WakeOnlyNfds);
            const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
            object gate = typeof(RuntimeMicrostateAccounting).GetField(PollHarnessNames1557.AccountingGate, members)!.GetValue(fixture.Accounting)!;
            lock (gate)
            {
                var rows = (IDictionary)typeof(RuntimeMicrostateAccounting).GetField(PollHarnessNames1557.AccountingRecords, members)!.GetValue(fixture.Accounting)!;
                RuntimeMicrostateRecord record = rows.Values.Cast<RuntimeMicrostateRecord>().Single();
                Require(record.Type == PollHarnessNames1557.PollType && record.Identity == 0 && !record.Managed
                    && record.Active && ReferenceEquals(record.Owner, fixture.Accounting)
                    && record.PhysicalThread.OperatingSystemId == engine.ActualThreadId);
            }
            Require(engine.ActualThreadId > 0 && engine.ActualThreadId != RuntimeOsThreadIdentity.Current());

            return new { ActualPollThreadId = engine.ActualThreadId, KernelPoll = syscall, PollIdentity = 0, Managed = false };
        }
        if (mode == PollHarnessNames1557.LostWake)
        {
            await fixture.Probe!.Reached.WaitAsync(PollHarnessNames1557.Deadline);
            var owner = fixture.Register(fixture.Pipe.ReadFd);
            using var cancellation = new CancellationTokenSource();
            Task<short> pending = engine.WaitReadyAsync(owner, DescriptorPortParameters.PollInput, cancellation.Token);
            cancellation.Cancel();
            Require(!pending.IsCompleted);
            fixture.Probe.Release();
            try
            {
                _ = await pending.WaitAsync(PollHarnessNames1557.LostWakeDeadline);
            }
            catch (TimeoutException error)
            {
                error.Data[PollHarnessNames1557.LostWakePending] = !pending.IsCompleted;
                throw;
            }
            catch (OperationCanceledException error)
            {
                Require(pending.IsCanceled && error.CancellationToken == cancellation.Token);

                return new
                {
                    ActualEmptySnapshotHeld = true,
                    NewRequestAdmittedAndCanceled = true,
                    NativePollActuallyReleased = true,
                    OriginalWaitCanceledBeforeOwnerRetireOrServiceStop = true
                };
            }
            throw new InvalidOperationException(PollHarnessNames1557.Contract);
        }
        var first = fixture.Register(mode == PollHarnessNames1557.Invalid ? PollHarnessNames1557.InvalidFd : fixture.Pipe.ReadFd);
        if (mode == PollHarnessNames1557.Invalid)
        {
            var pending = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, CancellationToken.None);
            Exception error = await Failure(pending, error => error is NativePortIOException native && native.Error == LinuxPortParameters.BadDescriptor);

            return new { ActualNativeBadDescriptor = error.ToString() };
        }
        if (mode == PollHarnessNames1557.Data)
        {
            var pending = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, CancellationToken.None);
            string syscall = await KernelPoll(engine, PollHarnessNames1557.OneWaitNfds);
            Require(!pending.IsCompleted);
            fixture.Pipe.WriteByte();
            Require(((await pending.WaitAsync(PollHarnessNames1557.Deadline)) & DescriptorPortParameters.PollInput) != 0);
            Require(fixture.Pipe.ReadOne(out byte value) == 1 && value == PollHarnessNames1557.DataByte);
            var output = fixture.Register(fixture.Pipe.WriteFd);
            Require(((await engine.WaitReadyAsync(output, DescriptorPortParameters.PollOutput, CancellationToken.None)
                .WaitAsync(PollHarnessNames1557.Deadline)) & DescriptorPortParameters.PollOutput) != 0);

            return new { KernelPollBeforeActualData = syscall, ActualReadByte = value, ActualWriteReady = true };
        }
        if (mode == PollHarnessNames1557.Cancel)
        {
            using var cancellation = new CancellationTokenSource();
            var pending = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, cancellation.Token);
            string syscall = await KernelPoll(engine, PollHarnessNames1557.OneWaitNfds);
            cancellation.Cancel();
            Exception error = await Failure(
                pending,
                error => error is OperationCanceledException canceled
                && canceled.CancellationToken == cancellation.Token && pending.IsCanceled
            );

            return new { KernelPollBeforeCancel = syscall, ExactCallerCancellation = error.ToString() };
        }
        if (mode == PollHarnessNames1557.TwoOwners)
        {
            var second = fixture.Register(fixture.Pipe.ReadFd);
            var a = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, CancellationToken.None);
            var b = engine.WaitReadyAsync(second, DescriptorPortParameters.PollInput, CancellationToken.None);
            string syscall = await KernelPoll(engine, PollHarnessNames1557.TwoWaitNfds);
            fixture.Pipe.WriteByte();
            short[] ready = await Task.WhenAll(a, b).WaitAsync(PollHarnessNames1557.Deadline);
            Require(first.Generation != second.Generation && ready.All(value => (value & DescriptorPortParameters.PollInput) != 0));
            Require(fixture.Pipe.ReadOne(out byte value) == 1 && value == PollHarnessNames1557.DataByte);

            return new { KernelPollBothExactOwners = syscall, TwoDistinctOwnerGenerations = true };
        }
        if (mode == PollHarnessNames1557.Reuse)
        {
            var old = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, CancellationToken.None);
            string syscall = await KernelPoll(engine, PollHarnessNames1557.OneWaitNfds);
            await first.BeginRetire().WaitAsync(PollHarnessNames1557.Deadline);
            _ = await Failure(old, error => ReferenceEquals(error, first.Closed));
            var replacement = fixture.NewPipe();
            int descriptor = fixture.Pipe.ReadFd;
            fixture.Pipe.ReplaceOwnedReadFrom(replacement);
            var second = fixture.Register(descriptor);
            var pending = engine.WaitReadyAsync(second, DescriptorPortParameters.PollInput, CancellationToken.None);
            replacement.WriteByte();
            Require(((await pending.WaitAsync(PollHarnessNames1557.Deadline)) & DescriptorPortParameters.PollInput) != 0);
            Require(first.Generation != second.Generation && fixture.Pipe.ReadOne(out byte value) == 1 && value == PollHarnessNames1557.DataByte);

            return new
            {
                OldKernelPollRetired = syscall,
                SameOwnedDescriptorNumber = descriptor,
                ActualDup3AfterOldRetirement = true,
                OldTaskKeptOwnClosedCause = true,
                NewGenerationReady = true
            };
        }
        if (mode == PollHarnessNames1557.Hangup)
        {
            var pending = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, CancellationToken.None);
            string syscall = await KernelPoll(engine, PollHarnessNames1557.OneWaitNfds);
            fixture.Pipe.CloseWrite();
            short events = await pending.WaitAsync(PollHarnessNames1557.Deadline);
            Require((events & LinuxDescriptorPollNames.Hangup) != 0 && fixture.Pipe.ReadOne(out _) == 0);

            return new { KernelPollBeforeActualWriterClose = syscall, ActualHangup = true, ActualReadEof = true };
        }
        if (mode == PollHarnessNames1557.Stop)
        {
            var pending = engine.WaitReadyAsync(first, DescriptorPortParameters.PollInput, CancellationToken.None);
            string syscall = await KernelPoll(engine, PollHarnessNames1557.OneWaitNfds);
            await engine.DisposeAsync().AsTask().WaitAsync(PollHarnessNames1557.Deadline);
            _ = await Failure(pending, error => ReferenceEquals(error, first.Closed));
            Require(engine.ActualThreadJoined && engine.PhysicalLifetimeEnded
                && LinuxPortNative.fcntl(fixture.Pipe.ReadFd, LinuxPortParameters.GetFlags, 0) >= 0);

            return new { KernelPollBeforeStop = syscall, ActualThreadJoinedBeforeCleanup = true, BorrowedFdStillOpen = true };
        }
        throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
