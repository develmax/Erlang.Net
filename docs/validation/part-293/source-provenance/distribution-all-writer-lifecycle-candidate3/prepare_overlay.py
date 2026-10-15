import pathlib,subprocess,hashlib,json
root=pathlib.Path(__file__).resolve().parents[2]
directory=pathlib.Path(__file__).resolve().parent
overlay=directory/'overlay'
pin='9c5b8a809b2a6bcd76965cdeae2d9dbd2ec873e2'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
before={}

def source(path):
    raw=subprocess.check_output(['git','show',pin+':'+path],cwd=root)
    before[path]=raw
    return raw.decode()

def write(path,text):
    p=overlay/path;p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding='utf8',newline='\n')

def replace_once(text,old,new):
    assert text.count(old)==1,old
    return text.replace(old,new)

def replace_method(text,signature,replacement):
    start=text.index(signature)
    opening=text.index('{',start)
    level=1;end=opening+1
    while level:
        if text[end]=='{':level+=1
        elif text[end]=='}':level-=1
        end+=1
    return text[:start]+replacement+text[end:]

p='src/Erlang.Runtime/DistributionConnectedEntry.cs'
s=source(p)
s=replace_once(s,'    internal HostDistribution Queue { get; } = queue;','''    internal HostDistribution Queue { get; } = queue;
    internal DistributionOutputSequencer Output { get; } = new();
    internal Task SystemOutput { get; set; } = Task.CompletedTask;
''')
write(p,s)
p='src/Erlang.Runtime/RuntimeDistributionPending.Connected.cs'
s=source(p)
s=replace_once(s,'        bool rawInput\n    )\n    {','        bool rawInput\n    )\n    {\n        using var outputTransfer = BeginOutputTransfer();')
s=replace_method(s,'    private void RetireConnected(','''    private void RetireConnected(RuntimeDistributionPendingEntry entry, Term? reason = null)
    {
        if (entry.Connected is not { } connected)
        {
            RetireControllerAdoptions(entry: entry);

            return;
        }
        // Reentrant internal signals must see a retired identity before any callbacks run.
        entry.Connected = null;
        entry.State = DistributionPendingState.Exiting;
        connected.Controller.DistributionRole = RuntimeDistributionRole.None;
        var failures = new List<Exception>();
        RetireStep(connected.Output.Dispose);
        RetireStep(connected.Queue.Dispose);
        RetireStep(() => RetireControllerAdoptions(entry: entry));
        RetireStep(() => runtime.RetireConnectedProcessLinks(entry, connected));
        RetireStep(() => runtime.RetireConnectedProcessMonitors(entry, connected));
        RetireStep(() => NotifyConnectedNode(
            entry,
            connected,
            RuntimeLocalNodeAdmissionNames.NodeDown,
            reason ?? Term.A(DistributionConnectionReasons.Closed)
        ));
        entry.State = DistributionPendingState.Idle;
        if (failures.Count != 0)
            throw new AggregateException(failures);

        void RetireStep(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
    }''')
s=replace_method(s,'    private void EnqueueConnectedOutputOwned(','''    private void EnqueueConnectedOutputOwned(
        Term handle,
        DistributionPassThroughMessage message,
        ProcessContext? sender
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        DistributionOutputTicket ticket;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        uint connectionId;
        EtfEncodingOptions options;
        lock (gate)
        {
            entry = ReadHandle(handle, out connectionId, out resource);
            try
            {
                connected = RequireConnected(entry, connectionId);
                if (sender is not null)
                    RequireProcess(sender);
                options = new EtfEncodingOptions
                {
                    NodeIdentity = runtime.LocalIdentity,
                    NodeSnapshot = runtime.LocalIdentity.Snapshot,
                    FunctionOwner = sender?.Self
                };
                ticket = ReserveOwnedOutput(connected, sender ?? new object(), immediate: true);
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
        using (resource)
        {
            try
            {
                byte[] bytes = DistributionPassThroughCodec.Encode(message, negotiatedFlags: connected.Flags, options: options);
                var buffer = new HostDistributionBuffer([bytes.AsMemory()]);
                lock (gate)
                {
                    if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                        throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
                    if (sender is not null)
                        RequireProcess(sender);
                    if (!connected.Queue.TryEnqueueFinalizedHostBuffer(connected.QueueHandle, buffer))
                        throw new DistributionOutputWouldBlockException();
                }
            }
            finally
            {
                ReleaseOwnedOutput(connected, ticket);
            }
        }
    }''')
