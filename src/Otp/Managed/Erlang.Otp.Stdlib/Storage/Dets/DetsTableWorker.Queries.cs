// Derived from OTP-29.1.1 dets scans, fixation and information, Apache-2.0.
// Copyright Ericsson AB 1996-2026. See the unchanged upstream source notices.
using System.Buffers.Binary;
using static Erlang.DetsModule;

namespace Erlang;

internal sealed partial class DetsTableWorker
{
    private Term First(uint from = 0)
    {
        for (uint slot = from; slot < file.Next; slot++)
        {
            IReadOnlyList<Term> objects = file.ReadSlot(slot);
            if (objects.Count != 0)
            {
                return file.Key(objects[0]);
            }
        }

        return End;
    }

    private Term Next(Term key)
    {
        uint slot = file.Slot(key);
        Term[] keys = file.ReadSlot(slot).Select(file.Key).Distinct().ToArray();
        int index = Array.FindIndex(keys, candidate => candidate.Equals(key));

        return Term.Tuple(Ok, index >= 0 && index + 1 < keys.Length ? keys[index + 1] : First(slot + 1));
    }

    private async ValueTask Fix(ProcessContext context, Pid caller)
    {
        if (fixedBy.Count == 0)
        {
            fixedTime = await Call(context, DetsDependencyNames.Erlang, DetsExternalFunctions.MonotonicTime);
            var offset = (Integer)await Call(context, DetsDependencyNames.Erlang, DetsExternalFunctions.TimeOffset);
            Term micros = await Call(
                context,
                DetsDependencyNames.Erlang,
                DetsExternalFunctions.ConvertTimeUnit,
                new Integer(((Integer)fixedTime).Value + offset.Value),
                Term.A(DetsProtocolNames.NativeTimeUnit),
                Term.A(DetsProtocolNames.MicrosecondTimeUnit)
            );
            var total = ((Integer)micros).Value;
            fixedTimestamp = Term.Tuple(
                new Integer(total / (DetsFixTimeLayout.MicrosecondsPerSecond * DetsFixTimeLayout.SecondsPerMegaSecond)),
                new Integer(total / DetsFixTimeLayout.MicrosecondsPerSecond % DetsFixTimeLayout.SecondsPerMegaSecond),
                new Integer(total % DetsFixTimeLayout.MicrosecondsPerSecond)
            );
            file.SetFixed(true);
        }
        if (!fixedBy.ContainsKey(caller))
        {
            context.Runtime.Link(context, caller);
        }
        fixedBy[caller] = fixedBy.GetValueOrDefault(caller) + 1;
    }

    private void Unfix(ProcessContext context, Pid caller, bool all)
    {
        if (!fixedBy.TryGetValue(caller, out int count))
        {
            return;
        }
        if (all || count == 1)
        {
            fixedBy.Remove(caller);
            context.Runtime.Unlink(context, caller);
        }
        else
        {
            fixedBy[caller] = count - 1;
        }
        if (fixedBy.Count == 0)
        {
            file.SetFixed(false);
        }
    }

    private static MatchSpecProgram Compile(Term specification)
    {
        MatchSpecCompilation compiled = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table);

        return compiled.Program ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    private bool Compatible(Term binary)
    {
        if (binary is not BitString { IsBinary: true } bytes)
        {
            return false;
        }
        try
        {
            Term term = runtime.ImportExternalTerm(bytes.ByteSpan).Term;

            return term is TupleTerm { Items.Count: DetsCopyParameterFields.Count } parameters && parameters.Items[0] is Atom { Name: DetsNames.HashParameters }
                && parameters.Items[DetsCopyParameterFields.Type].Equals(file.TypeTerm)
                && parameters.Items[DetsCopyParameterFields.KeyPosition].Equals(Term.I(file.KeyPosition)) && parameters.Items[DetsCopyParameterFields.HashMethod].Equals(Term.I(file.HashMethod))
                && parameters.Items[DetsCopyParameterFields.ChunkVersion].Equals(Term.I(DetsNames.BchunkVersion));
        }
        catch (ErlangException)
        {
            return false;
        }
    }

