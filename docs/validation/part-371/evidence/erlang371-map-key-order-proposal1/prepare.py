from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
TERMS=OUT/'source/src/Erlang.Terms'
TERMS.mkdir(parents=True,exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def write(name,text): (TERMS/name).write_text(text,encoding='utf-8')
base=ROOT/'src/Erlang.Terms/TermOrder.cs'
assert sha(base)=='1A10BE0EFFC5005976DDDD7745E1C48774291498E77B57D0F8FF503167F40F14'
s=base.read_text()
anchor='''                    pending.Push(MapSteps(x, y, exact).GetEnumerator());'''
assert s.count(anchor)==1
s=s.replace(anchor,'''                    pending.Push((purpose == TermComparisonPurpose.Ordering && !exact
                        && count == 0 && x.Entries.Count > 1
                        && x.Entries.Count <= TermMapOrderParameters.FlatMapMaximumEntries
                        ? TermMapOrdering.Steps(x, y)
                        : MapSteps(x, y, exact)).GetEnumerator());''')
start=s.index('        var pending = new Stack<IEnumerator<TermComparisonStep>>();',s.index('    private static int ComparePurposeSnapshot('))
end=s.index('\n    private static IEnumerable<TermComparisonStep> SequenceSteps(',start)
machine='''        var scopes = new Stack<TermComparisonScope>();
        scopes.Push(new TermComparisonScope(a, b, exact, purpose));
        try
        {
            while (true)
            {
                var scope = scopes.Peek();
                var step = scope.Current;
                if (step.Capture is { } capture)
                {
                    scopes.Push(new TermComparisonScope(
                        step.Left!,
                        step.Right!,
                        step.Exact,
                        step.Purpose ?? scope.Purpose,
                        capture
                    ));
                    continue;
                }
                int comparison = step.Left is null ? step.Result : CompareCurrent(
                    step.Left,
                    step.Right!,
                    step.Exact,
                    ref context,
                    scope.Pending,
                    scope.Purpose
                );
                if (comparison == 0 && scope.MoveNext())
                    continue;
                while (true)
                {
                    var finished = scopes.Pop();
                    finished.Dispose();
                    if (scopes.Count == 0)
                        return comparison;
                    finished.Capture!.Value = comparison;
                    scope = scopes.Peek();
                    if (scope.MoveNext())
                        break;
                    comparison = 0;
                }
            }
        }
        finally
        {
            while (scopes.TryPop(out var scope))
                scope.Dispose();
        }
    }
'''
s=s[:start]+machine+s[end:]
write('TermOrder.cs',s)
prior=ROOT/'artifacts/erlang370-termorder-map-proposal2/source/src/Erlang.Terms'
for name in ['TermComparisonStep.cs','TermComparisonScope.cs','TermComparisonResult.cs']:
    write(name,(prior/name).read_text())
write('TermMapOrderParameters.cs','''namespace Erlang;

internal static class TermMapOrderParameters
{
    internal const int FlatMapMaximumEntries = 32;
}
''')
write('TermMapOrderingResult.cs','''namespace Erlang;

internal sealed class TermMapOrderingResult
{
    internal KeyValuePair<Term, Term>[] Entries { get; set; } = [];
}
''')
write('TermMapOrdering.cs','''namespace Erlang;

internal static class TermMapOrdering
{
    internal static IEnumerable<TermComparisonStep> Steps(MapTerm left, MapTerm right)
    {
        var first = new TermMapOrderingResult();
        var second = new TermMapOrderingResult();
        foreach (var step in Sort(left.Entries, first))
            yield return step;
        foreach (var step in Sort(right.Entries, second))
            yield return step;
        for (int index = 0; index < first.Entries.Length; index++)
            yield return TermComparisonStep.Pair(first.Entries[index].Key, second.Entries[index].Key, true);
        for (int index = 0; index < first.Entries.Length; index++)
            yield return TermComparisonStep.Pair(first.Entries[index].Value, second.Entries[index].Value, false);
    }

    private static IEnumerable<TermComparisonStep> Sort(
        IReadOnlyList<KeyValuePair<Term, Term>> entries,
        TermMapOrderingResult result
    )
    {
        var source = entries.ToArray();
        var target = new KeyValuePair<Term, Term>[source.Length];
        var capture = new TermComparisonResult();
        for (int width = 1; width < source.Length; width *= 2)
        {
            for (int start = 0; start < source.Length; start += width * 2)
            {
                int left = start;
                int middle = Math.Min(start + width, source.Length);
                int right = middle;
                int end = Math.Min(start + width * 2, source.Length);
                for (int index = left; index < end; index++)
                {
                    if (left == middle)
                        target[index] = source[right++];
                    else if (right == end)
                        target[index] = source[left++];
                    else
                    {
                        yield return TermComparisonStep.Observe(
                            source[left].Key,
                            source[right].Key,
                            true,
                            TermComparisonPurpose.Ordering,
                            capture
                        );
                        target[index] = capture.Value <= 0 ? source[left++] : source[right++];
                    }
                }
            }
            (source, target) = (target, source);
        }
        result.Entries = source;
    }
}
''')
files=[]
for p in sorted((OUT/'source').rglob('*.cs')):
    rel=str(p.relative_to(OUT/'source')).replace('\\','/')
    files.append({'Source':str(p.relative_to(ROOT)).replace('\\','/'),'Destination':rel,'Sha256':sha(p),'Bytes':p.stat().st_size,'ExistsInMain':(ROOT/rel).exists(),'ExpectedCurrentSha256':sha(ROOT/rel) if (ROOT/rel).exists() else None})
review={
 'Status':'SOURCE_ONLY_NARROW_FLATMAP_ORDERING_CANDIDATE_HOLD_FOR_NATIVE14','PinnedCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','PinnedSource':'authority/utils.c','PinnedSourceSha256':sha(OUT/'authority/utils.c'),
 'PinnedBranch':'utils.c1958-2065 flatmap equal size, non-exact ordering: native keys in term order except atom-index partition; keys compare in exact numeric mode then values in requested non-exact mode. Local FUN comparison2157+ module/index/OLD_UNIQ/free/env. Existing managed canonical storage instead uses NEW_UNIQ for exact identity.',
 'Changed':'Only normal Ordering of equal-sized maps with2..32entries obtains a temporary source-order key view through auxiliary comparisons. Every other map branch, numeric equality, exact equality, canonical map key storage, lookup and hash stays unchanged.',
 'NoMapTermFunctionSerializationChange':True,'NoNewValidInputRejection':True,'NoIdentityCollapse':True,
 'IteratorSafety':'Bottom-up stable merge yields auxiliary comparisons; one explicit scope stack evaluates them. Never calls Compare recursively, LINQ Sort comparator, Dictionary lookup or hash from a comparison. One shared by-ref NodeComparisonContext spans all scopes, so local node aliases snapshot once. Finally drains every active scope and iterator on success, early inequality and exception.',
 'PerformanceScope':'Temporary arrays/merge work only for small nontrivial flatmap ordering, bounded32entries; no whole-runtime map storage reorder/cache or resource hash layout introduced. Costs must be measured, no speed claim.',
 'Unclosed':['HAMT >32 map ordering remains existing behavior, not promoted.','Exact nested map-key mode and OLD_UNIQ compare0/NEW_UNIQ exact-nequal key tie protocol remain source/actual-native gates. Stable sort retains both entries; it never coalesces them.','This patch cannot alone grant complete contracts for all ordsets term domains.','Final Main source/DLL capture, native14 comparison and existing deep comparison/hash/snapshot/cancellation cleanup regression required before acceptance.'],
 'CreatorCorrection':'Pinned OTP discards decoded NEW_FUN creator and serializes local init PID; existing Main follows that protocol. No creator-preservation patch or normalization proposed.',
 'NoMainBuildNetworkOrGit':True,
}
(OUT/'copy-plan.json').write_text(json.dumps({'Files':files,'NoMainEdits':True,'HoldForActualNativeWitness':True},indent=2)+'\n',encoding='utf-8')
(OUT/'review.json').write_text(json.dumps(review,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'ProductionFiles':len(files),'ReviewSha256':sha(OUT/'review.json')}))
