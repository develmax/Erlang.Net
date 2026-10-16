from pathlib import Path
import json,hashlib
out=Path('artifacts/proc-lib338-term-limit-fix');src=out/'source';d=src/'tools/Erlang.Differential';d.mkdir(parents=True,exist_ok=True)
rows=[]
for name in ['limit-small-map-values','limit-large-own-iterator']:
 text=(out/'fixtures'/(name+'.erl')).read_text();rows.append((name,text))
(d/'ErrorLoggerTermLimitCaseData.cs').write_text('namespace Erlang.Differential;\n\npublic static class ErrorLoggerTermLimitCaseData\n{\n    public static IReadOnlyList<(string Name, string Source)> Rows { get; } =\n    [\n'+',\n'.join('        ('+json.dumps(n)+', '+json.dumps(t)+')' for n,t in rows)+'\n    ];\n}\n',encoding='utf-8')
(d/'ErrorLoggerTermLimitCases.cs').write_text('''namespace Erlang.Differential;

public static class ErrorLoggerTermLimitCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = ErrorLoggerTermLimitCaseData.Rows.Select(row => new CompiledModuleCase(
        ProcLibLifecycleCaseNames.Prefix + row.Name,
        row.Source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibLifecycleCaseNames.Ok))
    )).ToArray();
}
''',encoding='utf-8')
files=[];base=Path('artifacts/proc-lib338-candidate')
for p in sorted(src.rglob('*')):
 if p.is_file():
  b=p.read_bytes();rel=p.relative_to(src);par=base/rel;files.append(dict(Path=rel.as_posix(),Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper(),ParentSha256=hashlib.sha256(par.read_bytes()).hexdigest().upper() if par.exists() else None))
fixtureRows=[]
for p in sorted((out/'fixtures').glob('*.erl')):
 b=p.read_bytes();fixtureRows.append(dict(Path=p.relative_to(out).as_posix(),Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper(),Expected='ok',OriginalExecuted=False))
integration=dict(Status='SOURCE_ONLY_ADDITIVE_INTEGRATION_INSTRUCTIONS',ExpectedExistingOnlyChangedFile='src/Erlang.Otp.Behaviours/ErrorLoggerTermLimiter.cs',AddCompiledCatalogSpread='.. ErrorLoggerTermLimitCases.Modules',AddHostTest='Test(Erlang.Tests.ErrorLoggerTermLimitHostNames.Test, Erlang.Tests.ErrorLoggerTermLimitHostChecks.Run);',ParseAllRequirement='Also parse ErrorLoggerTermLimitCaseData.Rows before firstguestcase.',BusyCandidateChanged=False)
(out/'integration.json').write_text(json.dumps(integration,indent=2)+'\n')
f=out/'source-freeze.json';f.write_text(json.dumps(dict(Status='SOURCE_ONLY_338_LIMIT_TERM_ITERATIVE_PREPASS_AND_ITERATOR_PROPOSAL',Files=files,Fixtures=fixtureRows,PinnedOtp='ad05823719d77c8faee87348ea39513d4e2f99c5',Reference='lib/stdlib/src/io_lib.erl:limit_term/2,test_limit/test_limit_tail/test_limit_tuple/test_limit_map_body/assoc,limit_map',Classification='Missing source optimization and HOST identity/performance retention; sorted map.Entries vs defaultiterator was not demonstrated Erlang value regression because existing defaultiterator uses sameentries. Reuseactualiterator avoids relying on representation.',Identity='Returnoriginal onlyif iterative pinnedtest_limit passes; do not use result.Equalsinput shortcut. Conservativefailswith equal rebuilt maps are explicitly HOSTchecked.',MapSemantics='Prepass decreasesdepth perassoc andcheckskeys+values; actual limiting keepskeys unmodified andallselectedvalues atfixedD-1. Select firstD+1 through MapIteration.Iterator/Next.',IntegrationSha256=hashlib.sha256((out/'integration.json').read_bytes()).hexdigest().upper(),SdkExecuted=False,OriginalExecuted=False,MainEdited=False,Candidate338Changed=False),indent=2)+'\n');print(len(files),hashlib.sha256(f.read_bytes()).hexdigest().upper())
