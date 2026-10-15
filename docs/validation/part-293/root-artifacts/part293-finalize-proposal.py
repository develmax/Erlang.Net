"""Artifact-only proposal. Root runs after terminal full, source/token gates and readiness.
No build, original observation, git mutation or network is initiated here.
"""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--full-exit',required=True);args=p.parse_args()
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-293';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='a527bb63924c2e2106d2c774c42523436d61384e';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():
 assert [p.relative_to(o).as_posix() for p in sorted(o.rglob('*')) if p.is_file()]==['tests.json']
 assert (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
freeze=j(a/'root-part293-final-source-freeze.json');assert freeze['ParentHead']==parent and len(freeze['Files'])==37 and freeze['TokensExact']
paths={x['Path'] for x in freeze['Files']};assert len(paths)==37
assert paths<={p for p in paths if p.startswith(('src/Erlang.Runtime/','tests/Erlang.Tests/'))},'No native collector or unrelated sources'
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx').decode().split('\0'))-{''}<=paths
for row in freeze['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256'],row['Path']
assert j(Path(args.full_exit))['ExitCode']==0 and b'Validation passed' in (a/'root-part293-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-291/tests.json');assert (t['Passed'],t['Failed'])==(6603,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert len(names)==len(set(names))==6603 and len(oldnames)==len(set(oldnames))==6599 and all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in set(oldnames)]==oldnames
added={'host_distribution_owned_exit_under_output_pressure','host_distribution_same_controller_owned_replies_under_output_pressure','host_distribution_publication_commit_cleanup_and_quiescence','runtime/distribution-output-reservation-cancellation-oversize-and-retirement'}
assert {n for n in names if n not in set(oldnames)}==added
integration=j(a/'integration-results.json');assert integration==j(r/'docs/validation/part-291/integration-results.json') and len(integration)==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json')) and sum(x['Module']=='erlang' for x in registry)==344
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6603 and ready['RegisteredMfas']==502 and ready['PreviousRegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-293/tests.json'
groups=j(a/'root-lifecycle292-isolated/focus-groups.json');assert len(groups)==34
focus_guard=j(a/'root-part293-isolated-focus-guard.json')
assert focus_guard['FocusedGroupsPassed']==34 and focus_guard['ExitCode']==0 and focus_guard['TokensExact'] and focus_guard['SourcePaths']==37
assert focus_guard['OnlyFinalFocusLogUsed'] and focus_guard['FinalFocusLogSha256']==h((a/'root-lifecycle292-isolated/focus-monitor-flags-revision.log').read_bytes())
assert focus_guard['FormattedSourceFreezeSha256']==h((a/'root-part293-final-source-freeze.json').read_bytes())
focus=(a/'root-lifecycle292-isolated/focus-monitor-flags-revision.log').read_text(encoding='utf-8-sig')
assert all(re.search(r'^PASS '+re.escape(name)+r'\s*$',focus,re.M) for name in groups)
assert j(a/'root-lifecycle292-isolated/fixture-budget-revision.json')['CorrectedBudget']==5
assert not j(a/'root-lifecycle292-isolated/monitor-flags-revision.json')['OldAssertionsChanged']
ci=j(a/'pack292-ci-38700913583-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD292_CI6599_FULL' and ci['Passed']==6599 and ci['Failed']==0 and ci['Published291SourceUnchanged']
closed='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1';assert h((r/'docs/changelog/0043.md').read_bytes())==closed
entry=(a/'part293-changelog-draft.md').read_text(encoding='utf-8-sig');volume=r/'docs/changelog/0044.md';combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode()
assert b'## Part 293' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400,'Explicit volume rotation required before finalization'
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts289–292 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part293:' not in b and re.search(br'\r?\n\r?\n',b)
attrs=r/'.gitattributes';assert b'docs/validation/part-293/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert dst.is_relative_to(o) and src.is_file() and '__pycache__' not in src.parts and 'LICENSE.local.txt' not in src.parts
 b=src.read_bytes()
 if expected:assert h(b)==expected
 if size is not None:assert len(b)==size
 if dst in copies:assert copies[dst][1]==h(b)
 copies[dst]=(src,h(b))
mp=a/'pack292-ci-detached-staging-manifest.json';m=j(mp);assert len(m)==12
for row in m:select(r/row['SourcePath'],r/row['Destination'],row['Sha256'],row['Bytes'])
select(mp,o/'ci292-staging-manifest.json')
for src in sorted(a.glob('root-part293-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
select(Path(args.full_exit),o/'root-artifacts'/Path(args.full_exit).name)
for name in ['part293-changelog-draft.md','part293-readiness-proposal.py','part293-finalize-proposal.py','part293-stage-guard-proposal.py','root-dispatch293-ordinary.ps1','pack293-read-two-runs.ps1']:select(a/name,o/'root-artifacts'/name)
# Bounded provenance; no source-overlay/build/archive duplication.
for base in ['distribution-all-writer-lifecycle-candidate3','distribution-all-writer-lifecycle-audit2']:
 for src in sorted((a/base).iterdir()):
  if src.is_file() and src.suffix in ['.json','.md','.py','.txt']:select(src,o/'source-provenance'/base/src.name)
for src in sorted((a/'root-lifecycle292-isolated').iterdir()):
 if src.is_file() and src.suffix in ['.log','.json','.cs']:select(src,o/'isolated-revisions'/src.name)
for base in ['root-admission292-overlay']:
 if (a/base/'source-freeze.json').is_file():select(a/base/'source-freeze.json',o/'source-provenance'/base/'source-freeze.json')
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts289–293 (active)'.encode()))
note='Part293: actual Main6603/full integration/packages passes, all6599 historical ordered names plus four HOST groups;37 source paths. Existing output writers share bounded admission; monitor/unlink publication commits with admission; public-FREE-independent exit cursors and same-controller system replies retain owned work and quiescence. Fixture-budget failure and actual legacy monitor-mask regression/fix preserved; old assertions unchanged. Registry502 and erlang344/351 (98.0% API presence) unchanged. Prior292 CI6599/full archived; original2915846+fd8 is historical. Changed-runtime original corpus, new pressure observations and same-head293 CI remain pending. General distribution/physical/NIF/upstream/performance remain unfinished.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-293/** binary\n')
assert h((r/'docs/changelog/0043.md').read_bytes())==closed
receipt={'Status':'ACTUAL_MAIN6603_FULL_OWNED_DISTRIBUTION_OUTPUT_AND_EXIT_PASS_NEW_NATIVE_AND_CI293_PENDING','ParentHead':parent,'SourcePaths':37,'Passed':6603,'Failed':0,'Historical6599OrderedNamesExact':True,'Added':[n for n in names if n in added],'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'IsolatedFocusedGroupsPassed':34,'HistoricalAssertionsChanged':False,'PriorCI292Passed':6599,'HistoricalOriginal291Passed':5846,'HistoricalOriginal291FdPassed':8,'ChangedRuntimeOriginalCompared':False,'NewPressureOriginalExecuted':False,'NewPressureCollectorIncluded':False,'SameHeadCI293Verified':False,'WholeCompatibility':False,'Closed0043Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6603,'SourcePaths':37,'ProofFiles':len(receipt['Files']),'ChangelogBytes':len(combined)}))
