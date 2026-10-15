"""Not executed by author. Root must supply final counts/added names/callback summary.
Placeholder callback prose deliberately blocks publication until concretely reviewed.
"""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--passed',type=int,required=True);p.add_argument('--source-count',type=int,required=True);p.add_argument('--added-tests',required=True);p.add_argument('--callback-summary',required=True);p.add_argument('--full-exit',required=True);args=p.parse_args()
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-295';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='200ef8550d0b533f2a59eb872ac3949179994d7c';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'] and (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
f=j(a/'root-part295-final-source-freeze.json');assert f['ParentHead']==parent and f['TokensExact'] and len(f['Files'])==args.source_count and args.source_count>=27
source_paths={x['Path'] for x in f['Files']};assert len(source_paths)==args.source_count
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=source_paths
assert j(Path(args.full_exit))['ExitCode']==0 and b'Validation passed' in (a/'root-part295-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-293/tests.json');assert (t['Passed'],t['Failed'])==(args.passed,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']];added=j(Path(args.added_tests))
assert len(names)==len(set(names))==args.passed and len(oldnames)==6603 and all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in set(oldnames)]==oldnames and [n for n in names if n not in set(oldnames)]==added and args.passed==6603+len(added)
assert set(added)=={'runtime/nif-image-attachment-transactions-and-quiescence','runtime/code-modification-permission-writers-cancellation-and-isolation','host/BEAM-on-load-cleanup-fault-quiescence-and-retirement'}
integration=j(a/'integration-results.json');assert integration==j(r/'docs/validation/part-293/integration-results.json') and len(integration)==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==args.passed and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-295/tests.json'
closed44='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed44 and not (r/'docs/changelog/0045.md').exists()
entry=(a/'part295-changelog-draft.md').read_text(encoding='utf-8-sig');callback=Path(args.callback_summary).read_text(encoding='utf-8-sig').strip();assert callback and '@@' not in callback
entry=entry.replace('@@CALLBACK_CHANGE@@',callback);assert '@@' not in entry
volume=('# Detailed work changelog — volume 0045\n\n'+entry).replace('\r\n','\n').replace('\n','\r\n').encode();assert len(volume)<=32768 and len(volume.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts289–294 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part295:' not in b and re.search(br'\r?\n\r?\n',b)
attrs=r/'.gitattributes';assert b'docs/validation/part-295/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert src.is_file() and dst.is_relative_to(o) and '__pycache__' not in src.parts and 'LICENSE.local.txt' not in src.parts
 b=src.read_bytes()
 if expected:assert h(b)==expected
 if size is not None:assert len(b)==size
 if dst in copies:assert copies[dst][1]==h(b)
 copies[dst]=(src,h(b))
def manifest(path):
 for x in j(path):select(r/x['SourcePath'],r/x['Destination'],x['Sha256'],x['Bytes'])
 select(path,o/'staging-manifests'/path.name)
original293=j(a/'pack293-original-38702943830-independent-guard.json')
assert original293['Status']=='PASS_EXACT_HEAD293_ORIGINAL5846_AND_FD8' and original293['Head']=='dd5208e900fa780bbfde8ef66a428371760dbb7d' and original293['Passed']==5846 and original293['Failed']==0 and original293['FdPassed']==8 and original293['Historical4903Module778Expression165DiagnosticRowsExact'] and original293['SixExclusionsContextPolicyExact']
manifest(a/'pack293-original-detached-staging-manifest-for295.json')
ci294_verified=False;pressure294_archived=False
if (a/'pack294-ci-detached-staging-manifest.json').exists():
 ci294=j(a/'pack294-ci-38705195593-independent-guard.json');assert ci294['Status']=='PASS_EXACT_HEAD294_CI6603_FULL' and ci294['Head']==parent and ci294['Passed']==6603 and ci294['Failed']==0 and ci294['OrderedNamesExact'] and len(ci294['Integration'])==11;ci294_verified=True
for optional in ['pack294-ci-detached-staging-manifest.json','pack294-pressure-detached-staging-manifest.json']:
 if (a/optional).exists():
  manifest(a/optional)
  if optional=='pack294-pressure-detached-staging-manifest.json':
   pressure=j(a/'pack294-pressure-38705231791-independent-guard.json');assert pressure['Status']=='PASS_EXACT_HEAD294_ORIGINAL_THREE_TYPED_PRESSURE_OBSERVATIONS' and pressure['Head']==parent and pressure['CasesPassed']==3 and pressure['FreshScenarioVms']==3 and pressure['PinnedOriginalProbeCompiled'] and not pressure['SdkHardBudgetEqualityClaimed'] and not pressure['GeneralDistributionCompatibility'];pressure294_archived=True
for name in ['root-part294-pressure-dispatch-attempt.json','root-part294-pressure-dispatch-result.json','root-dispatch294-pressure.ps1']:select(a/name,o/'previous-pressure294-dispatch'/name)
for src in sorted(a.glob('root-part295-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for src in [Path(args.full_exit),Path(args.added_tests),Path(args.callback_summary)]:select(src,o/'root-artifacts'/src.name)
for name in ['part295-changelog-draft.md','part295-callback-summary-proposal.md','part295-readiness-skeleton.py','part295-finalize-skeleton.py','part295-stage-skeleton.py','root-run295-full.ps1']:select(a/name,o/'root-artifacts'/name)
for src in sorted((a/'onload-callback-fault295-overlay').rglob('*')):
 if src.is_file() and not any(part in ['bin','obj','__pycache__'] for part in src.relative_to(a/'onload-callback-fault295-overlay').parts):select(src,o/'callback-overlay-provenance'/src.relative_to(a/'onload-callback-fault295-overlay'))
for src in sorted((a/'root-part295-first-full-failure').iterdir()):
 if src.is_file():select(src,o/'first-full-failure'/src.name)
for name in ['Program.cs','LoaderFocus.csproj']:select(a/'root-part295-loader-focus'/name,o/'loader-focus-driver'/name)
for base in ['nif-image-foundation-candidate2','nif-runtime-integration-audit2']:
 for src in sorted((a/base).iterdir()):
  if src.is_file() and src.suffix in ['.md','.json','.py','.txt']:select(src,o/'source-provenance'/base/src.name)
for base in ['root-nif-image292-isolated','root-nif-image293-isolated','root-nif-image295-isolated']:
 for src in sorted((a/base).iterdir()):
  if src.is_file() and src.suffix in ['.log','.json']:select(src,o/'isolated-history'/base/src.name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
(r/'docs/changelog/0045.md').write_bytes(volume);index.write_bytes(ib.replace(label,'parts289–294 (closed)'.encode())+'\r\n- [0045.md](docs/changelog/0045.md) — part295 (active)\r\n'.encode())
note=f'Part295: actual Main{args.passed}/full retains all6603 historical ordered tests;{args.source_count} token-bound source paths. Dormant lazy image attachments and17 serialized code-modification writer entries plus permanent callback cleanup HOST are managed prerequisites; no load_nif/native dispatch. Registry502/erlang344 of351 presence unchanged. Original2935846/fd8 archived; previous294 CI/pressure verification is explicit in receipt. New same-head CI/changed-source original remain pending. Full native lifetime/physical/upstream/performance parity unfinished.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-295/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed44
receipt={'Status':'ACTUAL_MAIN_FOUNDATION_AND_CALLBACK_FULL_PASS_NEW_NATIVE_AND_CI_PENDING','ParentHead':parent,'Passed':args.passed,'Failed':0,'SourcePaths':args.source_count,'Historical6603OrderedNamesExact':True,'Added':added,'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'Original293Passed':5846,'Original293FdPassed':8,'ChangedSourceOriginalVerified':False,'SameHeadCI295Verified':False,'LoadNifImplemented':False,'NativeDispatchInstalled':False,'Closed0044Sha256':closed44,'WholeCompatibility':False}
receipt['CI294Verified']=ci294_verified;receipt['Pressure294EvidenceArchived']=pressure294_archived
receipt['Pressure294TypedCasesPassed']=3 if pressure294_archived else None
receipt['Pressure294SemanticPassClaimedByThisFinalizer']=pressure294_archived
receipt['Pressure294Scope']='Three bounded original observations; no native versus SDK hard-budget equality or general distribution parity'
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':args.passed,'SourcePaths':args.source_count,'ProofFiles':len(receipt['Files']),'NewVolumeBytes':len(volume)}))
