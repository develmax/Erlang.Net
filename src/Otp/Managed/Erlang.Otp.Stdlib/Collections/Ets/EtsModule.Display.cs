// Derived from lib/stdlib/src/ets.erl, OTP-29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026.
namespace Erlang;

public static partial class EtsModule
{
    private static ValueTask<Term> Print(ProcessContext context, string format, params Term[] arguments)
        => Call(
            context,
            EtsDependencyModules.Io,
            EtsFunctionNames.Format,
            Term.String(format),
            Term.List(arguments)
        );

    private static async ValueTask<Term> Render(ProcessContext context, Term value)
        => await Call(
            context,
            EtsDependencyModules.Lists,
            EtsFunctionNames.Flatten,
            await Call(
                context,
                EtsDependencyModules.IoLib,
                EtsFunctionNames.Format,
                Term.String(EtsDisplayText.Term),
                Term.List(value)
            )
        );

    private static async ValueTask<Term> DisplayTables(ProcessContext context)
    {
        await DisplayRow(
            context,
            [A(EtsAtomNames.Id), A(EtsAtomNames.Name), A(EtsAtomNames.Type), A(EtsAtomNames.Size), A(EtsAtomNames.Mem), A(EtsAtomNames.Owner)]
        );
        await Call(
            context,
            EtsDependencyModules.Io,
            EtsFunctionNames.Format,
            Term.String(EtsDisplayText.TableRule)
        );
        Term tables = await Call(
            context,
            EtsDependencyModules.Lists,
            EtsFunctionNames.Sort,
            await Invoke(context, EtsFunctionNames.All, [])
        );
        foreach (Term table in Cons.Items(tables))
        {
            try
            {
                var values = new List<Term> { table };
                foreach (string key in new[] { EtsAtomNames.Name, EtsAtomNames.Type, EtsAtomNames.Size, EtsAtomNames.Memory, EtsAtomNames.Owner })
                {
                    values.Add(await Call(
                        context,
                        Name,
                        EtsFunctionNames.Info,
                        table,
                        A(key)
                    ));
                }
                Term owner = await Call(
                    context,
                    EtsDependencyModules.Erlang,
                    EtsFunctionNames.ProcessInfo,
                    values[5],
                    A(EtsAtomNames.RegisteredName)
                );
                if (owner is TupleTerm { Items.Count: 2 } registered && Is(registered.Items[0], EtsAtomNames.RegisteredName))
                {
                    values[5] = registered.Items[1];
                }
                await DisplayRow(context, values.ToArray());
            }
            catch (ErlangException error) when (error.ExceptionClass != EtsExceptionClasses.Throw)
            {
                await Print(context, EtsDisplayText.UnreadableTable, await Render(context, table));
            }
        }

        return A(EtsAtomNames.Ok);
    }

    private static async ValueTask<Term> DisplayRow(ProcessContext context, Term[] values)
    {
        int[] widths = EtsDisplayLayout.ColumnWidths;
        for (int index = 0; index < values.Length; index++)
        {
            Term text = await Render(context, values[index]);
            int length = Cons.Items(text).Count();
            values[index] = length >= widths[index] ? text
                : Cons.From(Cons.Items(text).Concat(Enumerable.Repeat<Term>(Term.I(EtsDisplayLayout.SpaceCodePoint), widths[index] - length)));
        }

        return await Print(context, EtsDisplayText.TableRow, values);
    }

