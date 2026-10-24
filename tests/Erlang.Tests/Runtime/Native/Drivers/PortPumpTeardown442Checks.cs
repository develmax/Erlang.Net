using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class PortPumpTeardown442Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly AsyncLocal<object?> Active = new();

    internal static async Task Run()
    {
        string folder = Path.Combine(PortPumpTeardown442Names.Folder, Guid.NewGuid().ToString(PortPumpTeardown442Names.Guid));
        Directory.CreateDirectory(folder);
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object backend = typeof(ProcessRuntime).GetProperty(BusyPortCancellationHostNames.Ports, Members)!.GetValue(runtime)!;
        Assembly assembly = typeof(ProcessRuntime).Assembly;
        Type optionsType = assembly.GetType(PortPumpTeardown442Names.Options, true)!;
        Type processType = assembly.GetType(PortPumpTeardown442Names.Process, true)!;
        Type portType = assembly.GetType(PortPumpTeardown442Names.Port, true)!;
        var outcomes = new List<object>();
        bool passed = true;
        foreach (int width in PortPumpTeardown442Names.PacketWidths)
        {
            foreach (var state in new[] { (Closed: false, CancelOnReadFault: false), (Closed: false, CancelOnReadFault: true), (Closed: true, CancelOnReadFault: true) })
            {
                string path = Path.Combine(folder, Guid.NewGuid().ToString(PortPumpTeardown442Names.Guid) + PortPumpTeardown442Names.Leaf);
                using var stream = new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    PortPumpTeardown442Names.StreamBufferBytes,
                    FileOptions.Asynchronous | FileOptions.DeleteOnClose
                );
                Term settings = width == 0 ? Term.List(Term.A(PortNames.ReadOnlyOption))
                    : Term.List(Term.A(PortNames.ReadOnlyOption), Term.Tuple(Term.A(PortNames.Packet), Term.I(width)));
                object options = optionsType.GetMethod(PortPumpTeardown442Names.Parse, [typeof(Term), typeof(Term)])!
                    .Invoke(null, [Term.Tuple(Term.A(PortDescriptorNames.Kind), Term.I(0), Term.I(1)), settings])!;
                using var process = (IDisposable)Activator.CreateInstance(
                    processType,
                    Members,
                    null,
                    [Stream.Null, stream],
                    null
                )!;
                object port = Activator.CreateInstance(
                    portType,
                    Members,
                    null,
                    [new PortTerm(runtime.Node, 1), context.Self, process, options],
                    null
                )!;
                using var stop = (CancellationTokenSource)portType.GetProperty(PortPumpTeardown442Names.Stop)!.GetValue(port)!;
                portType.GetField(PortPumpTeardown442Names.Closed)!.SetValue(port, state.Closed);
                int observedFaults = 0, cancellations = 0;
                EventHandler<FirstChanceExceptionEventArgs> observe = (_, error) =>
                {
                    if (!ReferenceEquals(Active.Value, port) || error.Exception is not NotSupportedException) return;
                    observedFaults++;
                    if (state.CancelOnReadFault && !stop.IsCancellationRequested)
                    {
                        cancellations++;
                        stop.Cancel();
                    }
                };
                // Stage real cancellation after an actual CLR read fault, before its exception filter.
                // No fabricated exception, product callback, sleep or asynchronous timing assumption.
                Active.Value = port;
                AppDomain.CurrentDomain.FirstChanceException += observe;
                bool completed = false, unsupported = false;
                try
                {
                    await (Task)backend.GetType().GetMethod(PortPumpTeardown442Names.Pump, Members)!.Invoke(backend, [port])!;
                    completed = true;
                }
                catch (NotSupportedException)
                {
                    unsupported = true;
                }
                finally
                {
                    AppDomain.CurrentDomain.FirstChanceException -= observe;
                    Active.Value = null;
                }
                bool expected = state.Closed && state.CancelOnReadFault;
                bool equal = completed == expected && unsupported != expected && observedFaults > 0
                    && cancellations == (state.CancelOnReadFault ? 1 : 0);
                passed &= equal;
                outcomes.Add(new
                {
                    width,
                    state.Closed,
                    state.CancelOnReadFault,
                    completed,
                    unsupported,
                    observedFaults,
                    cancellations,
                    equal,
                    ActualFileStream = true,
                    StreamDisposedBeforePump = false
                });
            }
        }
        File.WriteAllText(Path.Combine(folder, PortPumpTeardown442Names.Report), JsonSerializer.Serialize(new { Passed = passed, Outcomes = outcomes }));
        if (!passed) throw new InvalidOperationException(PortPumpTeardown442Names.Failure);
    }
}