write(p,s)
p='src/Erlang.Runtime/ProcessRuntime.DistributionLinks.cs'
s=source(p)
s=replace_method(s,'    internal ulong? BeginConnectedProcessUnlink(','')
s=replace_method(s,'    internal void ConnectedLinkProcessTerminated(','')
s=s.replace('.QueueConnectedControlSignal(','.ScheduleConnectedControlSignal(')
write(p,s)
p='src/Erlang.Runtime/ProcessRuntime.DistributionMonitors.cs'
s=source(p)
s=replace_method(s,'    internal ReferenceTerm BeginConnectedProcessMonitor(','')
s=replace_method(s,'    internal bool EndConnectedProcessMonitor(','')
s=replace_method(s,'    internal void ConnectedMonitorProcessTerminated(','')
s=s.replace('.QueueConnectedControlSignal(','.ScheduleConnectedControlSignal(')
write(p,s)
p='src/Erlang.Runtime/RuntimeDistributionPending.LinkSignals.cs'
s=source(p)
s=replace_once(s,'        ArgumentNullException.ThrowIfNull(remote);','        using var outputTransfer = BeginOutputTransfer();\n        ArgumentNullException.ThrowIfNull(remote);')
s=replace_method(s,'    public ulong? BeginConnectedUnlinkSignal(','''    public ulong? BeginConnectedUnlinkSignal(Term handle, ProcessContext local, Pid remote)
    {
        using var target = CaptureUnlinkPublicationTarget(handle, local, remote);

        return runtime.BeginConnectedProcessUnlink(target.Entry, target.Connection, local, remote);
    }''')
s=replace_method(s,'    internal bool QueueConnectedControlSignal(','')
write(p,s)
p='src/Erlang.Runtime/RuntimeDistributionPending.MonitorSignals.cs'
s=source(p)
s=replace_method(s,'    public ReferenceTerm BeginConnectedMonitorSignal(','''    public ReferenceTerm BeginConnectedMonitorSignal(Term handle, ProcessContext observer, Term remote)
    {
        using var target = CaptureMonitorPublicationTarget(handle, observer, remote);

        return runtime.BeginConnectedProcessMonitor(target.Entry, target.Connection, observer, remote);
    }''')
s=replace_method(s,'    public bool EndConnectedMonitorSignal(','''    public bool EndConnectedMonitorSignal(ProcessContext observer, ReferenceTerm reference)
    {
        ValidateMonitorRemoval(observer, reference);

        return runtime.EndConnectedProcessMonitor(observer, reference);
    }''')
write(p,s)
p='src/Erlang.Runtime/RuntimeDistributionPending.cs'
s=source(p)
s=replace_method(s,'    internal void ProcessTerminated(','''    internal void ProcessTerminated(ProcessContext process, Term reason)
    {
        lock (gate)
        {
            exitingInputControllers.Remove(process);
            var failures = new List<Exception>();
            RetireStep(() => RetireControllerAdoptions(controller: process));
            if (ReferenceEquals(kernelSetup, process))
            {
                kernelSetup = null;
                foreach (var entry in pending.Values.ToArray())
                    RetireStep(() => RetirePending(entry));
            }
            if (controllers.TryGetValue(process.Self, out var owned) && ReferenceEquals(owned.ReservedController, process))
                RetireStep(() => RetirePending(owned, ControllerNodeDownReason(reason)));
            if (failures.Count != 0)
                throw new AggregateException(failures);

            void RetireStep(Action cleanup)
            {
                try
                {
                    cleanup();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }
    }''')
s=replace_method(s,'    internal void LocalKernelStopped(','''    internal void LocalKernelStopped(Term reason)
    {
        lock (gate)
        {
            kernelSetup = null;
            var failures = new List<Exception>();
            foreach (var entry in pending.Values.ToArray())
            {
                try
                {
                    RetirePending(entry, KernelNodeDownReason(reason));
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count != 0)
                throw new AggregateException(failures);
        }
    }''')