    private static async ValueTask<Term> DisplayItem(
        ProcessContext context,
        Term width,
        Term item,
        long position
    )
    {
        using var source = EtsSource.Frame(
            context,
            EtsFunctionNames.DoDisplayItem,
            4,
            EtsSourceLines.RenderObject
        );
        Term text = await Render(context, item);
        Term available = await Call(
            context,
            EtsDependencyModules.Erlang,
            EtsAtomNames.Subtract,
            width,
            Term.I(EtsDisplayLayout.ObjectPrefixWidth)
        );
        if (TermOrder.Compare(Term.I(Cons.Items(text).Count()), available) > 0)
        {
            using var slice = context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.TruncateObject));
            Term cut = await Call(
                context,
                EtsDependencyModules.Erlang,
                EtsAtomNames.Subtract,
                width,
                Term.I(EtsDisplayLayout.TruncatedObjectOverhead)
            );
            text = Cons.From(
                Cons.Items(await Call(
                    context,
                    EtsDependencyModules.String,
                    EtsFunctionNames.Slice,
                    text,
                    Term.I(0),
                    cut
                )),
                Term.String(EtsDisplayText.TruncationMarker)
            );
        }

        return await Print(
            context,
            EtsDisplayText.NumberedObject,
            Term.I(position),
            text
        );
    }

    private static async ValueTask<bool> RegexMatches(ProcessContext context, Term item, Term regex)
        => Is(
            await Call(
                context,
                EtsDependencyModules.Re,
                EtsFunctionNames.Run,
                await Render(context, item),
                regex,
                Term.List(Term.Tuple(A(EtsAtomNames.Capture), A(EtsAtomNames.None)))
            ),
            EtsFunctionNames.Match
        );

    private static async ValueTask PrintNumber(
        ProcessContext context,
        Term table,
        Term number,
        Term? regex
    )
    {
        Term key = await Call(
            context,
            Name,
            EtsFunctionNames.First,
            table
        );
        while (regex is null || !Is(key, EtsAtomNames.EndOfTable))
        {
            var rows = new List<Term>();
            foreach (Term row in Cons.Items(await Call(
                context,
                Name,
                EtsFunctionNames.Lookup,
                table,
                key
            )))
            {
                if (regex is null || await RegexMatches(context, row, regex))
                {
                    rows.Add(row);
                }
            }
            Term rest = await Call(
                context,
                EtsDependencyModules.Erlang,
                EtsAtomNames.Subtract,
                number,
                Term.I(rows.Count)
            );
            if (TermOrder.Compare(rest, Term.I(1)) < 0)
            {
                Term row = await Call(
                    context,
                    EtsDependencyModules.Lists,
                    EtsFunctionNames.Nth,
                    number,
                    Cons.From(rows)
                );
                await Print(context, EtsDisplayText.TermLine, row);

                return;
            }
            number = rest;
            key = await Call(
                context,
                Name,
                EtsFunctionNames.Next,
                table,
                key
            );
        }
    }

    private static async ValueTask<Term> Browse(
        ProcessContext context,
        Term tableArg,
        Term height,
        Term width
    )
    {
        Term table = await SoftWhereis(context, tableArg);
        Term key = await Call(
            context,
            Name,
            EtsFunctionNames.First,
            table
        );
        Term? regex = null;
        long turn = 1;
        long position = 1;
        bool forceDisplay = false;
        while (true)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            bool end = Is(key, EtsAtomNames.EndOfTable);
            if (!end && (forceDisplay || TermOrder.Compare(Term.I(turn), height) < 0))
            {
                using var display = EtsSource.Frame(
                    context,
                    EtsFunctionNames.DoDisplay,
                    6,
                    EtsSourceLines.DisplayObjects,
                    tail: true
                );
                forceDisplay = false;
                foreach (Term row in Cons.Items(await Call(
                    context,
                    Name,
                    EtsFunctionNames.Lookup,
                    table,
                    key
                )))
                {
                    if (regex is null || await RegexMatches(context, row, regex))
                    {
                        using var items = EtsSource.Frame(
                            context,
                            EtsFunctionNames.DoDisplayItems,
                            4,
                            EtsSourceLines.DisplayObject
                        );
                        await DisplayItem(
                            context,
                            width,
                            row,
                            position++
                        );
                        turn++;
                    }
                }
                key = await Call(
                    context,
                    Name,
                    EtsFunctionNames.Next,
                    table,
                    key
                );
                continue;
            }
            Term prompt = A(end ? EtsDisplayText.EndPrompt : EtsDisplayText.BrowsePrompt);
            Term line = await Call(
                context,
                EtsDependencyModules.Io,
                EtsFunctionNames.GetLine,
                prompt
            );
            if (line is BitString)
            {
                line = await Call(
                    context,
                    EtsDependencyModules.Unicode,
                    EtsFunctionNames.CharactersToList,
                    line
                );
            }
            if (line.Equals(Term.String(EtsDisplayText.NewLine)))
            {
                line = Term.String(EtsDisplayText.ContinueCommand);
            }
            if (Is(line, EtsAtomNames.Eof) || line.Equals(Term.String(EtsDisplayText.QuitCommand)) || end && regex is null && line.Equals(Term.String(EtsDisplayText.ContinueCommand)))
            {
                return A(EtsAtomNames.Ok);
            }
            if (line.Equals(Term.String(EtsDisplayText.ContinueCommand)))
            {
                turn = 1;
                forceDisplay = regex is null;
                continue;
            }
            if (line.Equals(Term.String(EtsDisplayText.DeleteCommand)))
            {
                await Call(
                    context,
                    Name,
                    EtsFunctionNames.Delete,
                    table
                );

                return A(EtsAtomNames.Ok);
            }
            if (line is Cons { Head: Integer first } command && (first.Value == EtsDisplayLayout.PrintCommandCodePoint || first.Value == EtsDisplayLayout.RegexCommandCodePoint))
            {
                Term suffix = await Call(
                    context,
                    EtsDependencyModules.String,
                    EtsFunctionNames.Trim,
                    command.Tail,
                    A(EtsAtomNames.Trailing),
                    Term.String(EtsDisplayText.RegexLineEnding)
                );
                if (first.Value == EtsDisplayLayout.PrintCommandCodePoint)
                {
                    try
                    {
                        Term number;
                        try
                        {
                            number = await Call(
                                context,
                                EtsDependencyModules.Erlang,
                                EtsFunctionNames.ListToInteger,
                                suffix
                            );
                        }
                        catch (ErlangException error) when (error.ExceptionClass != EtsExceptionClasses.Throw)
                        {
                            await Call(
                                context,
                                EtsDependencyModules.Io,
                                EtsFunctionNames.PutChars,
                                Term.String(EtsDisplayText.InvalidNumber)
                            );
                            continue;
                        }
                        await PrintNumber(
                            context,
                            table,
                            number,
                            regex
                        );
                    }
                    catch (ErlangException)
                    {
                    }
                }
                else
                {
                    Term compiled = await Call(
                        context,
                        EtsDependencyModules.Re,
                        EtsFunctionNames.Compile,
                        suffix,
                        Term.List(A(EtsAtomNames.Unicode))
                    );
                    if (compiled is TupleTerm { Items.Count: 2 } pair && Is(pair.Items[0], EtsAtomNames.Ok))
                    {
                        regex = pair.Items[1];
                        key = await Call(
                            context,
                            Name,
                            EtsFunctionNames.First,
                            table
                        );
                        turn = 1;
                        position = 1;
                    }
                    else if (compiled is TupleTerm { Items.Count: 2 } error && error.Items[1] is TupleTerm { Items.Count: 2 } detail)
                    {
                        await Print(context, EtsDisplayText.UnicodeLine, detail.Items[0]);
                    }
                }
            }
        }
    }
}