    private async ValueTask<Term> Info(ProcessContext context, Term? tag)
    {
        if (tag is null)
        {
            return Term.List(
                Term.Tuple(Term.A(DetsInfoNames.Type), file.TypeTerm),
                Term.Tuple(Term.A(DetsInfoNames.Keypos), Term.I(file.KeyPosition)),
                Term.Tuple(Term.A(DetsInfoNames.Size), Term.I(file.ObjectCount)),
                Term.Tuple(Term.A(DetsInfoNames.FileSize), Term.I(file.Length)),
                Term.Tuple(Term.A(DetsInfoNames.Filename), options.File)
            );
        }
        if (tag is not Atom atom)
        {
            return Undefined;
        }
        switch (atom.Name)
        {
            case DetsInfoNames.Access: return Term.A(file.ReadOnly ? DetsNames.Read : DetsNames.ReadWrite);
            case DetsInfoNames.AutoSave: return options.AutoSave;
            case DetsInfoNames.DelayedWrite: return options.DelayedWrite;
            case DetsInfoNames.Filename: return options.File;
            case DetsInfoNames.FileSize: case DetsInfoNames.Memory: return Term.I(file.Length);
            case DetsInfoNames.Fixed: return Bool(file.Fixed);
            case DetsInfoNames.Hash: return Term.A(file.HashMethod == 1 ? DetsProtocolNames.Phash2 : DetsProtocolNames.Phash);
            case DetsInfoNames.Keypos: return Term.I(file.KeyPosition);
            case DetsInfoNames.Size: case DetsInfoNames.NoObjects: return Term.I(file.ObjectCount);
            case DetsInfoNames.NoKeys: return Term.I(file.KeyCount);
            case DetsInfoNames.NoSlots: return Term.Tuple(Term.I(file.MinimumSlots), Term.I(file.Next), Term.I(file.MaximumSlots));
            case DetsInfoNames.Pid: return context.Self;
            case DetsInfoNames.RamFile: return Bool(options.RamFile);
            case DetsInfoNames.SafeFixed:
            case DetsInfoNames.SafeFixedMonotonicTime:
                return file.Fixed ? Term.Tuple(
                    atom.Name == DetsInfoNames.SafeFixed ? fixedTimestamp : fixedTime,
                    Cons.From(fixedBy.Reverse().Select(pair => (Term)Term.Tuple(pair.Key, Term.I(pair.Value))))
                ) : Bool(false);
            case DetsInfoNames.Type: return file.TypeTerm;
            case DetsInfoNames.Version: return Term.I(DetsV9Layout.Version);
            case DetsInfoNames.BchunkFormat:
                Term parameters = file.Parameters(filename);

                return parameters.Equals(Undefined) ? Undefined : BitString.FromOwnedBytes(ExternalTermFormat.Encode(parameters));
            default:
                await ValueTask.CompletedTask;

                return Undefined;
        }
    }

    private static TupleTerm Continuation(
        string what,
        Term number,
        Term bin,
        Term allocation,
        Term table,
        Term process,
        Term program
    )
        => Term.Tuple(
            Term.A(DetsNames.Continuation),
            Term.A(what),
            number,
            bin,
            allocation,
            table,
            process,
            program
        );