s=replace_method(s,'    private void RetirePending(','''    private void RetirePending(RuntimeDistributionPendingEntry entry, Term? reason = null)
    {
        try
        {
            RetireConnected(entry, reason);
        }
        finally
        {
            if (entry.ReservedController is { } controller && controllers.TryGetValue(controller.Self, out var owned) && ReferenceEquals(owned, entry))
                controllers.Remove(controller.Self);
            entry.ReservedController = null;
            entry.State = DistributionPendingState.Idle;
            pending.Remove(entry.Node.Name);
        }
    }''')
s=replace_once(s,'''                    RetireConnected(entry);
                    entry.State = DistributionPendingState.Closed;
                    entry.ReservedController = null;''','''                    try
                    {
                        RetireConnected(entry);
                    }
                    catch (Exception exception)
                    {
                        // Shutdown still retires every channel; drainage reports any cleanup failure.
                        ownedOutputFailures.Add(exception);
                    }
                    finally
                    {
                        entry.State = DistributionPendingState.Closed;
                        entry.ReservedController = null;
                    }''')
write(p,s)
p='src/Erlang.Runtime/ProcessRuntime.cs'
s=source(p)
s=replace_once(s,'    internal void AdmitSignals(ProcessContext owner)\n    {','    internal void AdmitSignals(ProcessContext owner)\n    {\n        using var outputTransfer = distributionPending?.BeginOutputTransfer();')
s=replace_once(s,'''                        Terminate(ctx, reason);
                        lock (gate)
                            executingProcesses.Remove(ctx);
                        ctx.Unwound.TrySetResult();''','''                        try
                        {
                            Terminate(ctx, reason);
                        }
                        finally
                        {
                            lock (gate)
                                executingProcesses.Remove(ctx);
                            ctx.Unwound.TrySetResult();
                        }''')
s=replace_method(s,'    private void Terminate(', (directory/'termination-method.txt').read_text().rstrip())
s=replace_once(s,'        await Ports.Drain().ConfigureAwait(false);','''        await Ports.Drain().ConfigureAwait(false);
        await Task.WhenAll(
            distributionPending?.DrainOwnedOutputOperations() ?? Task.CompletedTask,
            DrainExitContinuations()
        ).ConfigureAwait(false);''')
write(p,s)
p='src/Erlang.Runtime/RuntimeDistributionPending.RawInput.cs'
s=source(p)
s=replace_once(s,'    internal Term PutConnectedData(ProcessContext caller, Term handle, Term data)\n    {','    internal Term PutConnectedData(ProcessContext caller, Term handle, Term data)\n    {\n        using var outputTransfer = BeginOutputTransfer();')
write(p,s)
p='src/Erlang.Runtime/ProcessRuntime.Halt.cs'
s=source(p)
s=replace_once(s,'            if (ownsStandardStreams)\n                StandardStreams.Dispose();','''            try
            {
                if (distributionPending is { } outputOwner)
                    await outputOwner.DrainOwnedOutputOperations().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                await DrainExitContinuations().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (ownsStandardStreams)
                StandardStreams.Dispose();''')
write(p,s)
files=[]
for p in sorted(overlay.rglob('*.cs')):
    relative=p.relative_to(overlay).as_posix()
    if relative not in before:
        result=subprocess.run(['git','cat-file','-e',pin+':'+relative],cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        assert result.returncode!=0,relative
    files.append(dict(Path=relative,BeforeSha256=sha(before[relative]) if relative in before else None,Sha256=sha(p.read_bytes()),Bytes=p.stat().st_size))
manifest=dict(Status='SOURCE-ONLY LIFECYCLE CANDIDATE: awaiting isolated target, focus, full and original validation; not promotable',BaselineCommit=pin,Files=files,BuildExecuted=False,NativeExecuted=False,MainModified=False,GitModified=False,OldUnsafeOverlayUsed=False,RemainingBlockers='README.md')
manifest['SourceInputs']=[dict(Path=path,Sha256=sha((directory/path).read_bytes()),Bytes=(directory/path).stat().st_size) for path in ['prepare_overlay.py','termination-method.txt','README.md','pinned-put-data-binding.json','pinned-put-data-extract.txt','pinned-link-immediate-extract.txt']]
target=directory/'source-freeze.json'
target.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf8')
print(json.dumps(dict(Paths=len(files),Manifest=str(target),Sha256=sha(target.read_bytes()))))
