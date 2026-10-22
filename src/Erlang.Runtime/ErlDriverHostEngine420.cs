using System.Runtime.InteropServices;
using System.Text.Json;

namespace Erlang;

internal sealed partial class ErlDriverHostEngine420
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, ErlDriverPort420> ports = new();
    private readonly Dictionary<nuint, ErlDriverPort420> termPorts = new();
    private readonly List<Task> retirement = new();
    private readonly ErlDriverHostImages420 images = new();
    private readonly ErlDriverNativeThreads420 threads = new();
    private readonly ErlDriverTermTokens420 tokens = new();
    private readonly ErlDriverServiceFault420 fault = new();
    private readonly ErlDriverHostPeer420 peer;
    private readonly string session;
    private readonly ErlDriverAbiLayout420 layout;
    private ErlDriverPrimitiveServices422? primitive;
    private ErlDriverThreadBridge420? bridge;
    private ErlDriverCallbackTable420? table;
    private ErlDriverPortJobs420? jobs;
    private ErlDriverPdlServices420? pdl;
    private ErlDriverTimerServices420? timers;
    private ErlDriverSelectServices420? select;
    private ErlDriverAsyncServices420? async;
    private ErlDriverMonitorServices420? monitors;
    private ErlDriverLinkedServices420? linked;
    private ErlDriverPrimitiveAccess420? memory;
    private ErlDriverStartEntry420.Invoke? start;
    private ulong lastPort;
    private bool closing;
    private Task? close;

    private ErlDriverHostEngine420(ErlDriverHostPeer420 peer, string session, ErlDriverAbiLayout420 layout)
    {
        this.peer = peer;
        this.session = session;
        this.layout = layout;
    }

    internal Task<Exception> Failure => fault.Completion;

    internal static async Task<ErlDriverHostEngine420> CreateAsync(
        ErlDriverHostPeer420 peer, string session, ErlDriverEngineInput420 input)
    {
        var abi = ErlDriverAbiLayout420.Read(input.LayoutJson, input.LayoutSha256);
        var measured = JsonSerializer.Deserialize<Dictionary<string, int>>(input.LayoutJson)
            ?? throw new InvalidDataException(ErlDriverAbiNames420.InvalidLayout);
        var engine = new ErlDriverHostEngine420(peer, session, abi);
        try
        {
            var proxy = new ErlDriverHostParentProxy420(peer);
            engine.primitive = new(
                measured,
                input.NativeErrnoNames,
                new ErlDriverPrimitiveProxy420(peer, input.SystemInfo),
                () => ErlDriverThreadRole420.IsSchedulerThread,
                engine.fault.Record
            );
            engine.bridge = new(
                input.BridgePath,
                input.BridgeSha256,
                engine.threads,
                engine.images.WorkerImage,
                engine.fault.Record
            );
            engine.memory = new(engine.primitive.Callbacks, abi);
            engine.jobs = new(peer, engine.fault);
            engine.pdl = new(engine.AcquireRead, engine.fault);
            engine.timers = new(engine.AcquireRead, engine.jobs, engine.fault);
            engine.select = new(
                engine.AcquireRead,
                engine.jobs,
                engine.bridge,
                engine.fault
            );
            engine.async = new(
                engine.AcquireRead,
                engine.bridge,
                engine.tokens,
                checked((uint)input.SystemInfo.AsyncThreads),
                engine.jobs,
                engine.pdl,
                engine.fault
            );
            engine.monitors = new(
                engine.AcquireRead,
                engine.pdl,
                proxy,
                engine.jobs,
                engine.tokens,
                engine.fault
            );
            engine.linked = new(
                engine.images,
                engine.bridge,
                abi,
                proxy,
                engine.fault
            );
            var program = new ErlDriverTermProgram420(engine.memory, engine.tokens.Resolve,
                bytes => ExternalTermFormat.Decode(bytes.Span));
            var term = new ErlDriverTermServices420(
                engine.tokens,
                program,
                engine.Acquire,
                engine.AcquireTerm,
                engine.DeliverTerm,
                engine.fault
            );
            var output = new ErlDriverOutputServices420(
                engine.Acquire,
                engine.DeliverOutput,
                engine.memory,
                abi,
                engine.fault
            );
            var queue = new ErlDriverQueueServices420(engine.AcquireRead, engine.fault);
            var basic = new ErlDriverPortBasicServices420(
                engine.Acquire,
                proxy,
                engine.tokens,
                engine.primitive.Callbacks,
                engine.fault
            );
            var callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal);
            foreach (var group in new[] { engine.primitive.Callbacks, engine.bridge.ReadCallbacks(abi),
                engine.pdl.Callbacks, engine.timers.Callbacks, engine.select.Callbacks, engine.async.Callbacks,
                engine.monitors.Callbacks, engine.linked.Callbacks, term.Callbacks, output.Callbacks, queue.Callbacks, basic.Callbacks })
                foreach (var pair in group) callbacks.Add(pair.Key, pair.Value);
            var services = new List<SafeHandle> { engine.primitive, engine.bridge };
            if (engine.async.NativeOwner is { } pool) services.Add(pool);
            engine.table = new(abi, callbacks, services);
            engine.start = Marshal.GetDelegateForFunctionPointer<ErlDriverStartEntry420.Invoke>(
                engine.bridge.ExportPointer(ErlDriverHostNativeNames420.StartEntry));
            engine.images.Configure(
                engine.bridge,
                engine.table,
                abi,
                engine.linked.BeginInitialization435
            );

            return engine;
        }
        catch (Exception error)
        {
            try
            {
                await engine.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
            throw;
        }
    }

    private ErlDriverPortReadLease420? AcquireRead(nint nativePort) => Acquire(nativePort).Lease;

    private ErlDriverPortAdmission420 Acquire(nint nativePort)
    {
        lock (gate)
        {
            if (!ports.TryGetValue(unchecked((ulong)nativePort), out var port)) return new(null, -1);
            if (port.Closing) return new(null, 0);
            var read = port.AcquireService();

            return new(read, read is null ? 0 : 1);
        }
    }

    private ErlDriverPortAdmission420 AcquireTerm(nuint token)
    {
        lock (gate)
        {
            if (!termPorts.TryGetValue(token, out var port)) return new(null, -1);
            if (port.Closing) return new(null, 0);
            var read = port.AcquireService();

            return new(read, read is null ? 0 : 1);
        }
    }

    private ErlDriverHostFrame420 Event(ErlDriverHostOperation420 operation, ErlDriverPortIdentity420 identity, byte[] data)
        => new(
            ErlDriverHostNames420.Version,
            session,
            ErlDriverHostKind420.Event,
            operation,
            0,
            identity.Driver,
            identity.Port,
            0,
            0,
            0,
            null,
            data
        );

    private int DeliverOutput(ErlDriverPort420 port, ErlDriverOutput420 output)
    {
        peer.SendEventAsync(Event(ErlDriverHostOperation420.PortOutput, port.Identity,
            ErlDriverPrimitiveWire420.Encode(output))).AsTask().GetAwaiter().GetResult();

        return 0;
    }

    private int DeliverTerm(ErlDriverPort420? port, nuint recipient, Term term)
    {
        if (tokens.Resolve(recipient) is not Pid target) return -1;
        var response = peer.RequestAsync(
            ErlDriverHostOperation420.PortTermOutput,
            port?.Identity.Driver ?? 0,
            port?.Identity.Port ?? 0,
            0,
            0,
            null,
            ErlDriverPrimitiveWire420.Encode(new ErlDriverTermDelivery420(
                tokens.ActorIdentity(recipient),
                ExternalTermFormat.Encode(target),
                ExternalTermFormat.Encode(term),
                port is null
            )),
            CancellationToken.None
        ).AsTask().GetAwaiter().GetResult();
        if (response.NativeResult is < 0 or > 1) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return response.NativeResult;
    }
}