    private byte[] Map()
    {
        var blocks = file.Blocks();
        var bytes = new byte[checked(blocks.Count * DetsNames.BlockMapEntryBytes)];
        for (int index = 0; index < blocks.Count; index++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(index * DetsNames.BlockMapEntryBytes), checked((uint)blocks[index].Address));
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(index * DetsNames.BlockMapEntryBytes + DetsV9Layout.WordBytes), blocks[index].Size);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(index * DetsNames.BlockMapEntryBytes + DetsV9Layout.ObjectHeaderBytes), blocks[index].Slot);
        }

        return bytes;
    }

    private async ValueTask<Term> Scan(ProcessContext context, Pid caller, Term[] args)
    {
        TupleTerm continuation;
        bool safe;
        if (args.Length == 5)
        {
            if (args[0] is not Atom what || args[1] is not Atom { Name: DetsNames.Default }
                && (args[1] is not Integer number || number.Value < 0))
            {
                return Badarg;
            }
            safe = args[4] is Atom { Name: DetsNames.True };
            var keys = ConstantKeys(args[3]);
            if (keys.Count != 0)
            {
                Term unique = await Call(
                    context,
                    DetsDependencyNames.Lists,
                    DetsExternalFunctions.Usort,
                    Cons.From(keys)
                );
                Term objects = Cons.From(Cons.Items(unique).SelectMany(file.Lookup));
                var program = (TupleTerm)args[2];
                Term selected = await Call(
                    context,
                    DetsDependencyNames.Ets,
                    DetsExternalFunctions.MatchSpecRun,
                    objects,
                    program.Items[1]
                );

                return Term.Tuple(
                    selected,
                    Continuation(
                        what.Name,
                        Term.A(DetsNames.Default),
                        Term.A(DetsProtocolNames.Eof),
                        BitString.FromOwnedBytes(Array.Empty<byte>()),
                        table,
                        context.Self,
                        Undefined
                    )
                );
            }
            byte[] map = Map();
            if (safe)
            {
                await Fix(context, caller);
            }
            continuation = Continuation(
                what.Name,
                args[1],
                map.Length == 0 ? Term.A(DetsProtocolNames.Eof) : BitString.FromOwnedBytes(Array.Empty<byte>()),
                Term.Tuple(Term.I(0), Term.I(map.Length), BitString.FromOwnedBytes(map)),
                table,
                context.Self,
                args[2]
            );
        }
        else
        {
            if (args.Length != 2 || args[0] is not TupleTerm { Items.Count: DetsContinuationFields.Count } state
                || state.Items[0] is not Atom { Name: DetsNames.Continuation } || !state.Items[DetsContinuationFields.Process].Equals(context.Self))
            {
                return Badarg;
            }
            continuation = state;
            safe = args[1] is Atom { Name: DetsNames.True };
        }
        if (continuation.Items[DetsContinuationFields.Binary] is Atom { Name: DetsProtocolNames.Eof })
        {
            if (safe)
            {
                Unfix(context, caller, all: false);
            }

            return End;
        }
        if (continuation.Items[DetsContinuationFields.Allocation] is not TupleTerm { Items.Count: 3 } allocation || allocation.Items[0] is not Integer offset
            || allocation.Items[2] is not BitString { IsBinary: true } binary)
        {
            return Badarg;
        }
        int cursor = checked((int)offset.Value);
        byte[] mapBytes = binary.ByteSpan.ToArray();
        var results = new List<Term>();
        int rawObjects = 0;
        long bytesRead = 0;
        while (cursor < mapBytes.Length)
        {
            uint address = BinaryPrimitives.ReadUInt32BigEndian(mapBytes.AsSpan(cursor));
            // A fixed table can replace an unread collection in the same block.
            // The allocation map preserves its address, not an immutable size.
            byte[]? bytes = file.ReadCollection(address);
            if (bytes is null)
            {
                cursor += DetsNames.BlockMapEntryBytes;

                continue;
            }
            var objects = DetsV9Recovery.DecodeCollection(
                bytes,
                file.Type,
                file.KeyPosition,
                runtime: context.Runtime
            );
            Term program = continuation.Items[DetsContinuationFields.MatchProgram];
            Term selected = program is Atom { Name: DetsNames.True } ? Cons.From(objects)
                : program is TupleTerm { Items.Count: 2 } compiled ? await Call(
                    context,
                    DetsDependencyNames.Ets,
                    DetsExternalFunctions.MatchSpecRun,
                    Cons.From(objects),
                    compiled.Items[1]
                )
                : throw new ErlangException(ErlangErrorReasons.BadArgument);
            results.AddRange(Cons.Items(selected));
            rawObjects += objects.Count;
            bytesRead += bytes.Length;
            cursor += DetsNames.BlockMapEntryBytes;
            bool enough = continuation.Items[DetsContinuationFields.ObjectLimit] is Integer limit ? rawObjects >= limit.Value : bytesRead >= DetsNames.ChunkBytes;
            if (results.Count != 0 && enough)
            {
                break;
            }
        }
        if (results.Count == 0)
        {
            if (safe)
            {
                Unfix(context, caller, all: false);
            }

            return End;
        }
        Term next = Continuation(
            ((Atom)continuation.Items[DetsContinuationFields.Kind]).Name,
            continuation.Items[DetsContinuationFields.ObjectLimit],
            cursor >= mapBytes.Length ? Term.A(DetsProtocolNames.Eof) : BitString.FromOwnedBytes(Array.Empty<byte>()),
            Term.Tuple(Term.I(cursor), Term.I(mapBytes.Length), binary),
            table,
            context.Self,
            continuation.Items[DetsContinuationFields.MatchProgram]
        );

        return Term.Tuple(Cons.From(results), next);
    }

    private List<Term> ConstantKeys(Term specification)
    {
        var keys = new List<Term>();
        foreach (Term clause in Cons.Items(specification))
        {
            if (clause is not TupleTerm { Items.Count: 3 } item || item.Items[0] is not TupleTerm head)
            {
                return new List<Term>();
            }
            if (head.Items.Count < file.KeyPosition)
            {
                continue;
            }
            Term key = head.Items[file.KeyPosition - 1];
            if (ContainsVariable(key))
            {
                return new List<Term>();
            }
            keys.Add(key);
        }

        return keys;
    }

    private static bool ContainsVariable(Term value) => value switch
    {
        Atom atom => atom.Name == DetsMatchNames.Wildcard || atom.Name.StartsWith('$')
            && System.Numerics.BigInteger.TryParse(atom.Name.AsSpan(1), out _),
        TupleTerm tuple => tuple.Items.Any(ContainsVariable),
        Cons list => ContainsVariable(list.Head) || ContainsVariable(list.Tail),
        _ => false
    };

    private Term Bchunk(ProcessContext context, string operation, Term state)
    {
        if (operation == DetsRequestNames.BchunkInit)
        {
            Term parameters = file.Parameters(filename);
            if (parameters.Equals(Undefined))
            {
                return Error(Term.A("old_version"));
            }
            byte[] map = Map();
            var continuation = Continuation(
                DetsNames.Bchunk,
                Term.A(DetsNames.Default),
                map.Length == 0 ? Term.A(DetsProtocolNames.Eof) : BitString.FromOwnedBytes(Array.Empty<byte>()),
                Term.Tuple(Term.I(0), Term.I(map.Length), BitString.FromOwnedBytes(map)),
                table,
                context.Self,
                Undefined
            );

            return Term.Tuple(continuation, Term.List(BitString.FromOwnedBytes(ExternalTermFormat.Encode(parameters))));
        }
        if (state is not TupleTerm { Items.Count: DetsContinuationFields.Count } previous || previous.Items[DetsContinuationFields.Kind] is not Atom { Name: DetsNames.Bchunk }
            || !previous.Items[DetsContinuationFields.Process].Equals(context.Self))
        {
            return Badarg;
        }
        if (previous.Items[DetsContinuationFields.Binary] is Atom { Name: DetsProtocolNames.Eof })
        {
            return End;
        }
        var allocation = (TupleTerm)previous.Items[DetsContinuationFields.Allocation];
        int offset = checked((int)((Integer)allocation.Items[0]).Value);
        var binary = (BitString)allocation.Items[2];
        byte[] mapBytes = binary.ByteSpan.ToArray();
        var chunks = new List<Term>();
        long totalBytes = 0;
        while (offset < mapBytes.Length && (chunks.Count == 0 || totalBytes < DetsNames.ChunkBytes * 4))
        {
            uint address = BinaryPrimitives.ReadUInt32BigEndian(mapBytes.AsSpan(offset));
            uint size = BinaryPrimitives.ReadUInt32BigEndian(mapBytes.AsSpan(offset + DetsV9Layout.WordBytes));
            uint slot = BinaryPrimitives.ReadUInt32BigEndian(mapBytes.AsSpan(offset + DetsV9Layout.ObjectHeaderBytes));
            int exponent = DetsBuddyAllocator.Exponent(size);
            byte[] bytes = file.ReadBlock(address, checked((int)(1L << exponent)), checked((int)size));
            chunks.Add(Term.Tuple(Term.I(exponent + 1), Term.I(slot), BitString.FromOwnedBytes(bytes)));
            totalBytes += bytes.Length;
            offset += DetsNames.BlockMapEntryBytes;
        }

        return Term.Tuple(
            Continuation(
                DetsNames.Bchunk,
                previous.Items[DetsContinuationFields.ObjectLimit],
                offset >= mapBytes.Length ? Term.A(DetsProtocolNames.Eof) : previous.Items[DetsContinuationFields.Binary],
                Term.Tuple(Term.I(offset), Term.I(mapBytes.Length), binary),
                table,
                context.Self,
                Undefined
            ),
            Cons.From(chunks)
        );
    }
}
