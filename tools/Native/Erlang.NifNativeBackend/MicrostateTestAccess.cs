using System.Numerics;

namespace Erlang.NifNativeBackend;

internal static class MicrostateTestAccess
{
    internal static Task<Term> Set(ProcessRuntime runtime, Term action)
        => CpuStatisticsAccess.Actor<Term>(
            runtime,
            async context => await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.SystemFlag,
                Term.A(RuntimeStatisticsNames.MicrostateAccounting),
                action
            )
        );

    internal static async Task<Term> Sample(ProcessRuntime runtime)
    {
        Term value = await CpuStatisticsAccess.Actor<Term>(
            runtime,
            async context => await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeStatisticsNames.Function,
                Term.A(RuntimeStatisticsNames.MicrostateAccounting)
            )
        );
        Capture(MicrostateControlCaseNames.PublicSample, value);

        return value;
    }

    private static readonly string CaptureDirectory = Path.Combine(
        AppContext.BaseDirectory,
        MicrostateControlCaseNames.Artifacts,
        MicrostateControlCaseNames.OutputPrefix + Guid.NewGuid().ToString(MicrostateControlCaseNames.GuidFormat)
    );
    private static int captureSequence;

    internal static void Capture(string role, Term completeValue)
    {
        Directory.CreateDirectory(CaptureDirectory);
        int sequence = Interlocked.Increment(ref captureSequence);
        string name = Path.Combine(
            CaptureDirectory,
            sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) + MicrostateControlCaseNames.Separator + role
        );
        File.WriteAllBytes(
            name + MicrostateControlCaseNames.RawExtension,
            ExternalTermFormat.Encode(completeValue,
            new EtfEncodingOptions { MinorVersion = 2 })
        );
        File.WriteAllBytes(
            name + MicrostateControlCaseNames.DeterministicExtension,
            ExternalTermFormat.Encode(completeValue,
            new EtfEncodingOptions { MinorVersion = 2, Deterministic = true })
        );
    }

    internal static MicrostateSample[] Parse(Term term)
    {
        var result = new List<MicrostateSample>();
        var identities = new HashSet<(string, int)>();
        foreach (Term item in Cons.Items(term))
        {
            CpuStatisticsAccess.Require(item is MapTerm);
            var map = (MapTerm)item;
            CpuStatisticsAccess.Require(map.Entries.Count == 3);
            string type = ((Atom)Get(map, MicrostateCaseNames.Type)).Name;
            int identity = checked((int)((Integer)Get(map, MicrostateCaseNames.Identity)).Value);
            CpuStatisticsAccess.Require(identity > 0 && identities.Add((type, identity)));
            var counters = (MapTerm)Get(map, MicrostateCaseNames.Counters);
            CpuStatisticsAccess.Require(counters.Entries.Select(pair => ((Atom)pair.Key).Name).Order()
                .SequenceEqual(MicrostateCaseNames.Keys.Order()));
            var values = counters.Entries.ToDictionary(pair => ((Atom)pair.Key).Name, pair => ((Integer)pair.Value).Value);
            CpuStatisticsAccess.Require(values.Values.All(value => value.Sign >= 0));
            result.Add(new(type, identity, values));
        }

        return result.ToArray();
    }

    private static Term Get(MapTerm map, string key)
        => map.Entries.Single(pair => pair.Key.Equals(Term.A(key))).Value;

    internal static BigInteger Sum(IEnumerable<MicrostateSample> values, string state, string? type = null)
        => values.Where(value => type is null || value.Type == type).Aggregate(BigInteger.Zero,
            (sum, value) => sum + value.Counters[state]);

    internal static void Boolean(Term term, bool value)
        => CpuStatisticsAccess.Require(term.Equals(CoreModules.Boolean(value)));

    internal static MicrostateSample[] Frozen(RuntimeMicrostateAccounting counter)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        object ledgerGate = typeof(RuntimeMicrostateAccounting).GetField(MicrostateControlCaseNames.LedgerGate, flags)!.GetValue(counter)!;
        var records = (Dictionary<(RuntimeMicrostateThreadIdentity, string), RuntimeMicrostateRecord>)typeof(RuntimeMicrostateAccounting)
            .GetField(MicrostateControlCaseNames.LedgerRecords, flags)!.GetValue(counter)!;
        lock (ledgerGate)
            return records.Values.Select(record => new MicrostateSample(
                record.Type,
                record.Identity,
                RuntimeMicrostateNames.States.Select((name, index) => (name, index))
                    .ToDictionary(pair => pair.name, pair => record.Counters[pair.index])
            )).ToArray();
    }

    internal static MicrostateSample Row(IEnumerable<MicrostateSample> values, RuntimeMicrostateRecord record)
        => values.Single(value => value.Type == record.Type && value.Identity == record.Identity);

}
