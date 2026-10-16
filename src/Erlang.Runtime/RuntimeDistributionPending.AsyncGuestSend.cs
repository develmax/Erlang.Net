namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private async ValueTask<DistributionGuestSendResult> SendConnectedPidAsynchronously(
        ProcessContext producer,
        Pid remote,
        Term payload,
        bool priority
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        uint connectionId;
        LocalNodeSnapshot snapshot;
        DistributionOutputTicket ticket;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        lock (gate)
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            RequireProcess(producer);
            if (remote.LocalIdentity is not null && !ReferenceEquals(remote.LocalIdentity, runtime.LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (remote.Node == runtime.Node || !entries.TryGetValue(remote.Node, out var weak) ||
                !weak.TryGetTarget(out var found) || found.Connected is not { } active ||
                !IsConnectedSignalGeneration(found, active, found.ConnectionId))
                return DistributionGuestSendResult.NotConnected;
            entry = found;
            connected = active;
            connectionId = entry.ConnectionId;
            snapshot = runtime.LocalIdentity.Snapshot;
            resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                // A unique record owner avoids retaining the producer after accepted admission.
                ticket = ReserveOwnedOutput(connected, new object());
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
        HostDistributionStagedOutput? staged = null;
        AsyncDistributionOutput? output = null;
        Task? worker = null;
        try
        {
            while (true)
            {
                SequenceTraceSendPreview preview;
                lock (gate)
                {
                    Validate();
                    preview = runtime.SequenceTracing.PreviewSent(producer);
                }
                var message = GuestPidMessage(
                    connected.Flags,
                    producer.Self,
                    remote,
                    payload,
                    priority,
                    preview.Next
                );
                var packet = new HostDistributionBuffer([DistributionPassThroughCodec.Encode(
                    message,
                    negotiatedFlags: connected.Flags,
                    options: new EtfEncodingOptions
                    {
                        NodeIdentity = runtime.LocalIdentity,
                        NodeSnapshot = snapshot,
                        FunctionOwner = producer.Self
                    }
                ).AsMemory()]);
                lock (gate)
                {
                    Validate();
                    if (runtime.SequenceTracing.IsCurrentSendPreview(producer, preview))
                    {
                        staged = connected.Queue.TryStageOutput(connected.QueueHandle, packet)
                            ?? throw new ErlangException(ErlangErrorReasons.SystemLimit);
                        output = new(
                            entry,
                            connected,
                            connectionId,
                            ticket,
                            resource,
                            staged
                        );
                        // Start before commit; the worker cannot publish until Acceptance resolves true.
                        worker = PublishAsyncDistributionOutput(output);
                        _ = runtime.SequenceTracing.Sent(producer, remote, payload);
                        runtime.ProcessTracing.Sent(
                            producer,
                            remote,
                            payload,
                            exists: true
                        );
                        producer.SavedCalls.Sent();
                        output.Acceptance.TrySetResult(true);

                        return DistributionGuestSendResult.Accepted;
                    }
                }
                // A changing global tracer can invalidate a preview; retry without a busy wait or token advance.
                await producer.ReduceAsync();
            }
        }
        catch (OperationCanceledException) when (worker is null && !CapturedOutputGenerationIsCurrent(entry, connected, connectionId))
        {
            lock (gate)
            {
                _ = runtime.SequenceTracing.Sent(producer, remote, payload);
                runtime.ProcessTracing.Sent(
                    producer,
                    remote,
                    payload,
                    exists: true
                );
                producer.SavedCalls.Sent();
            }

            return DistributionGuestSendResult.Accepted;
        }
        catch (Exception primary)
        {
            if (worker is not null)
            {
                output!.Acceptance.TrySetResult(false);
                try
                {
                    await worker.ConfigureAwait(false);
                }
                catch (Exception settlement)
                {
                    throw new AggregateException(primary, settlement);
                }
            }
            throw;
        }
        finally
        {
            if (worker is null)
            {
                try
                {
                    staged?.Dispose();
                }
                finally
                {
                    resource.Dispose();
                    ReleaseOwnedOutput(connected, ticket);
                }
            }
        }

        void Validate()
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired);
            RequireProcess(producer);
        }
    }

    private async Task PublishAsyncDistributionOutput(AsyncDistributionOutput output)
    {
        Exception? failure = null;
        try
        {
            if (!await output.Acceptance.Task.ConfigureAwait(false))
                return;
            await output.Connected.Output.Publish(
                output.Ticket,
                () => output.Staged.Packet,
                _ =>
                {
                    lock (gate)
                    {
                        Validate();

                        return output.Connected.Queue.TryEnqueueStagedOutput(output.Staged);
                    }
                },
                packet =>
                {
                    lock (gate)
                    {
                        Validate();

                        return output.Connected.Queue.WaitForOutputCapacity(output.Connected.QueueHandle, packet);
                    }
                },
                CancellationToken.None,
                deferStart: System.Threading.Monitor.IsEntered(gate)
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!CapturedOutputGenerationIsCurrent(output.Entry, output.Connected, output.ConnectionId))
        {
        }
        catch (Exception exception)
        {
            failure = exception;
            try
            {
                RetireFailedOutputGeneration(output.Entry, output.Connected, output.ConnectionId);
            }
            catch (Exception retirementFailure)
            {
                failure = new AggregateException(exception, retirementFailure);
            }
        }
        finally
        {
            try
            {
                output.Staged.Dispose();
            }
            finally
            {
                output.Resource.Dispose();
                ReleaseOwnedOutput(output.Connected, output.Ticket, failure);
            }
        }

        void Validate()
        {
            if (!IsConnectedSignalGeneration(output.Entry, output.Connected, output.ConnectionId))
                throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired);
        }
    }
}
