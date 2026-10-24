using Erlang;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class ActualMinimalBeamGuestSendHostChecks
{
    internal static IReadOnlyDictionary<string, string> LastProvenance
    {
        get; private set;
    } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    internal static async Task Run()
    {
        Assembly tests = typeof(ActualMinimalBeamGuestSendHostChecks).Assembly;
        byte[] bytes = Resource(tests, ActualMinimalBeamGuestSendHostNames.BeamResource);
        byte[] provenance = Resource(tests, ActualMinimalBeamGuestSendHostNames.ProvenanceResource);
        Check(Hash(bytes) == ActualMinimalBeamGuestSendHostNames.BeamSha256);
        Check(Hash(provenance) == ActualMinimalBeamGuestSendHostNames.ProvenanceSha256);
        BeamModule beam = BeamModule.Prepare(bytes);
        Check(beam.Name == ActualMinimalBeamGuestSendHostNames.Module);
        Check(beam.InputSha256 == ActualMinimalBeamGuestSendHostNames.BeamSha256);
        Check(beam.Supports(ActualMinimalBeamGuestSendHostNames.Bang, ActualMinimalBeamGuestSendHostNames.BangArity));
        CheckActualSend(beam);
        await PublicationAndOrder(beam);
        await PressureAndRegisters(beam);
        Assembly runtime = typeof(BeamModule).Assembly;
        LastProvenance = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            [ActualMinimalBeamGuestSendHostNames.TestsSha] = Hash(File.ReadAllBytes(tests.Location)),
            [ActualMinimalBeamGuestSendHostNames.RuntimeSha] = Hash(File.ReadAllBytes(runtime.Location)),
            [ActualMinimalBeamGuestSendHostNames.TestsMvid] = tests.ManifestModule.ModuleVersionId.ToString(),
            [ActualMinimalBeamGuestSendHostNames.RuntimeMvid] = runtime.ManifestModule.ModuleVersionId.ToString(),
            [ActualMinimalBeamGuestSendHostNames.CapturedBeamSha] = Hash(bytes),
            [ActualMinimalBeamGuestSendHostNames.CapturedProvenanceSha] = Hash(provenance)
        });
    }

    private static async Task PublicationAndOrder(BeamModule beam)
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        Term payload = Term.Tuple(Term.A(ActualMinimalBeamGuestSendHostNames.Payload), fixture.Producer.Process.Pid);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await NormalDistribution(context);
            Term leader = await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ActualMinimalBeamGuestSendHostNames.GroupLeader,
                context.Self,
                connection.Remote
            );
            Check(leader.Equals(Term.A(ErlangBooleanAtoms.True)));
            ValueTask<Term> operation = beam.Invoke(
                context,
                ActualMinimalBeamGuestSendHostNames.Bang,
                connection.Remote,
                payload
            );
            Check(ReferenceEquals(await operation, payload));
            Check(ActiveReferences(context, beam) == 0);

            return true;
        });
        var leaderPacket = await fixture.Take(connection);
        Check(leaderPacket.Payload is null && leaderPacket.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.GroupLeader), fixture.Producer.Process.Pid, connection.Remote)));
        CheckSend(await fixture.Take(connection), connection.Remote, payload);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(connection.Host).QueuedPackets == 0);
    }

    private static async Task PressureAndRegisters(BeamModule beam)
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        int capacity = fixture.Fill(connection);
        Check(capacity > 0);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        Term payload = Term.Tuple(Term.A(ActualMinimalBeamGuestSendHostNames.Payload), connection.Remote);
        Task<Term> waiting = fixture.Producer.InvokeAsync(async context =>
        {
            try
            {
                await NormalDistribution(context);
                Term[] arguments = [connection.Remote, payload];
                ValueTask<Term> operation = beam.Invoke(context, ActualMinimalBeamGuestSendHostNames.Bang, arguments);
                Check(!operation.IsCompleted);
                arguments[0] = context.Self;
                arguments[1] = Term.A(ActualMinimalBeamGuestSendHostNames.ChangedPayload);
                entered.TrySetResult(context);

                return await operation;
            }
            catch (Exception exception)
            {
                entered.TrySetException(exception);
                throw;
            }
        });
        ProcessContext active = await entered.Task.WaitAsync(ActualMinimalBeamGuestSendHostNames.Deadline);
        Check(!waiting.IsCompleted && ActiveReferences(active, beam) > 0);
        await fixture.DrainFiller(connection, 1);
        Check(ReferenceEquals(await waiting.WaitAsync(ActualMinimalBeamGuestSendHostNames.Deadline), payload));
        Check(ActiveReferences(active, beam) == 0);
        await fixture.DrainFiller(connection, capacity - 1);
        CheckSend(await fixture.Take(connection), connection.Remote, payload);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(connection.Host).QueuedPackets == 0);
    }

    private static async Task NormalDistribution(ProcessContext context)
    {
        await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ActualMinimalBeamGuestSendHostNames.ProcessFlag,
            Term.A(ActualMinimalBeamGuestSendHostNames.AsyncDistribution),
            Term.A(ErlangBooleanAtoms.False)
        );
    }

    private static void CheckSend(DistributionDecodedProcessPacket packet, Pid remote, Term payload)
    {
        Check(packet.Control.Equals(Term.Tuple(Term.I(DistributionControlOpcodes.Send),
            Term.A(DistributionPassThroughLayout.EmptyCookie), remote)));
        Check(packet.Payload is not null && packet.Payload.Equals(payload));
    }

    private static void CheckActualSend(BeamModule beam)
    {
        object code = Field(beam, ActualMinimalBeamGuestSendHostNames.Code);
        object function = ((IEnumerable)Property(code, ActualMinimalBeamGuestSendHostNames.Functions)).Cast<object>().Single(value =>
            (string)Property(value, ActualMinimalBeamGuestSendHostNames.Name) == ActualMinimalBeamGuestSendHostNames.Bang &&
            (int)Property(value, ActualMinimalBeamGuestSendHostNames.Arity) == ActualMinimalBeamGuestSendHostNames.BangArity);
        var instructions = ((IEnumerable)Property(code, ActualMinimalBeamGuestSendHostNames.Instructions)).Cast<object>()
            .Skip((int)Property(function, ActualMinimalBeamGuestSendHostNames.FirstInstruction))
            .Take((int)Property(function, ActualMinimalBeamGuestSendHostNames.EndInstruction) -
                (int)Property(function, ActualMinimalBeamGuestSendHostNames.FirstInstruction));
        Check(instructions.Count(value => (string)Property(Property(value, ActualMinimalBeamGuestSendHostNames.Definition),
            ActualMinimalBeamGuestSendHostNames.Name) == ActualMinimalBeamGuestSendHostNames.SendInstruction) == ActualMinimalBeamGuestSendHostNames.ExpectedSendInstructions);
    }

    private static int ActiveReferences(ProcessContext context, BeamModule beam)
    {
        object image = Property(Field(beam, ActualMinimalBeamGuestSendHostNames.Execution), ActualMinimalBeamGuestSendHostNames.Image);
        object gate = Field(context, ActualMinimalBeamGuestSendHostNames.CodeGate);
        lock (gate)
        {
            var active = (IDictionary)Field(context, ActualMinimalBeamGuestSendHostNames.ActiveCode);

            return active.Contains(image) ? (int)active[image]! : 0;
        }
    }

    private static object Field(object value, string name) => value.GetType()
        .GetField(name, ActualMinimalBeamGuestSendHostNames.Members)!.GetValue(value)!;

    private static object Property(object value, string name) => value.GetType()
        .GetProperty(name, ActualMinimalBeamGuestSendHostNames.Members)!.GetValue(value)!;

    private static byte[] Resource(Assembly assembly, string suffix)
    {
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ActualMinimalBeamGuestSendHostNames.Failure);
    }
}
