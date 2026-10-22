using System.Numerics;
using System.Text;
using System.Globalization;
namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreInfo420(
        ProcessContext context,
        Term identifier,
        Term? requested,
        out byte[]? printed
    )
    {
        printed = null;
        lock (gate)
        {
            RequireActor(context);
            if (identifier is not Atom && (identifier is not ResourceReferenceTerm resource || resource.Entry.Kind != ReferenceResourceKind.EtsTable)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Type);
            RuntimeEtsFoundationTable table;
            try
            {
                table = Resolve(identifier);
            }
            catch (InvalidOperationException error) when (error.Message == RuntimeEtsFoundationNames.InvalidTable)
            {
                return Term.A(EtsInfoNames420.Undefined);
            }
            if (!coreOptions407.TryGetValue(table, out var options)) throw new NotSupportedException(RuntimeEtsCoreNames407.HostTable);
            // DB_INFO permits nonowners of private tables. Read/write access is deliberately not requested here.
            if (requested is null)
            {
                Term result = Nil.Value;
                foreach (string field in EtsInfoNames420.Fields) result = new Cons(Term.Tuple(Term.A(field), CoreInfoItem420(
                    context,
                    table,
                    options,
                    field
                )), result);

                return result;
            }
            if (requested is not Atom item) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            if (item.Name == EtsInfoNames420.Data)
            {
                printed = CorePrintTable420(context, table, options);

                return Term.A(ErlangBooleanAtoms.True);
            }

            return CoreInfoItem420(
                context,
                table,
                options,
                item.Name
            );
        }
    }

    private Term CoreInfoItem420(
        ProcessContext context,
        RuntimeEtsFoundationTable table,
        RuntimeEtsCoreOptions407 options,
        string item
    )
    {
        context.Cancellation.ThrowIfCancellationRequested();

        return item switch
        {
            EtsInfoNames420.Protection => Term.A(table.Protection switch
            {
                RuntimeEtsProtection.Private => RuntimeEtsCoreNames407.Private,
                RuntimeEtsProtection.Public => RuntimeEtsCoreNames407.Public,
                _ => RuntimeEtsCoreNames407.Protected
            }),
            EtsInfoNames420.KeyPosition => Term.I(options.KeyPosition),
            EtsInfoNames420.Type => Term.A(options.Kind switch
            {
                EtsCoreTableKind407.OrderedSet => RuntimeEtsCoreNames407.OrderedSet,
                EtsCoreTableKind407.Bag => RuntimeEtsCoreNames407.Bag,
                EtsCoreTableKind407.DuplicateBag => RuntimeEtsCoreNames407.DuplicateBag,
                _ => RuntimeEtsCoreNames407.Set
            }),
            EtsInfoNames420.Named => Term.A(table.Named ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
            EtsInfoNames420.Node => runtime.NodeAtom,
            EtsInfoNames420.Size => Term.I(table.Objects.Count),
            EtsInfoNames420.Name => table.Name,
            EtsInfoNames420.Heir => table.Heir419 is { } heir ? heir.Pid : Term.A(EtsInfoNames420.None),
            EtsInfoNames420.Owner => table.Owner,
            EtsInfoNames420.Memory => new Integer((new BigInteger(CoreOwnedCensus420(
                table,
                options,
                RuntimeManagedObjectExtent420.Measure,
                context.Cancellation
            ).Bytes) + IntPtr.Size - 1) / IntPtr.Size),
            EtsInfoNames420.Compressed or EtsInfoNames420.WriteConcurrency or EtsInfoNames420.ReadConcurrency or EtsInfoNames420.Decentralized => Term.A(ErlangBooleanAtoms.False),
            EtsInfoNames420.Id => table.Handle,
            EtsInfoNames420.Fixed => Term.A(table.FixedCount420 != 0 ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
            EtsInfoNames420.SafeFixed => CoreFixedInfo420(table, false),
            EtsInfoNames420.MonotonicFixed => CoreFixedInfo420(table, true),
            EtsInfoNames420.Stats => CoreStats420(table, options),
            _ => throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None)
        };
    }

    private static Term CoreFixedInfo420(RuntimeEtsFoundationTable table, bool monotonic)
    {
        if (table.FixedCount420 == 0) return Term.A(ErlangBooleanAtoms.False);
        Term time;
        if (monotonic) time = new Integer(ErlangClockState.Floor(table.FixedAtMonotonic420 * ErlangClockUnits413.NativeFrequency, ErlangClockParameters.NanosecondsPerSecond));
        else
        {
            var micros = ErlangClockState.Floor(
                table.FixedAtMonotonic420 + table.FixedOffset420,
                ErlangClockParameters.NanosecondsPerSecond / ErlangClockParameters.MicrosecondsPerSecond
            );
            var seconds = BigInteger.DivRem(micros, ErlangClockParameters.MicrosecondsPerSecond, out var microseconds);
            var mega = BigInteger.DivRem(seconds, ErlangClockParameters.SecondsPerMegaSecond, out var remainder);
            time = Term.Tuple(new Integer(mega), new Integer(remainder), new Integer(microseconds));
        }
        // Own process ordering uses the actual registered fixer identities; no fabricated native addresses.
        Term fixers = Nil.Value;
        foreach (var fix in table.FixedOrder420.OrderBy(fix => fix.Process, Comparer<Pid>.Create((left, right) => TermOrder.Compare(left, right))))
            fixers = new Cons(Term.Tuple(fix.Process, Term.I(fix.Count)), fixers);

        return Term.Tuple(time, fixers);
    }

    private static Term CoreStats420(RuntimeEtsFoundationTable table, RuntimeEtsCoreOptions407 options)
    {
        if (options.Kind == EtsCoreTableKind407.OrderedSet) return Term.A(ErlangBooleanAtoms.False);
        var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);
        ulong sum = 0, squares = 0, kept = 0;
        int min = int.MaxValue, max = 0;
        foreach (var bucket in index.Buckets)
        {
            int length = 0;
            foreach (var group in bucket)
            {
                length = checked(length + group.Rows.Length + (group.Tombstone is null ? 0 : 1));
                if (group.Tombstone is not null) kept++;
            }
            sum += (ulong)length;
            squares += (ulong)length * (ulong)length;
            min = Math.Min(min, length);
            max = Math.Max(max, length);
        }
        float average = (float)sum / index.BucketCount;
        // Source assigns avg through float before expected/real standard deviation.
        double real = Math.Sqrt((squares - (double)average * sum) / index.BucketCount), expected = Math.Sqrt(average * (1 - 1.0 / index.BucketCount));

        return Term.Tuple(
            Term.I(index.BucketCount),
            new FloatTerm(average),
            new FloatTerm(real),
            new FloatTerm(expected),
            Term.I(min),
            Term.I(max),
            new Integer(kept),
            Term.I(1)
        );
    }

    private byte[] CorePrintTable420(ProcessContext context, RuntimeEtsFoundationTable table, RuntimeEtsCoreOptions407 options)
    {
        var text = new StringBuilder();

        string Display(Term value)
        {
            byte[] bytes = new TermDisplayWriter(runtime).Write(value);

            return Encoding.UTF8.GetString(bytes.AsSpan(0, bytes.Length - 1));
        }

        void Line(string label, Term value) => text.Append(label).Append(Display(value)).Append(EtsInfoNames420.NewLine);
        Line(EtsInfoNames420.TableLabel, table.Named ? table.Name : table.Handle);
        Line(EtsInfoNames420.NameLabel, table.Name);
        var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);
        if (options.Kind == EtsCoreTableKind407.OrderedSet)
        {
            foreach (var group in index.Ordered) foreach (var row in group.Rows) text.Append(Display(CoreRead407(row.Payload, context.Cancellation))).Append(EtsInfoNames420.NewLine);
        }
        else for (int bucket = 0; bucket < index.Buckets.Length; bucket++)
        {
            if (index.Buckets[bucket].Length == 0) continue;
            text.Append(bucket.ToString(CultureInfo.InvariantCulture)).Append(EtsInfoNames420.BucketSeparator);
            bool first = true;
            foreach (var group in index.Buckets[bucket])
            {
                var rows = group.Tombstone is { } dead ? new[] { dead } : group.Rows.Reverse();
                foreach (var row in rows)
                {
                    if (!first) text.Append(EtsInfoNames420.Comma);
                    if (group.Deleted) text.Append(EtsInfoNames420.Deleted);
                    text.Append(Display(CoreRead407(row.Payload, context.Cancellation)));
                    first = false;
                }
            }
            text.Append(EtsInfoNames420.CloseBucket).Append(EtsInfoNames420.NewLine);
        }
        Line(EtsInfoNames420.ObjectsLabel, CoreInfoItem420(
            context,
            table,
            options,
            EtsInfoNames420.Size
        ));
        Line(EtsInfoNames420.WordsLabel, CoreInfoItem420(
            context,
            table,
            options,
            EtsInfoNames420.Memory
        ));
        Line(EtsInfoNames420.TypeLabel, CoreInfoItem420(
            context,
            table,
            options,
            EtsInfoNames420.Type
        ));
        Line(EtsInfoNames420.ProtectionLabel, CoreInfoItem420(
            context,
            table,
            options,
            EtsInfoNames420.Protection
        ));
        Line(EtsInfoNames420.CompressedLabel, Term.A(ErlangBooleanAtoms.False));
        Line(EtsInfoNames420.WriteLabel, Term.A(ErlangBooleanAtoms.False));
        Line(EtsInfoNames420.ReadLabel, Term.A(ErlangBooleanAtoms.False));

        return NativePrintfOutput.Encode(Encoding.UTF8.GetBytes(text.ToString()));
    }
}
