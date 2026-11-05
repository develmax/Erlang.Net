// Derived from OTP-29.1.1 dets file opening/repair/initialization, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Retained original sources contain full notices.
using System.Numerics;
using System.Text;
using static Erlang.DetsModule;

namespace Erlang;

internal sealed partial class DetsTableWorker
{
    internal static string Filename(Term term)
    {
        var text = new StringBuilder();
        var pendingNames = new Stack<Term>();
        pendingNames.Push(term is Atom atom ? Term.String(atom.Name) : term);
        while (pendingNames.Count != 0)
        {
            Term current = pendingNames.Pop();
            switch (current)
            {
                case Nil: break;
                case Cons cell: pendingNames.Push(cell.Tail); pendingNames.Push(cell.Head); break;
                case Integer character when character.Value > 0 && character.Value <= 0x10ffff && Rune.IsValid((int)character.Value):
                    text.Append(new Rune((int)character.Value));
                    break;
                default: throw new DetsFileException(DetsFailureNames.InvalidArgument);
            }
        }
        if (text.Length == 0)
        {
            throw new DetsFileException(DetsFailureNames.NoEntry);
        }

        return text.ToString();
    }

    private void OpenStorage(ProcessContext context, bool requireExisting)
    {
        filename = Filename(options.File);
        bool exists = File.Exists(filename);
        if (!exists && (requireExisting || options.ReadOnly))
        {
            throw new FileNotFoundException(filename);
        }
        stream = options.RamFile ? new MemoryStream() : new FileStream(
            filename,
            exists ? FileMode.Open : FileMode.CreateNew,
            options.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
            FileShare.ReadWrite,
            bufferSize: 1
        );
        if (options.RamFile && exists)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            stream.Write(bytes);
        }
        if (!exists)
        {
            CreateEmpty(
                options.Type,
                checked((int)options.KeyPosition),
                options.MinimumSlots,
                options.MaximumSlots
            );
        }
        else
        {
            if (stream.Length >= DetsV9Layout.VersionOffset + DetsV9Layout.WordBytes)
            {
                var bytes = new byte[DetsV9Layout.WordBytes];
                stream.Position = DetsV9Layout.VersionOffset;
                stream.ReadExactly(bytes);
                if (System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes) <= 8)
                {
                    throw new DetsFileException(DetsFailureNames.Format8NoLongerSupported);
                }
            }
            DetsV9Header header = DetsV9Header.Read(stream);
            if (header.Version <= 8)
            {
                throw new DetsFileException(DetsFailureNames.Format8NoLongerSupported);
            }
            if (requireExisting)
            {
                options = DetsOpenOptions.Parse(
                    options.File,
                    Term.List(
                        Term.Tuple(
                            Term.A(DetsNames.Type),
                            Term.A(header.Type switch
                            {
                                DetsFileType.Set => DetsNames.Set,
                                DetsFileType.Bag => DetsNames.Bag,
                                _ => DetsNames.DuplicateBag
                            })
                        ),
                        Term.Tuple(Term.A(DetsNames.KeyPosition), Term.I(header.KeyPosition))
                    )
                );
            }
            bool force = options.Repair is Atom { Name: DetsNames.Force };
            bool repair = force;
            bool compact = false;
            try
            {
                header.Validate();
                _ = new DetsV9Reader(stream).ReadFreeBlocks();
                compact = force && (header.ObjectCount == 0 || header.CollectionCounters.Any(value => value != 0))
                    && (options.MinimumSlots is null || options.MinimumSlots == header.MinimumSlots)
                    && (options.MaximumSlots is null || options.MaximumSlots == header.MaximumSlots);
            }
            catch (DetsFileException error) when (error.Reason is DetsFileReasons.NotClosed or DetsFileReasons.BadFreelists)
            {
                if (options.ReadOnly)
                {
                    throw;
                }
                if (options.Repair is Atom { Name: DetsNames.False })
                {
                    throw new DetsFileException(DetsFailureNames.NeedsRepair);
                }
                repair = true;
            }
            if (header.Type != options.Type)
            {
                throw new DetsFileException(DetsFailureNames.TypeMismatch);
            }
            if (header.KeyPosition != options.KeyPosition)
            {
                throw new DetsFileException(DetsFailureNames.KeyPositionMismatch);
            }
            if (repair)
            {
                if (options.ReadOnly)
                {
                    throw new DetsFileException(DetsNames.AccessMode);
                }
                if (!options.RamFile)
                {
                    using (var output = new FileStream(
                        filename + DetsFileNames.TemporarySuffix,
                        FileMode.Create,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        bufferSize: 1
                    ))
                    {
                        if (compact)
                        {
                            DetsV9Recovery.Compact(
                                stream,
                                output,
                                header,
                                context.Runtime
                            );
                        }
                        else
                        {
                            DetsV9Recovery.Rewrite(
                                stream,
                                output,
                                header,
                                options,
                                context.Runtime
                            );
                        }
                        output.Flush(flushToDisk: true);
                    }
                    stream.Dispose();
                    File.Move(filename + DetsFileNames.TemporarySuffix, filename, overwrite: true);
                    stream = new FileStream(
                        filename,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite,
                        bufferSize: 1
                    );
                }
                else
                {
                    using var repaired = new MemoryStream();
                    if (compact)
                    {
                        DetsV9Recovery.Compact(
                            stream,
                            repaired,
                            header,
                            context.Runtime
                        );
                    }
                    else
                    {
                        DetsV9Recovery.Rewrite(
                            stream,
                            repaired,
                            header,
                            options,
                            context.Runtime
                        );
                    }
                    repaired.Position = 0;
                    stream.SetLength(0);
                    repaired.CopyTo(stream);
                }
                stream.Flush();
            }
        }
        file = new DetsV9File(stream, options.ReadOnly, runtime: context.Runtime);
        if (!exists)
        {
            file.Touch();
        }
    }

    private void CreateEmpty(
        DetsFileType type,
        int keyPosition,
        BigInteger? minimum,
        BigInteger? maximum
    )
    {
        int max = (int)BigInteger.Min(maximum ?? DetsV9Layout.MaximumSlots, DetsV9Layout.MaximumSlots);
        int min = (int)BigInteger.Min(minimum ?? DetsV9Layout.MinimumSlots, max);
        DetsV9Writer.Write(
            stream!,
            Array.Empty<Term>(),
            type,
            keyPosition,
            ErlangPortableHash.Phash2,
            Math.Max(DetsV9Layout.MinimumSlots, min),
            Math.Max(DetsV9Layout.MinimumSlots, max)
        );
    }

    private void Reset(ProcessContext? context = null, BigInteger? minimum = null, TupleTerm? parameters = null)
    {
        if (file.ReadOnly)
        {
            throw new DetsFileException(DetsNames.AccessMode);
        }
        var ownerRuntime = context?.Runtime ?? runtime;
        if (parameters is null)
        {
            CreateEmpty(
                file.Type,
                file.KeyPosition,
                minimum ?? file.MinimumSlots,
                file.MaximumSlots
            );
        }
        else
        {
            int next = checked((int)((Integer)parameters.Items[DetsCopyParameterFields.Next]).Value);
            int max = checked((int)((Integer)parameters.Items[DetsCopyParameterFields.Maximum]).Value);
            CreateEmpty(
                file.Type,
                file.KeyPosition,
                next,
                Math.Max(next, max)
            );
            var header = DetsV9Header.Read(stream!);
            byte[] replacement = DetsV9Writer.Header(
                file.Type,
                file.KeyPosition,
                next,
                checked((int)((Integer)parameters.Items[DetsCopyParameterFields.Minimum]).Value),
                max,
                0,
                0,
                header.Freelist,
                header.Base,
                new uint[header.CollectionCounters.Count],
                true,
                checked((uint)((Integer)parameters.Items[DetsCopyParameterFields.Split]).Value),
                checked((uint)((Integer)parameters.Items[DetsCopyParameterFields.Modulus]).Value),
                checked((uint)((Integer)parameters.Items[DetsCopyParameterFields.HashMethod]).Value)
            );
            stream!.Position = 0;
            stream.Write(replacement);
        }
        file = new DetsV9File(stream!, false, runtime: ownerRuntime);
        file.Touch();
    }

    private Term FileFailure(Exception error)
    {
        Term path = options?.File ?? Undefined;
        if (error is DetsFileException dets)
        {
            if (dets.Reason == DetsFileReasons.BadObject)
            {
                return Error(Term.Tuple(Term.Tuple(Term.A(dets.Reason), Term.A(dets.Location ?? DetsProtocolNames.ReadBuckets)), path));
            }

            return dets.Reason is DetsFailureNames.InvalidArgument or DetsFailureNames.NoEntry ? Error(Term.Tuple(Term.A(DetsFailureNames.FileError), path, Term.A(dets.Reason)))
                : Error(Term.Tuple(Term.A(dets.Reason), path));
        }
        if (error is ErlangException erlang)
        {
            return erlang.Reason.Equals(Badarg) ? Badarg : Error(erlang.Reason);
        }
        string reason = error switch
        {
            FileNotFoundException or DirectoryNotFoundException => DetsFailureNames.NoEntry,
            UnauthorizedAccessException => DetsFailureNames.AccessDenied,
            ArgumentException => DetsFailureNames.InvalidArgument,
            IOException io when (io.HResult & 0xffff) is 112 or 39 => DetsFailureNames.NoSpace,
            _ => DetsFailureNames.IoError
        };

        return Error(Term.Tuple(Term.A(DetsFailureNames.FileError), path, Term.A(reason)));
    }

    private async ValueTask<Term> InitializeTable(ProcessContext context, Term[] args)
    {
        if (args[0] is not FunctionTerm current)
        {
            return Badarg;
        }
        if (file.ReadOnly || file.Fixed)
        {
            try
            {
                _ = await current.Invoke(context, [Term.A(DetsNames.Close)]);
            }
            catch (ErlangException) { }

            return Error(Term.Tuple(Term.A(file.ReadOnly ? DetsNames.AccessMode : DetsFailureNames.FixedTable), file.ReadOnly ? options.File : table));
        }
        string format = ((Atom)args[1]).Name;
        BigInteger? minimum = args[2] is Integer count ? count.Value : null;
        if (format == DetsProtocolNames.Term && minimum > file.MaximumSlots)
        {
            return Badarg;
        }
        Reset(context, minimum);
        bool headerRead = format != DetsNames.Bchunk;
        TupleTerm? importedParameters = null;
        try
        {
            while (true)
            {
                Term value;
                try
                {
                    value = await current.Invoke(context, [Term.A(DetsNames.Read)]);
                }
                catch (ErlangException error)
                {
                    Term caught = error.ExceptionClass == ErlangExceptionClasses.Throw ? error.Reason
                        : Term.Tuple(
                            Term.A(DetsProtocolNames.ExitSignal),
                            error.ExceptionClass == ErlangExceptionClasses.Error
                            ? Term.Tuple(error.Reason, error.StackTraceTerm) : error.Reason
                        );

                    return Term.Tuple(Term.A(DetsProtocolNames.Thrown), caught);
                }
                if (value is Atom { Name: DetsNames.EndOfInput })
                {
                    if (!headerRead)
                    {
                        return Error(Term.Tuple(Term.A(DetsFailureNames.InitializeFunction), value));
                    }
                    if (importedParameters is not null && (!importedParameters.Items[DetsCopyParameterFields.ObjectCount].Equals(Term.I(file.ObjectCount))
                        || !importedParameters.Items[DetsCopyParameterFields.KeyCount].Equals(Term.I(file.KeyCount))
                        || !importedParameters.Items[DetsCopyParameterFields.Collections].Equals(((TupleTerm)file.Parameters(filename)).Items[DetsCopyParameterFields.Collections])))
                    {
                        return Error(Term.A(DetsFailureNames.InvalidObjectsList));
                    }

                    return Ok;
                }
                if (value is not TupleTerm { Items.Count: 2 } pair || pair.Items[1] is not FunctionTerm next)
                {
                    return Error(Term.Tuple(Term.A(DetsFailureNames.InitializeFunction), value));
                }
                current = next;
                Term[] values;
                try
                {
                    values = Cons.Items(pair.Items[0]).ToArray();
                }
                catch (ErlangException)
                {
                    return Error(Term.Tuple(Term.A(DetsFailureNames.InitializeFunction), value));
                }
                if (!headerRead)
                {
                    if (values.Length == 0)
                    {
                        continue;
                    }
                    if (!Compatible(values[0]))
                    {
                        return Error(Term.Tuple(Term.A(DetsFailureNames.InitializeFunction), values[0]));
                    }
                    var bytes = (BitString)values[0];
                    var parameters = (TupleTerm)context.Runtime.ImportExternalTerm(bytes.ByteSpan).Term;
                    importedParameters = parameters;
                    Reset(context, parameters: parameters);
                    values = values.Skip(1).ToArray();
                    headerRead = true;
                }
                try
                {
                    if (format == DetsNames.Bchunk)
                    {
                        foreach (Term item in values)
                        {
                            if (item is not TupleTerm { Items.Count: 3 } chunk || chunk.Items[0] is not Integer exponent
                                || exponent.Value < DetsV9Layout.MinimumCollectionExponent + 1
                                || exponent.Value > DetsV9Layout.MaximumCollectionExponent + 1
                                || chunk.Items[1] is not Integer slot
                                || chunk.Items[2] is not BitString { IsBinary: true } bytes)
                            {
                                return Error(Term.A(DetsFailureNames.InvalidObjectsList));
                            }
                            if (bytes.ByteSpan.Length != 1L << ((int)exponent.Value - 1))
                            {
                                return Error(Term.A(DetsFailureNames.InvalidObjectsList));
                            }
                            file.ImportChunk(checked((uint)slot.Value), bytes.ByteSpan.ToArray());
                        }
                    }
                    else
                    {
                        file.Insert(values, onlyNew: false);
                    }
                }
                catch (Exception error) when (error is ErlangException or DetsFileException or OverflowException)
                {
                    return Error(Term.A(DetsFailureNames.InvalidObjectsList));
                }
            }
        }
        finally
        {
            try
            {
                _ = await current.Invoke(context, [Term.A(DetsNames.Close)]);
            }
            catch (ErlangException) { }
        }
    }
}
