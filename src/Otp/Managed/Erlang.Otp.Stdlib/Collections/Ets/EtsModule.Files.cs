// Derived from lib/stdlib/src/ets.erl, OTP-29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026.
namespace Erlang;

public static partial class EtsModule
{
    private static void Throw(Term reason) => throw new ErlangException(reason, EtsExceptionClasses.Throw);

    private static Term ErrorResult(Term reason) => Term.Tuple(A(EtsAtomNames.Error), reason);

    private static bool ErrorPair(Term value, out Term reason)
    {
        if (value is TupleTerm { Items.Count: 2 } pair && Is(pair.Items[0], EtsAtomNames.Error))
        {
            reason = pair.Items[1];

            return true;
        }
        reason = Nil.Value;

        return false;
    }

    private static Term Field(IReadOnlyList<Term> fields, string name, Term? fallback = null)
    {
        foreach (Term field in fields)
        {
            if (field is TupleTerm { Items.Count: >= 1 } pair && Is(pair.Items[0], name))
            {
                return pair;
            }
        }

        return fallback ?? A(EtsAtomNames.False);
    }

    private static Term FieldValue(IReadOnlyList<Term> fields, string name, Term? fallback = null)
        => Field(fields, name) is TupleTerm { Items.Count: 2 } pair ? pair.Items[1] : fallback ?? A(EtsAtomNames.False);

    private static EtsFileOptions ParseSaveOptions(ProcessContext context, Term options)
    {
        if (!IsList(options))
        {
            throw EtsSource.Error(
                context,
                EtsFunctionNames.ParseFtOptions,
                [options],
                A(EtsErrorNames.FunctionClause),
                EtsSourceLines.ParseSaveOptions
            );
        }
        var result = new EtsFileOptions();
        bool extended = false;
        Term cursor = options;
        while (cursor is Cons cell)
        {
            if (cell.Head is TupleTerm { Items.Count: 2 } pair && Is(pair.Items[0], EtsFileFields.Sync)
                && (Is(pair.Items[1], EtsAtomNames.True) || Is(pair.Items[1], EtsAtomNames.False)))
            {
                result.Sync |= Is(pair.Items[1], EtsAtomNames.True);
            }
            else if (cell.Head is TupleTerm { Items.Count: 2 } info && Is(info.Items[0], EtsFileFields.ExtendedInfo) && !extended)
            {
                Term entries = info.Items[1];
                while (entries is Cons entry)
                {
                    if (Is(entry.Head, EtsFileFields.ObjectCount))
                    {
                        result.ObjectCount = true;
                    }
                    else if (Is(entry.Head, EtsFileFields.Md5sum))
                    {
                        result.Md5 = true;
                    }
                    else
                    {
                        Throw(Term.Tuple(A(EtsErrorNames.UnknownOption), Term.List(Term.Tuple(A(EtsFileFields.ExtendedInfo), Term.List(entry.Head)))));
                    }
                    entries = entry.Tail;
                }
                if (entries is not Nil)
                {
                    Throw(Term.Tuple(A(EtsErrorNames.MalformedOption), entries));
                }
                extended = true;
            }
            else
            {
                Throw(Term.Tuple(A(EtsErrorNames.UnknownOption), cell.Head));
            }
            cursor = cell.Tail;
        }
        if (cursor is not Nil)
        {
            Throw(Term.Tuple(A(EtsErrorNames.MalformedOption), cursor));
        }

        return result;
    }

    private static async ValueTask<Term> Log(
        ProcessContext context,
        Term name,
        Term state,
        Term terms,
        bool digest
    )
    {
        Term values = terms;
        if (digest)
        {
            var binaries = new List<Term>();
            foreach (Term item in Cons.Items(terms))
            {
                Term binary = await Call(
                    context,
                    EtsDependencyModules.Erlang,
                    EtsFunctionNames.TermToBinary,
                    item
                );
                state = await Call(
                    context,
                    EtsDependencyModules.Erlang,
                    EtsFunctionNames.Md5Update,
                    state,
                    binary
                );
                binaries.Add(binary);
            }
            values = Cons.From(binaries);
        }
        Term logged = await Call(
            context,
            EtsDependencyModules.DiskLog,
            digest ? EtsFunctionNames.BlogTerms : EtsFunctionNames.LogTerms,
            name,
            values
        );
        if (ErrorPair(logged, out Term reason))
        {
            Throw(reason);
        }
        if (!Is(logged, EtsAtomNames.Ok))
        {
            throw new ErlangException(Term.Tuple(A(EtsErrorNames.CaseClause), logged));
        }

        return digest ? state : A(EtsAtomNames.True);
    }

