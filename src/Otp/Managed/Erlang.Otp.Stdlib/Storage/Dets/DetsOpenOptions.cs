// Derived from Erlang/OTP 29.1.1 dets.erl defaults/repl (Apache-2.0).
// Copyright Ericsson AB 1996-2026. Upstream notices are retained in Otp/dets.erl.
using System.Numerics;

namespace Erlang;

/// <summary>Source-level options; keep arbitrary precision until the file engine admits them.</summary>
internal sealed class DetsOpenOptions
{
    private DetsOpenOptions(Term table)
    {
        File = ToList(table);
    }

    internal Term File { get; private set; }
    internal DetsFileType Type { get; private set; } = DetsFileType.Set;
    internal BigInteger KeyPosition { get; private set; } = BigInteger.One;
    internal Term Repair { get; private set; } = Term.A(DetsNames.True);
    internal BigInteger? MinimumSlots { get; private set; }
    internal BigInteger? MaximumSlots { get; private set; }
    internal bool RamFile { get; private set; }
    internal Term DelayedWrite { get; private set; } = DefaultDelayedWrite;
    internal Term AutoSave { get; private set; } = Term.I(DetsNames.DefaultAutoSaveMilliseconds);
    internal bool ReadOnly { get; private set; }
    internal bool Debug { get; private set; }

    internal TupleTerm ToRecord() => Term.Tuple(
        Term.A(DetsProtocolNames.OpenArguments),
        File,
        Term.A(Type switch
        {
            DetsFileType.Set => DetsNames.Set,
            DetsFileType.Bag => DetsNames.Bag,
            _ => DetsNames.DuplicateBag
        }),
        new Integer(KeyPosition),
        Repair,
        MinimumSlots is { } minimum ? new Integer(minimum) : Term.A(DetsNames.Default),
        MaximumSlots is { } maximum ? new Integer(maximum) : Term.A(DetsNames.Default),
        Term.A(RamFile ? DetsNames.True : DetsNames.False),
        DelayedWrite,
        AutoSave,
        Term.A(ReadOnly ? DetsNames.Read : DetsNames.ReadWrite),
        Term.A(Debug ? DetsNames.True : DetsNames.False)
    );

    internal static DetsOpenOptions FromRecord(Term table, Term record)
    {
        if (record is not TupleTerm { Items.Count: DetsOpenArgumentFields.Count } tuple || tuple.Items[0] is not Atom { Name: DetsProtocolNames.OpenArguments })
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        string[] names = [DetsNames.File, DetsNames.Type, DetsNames.KeyPosition, DetsNames.Repair,
            DetsNames.MinimumSlots, DetsNames.MaximumSlots, DetsNames.RamFile, DetsNames.DelayedWrite,
            DetsNames.AutoSave, DetsNames.Access, DetsNames.Debug];

        return Parse(table, Cons.From(names.Select((name, index) => (Term)Term.Tuple(Term.A(name), tuple.Items[index + 1]))));
    }

    internal static DetsOpenOptions Parse(Term table, Term options)
    {
        var result = new DetsOpenOptions(table);
        IEnumerable<Term> items = options is Cons or Nil ? Cons.Items(options) : new[] { options };
        foreach (Term option in items)
        {
            if (option is not TupleTerm { Items.Count: 2 } pair || pair.Items[0] is not Atom key)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            Term value = pair.Items[1];
            switch (key.Name)
            {
                case DetsNames.Access:
                    RequireAtom(value, DetsNames.Read, DetsNames.ReadWrite);
                    result.ReadOnly = value is Atom { Name: DetsNames.Read };
                    break;
                case DetsNames.AutoSave:
                    NonnegativeOrInfinity(value);
                    result.AutoSave = value;
                    break;
                case DetsNames.CacheSize:
                    NonnegativeOrInfinity(value);
                    break;
                case DetsNames.DelayedWrite:
                    if (value is Atom { Name: DetsNames.Default })
                    {
                        result.DelayedWrite = DefaultDelayedWrite;
                        break;
                    }
                    if (value is not TupleTerm { Items.Count: 2 } cache
                        || cache.Items[0] is not Integer delay || delay.Value < 0
                        || cache.Items[1] is not Integer bytes || bytes.Value < 0)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    result.DelayedWrite = value;
                    break;
                case DetsNames.File:
                    result.File = ToList(value);
                    break;
                case DetsNames.KeyPosition:
                    if (value is not Integer position || position.Value <= 0)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    result.KeyPosition = position.Value;
                    break;
                case DetsNames.MinimumSlots:
                case DetsNames.EstimatedObjects:
                    if (value is Atom { Name: DetsNames.Default })
                    {
                        result.MinimumSlots = null;
                        break;
                    }
                    if (value is not Integer minimum || minimum.Value < 0)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    result.MinimumSlots = BigInteger.Max(minimum.Value, DetsV9Layout.MinimumSlots);
                    break;
                case DetsNames.MaximumSlots:
                    if (value is Atom { Name: DetsNames.Default })
                    {
                        result.MaximumSlots = null;
                        break;
                    }
                    if (value is not Integer maximum || maximum.Value <= 0 || maximum.Value >= DetsV9Layout.AddressSpaceBytes)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    result.MaximumSlots = maximum.Value;
                    break;
                case DetsNames.RamFile:
                    result.RamFile = Boolean(value);
                    break;
                case DetsNames.Repair:
                    RequireAtom(
                        value,
                        DetsNames.True,
                        DetsNames.False,
                        DetsNames.Force
                    );
                    result.Repair = value;
                    break;
                case DetsNames.Type:
                    RequireAtom(
                        value,
                        DetsNames.Set,
                        DetsNames.Bag,
                        DetsNames.DuplicateBag
                    );
                    result.Type = ((Atom)value).Name switch
                    {
                        DetsNames.Set => DetsFileType.Set,
                        DetsNames.Bag => DetsFileType.Bag,
                        _ => DetsFileType.DuplicateBag
                    };
                    break;
                case DetsNames.Version:
                    if (value is not Atom { Name: DetsNames.Default }
                        && !(value is Integer version && version.Value == DetsV9Layout.Version))
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    break;
                case DetsNames.Debug:
                    result.Debug = Boolean(value);
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        // defaults/2 checks is_list, not a native filesystem string conversion.
        if (!IsList(result.File) || result.MinimumSlots is { } min && result.MaximumSlots is { } max && min > max)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return result;
    }

    private static Term DefaultDelayedWrite => Term.Tuple(
        Term.I(DetsNames.DefaultWriteDelayMilliseconds),
        Term.I(DetsNames.DefaultWriteCacheBytes)
    );

    private static Term ToList(Term term)
        => term is Atom atom ? Term.String(atom.Name) : term;

    private static bool IsList(Term value)
    {
        while (value is Cons cell)
        {
            value = cell.Tail;
        }

        return value is Nil;
    }

    private static bool Boolean(Term value)
    {
        RequireAtom(value, DetsNames.True, DetsNames.False);

        return value is Atom { Name: DetsNames.True };
    }

    private static void RequireAtom(Term value, params string[] names)
    {
        if (value is not Atom atom || !names.Contains(atom.Name, StringComparer.Ordinal))
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private static void NonnegativeOrInfinity(Term value)
    {
        if (value is Atom { Name: DetsNames.Infinity } || value is Integer number && number.Value >= 0)
        {
            return;
        }

        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
}
