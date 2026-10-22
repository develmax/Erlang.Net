using Erlang;
using static Erlang.Tests.EtsConcurrency411Names;

namespace Erlang.Tests;

internal static class EtsConcurrency411Checks
{
    private static Term Option(string name, Term value) => Term.Tuple(Term.A(name), value);

    private static void Expect(
        Term options,
        bool read,
        string write,
        int locks = 0,
        bool privateTable = false
    )
    {
        object parsed = EtsConcurrency411Access.Parse(options);
        EtsFoundation405Access.Require((bool)EtsConcurrency411Access.Read(parsed, ReadProperty) == read);
        EtsFoundation405Access.Require(EtsConcurrency411Access.Read(parsed, WriteProperty).ToString() == write);
        EtsFoundation405Access.Require((int)EtsConcurrency411Access.Read(parsed, LocksProperty) == locks);
        if (privateTable) EtsFoundation405Access.Require(EtsConcurrency411Access.Read(parsed, ProtectionProperty).ToString() == PrivateProtection);
    }

    internal static Task Flags()
    {
        Term yes = Term.A(ErlangBooleanAtoms.True), no = Term.A(ErlangBooleanAtoms.False), auto = Term.A(Auto);
        Term fixedLocks = Term.Tuple(Term.A(FixedLocks), Term.I(8));
        Expect(Nil.Value, false, Disabled);
        Expect(Term.List(Option(Read, yes), Option(Write, auto)), true, Automatic);
        Expect(Term.List(
            Option(Read, yes),
            Option(Read, no),
            Option(Write, auto),
            Option(Write, yes)
        ), false, Enabled);
        Expect(Term.List(
            Option(Write, yes),
            Option(Write, auto),
            Option(Read, no),
            Option(Read, yes)
        ), true, Automatic);
        Expect(
            Term.List(Option(Write, fixedLocks)),
            false,
            Fixed,
            8
        );
        Expect(Term.List(Option(Write, fixedLocks), Option(Write, no)), false, Disabled);
        Expect(Term.List(Option(Write, fixedLocks), Term.A(Ordered), Option(Write, auto)), false, Automatic);
        Expect(
            Term.List(Term.A(Private), Option(Read, yes), Option(Write, auto)),
            true,
            Automatic,
            privateTable: true
        );

        return Task.CompletedTask;
    }

    internal static Task Invalid()
    {
        Term yes = Term.A(ErlangBooleanAtoms.True), auto = Term.A(Auto), invalid = Term.A(EtsConcurrency411Names.Invalid);
        foreach (Term options in new[]
        {
            Term.List(Option(Read, auto)), Term.List(Option(Read, Term.I(1))), Term.List(Option(Write, invalid)),
            Term.List(Term.Tuple(Term.A(Read))), Term.List(Term.Tuple(Term.A(Write), yes, yes)),
            Term.List(Option(Read, invalid), Option(Read, yes)), Term.List(Option(Write, invalid), Option(Write, auto)),
            new Cons(Option(Read, yes), invalid),
            Term.List(Option(Write, Term.Tuple(Term.A(FixedLocks), Term.I(0)))),
            Term.List(Option(Write, Term.Tuple(Term.A(FixedLocks), Term.I(32769)))),
            Term.List(Option(Write, Term.Tuple(Term.A(FixedLocks), Term.I(8))), Term.A(Ordered)),
            Term.List(Term.A(Private), Term.A(Ordered), Option(Write, Term.Tuple(Term.A(FixedLocks), Term.I(8))))
        }) EtsConcurrency411Access.Refuse(options, BadArgumentType);
        EtsConcurrency411Access.Refuse(Term.List(Term.A(Compressed)), nameof(NotSupportedException));
        EtsConcurrency411Access.Refuse(Term.List(Option(Counters, yes)), nameof(NotSupportedException));
        _ = EtsConcurrency411Access.Parse(Term.List(Option(Heir, Term.A(None))));
        bool canceled = false;
        try
        {
            _ = EtsConcurrency411Access.Parse(Term.List(Option(Read, yes)), new CancellationToken(true));
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        EtsFoundation405Access.Require(canceled);

        return Task.CompletedTask;
    }

    internal static async Task ConcurrentRoundTrip()
    {
        await using var runtime = new ProcessRuntime(WriterCount);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        var writers = new List<RuntimeNodeFoundationHostActor>();
        var bodies = EtsSelect409Access.Bodies(runtime);
        Exception? original = null;
        try
        {
            Term table = await owner.InvokeAsync(async context => await EtsSelect409Access.Call(
                bodies,
                context,
                New,
                Term.A(TableName),
                Term.List(Term.A(Public), Option(Read, Term.A(ErlangBooleanAtoms.True)), Option(Write, Term.A(Auto)))
            ));
            for (int index = 0; index < WriterCount; index++) writers.Add(new(runtime));
            await Task.WhenAll(writers.Select((actor, writer) => actor.InvokeAsync(async context =>
            {
                for (int key = 0; key < KeysPerWriter; key++)
                {
                    Term identity = Term.I(writer * KeysPerWriter + key);
                    Term row = Term.Tuple(identity, Term.I(key));
                    _ = await EtsSelect409Access.Call(
                        bodies,
                        context,
                        Insert,
                        table,
                        row
                    );
                    EtsFoundation405Access.Require((await EtsSelect409Access.Call(
                        bodies,
                        context,
                        Lookup,
                        table,
                        identity
                    )).Equals(Term.List(row)));
                }

                return true;
            })));
            await owner.InvokeAsync(async context =>
            {
                Term program = Term.List(Term.Tuple(Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.List(Term.A(MatchSpecNames.Whole))));
                EtsFoundation405Access.Require(Cons.Items(await EtsSelect409Access.Call(
                    bodies,
                    context,
                    Select,
                    table,
                    program
                )).Count() == WriterCount * KeysPerWriter);
                _ = await EtsSelect409Access.Call(
                    bodies,
                    context,
                    Delete,
                    table
                );

                return true;
            });
            EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
        }
        catch (Exception error)
        {
            original = error;
            throw;
        }
        finally
        {
            List<Exception>? failures = null;
            foreach (var writer in writers)
            {
                try
                {
                    await writer.DisposeAsync();
                }
                catch (Exception error)
                {
                    failures ??= original is null ? [] : [original];
                    failures.Add(error);
                }
            }
            if (failures is not null) throw new AggregateException(failures);
        }
    }
}
