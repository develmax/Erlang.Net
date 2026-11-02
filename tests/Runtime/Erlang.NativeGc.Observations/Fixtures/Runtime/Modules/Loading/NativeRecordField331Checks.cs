using Erlang.Compiler;
using static Erlang.Tests.NativeRecordDefaults331Names;

namespace Erlang.Tests;

internal static class NativeRecordField331Checks
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    internal static async Task Run(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
    {
        await actor.InvokeAsync(async context =>
        {
            var monitor = (TupleTerm)await Execution.EvaluateAsync(
                new Expr.Call(
                    SourceLiteral819Names.DictionaryModule,
                    SpawnMonitor,
                    [new Expr.Literal(Term.A(Worker)), new Expr.Literal(Term.A(WorkerFunction)), new Expr.Literal(Term.List(context.Self))]
                ),
                context
            );
            TupleTerm? result = await context.ReceiveAsync<TupleTerm>(
                candidate => candidate is TupleTerm tuple
                && (tuple.Items.Count == 2 && tuple.Items[0].Equals(Term.A(Finished))
                    || tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(Down)) && tuple.Items[1].Equals(monitor.Items[1]) && tuple.Items[3].Equals(monitor.Items[0])) ? tuple : null,
                Deadline
            );
            if (result is null || result.Items.Count != 2)
                throw new InvalidOperationException(WorkerFailure + (result is null ? NativeRecordDefaults313Names.Failure : result.Items[4].ToString()));
            Term outcome = result.Items[1];
            TupleTerm? down = await context.ReceiveAsync<TupleTerm>(
                candidate => candidate is TupleTerm { Items.Count: 5 } tuple
                && tuple.Items[0].Equals(Term.A(Down)) && tuple.Items[1].Equals(monitor.Items[1]) && tuple.Items[3].Equals(monitor.Items[0]) ? tuple : null,
                Deadline
            );
            if (down is null || !down.Items[4].Equals(Term.A(Normal)))
                throw new InvalidOperationException(WorkerFailure + down);
            byte[] expected = NativeRecordDefaults313BeamChecks.Resource(OriginalImage, OriginalHash);
            if (!outcome.Equals(ExternalTermFormat.Decode(expected)) || !ExternalTermFormat.Encode(outcome).AsSpan().SequenceEqual(expected))
                throw new InvalidOperationException(OutcomeFailure + outcome);

            return true;
        });
    }
}
