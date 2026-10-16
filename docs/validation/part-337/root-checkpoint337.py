from pathlib import Path
import argparse,hashlib,json,shutil,subprocess

parser=argparse.ArgumentParser()
parser.add_argument('--guard-sha256',required=True)
args=parser.parse_args()
r=Path.cwd();a=r/'artifacts';c=a/'proc-lib337-candidate';d=a/'proc-lib337-full-source2';b=r/'docs/validation/part-337'
j=lambda path:json.loads(path.read_bytes())
h=lambda data:hashlib.sha256(data).hexdigest().upper()
head='49f586e8a24c140efdf522842d65a7aa67abb0ad'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==head
assert h((d/'full-guard.json').read_bytes())==args.guard_sha256.upper()
g=j(d/'full-guard.json');f=j(d/'source-freeze.json');tests=j(d/'tests.json');oldtests=j(r/'docs/validation/part-335/final-candidate/tests.json')
assert f['ParentSha']==head and g['SourceFreezeSha256']==h((d/'source-freeze.json').read_bytes())
assert (g['Passed'],g['Failed'],g['Registry'],g['ProcLibMfas'])==(7017,0,540,29)
names=[row['Name'] for row in tests['Tests']];oldnames=[row['Name'] for row in oldtests['Tests']]
assert len(names)==len(set(names))==7017 and len(oldnames)==6952 and [name for name in names if name in set(oldnames)]==oldnames
assert all(row['Status']=='Passed' for row in tests['Tests']) and tests['Failed']==0
exit=j(d/'full-exit.json');assert exit['ExitCode']==0 and exit['Settled'] and not exit['TimedOut']
assert len(j(d/'integration-results.json'))==11
owned={row['Path']:row for row in f['Files']};assert len(owned)==54
closure=j(d/'source-resource-closure.json')['Files'];assert len(closure)==2593
for manifest in ['source-resource-closure.json','package-resources.json']:
    for row in j(d/manifest)['Files']:
        data=(c/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'],row['Path']
        if row['Path'] not in owned:assert (r/row['Path']).read_bytes()==data,row['Path']
assert len(j(d/'package-resources.json')['Files'])==6
old=j(r/'docs/supported-mfas.json');new=j(d/'supported-mfas.json');key=lambda row:(row['Module'],row['Function'],row['Arity']);newrows={key(row):row for row in new}
assert len(old)==536 and len(newrows)==len(new)==540
assert all(newrows[key(row)]==row for row in old)
assert set(newrows)-set(map(key,old))=={('proc_lib','format',1),('proc_lib','format',2),('proc_lib','format',3),('proc_lib','report_cb',2)}
peer=a/'formatter337-independent-peer/guard.json'
assert h(peer.read_bytes())=='858283E4DD3294E0C89D87A2876F1E4DAD5B20E1E00B29673AA250F847524C14'
match=j(a/'root-formatter-original-full-comparison4.json');assert match['Status']=='MATCH' and match['Compared']==match['Passed']==76 and not match['TextNormalized']
assert not b.exists()
for path,row in owned.items():
    assert h((c/path).read_bytes())==row['Sha256'],path
    (r/path).parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(c/path,r/path)
for row in closure:assert h((r/row['Path']).read_bytes())==row['Sha256'],row['Path']

def write(path,text):
    (r/path).write_text(text,encoding='utf-8',newline='\n')

def prepend(path,text):
    first,rest=(r/path).read_text(encoding='utf-8').split('\n',1)
    write(path,first+'\n\n'+text+'\n\n'+rest.lstrip('\n'))

summary=('Part337 delivers proc_lib format/1,2,3 and report_cb/2 with an independent76/76 original term comparison (57 reports including12 raw iolists,19 backend primitives). '
    'Actual full7017/7017 tests and11 integration/package outcomes pass; all6952 prior ordered names and536 registry rows/custom Limits remain exact. '
    'Registry540 across9 modules; proc_lib29/39, sys2/36, erlang351 unchanged. Original expectations come solely from independently captured pinned OTP returns. '
    'Both source-derived28 pretty assertions and actual callback/deep-continuation controls pass separately. Source37 new production files and exact package6 are bound. '
    'Published335 CI6952/11 and original6114/fd8 are independently complete, with the existing map stdout variance retained as a strict refusal. Published336 collector76 and CI6952/11 are verified. '
    'Ten proc_lib exports remain: initializer/hibernate/wake-up and six node overloads, with genuine crash logging and connected spawning still being implemented. '
    'Generic IO formatting, broad Unicode-version/configured printable-range, native physical/scheduler and performance equivalence remain partial. Current337 CI/enlarged ordinary oracle await publication; API presence is not whole-module completion.')
for path in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/PROC_LIB_COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/semantic-differences.md']:
    prepend(path,summary)
prepend('docs/DECISIONS.md','Part337 — Port proc_lib report/exception orchestration and the required pretty/control algorithms directly over immutable C# terms and existing runtime primitives. Retain actual raw iolist structure, not only rendered text. Obtain expected ETF exclusively from pinned source-built OTP; a separate independent decoder verifies76 comparisons. Keep generic io_lib/string and unverified logger/remote drafts outside this registration slice.')
prepend('docs/dependency-decisions.md','Part337 — Inspect pinned proc_lib, io_lib_format, io_lib_pretty, erl_error, string and Unicode generator/data. Reuse the existing terms, runtime identity/float/function-information facilities, System.Text.Rune/strict UTF8, and current BCL grapheme measurement with its Unicode-version gap explicit. A general .NET text/JSON serializer cannot preserve Erlang raw report lists or source depth/budget rules; no external formatter, actor framework or new package is introduced. Preserve existing third-party notices and keep the owner-local public-license proposal uncommitted.')
prepend('docs/NEXT_STEPS.md','Part337: publish the original-verified formatter and dispatch the enlarged ordinary oracle once. Complete all ten remaining proc_lib exports with actual init_p/crash-report/logger, hibernate/wake_up and connected remote-spawn behavior. Source-only logger and remote prototypes remain uncounted until their source findings and actual checks are resolved. Preserve original76 baselines and failed70/75 comparisons; no unsupported backend may masquerade as a completed module.')
prepend('docs/DEPENDENCY_PRIORITIES.md','Part337 — proc_lib29/39 now has original-verified report formatting. Prioritize the real logger/error_logger/application-environment dependencies and shared initializer/crash-report path, then connected spawn protocol/ownership and six node overloads. Test real delivery and resource/order boundaries; retain unsupported native tuning/physical/performance scope explicitly.')
scope=j(r/'docs/module-readiness-scope.json');scope['ComparisonTestReport']='validation/part-335/final-candidate/tests.json'
component=next(row for row in scope['Components'] if row['Id']=='proc_lib adapters')
component['Implemented'].append('Part337: format/1,2,3 and report_cb/2; independent original57report+19primitive exactterm matches, raw12 nested lists preserved. Actual7017/11,28sourcepretty/async-callback/deep controls. Registry540/proc_lib29/39. Published335 original6114/fd8 and336 original76 verified separately.')
component['Remaining']=['Ten initializer/hibernate/wake-up and node-overload exports; real crash logger integration','Connected remote RPC/numeric PID channels and unrestricted native tuning','Configured +pcunicode/BCL Unicode-version parity and generic finite Unicode/write controls outside generated report paths','Complete gen/sys, stack/tracing/scheduler/physical and performance parity']
write('docs/module-readiness-scope.json',json.dumps(scope,indent=2)+'\n')
shutil.copyfile(d/'supported-mfas.json',r/'docs/supported-mfas.json');shutil.copyfile(d/'tests.json',a/'tests.json')
b.mkdir(parents=True);(b/'.gitattributes').write_bytes(b'* binary\n')
for source in d.iterdir():
    if source.is_file() and source.suffix in ['.json','.log']:
        target=b/'final-candidate'/source.name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
entry=(a/'part337-changelog-draft.md').read_text(encoding='utf-8').replace('{{PASSED}}',str(g['Passed']))
log=r/'docs/changelog/0052.md';text=log.read_text(encoding='utf-8')+'\n'+entry
assert len(text.encode())<=32768 and len(text.splitlines())<=400
write('docs/changelog/0052.md',text)
write('CHANGELOG.md',(r/'CHANGELOG.md').read_text(encoding='utf-8').replace('parts332–336','parts332–337'))
receipt=dict(Status='EXACT337_RUNTIME_PROMOTED_PENDING_FINAL_READINESS_AND_STAGE',ParentHead=head,Passed=7017,Registry=540,ProcLib=29,OwnedPaths=54,SourceClosure=2593,PackageResources=6,FullGuardSha256=args.guard_sha256.upper(),IndependentOriginalComparison=76,CurrentHeadCiVerified=False)
(a/'root-part337-promotion.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt))
