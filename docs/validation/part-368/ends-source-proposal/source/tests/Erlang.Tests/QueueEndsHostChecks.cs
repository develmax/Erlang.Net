using Erlang.Differential;

namespace Erlang.Tests;

public static class QueueEndsHostChecks
{
    public static async Task Representation()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            for (int count = 0; count <= QueueEndsHostNames.MaximumLength; count++)
            {
                Term source = Cons.From(Enumerable.Range(0, count).Select(value => Term.I(value)));
                Term queue = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.FromList, source);
                Term retained = queue;
                Term inserted = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.InReverse, Term.I(-1), queue);
                Term insertedList = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.ToList, inserted);
                QueueEndsMfaContracts.Require(insertedList.Equals(new Cons(Term.I(-1), source)));
                if (count > 0)
                {
                    Term removed = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.OutReverse, queue);
                    var pair = (TupleTerm)removed;
                    QueueEndsMfaContracts.Require(pair.Items[0].Equals(Term.Tuple(Term.A(QueueEndsHostNames.Value), Term.I(count - 1))));
                    Term remaining = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.ToList, pair.Items[1]);
                    QueueEndsMfaContracts.Require(remaining.Equals(Cons.From(Enumerable.Range(0, count - 1).Select(value => Term.I(value)))));
                    Term dropped = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.DropReverse, queue);
                    QueueEndsMfaContracts.Require(dropped.Equals(pair.Items[1]));
                }
                QueueEndsMfaContracts.Require((await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.ToList, retained)).Equals(source));
            }
            return Nil.Value;
        });
    }

    public static async Task ImproperLists()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tail = Term.A(QueueEndsHostNames.Tail);
            Term improper = new Cons(Term.I(1), tail);
            Term rear = Term.Tuple(improper, Nil.Value);
            Term front = Term.Tuple(Nil.Value, improper);
            foreach (string function in new[] { QueueModuleNames.Get, QueueModuleNames.Head })
                QueueEndsMfaContracts.Require((await runtime.Modules.Call(context, QueueModuleNames.Module, function, front)).Equals(Term.I(1)));
            foreach (string function in new[] { QueueModuleNames.GetReverse, QueueModuleNames.Last, QueueModuleNames.Daeh })
                QueueEndsMfaContracts.Require((await runtime.Modules.Call(context, QueueModuleNames.Module, function, rear)).Equals(Term.I(1)));
            QueueEndsMfaContracts.Require((await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.OutReverse, rear))
                .Equals(Term.Tuple(Term.Tuple(Term.A(QueueEndsHostNames.Value), Term.I(1)), Term.Tuple(tail, Nil.Value))));
            QueueEndsMfaContracts.Require((await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.Drop, front)).Equals(Term.Tuple(Nil.Value, tail)));
            QueueEndsMfaContracts.Require((await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.DropReverse, rear)).Equals(Term.Tuple(tail, Nil.Value)));
            foreach (string function in new[] { QueueModuleNames.Get, QueueModuleNames.Head, QueueModuleNames.GetReverse, QueueModuleNames.Last, QueueModuleNames.Daeh, QueueModuleNames.Peek, QueueModuleNames.PeekReverse })
            {
                Term input = function is QueueModuleNames.Get or QueueModuleNames.Head or QueueModuleNames.Peek ? rear : front;
                try
                {
                    await runtime.Modules.Call(context, QueueModuleNames.Module, function, input);
                    throw new InvalidOperationException(QueueEndsHostNames.Failure);
                }
                catch (ErlangException error)
                {
                    QueueEndsMfaContracts.Require(error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)));
                    var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
                    QueueEndsMfaContracts.Require(frame.Items[0].Equals(Term.A(ListModuleNames.Module))
                        && frame.Items[1].Equals(Term.A(ListModuleNames.Last))
                        && frame.Items[2].Equals(Term.List(tail)));
                }
            }
            return Nil.Value;
        });
    }

    public static async Task Compiled()
    {
        var assembly = typeof(QueueEndsHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(QueueEndsHostNames.FixtureFile, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), QueueEndsHostNames.FixtureFile);
        Term actual = await CompiledModuleExecution.Run(artifact);
        Term expected = Cons.From(QueueEndsMfaContracts.Exports.Select(export => QueueEndsMfaContracts.Expected(export.Function)));
        QueueEndsMfaContracts.Require(actual.Equals(expected));
    }
}
