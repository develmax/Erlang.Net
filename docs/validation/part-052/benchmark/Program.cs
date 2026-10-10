using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

const int QueryCount = 20000;
const int Repetitions = 5;
var results = new List<object>();
foreach (int size in new[] { 8, 64, 1024, 4096 })
{
    var map = new MapTerm(Enumerable.Range(0, size).Select(i => new KeyValuePair<Term, Term>(Term.I(i),Term.I(i))));
    var keys = Enumerable.Range(0,QueryCount).Select(i => Term.I(i % (size + size / 4))).ToArray();
    Term? Linear(Term key) => map.Entries.FirstOrDefault(e => e.Key.Equals(key)).Value;
    Term? Indexed(Term key) => map.TryGet(key,out var value) ? value : null;
    (double Milliseconds,long Allocated,long Checksum) Measure(Func<Term,Term?> lookup)
    {
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long checksum = 0;
        var watch = Stopwatch.StartNew();
        foreach (var key in keys)
        {
            var value = lookup(key);
            checksum += value is Integer integer ? (long)integer.Value : -1;
        }
        watch.Stop();

        return (watch.Elapsed.TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-allocated,checksum);
    }
    // Warm both paths. Alternate sample order to reduce order bias; no timing assertions.
    Measure(Linear);
    Measure(Indexed);
    var linear = new List<(double Milliseconds,long Allocated,long Checksum)>();
    var indexed = new List<(double Milliseconds,long Allocated,long Checksum)>();
    for(int sample=0;sample<Repetitions;sample++)
    {
        if(sample%2==0) { linear.Add(Measure(Linear));indexed.Add(Measure(Indexed)); }
        else { indexed.Add(Measure(Indexed));linear.Add(Measure(Linear)); }
    }
    if(linear.Any(x=>x.Checksum!=indexed[0].Checksum)||indexed.Any(x=>x.Checksum!=linear[0].Checksum)) throw new InvalidOperationException("Lookup results differ");
    double before=linear.OrderBy(x=>x.Milliseconds).ElementAt(Repetitions/2).Milliseconds;
    double after=indexed.OrderBy(x=>x.Milliseconds).ElementAt(Repetitions/2).Milliseconds;
    results.Add(new{MapSize=size,QueryCount,Repetitions,LinearSamples=linear.Select(x=>new{x.Milliseconds,x.Allocated,x.Checksum}),IndexedSamples=indexed.Select(x=>new{x.Milliseconds,x.Allocated,x.Checksum}),LinearMedianMs=before,IndexedMedianMs=after,MedianRatio=before/after});
}
var report=new{Runtime=RuntimeInformation.FrameworkDescription,OS=RuntimeInformation.OSDescription,Architecture=RuntimeInformation.ProcessArchitecture.ToString(),Build="Release",Workload="Prebuilt integer-key maps; cycling keys with 20% misses; same query sequence; no map construction in timed region",Limits="C# old linear algorithm versus retained exact-key Dictionary only; not an OTP/BEAM benchmark or full-language speed claim; retained map memory not measured; timing influenced by host",Results=results};
File.WriteAllText("artifacts/part-052-map-lookup-benchmark.json",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Saved map lookup samples, allocations and matching checksums.");
