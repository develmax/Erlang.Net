using System.Runtime.InteropServices;

namespace Erlang;

public sealed partial class RuntimeRegexEngine411
{
    internal ProcessRuntime Owner413 => runtime;

    private uint PatternInfo413(ReCode411 code, uint item)
    {
        using var value = new ReBuffer411(new byte[sizeof(uint)]);
        if (api.Info(code.Pointer, item, value.Pointer) != 0)
            throw new InvalidOperationException(ReExtendedNames413.NativeMetadataFailure);

        return unchecked((uint)Marshal.ReadInt32(value.Pointer));
    }

    private Term Inspect413(ProcessContext context, Term expression, Term item)
    {
        Enter(context);
        ReCode411? code = null;
        try
        {
            code = Authenticate(expression, canonicalMetadata: false);
            if (!item.Equals(Term.A(ReExtendedNames413.NameList)))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            uint options = PatternInfo413(code, ReExtendedNames413.AllOptions);
            uint count = PatternInfo413(code, ReExtendedNames413.NameCount);
            var result = new List<Term>();
            if (count != 0)
            {
                int size = checked((int)PatternInfo413(code, ReExtendedNames413.NameEntrySize));
                using var pointer = new ReBuffer411(new byte[IntPtr.Size]);
                if (size <= ReExtendedNames413.NameNumberBytes || api.Info(code.Pointer, ReExtendedNames413.NameTable, pointer.Pointer) != 0)
                    throw new InvalidOperationException(ReExtendedNames413.NativeMetadataFailure);
                IntPtr table = Marshal.ReadIntPtr(pointer.Pointer);
                if (table == IntPtr.Zero)
                    throw new InvalidOperationException(ReExtendedNames413.NativeMetadataFailure);
                byte[]? previous = null;
                for (int index = 0; index < count; index++)
                {
                    Check(context);
                    var entry = new byte[size - ReExtendedNames413.NameNumberBytes];
                    Marshal.Copy(
                        IntPtr.Add(table, checked(index * size + ReExtendedNames413.NameNumberBytes)),
                        entry,
                        0,
                        entry.Length
                    );
                    int end = Array.IndexOf(entry, (byte)0);
                    if (end < 0)
                        throw new InvalidOperationException(ReExtendedNames413.NativeMetadataFailure);
                    var name = entry.AsSpan(0, end).ToArray();
                    if (previous is null || (options & ReExtendedNames413.DuplicateNames) == 0 || !name.AsSpan().SequenceEqual(previous))
                        result.Add(new BitString(name));
                    previous = name;
                }
            }

            return Term.Tuple(Term.A(ReExtendedNames413.NameList), Term.List(result.ToArray()));
        }
        finally
        {
            if (code is not null) EndRead(code);
            Exit(context);
        }
    }

    private async ValueTask<Term> InternalRun413Async(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Enter(context);
        try
        {
            bool first = arguments[3] switch
            {
                Atom { Name: ErlangBooleanAtoms.True } => true,
                Atom { Name: ErlangBooleanAtoms.False } => false,
                _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
            };

            return await RunCore413Async(
                context,
                arguments[0],
                arguments[1],
                arguments[2],
                first,
                ownAdmission: false
            ).ConfigureAwait(false);
        }
        finally
        {
            Exit(context);
        }
    }
}