    private static async ValueTask<Term> Save(
        ProcessContext context,
        Term tableArg,
        Term file,
        Term options
    )
    {
        try
        {
            Term info = await Call(
                context,
                Name,
                EtsFunctionNames.Info,
                tableArg
            );
            if (Is(info, EtsAtomNames.Undefined))
            {
                Throw(A(EtsErrorNames.Badtab));
            }
            Term[] fields = Cons.Items(info).ToArray();
            Term table = FieldValue(fields, EtsAtomNames.Id);
            EtsFileOptions parsed;
            using (context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.SaveOptions)))
            {
                parsed = ParseSaveOptions(context, options);
            }
            await Call(
                context,
                EtsDependencyModules.File,
                EtsFunctionNames.Delete,
                file
            );
            Term stat = await Call(
                context,
                EtsDependencyModules.File,
                EtsFunctionNames.ReadFileInfo,
                file
            );
            if (!stat.Equals(ErrorResult(A(EtsAtomNames.Enoent))))
            {
                Throw(A(EtsAtomNames.Eaccess));
            }
            Term name = await Call(context, EtsDependencyModules.Erlang, EtsFunctionNames.MakeRef);
            Term opened = await Call(
                context,
                EtsDependencyModules.DiskLog,
                EtsFunctionNames.Open,
                Term.List(
                    Term.Tuple(A(EtsAtomNames.Name), name),
                    Term.Tuple(A(EtsAtomNames.File), file),
                    Term.Tuple(A(EtsAtomNames.Repair), A(EtsAtomNames.Truncate))
                )
            );
            if (ErrorPair(opened, out Term openReason))
            {
                Throw(openReason);
            }
            if (!opened.Equals(Term.Tuple(A(EtsAtomNames.Ok), name)))
            {
                throw new ErlangException(Term.Tuple(A(EtsErrorNames.CaseClause), opened));
            }
            try
            {
                Term header = Term.Tuple(fields.Concat(new Term[]
                {
                    Term.Tuple(A(EtsFileFields.MajorVersion), Term.I(EtsFileFormat.MajorVersion)),
                    Term.Tuple(A(EtsFileFields.MinorVersion), Term.I(EtsFileFormat.MinorVersion)),
                    Term.Tuple(A(EtsFileFields.ExtendedInfo), parsed.Extended)
                }).ToArray());
                Term state = parsed.Md5 ? await Call(context, EtsDependencyModules.Erlang, EtsFunctionNames.Md5Init) : A(EtsAtomNames.True);
                try
                {
                    await Call(
                        context,
                        Name,
                        EtsFunctionNames.SafeFixtable,
                        table,
                        A(EtsAtomNames.True)
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == EtsExceptionClasses.Error && Is(error.Reason, EtsErrorNames.Badarg))
                {
                    Throw(A(EtsErrorNames.Badtab));
                }
                long count = 0;
                try
                {
                    state = await Log(
                        context,
                        name,
                        state,
                        Term.List(header),
                        parsed.Md5
                    );
                    Term cursor = await Call(
                        context,
                        Name,
                        EtsFunctionNames.Select,
                        table,
                        Term.List(Term.Tuple(A(EtsAtomNames.Wildcard), Nil.Value, Term.List(A(EtsAtomNames.WholeObject)))),
                        Term.I(EtsFileFormat.ObjectChunkSize)
                    );
                    while (!Is(cursor, EtsAtomNames.EndOfTable))
                    {
                        var chunk = (TupleTerm)cursor;
                        count += Cons.Items(chunk.Items[0]).LongCount();
                        state = await Log(
                            context,
                            name,
                            state,
                            chunk.Items[0],
                            parsed.Md5
                        );
                        cursor = await Call(
                            context,
                            Name,
                            EtsFunctionNames.Select,
                            chunk.Items[1]
                        );
                    }
                }
                finally
                {
                    await Ignore(() => Call(
                        context,
                        Name,
                        EtsFunctionNames.SafeFixtable,
                        table,
                        A(EtsAtomNames.False)
                    ));
                }
                var end = new List<Term>();
                if (parsed.ObjectCount)
                {
                    end.Add(Term.Tuple(A(EtsAtomNames.Count), Term.I(count)));
                }
                if (parsed.Md5)
                {
                    end.Add(Term.Tuple(A(EtsAtomNames.Md5), await Call(
                        context,
                        EtsDependencyModules.Erlang,
                        EtsFunctionNames.Md5Final,
                        state
                    )));
                }
                if (end.Count > 0)
                {
                    await Log(
                        context,
                        name,
                        state,
                        Term.List(Term.List(A(EtsAtomNames.EndOfTable), Cons.From(end))),
                        parsed.Md5
                    );
                }
                if (parsed.Sync)
                {
                    Term sync = await Call(
                        context,
                        EtsDependencyModules.DiskLog,
                        EtsFileFields.Sync,
                        name
                    );
                    if (ErrorPair(sync, out Term syncReason))
                    {
                        Throw(syncReason);
                    }
                }

                return await Call(
                    context,
                    EtsDependencyModules.DiskLog,
                    EtsFunctionNames.Close,
                    name
                );
            }
            catch (ErlangException)
            {
                await Call(
                    context,
                    EtsDependencyModules.DiskLog,
                    EtsFunctionNames.Close,
                    name
                );
                await Call(
                    context,
                    EtsDependencyModules.File,
                    EtsFunctionNames.Delete,
                    file
                );
                throw;
            }
        }
        catch (ErlangException error) when (error.ExceptionClass is EtsExceptionClasses.Throw or EtsExceptionClasses.Exit)
        {
            return ErrorResult(error.Reason);
        }
    }

    private static async ValueTask<Term> Chunk(
        ProcessContext context,
        Term name,
        Term cursor,
        int size,
        bool binary,
        bool verify
    )
    {
        Term result = await Call(
            context,
            EtsDependencyModules.DiskLog,
            binary ? EtsFunctionNames.Bchunk : EtsFunctionNames.Chunk,
            name,
            cursor,
            Term.I(size)
        );
        if (result is TupleTerm { Items.Count: 3 } triple)
        {
            if (verify && triple.Items[2] is Integer skipped && skipped.Value > 0)
            {
                Throw(A(EtsErrorNames.Badfile));
            }

            return Term.Tuple(triple.Items[0], triple.Items[1]);
        }

        return result;
    }

    private static async ValueTask<Term> OpenRead(ProcessContext context, Term file, bool verify)
    {
        Term name = await Call(context, EtsDependencyModules.Erlang, EtsFunctionNames.MakeRef);
        Term opened = await Call(
            context,
            EtsDependencyModules.DiskLog,
            EtsFunctionNames.Open,
            Term.List(
                Term.Tuple(A(EtsAtomNames.Name), name),
                Term.Tuple(A(EtsAtomNames.File), file),
                Term.Tuple(A(EtsAtomNames.Mode), A(EtsAtomNames.ReadOnly))
            )
        );
        if (opened.Equals(Term.Tuple(A(EtsAtomNames.Ok), name)))
        {
            return name;
        }
        if (opened is TupleTerm { Items.Count: 4 } repaired && Is(repaired.Items[0], EtsAtomNames.Repaired) && repaired.Items[1].Equals(name))
        {
            if (verify)
            {
                await Call(
                    context,
                    EtsDependencyModules.DiskLog,
                    EtsFunctionNames.Close,
                    name
                );
                Throw(A(EtsErrorNames.Badfile));
            }

            return name;
        }
        if (ErrorPair(opened, out Term reason))
        {
            Throw(Term.Tuple(A(EtsErrorNames.ReadError), reason));
        }
        Throw(opened);

        return name;
    }

    private static async ValueTask<EtsFileHeader> Header(ProcessContext context, Term name, bool verify)
    {
        Term result = await Chunk(
            context,
            name,
            A(EtsAtomNames.Start),
            1,
            verify,
            verify
        );
        if (result is not TupleTerm { Items.Count: 2 } pair || pair.Items[1] is not Cons { Tail: Nil } item)
        {
            Throw(A(EtsErrorNames.Badfile));
            throw new InvalidOperationException();
        }
        Term header = verify && item.Head is BitString ? await Call(
            context,
            EtsDependencyModules.Erlang,
            EtsFunctionNames.BinaryToTerm,
            item.Head
        ) : item.Head;
        if (header is not TupleTerm tuple)
        {
            Throw(A(EtsErrorNames.Badfile));
            throw new InvalidOperationException();
        }
        string[] mandatory = [EtsAtomNames.Name, EtsAtomNames.Type, EtsFileFields.Protection, EtsFileFields.NamedTable, EtsFileFields.Keypos, EtsAtomNames.Size];
        if (tuple.Items.Count(field => field is TupleTerm { Items.Count: 2 } fieldPair
            && fieldPair.Items[0] is Atom tag && mandatory.Contains(tag.Name)) != mandatory.Length)
        {
            Throw(A(EtsErrorNames.Badfile));
        }
        Term extra = FieldValue(tuple.Items, EtsFileFields.ExtendedInfo, Nil.Value);
        var parsed = new EtsFileOptions();
        if (IsList(extra))
        {
            parsed.ObjectCount = Cons.Items(extra).Any(value => Is(value, EtsFileFields.ObjectCount));
            parsed.Md5 = Cons.Items(extra).Any(value => Is(value, EtsFileFields.Md5sum));
        }
        Term digest = verify && parsed.Md5
            ? await Call(
                context,
                EtsDependencyModules.Erlang,
                EtsFunctionNames.Md5Update,
                await Call(context, EtsDependencyModules.Erlang, EtsFunctionNames.Md5Init),
                item.Head
            ) : A(EtsAtomNames.False);

        // OTP deliberately reads major/minor in the verifying branch, major_version/minor_version otherwise.
        return new EtsFileHeader(
            FieldValue(tuple.Items, verify ? EtsFileFields.Major : EtsFileFields.MajorVersion, Term.I(0)),
            FieldValue(tuple.Items, verify ? EtsFileFields.Minor : EtsFileFields.MinorVersion, Term.I(0)),
            parsed,
            digest,
            tuple.Items,
            pair.Items[0]
        );
    }

    private static (bool Verify, Term Table) ParseLoadOptions(Term options)
    {
        bool verify = false;
        Term table = Nil.Value;
        Term cursor = options;
        while (cursor is Cons cell)
        {
            if (cell.Head is TupleTerm { Items.Count: 2 } pair && Is(pair.Items[0], EtsFileFields.Verify)
                && (Is(pair.Items[1], EtsAtomNames.True) || Is(pair.Items[1], EtsAtomNames.False)))
            {
                verify |= Is(pair.Items[1], EtsAtomNames.True);
            }
            else if (cell.Head is TupleTerm { Items.Count: 2 } given && Is(given.Items[0], EtsAtomNames.Table) && table is Nil)
            {
                table = given.Items[1];
            }
            else
            {
                Throw(Term.Tuple(A(EtsErrorNames.UnknownOption), cell.Head));
            }
            cursor = cell.Tail;
        }
        if (cursor is not Nil)
        {
            Throw(Term.Tuple(A(EtsErrorNames.MalformedOption), cursor));
        }

        return (verify, table);
    }

    private static TupleTerm RequireFileField(
        ProcessContext context,
        IReadOnlyList<Term> fields,
        string key,
        int line
    )
    {
        using var location = context.EnterSourceLocation(EtsSource.Location(line));
        Term found = Field(fields, key);
        if (found is TupleTerm { Items.Count: 2 } pair)
        {
            return pair;
        }

        throw context.CaptureSourceFailure(new ErlangException(Term.Tuple(A(EtsErrorNames.Badmatch), found)));
    }

    private static ErlangException FileCaseFailure(ProcessContext context, Term value, int line)
    {
        using var location = context.EnterSourceLocation(EtsSource.Location(line));

        return context.CaptureSourceFailure(new ErlangException(Term.Tuple(A(EtsErrorNames.CaseClause), value)));
    }

    private static async ValueTask<Term> CreateFileTable(ProcessContext context, IReadOnlyList<Term> fields, Term existing)
    {
        using var source = EtsSource.Frame(
            context,
            EtsFunctionNames.CreateTab,
            2,
            EtsSourceLines.CreateTableName
        );
        Term name = RequireFileField(
            context,
            fields,
            EtsAtomNames.Name,
            EtsSourceLines.CreateTableName
        ).Items[1];
        Term type = RequireFileField(
            context,
            fields,
            EtsAtomNames.Type,
            EtsSourceLines.CreateTableType
        ).Items[1];
        Term protection = RequireFileField(
            context,
            fields,
            EtsFileFields.Protection,
            EtsSourceLines.CreateTableProtection
        ).Items[1];
        Term keypos = RequireFileField(
            context,
            fields,
            EtsFileFields.Keypos,
            EtsSourceLines.CreateTableKeyPosition
        );
        _ = RequireFileField(
            context,
            fields,
            EtsAtomNames.Size,
            EtsSourceLines.CreateTableSize
        );
        var options = new List<Term> { type, protection, keypos };
        Term named = Field(fields, EtsFileFields.NamedTable);
        if (named is TupleTerm { Items.Count: 2 } namedPair && Is(namedPair.Items[1], EtsAtomNames.True))
        {
            options.Insert(0, A(EtsFileFields.NamedTable));
        }
        else if (named is not TupleTerm { Items.Count: 2 } namedFalse || !Is(namedFalse.Items[1], EtsAtomNames.False))
        {
            throw FileCaseFailure(context, named, EtsSourceLines.CreateTableNamed);
        }
        Term compressed = Field(fields, EtsFileFields.Compressed);
        if (compressed is TupleTerm { Items.Count: 2 } compressedPair && Is(compressedPair.Items[1], EtsAtomNames.True))
        {
            options.Insert(0, A(EtsFileFields.Compressed));
        }
        else if (!Is(compressed, EtsAtomNames.False) && (compressed is not TupleTerm { Items.Count: 2 } compressedFalse || !Is(compressedFalse.Items[1], EtsAtomNames.False)))
        {
            throw FileCaseFailure(context, compressed, EtsSourceLines.CreateTableCompressed);
        }
        foreach (string option in new[] { EtsFileFields.WriteConcurrency, EtsFileFields.ReadConcurrency })
        {
            Term value = Field(fields, option);
            if (value is TupleTerm { Items.Count: 2 })
            {
                options.Insert(0, value);
            }
            else if (option == EtsFileFields.ReadConcurrency && !Is(value, EtsAtomNames.False))
            {
                throw FileCaseFailure(context, value, EtsSourceLines.CreateTableReadConcurrency);
            }
        }
        if (existing is not Nil)
        {
            return existing;
        }
        try
        {
            return await Call(
                context,
                Name,
                EtsFunctionNames.New,
                name,
                Cons.From(options)
            );
        }
        catch (ErlangException)
        {
            Throw(A(EtsErrorNames.CannotCreateTable));

            return Nil.Value;
        }
    }

    private static async ValueTask<Term> Load(ProcessContext context, Term file, Term options)
    {
        try
        {
            var parsed = ParseLoadOptions(options);
            Term name = await OpenRead(context, file, parsed.Verify);
            EtsFileHeader header;
            try
            {
                header = await Header(context, name, parsed.Verify);
            }
            catch (ErlangException error) when (error.ExceptionClass == EtsExceptionClasses.Throw && Is(error.Reason, EtsErrorNames.Badfile))
            {
                await Call(
                    context,
                    EtsDependencyModules.DiskLog,
                    EtsFunctionNames.Close,
                    name
                );
                throw;
            }
            try
            {
                if (TermOrder.Compare(header.Major, Term.I(EtsFileFormat.MajorVersion)) > 0)
                {
                    Throw(Term.Tuple(A(EtsErrorNames.UnsupportedFileVersion), Term.Tuple(header.Major, header.Minor)));
                }
                Term table;
                using (context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.LoadCreateTable)))
                {
                    table = await CreateFileTable(context, header.Fields, parsed.Table);
                }
                try
                {
                    EtsFileOptions validation = parsed.Verify ? header.Options : new EtsFileOptions();
                    Term cursor = header.Cursor;
                    Term digest = header.Digest;
                    Term last = Nil.Value;
                    long count = 0;
                    while (true)
                    {
                        Term chunk = await Chunk(
                            context,
                            name,
                            cursor,
                            EtsFileFormat.ObjectChunkSize,
                            validation.Md5,
                            parsed.Verify
                        );
                        if (Is(chunk, EtsAtomNames.Eof))
                        {
                            break;
                        }
                        if (chunk is not TupleTerm { Items.Count: 2 } pair)
                        {
                            throw new ErlangException(Term.Tuple(A(EtsErrorNames.CaseClause), chunk));
                        }
                        cursor = pair.Items[0];
                        var data = new List<Term>();
                        last = Nil.Value;
                        Term remaining = pair.Items[1];
                        while (remaining is Cons cell)
                        {
                            Term term = cell.Head;
                            if (validation.Md5)
                            {
                                try
                                {
                                    term = await Call(
                                        context,
                                        EtsDependencyModules.Erlang,
                                        EtsFunctionNames.BinaryToTerm,
                                        term
                                    );
                                }
                                catch (ErlangException)
                                {
                                    remaining = cell.Tail;
                                    continue;
                                }
                            }
                            if (term is Cons { Tail: Cons { Tail: Nil } } end && Is(end.Head, EtsAtomNames.EndOfTable)
                                && (validation.Md5 || cell.Tail is Nil))
                            {
                                last = term;
                                break;
                            }
                            if (validation.Md5)
                            {
                                digest = await Call(
                                    context,
                                    EtsDependencyModules.Erlang,
                                    EtsFunctionNames.Md5Update,
                                    digest,
                                    cell.Head
                                );
                            }
                            data.Add(term);
                            count++;
                            remaining = cell.Tail;
                        }
                        if (data.Count == 0)
                        {
                            break;
                        }
                        await Call(
                            context,
                            Name,
                            EtsFunctionNames.Insert,
                            table,
                            Cons.From(data)
                        );
                    }
                    if (last is Nil)
                    {
                        if (validation.Md5 || validation.ObjectCount)
                        {
                            Throw(A(EtsErrorNames.Badfile));
                        }
                        if (parsed.Verify && !Term.I(count).Equals(FieldValue(header.Fields, EtsAtomNames.Size)))
                        {
                            Throw(A(EtsErrorNames.InvalidObjectCount));
                        }
                    }
                    else
                    {
                        Term[] endInfo = Cons.Items(((Cons)((Cons)last).Tail).Head).ToArray();
                        if (validation.Md5 && !(await Call(
                            context,
                            EtsDependencyModules.Erlang,
                            EtsFunctionNames.Md5Final,
                            digest
                        )).Equals(FieldValue(endInfo, EtsAtomNames.Md5)))
                        {
                            Throw(A(EtsErrorNames.ChecksumError));
                        }
                        Term expected = validation.ObjectCount ? FieldValue(endInfo, EtsAtomNames.Count) : FieldValue(header.Fields, EtsAtomNames.Size);
                        if ((validation.ObjectCount || parsed.Verify && !validation.Md5) && !Term.I(count).Equals(expected))
                        {
                            Throw(A(EtsErrorNames.InvalidObjectCount));
                        }
                    }

                    return Term.Tuple(A(EtsAtomNames.Ok), table);
                }
                catch (ErlangException)
                {
                    await Call(
                        context,
                        Name,
                        EtsFunctionNames.Delete,
                        table
                    );
                    throw;
                }
            }
            finally
            {
                await Call(
                    context,
                    EtsDependencyModules.DiskLog,
                    EtsFunctionNames.Close,
                    name
                );
            }
        }
        catch (ErlangException error) when (error.ExceptionClass is EtsExceptionClasses.Throw or EtsExceptionClasses.Exit)
        {
            return ErrorResult(error.Reason);
        }
    }

    private static async ValueTask<Term> FileInfo(ProcessContext context, Term file)
    {
        if (file is not Atom && !IsList(file))
        {
            throw EtsSource.Error(
                context,
                EtsFunctionNames.TabfileInfo,
                [file],
                A(EtsErrorNames.FunctionClause),
                EtsSourceLines.FileInformation
            );
        }
        try
        {
            Term name = await OpenRead(context, file, false);
            EtsFileHeader header;
            try
            {
                header = await Header(context, name, false);
            }
            catch (ErlangException error) when (error.ExceptionClass == EtsExceptionClasses.Throw && Is(error.Reason, EtsErrorNames.Badfile))
            {
                await Call(
                    context,
                    EtsDependencyModules.DiskLog,
                    EtsFunctionNames.Close,
                    name
                );
                throw;
            }
            Term closed = await Call(
                context,
                EtsDependencyModules.DiskLog,
                EtsFunctionNames.Close,
                name
            );
            if (ErrorPair(closed, out Term reason))
            {
                Throw(reason);
            }
            var fields = new List<Term>();
            foreach (string tag in new[] { EtsAtomNames.Name, EtsAtomNames.Type, EtsFileFields.Protection, EtsFileFields.NamedTable, EtsFileFields.Keypos, EtsAtomNames.Size })
            {
                fields.Add(Field(header.Fields, tag));
            }
            fields.Add(Field(header.Fields, EtsFileFields.ExtendedInfo, Term.Tuple(A(EtsFileFields.ExtendedInfo), Nil.Value)));
            fields.Add(Term.Tuple(A(EtsFileFields.Version), Term.Tuple(header.Major, header.Minor)));

            return Term.Tuple(A(EtsAtomNames.Ok), Cons.From(fields));
        }
        catch (ErlangException error) when (error.ExceptionClass is EtsExceptionClasses.Throw or EtsExceptionClasses.Exit)
        {
            return ErrorResult(error.Reason);
        }
    }
}
