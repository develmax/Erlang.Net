// Derived from OTP-29.1.1 dets debug/file APIs, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Original source and notices remain retained.
using System.Buffers.Binary;
using static Erlang.DetsNames;

namespace Erlang;

public static partial class DetsModule
{
    private static async ValueTask<Term> FileOperation(ProcessContext context, string function, Term fileTerm)
    {
        try
        {
            string filename = DetsTableWorker.Filename(fileTerm);
            using var stream = new FileStream(
                filename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );
            if (stream.Length < DetsV9Layout.VersionOffset + DetsV9Layout.WordBytes)
            {
                return function == DetsFunctions.IsDetsFile ? Bool(false) : Error(Term.Tuple(Term.A(DetsFailureNames.TooShort), fileTerm));
            }
            var versionBytes = new byte[DetsV9Layout.WordBytes];
            stream.Position = DetsV9Layout.VersionOffset;
            stream.ReadExactly(versionBytes);
            uint version = BinaryPrimitives.ReadUInt32BigEndian(versionBytes);
            if (version <= 8)
            {
                return Error(Term.Tuple(Term.A(DetsFailureNames.Format8NoLongerSupported), fileTerm));
            }
            if (version != DetsV9Layout.Version)
            {
                return function == DetsFunctions.IsDetsFile ? Bool(false) : Error(Term.Tuple(Term.A(DetsFileReasons.NotDets), fileTerm));
            }
            if (stream.Length < DetsV9Layout.MetadataBytes)
            {
                return function == DetsFunctions.IsDetsFile ? Bool(false) : Error(Term.Tuple(Term.A(DetsFailureNames.TooShort), fileTerm));
            }
            DetsV9Header header = DetsV9Header.Read(stream);
            if (function == DetsFunctions.IsDetsFile)
            {
                return Bool(header.Cookie == DetsV9Layout.Cookie);
            }
            if (function == DetsFunctions.FileInfo)
            {
                if (header.ClosedProperly == 0)
                {
                    return Error(Term.A(DetsFileReasons.NotClosed));
                }
                if (header.Cookie != DetsV9Layout.Cookie)
                {
                    return Error(Term.A(DetsFileReasons.NotDets));
                }

                return Term.Tuple(
                    Ok,
                    Term.List(
                        Term.Tuple(Term.A(DetsHeaderNames.ClosedProperly), Term.I(header.ClosedProperly)),
                        Term.Tuple(Term.A(DetsInfoNames.Keypos), Term.I(header.KeyPosition)),
                        Term.Tuple(Term.A(DetsHeaderNames.M), Term.I(header.M)),
                        Term.Tuple(Term.A(DetsHeaderNames.N), Term.I(header.N)),
                        Term.Tuple(Term.A(DetsHeaderNames.Next), Term.I(header.Next)),
                        Term.Tuple(Term.A(DetsInfoNames.NoObjects), Term.I(header.ObjectCount)),
                        Term.Tuple(Term.A(DetsInfoNames.NoKeys), Term.I(header.KeyCount)),
                        Term.Tuple(
                            Term.A(DetsInfoNames.Type),
                            Term.A(header.Type switch
                            {
                                DetsFileType.Set => Set,
                                DetsFileType.Bag => Bag,
                                DetsFileType.DuplicateBag => DuplicateBag,
                                _ => DetsFailureNames.InvalidTypeName
                            })
                        ),
                        Term.Tuple(Term.A(DetsInfoNames.Version), Term.I(header.Version))
                    )
                );
            }
            if (function == DetsFunctions.Fsck)
            {
                try
                {
                    header.Validate();
                }
                catch (DetsFileException error) when (error.Reason == DetsFileReasons.NotClosed) { }
                using (var output = new FileStream(
                    filename + DetsFileNames.TemporarySuffix,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None
                ))
                {
                    DetsV9Recovery.Rewrite(
                        stream,
                        output,
                        header,
                        DetsOpenOptions.Parse(fileTerm, Nil.Value),
                        context.Runtime
                    );
                    output.Flush(flushToDisk: true);
                }
                stream.Dispose();
                System.IO.File.Move(filename + DetsFileNames.TemporarySuffix, filename, overwrite: true);

                return Ok;
            }
            var reader = new DetsV9Reader(stream, runtime: context.Runtime);
            var free = reader.ReadFreeBlocks().OrderBy(block => block.Address).ToArray();
            var ranges = new List<(long Start, long End)>();
            foreach (var block in free)
            {
                long end = block.Address + (1L << block.Exponent);
                if (ranges.Count != 0 && ranges[^1].End == block.Address)
                {
                    ranges[^1] = (ranges[^1].Start, end);
                }
                else
                {
                    ranges.Add((block.Address, end));
                }
            }
            _ = await Call(
                context,
                DetsDependencyNames.Io,
                DetsExternalFunctions.Format,
                Term.String(DetsText.FreeListStart),
                Nil.Value
            );
            _ = await Call(
                context,
                DetsDependencyNames.Io,
                DetsExternalFunctions.Format,
                Term.String(DetsText.PrintedTerm),
                Term.List(Cons.From(ranges.AsEnumerable().Reverse()
                .Select(range => (Term)Term.Tuple(Term.I(range.Start), Term.I(range.End)))))
            );
            _ = await Call(
                context,
                DetsDependencyNames.Io,
                DetsExternalFunctions.Format,
                Term.String(DetsText.FreeListEnd),
                Nil.Value
            );
            for (uint slot = 0; slot < header.Next; slot++)
            {
                long position = reader.SlotPosition(slot);
                if (slot % DetsV9Layout.SlotsPerSegment == 0)
                {
                    _ = await Call(
                        context,
                        DetsDependencyNames.Io,
                        DetsExternalFunctions.Format,
                        Term.String(DetsText.Segment),
                        Term.List(Term.I(position), Term.I(slot / DetsV9Layout.SlotsPerSegment))
                    );
                }
                var pointer = new byte[DetsV9Layout.SlotBytes];
                stream.Position = position;
                stream.ReadExactly(pointer);
                uint size = BinaryPrimitives.ReadUInt32BigEndian(pointer);
                uint address = BinaryPrimitives.ReadUInt32BigEndian(pointer.AsSpan(DetsV9Layout.WordBytes));
                if (address == 0)
                {
                    continue;
                }
                _ = await Call(
                    context,
                    DetsDependencyNames.Io,
                    DetsExternalFunctions.Format,
                    Term.String(DetsText.Slot),
                    Term.List(
                        Term.I(position),
                        Term.I(slot),
                        Term.I(address),
                        Term.I(size),
                        Cons.From(reader.ReadSlot(slot))
                    )
                );
            }

            return Ok;
        }
        catch (DetsFileException error)
        {
            return function == DetsFunctions.IsDetsFile && error.Reason == DetsFileReasons.NotDets ? Bool(false)
                : Error(Term.A(error.Reason));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            string reason = error is FileNotFoundException or DirectoryNotFoundException ? DetsFailureNames.NoEntry
                : error is UnauthorizedAccessException ? DetsFailureNames.AccessDenied : error is ArgumentException ? DetsFailureNames.InvalidArgument : DetsFailureNames.IoError;

            return Error(Term.Tuple(Term.A(DetsFailureNames.FileError), fileTerm, Term.A(reason)));
        }
    }
}
