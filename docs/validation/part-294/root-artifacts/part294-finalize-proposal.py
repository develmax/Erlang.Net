"""Proposal only. Root supplies final freeze/tool gate and regenerated historical readiness.
No build, Main tests, git write, native execution or workflow dispatch is performed.
"""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--tool-gate',required=True);args=p.parse_args()
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-294';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='dd5208e900fa780bbfde8ef66a428371760dbb7d';assert git('rev-parse','HEAD').decode().strip()==parent
assert not o.exists()
f=j(a/'root-part294-final-source-freeze.json');assert f['ParentHead']==parent and len(f['Files'])==8 and f['TokensExact']
paths={x['Path'] for x in f['Files']};assert paths==set(j(a/'root-part294-source-paths.json'))
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert not git('diff','HEAD','--name-only','--','src','tests','Erlang.Net.slnx').strip()
assert set(git('diff','HEAD','--name-only','-z','--','tools','.github/workflows').decode().split('\0'))-{''}<=paths
assert j(a/'root-part294-tokens-before.json')==j(a/'root-part294-tokens-after.json')
gate=j(Path(args.tool_gate));assert gate['TargetExitCode']==0 and gate['CliPrewriteRefusalsPassed']==14 and gate['RawProcessControlsPassed']==2 and gate['TokensExact']
assert gate['ParentHead']==parent and gate['SourcePaths']==8 and not gate['HistoricalMain6603Repeated'] and not gate['OriginalPressureExecuted'] and not gate['WorkflowDispatched']
target=(a/'root-part294-tool-target.log').read_text(encoding='utf-8-sig');assert 'Предупреждений: 0' in target and 'Ошибок: 0' in target
cli=j(a/'root-part294-cli-controls/proof.json');raw=j(a/'root-part294-raw-controls/receipt.json')
assert cli['Passed']==len(cli['Cases'])==14 and not cli['NativeExecuted'] and cli['SyntheticNegativeEvidenceOnly']
assert all(x['ExitCode']==1 and x['ExpectedFailureTextPresent'] and x['NoOutputMutation'] and not x['NativeExecutableInvoked'] for x in cli['Cases'])
assert all(x['ExecutedDllSha256']==cli['ActualDllSha256'] for x in cli['Cases'])
assert raw['Status']=='PASS' and raw['Raw']=={'ExitCode':3,'TimedOut':False,'RawComplete':True}
assert raw['InheritedPipes']=={'ExitCode':0,'TimedOut':False,'RawComplete':False} and raw['InheritedElapsedMilliseconds']<raw['CaptureDeadlineSeconds']*1000
assert raw['Cleanup']['IdentityVerified'] and raw['Cleanup']['ChildExited'] and raw['Cleanup']['CaptureJoined'] and not raw['Cleanup']['CaptureFaulted'] and not raw['NativeOtpExecuted']
output=r/'tools/Erlang.DistributionPressureObservations/bin/Debug/net10.0';dll=output/'Erlang.DistributionPressureObservations.dll';assert h(dll.read_bytes())==cli['ActualDllSha256']==raw['Binding']['ActualAssemblySha256']
candidate=a/'distribution-pressure-original-candidate1';cf=j(candidate/'source-freeze.json');assert len(cf['SharedSourceLinks'])==13
for x in cf['SharedSourceLinks']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
ci=j(a/'pack293-ci-38702919281-independent-guard.json');assert ci['Head']==parent and ci['Passed']==6603 and ci['Failed']==0 and ci['OrderedNamesExact'] and ci['RegisteredMfas']==502 and len(ci['Integration'])==11
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6603 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0
scope=j(r/'docs/module-readiness-scope.json');assert scope['ComparisonTestReport']=='validation/part-293/tests.json';component=next(c for c in scope['Components'] if c['Id']=='PressureObservations');assert component['TestPrefixes']==[] and component['Status']=='Partially compatible'
closed='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1';assert h((r/'docs/changelog/0043.md').read_bytes())==closed
volume=r/'docs/changelog/0044.md';entry=(a/'part294-changelog-draft.md').read_text(encoding='utf-8-sig');combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode();assert b'## Part 294' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts289–293 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part294:' not in b and re.search(br'\r?\n\r?\n',b)
attrs=r/'.gitattributes';assert b'docs/validation/part-294/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert dst.is_relative_to(o) and src.is_file() and '__pycache__' not in src.parts and 'LICENSE.local.txt' not in src.parts
 b=src.read_bytes()
 if expected:assert h(b)==expected
 if size is not None:assert len(b)==size
 if dst in copies:assert copies[dst][1]==h(b)
 copies[dst]=(src,h(b))
def manifest(path):
 m=j(path)
 for row in m:select(r/row['SourcePath'],r/row['Destination'],row['Sha256'],row['Bytes'])
 select(path,o/'staging-manifests'/path.name)
manifest(a/'pack293-ci-detached-staging-manifest.json')
native_path=a/'pack293-original-38702943830-independent-guard.json';native_manifest=a/'pack293-original-detached-staging-manifest.json';native_verified=False
if native_path.exists() or native_manifest.exists():
 assert native_path.exists() and native_manifest.exists();ng=j(native_path);assert ng['Head']==parent and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8 and ng['Historical4903Module778Expression165DiagnosticRowsExact']
 manifest(native_manifest);native_verified=True
for src in sorted(a.glob('root-part294-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
select(Path(args.tool_gate),o/'root-artifacts'/Path(args.tool_gate).name)
for name in ['part294-changelog-draft.md','part294-readiness-proposal.py','part294-finalize-proposal.py','part294-stage-guard-proposal.py']:select(a/name,o/'root-artifacts'/name)
select(a/'root-pressure294-cli-controls.py',o/'cli-control-driver/root-pressure294-cli-controls.py')
for src in sorted(candidate.iterdir()):
 if src.is_file():select(src,o/'proposal-provenance'/src.name)
for src in sorted((a/'root-part294-raw-controls').rglob('*')):
 if src.is_file():select(src,o/'raw-controls'/src.relative_to(a/'root-part294-raw-controls'))
# Repeated synthetic evidence is byte-exact but content-addressed, with every path retained.
closure=[]
canonical_names={'Erlang.DistributionPressureObservations'+suffix for suffix in ['.dll','.exe','.pdb','.deps.json','.runtimeconfig.json']}
for src in sorted((a/'root-part294-cli-controls').rglob('*')):
 if src.is_file():
  relative=src.relative_to(a/'root-part294-cli-controls');b=src.read_bytes();digest=h(b)
  canonical=len(relative.parts)>2 and relative.parts[1]=='collector' and src.name in canonical_names
  if canonical:
   assert (output/src.name).read_bytes()==b;dest=o/'actual-tool-output'/src.name
  else:dest=o/'cli-controls/blobs'/digest;select(src,dest,digest)
  closure.append({'Path':relative.as_posix(),'Bytes':len(b),'Sha256':digest,'RepositoryBlob':dest.relative_to(r).as_posix(),'CanonicalToolOutput':canonical})
rf=a/'root-pressure294-raw-focus';rs=j(rf/'source-freeze.json');assert len(rs['Files'])==9
for x in rs['Files']:select(rf/x['Path'],o/'raw-harness-source'/x['Path'],x['Sha256'],x['Bytes'])
select(rf/'source-freeze.json',o/'raw-harness-source/source-freeze.json')
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/'historical293-readiness'/name)
output_closure=[]
for src in sorted(output.iterdir()):
 if src.is_file() and (src.suffix in ['.dll','.exe','.pdb','.json','.erl'] or src.name.endswith('.deps.json')):
  select(src,o/'actual-tool-output'/src.name)
  output_closure.append({'Path':src.relative_to(r).as_posix(),'Bytes':src.stat().st_size,'Sha256':h(src.read_bytes()),'RepositoryPath':'docs/validation/part-294/actual-tool-output/'+src.name})
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
(o/'cli-controls-closure.json').write_text(json.dumps(closure,indent=2)+'\n',encoding='utf-8')
(o/'actual-tool-output-closure.json').write_text(json.dumps(output_closure,indent=2)+'\n',encoding='utf-8')
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts289–294 (active)'.encode()))
note='Part294: standalone original pressure-observation collector source8/ownedCSharp2 builds with zero warnings/errors;14 actual Main-DLL pre-write refusals and two supplied-DLL raw/owned-child capture controls pass.13 existing source links preserved. No runtime/compiler/tests/solution/MFA change or repeated local Main full; historical2936603/readiness and exact-head CI6603/full reused. Original three pressure VMs and pinned probe compilation remain pending; ordinary293 status is explicit in the receipt and does not cover new pressure. Registry502 and erlang344/351 API presence unchanged; whole module/native pressure/physical/NIF/upstream/performance incomplete.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
ports=r/'docs/OTP-PORTS.md';assert b'## Part 294:' not in ports.read_bytes();ports.write_bytes(ports.read_bytes()+b'\n## Part 294: original pressure test infrastructure\n\nThe collector is C# and source-links existing process/build/queue helpers. Pinned OTP dist.c/erl_process.c guide independent observation contracts; native soft busy pressure and managed hard queue budgets remain distinct. Erlang is original-VM test input only. The manual workflow Python fragment records source-build provenance. These test-infrastructure languages add no production dependency or generic runtime implementation; original probe compilation and three observations remain pending.\n')
deps=r/'docs/dependency-decisions.md';deps.write_bytes(deps.read_bytes()+b'\nPart294: standalone C# pressure observer reuses13 existing source files and BCL; no new NuGet dependency. Erlang original-VM test input and workflow Python build-provenance fragment are test-infrastructure exceptions, not production runtime implementations. No solution/public package/license-local publication.\n')
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-294/** binary\n')
receipt={'Status':'STANDALONE_PRESSURE_OBSERVER_TARGET_CLI14_RAW2_PASS_NATIVE3_PENDING','ParentHead':parent,'SourcePaths':8,'OwnedCSharpFiles':2,'SharedSourceLinks':13,'CliRefusalsPassed':14,'RawControlsPassed':2,'ExecutedToolDllSha256':cli['ActualDllSha256'],'Historical293MainPassed':6603,'CI293Passed':6603,'Original293Verified':native_verified,'Original293Passed':5846 if native_verified else None,'Original293FdPassed':8 if native_verified else None,'NativePressureThreeExecuted':False,'PinnedProbeCompiled':False,'NewMainFullRepeated':False,'RuntimeCompilerTestsChanged':False,'Registry':502,'ErlangPresence':344,'ErlangReference':351,'WholeCompatibility':False,'Closed0043Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');assert h((r/'docs/changelog/0043.md').read_bytes())==closed;print(json.dumps({'SourcePaths':8,'ProofFiles':len(receipt['Files']),'Original293Verified':native_verified,'NativePressureThreeExecuted':False,'ChangelogBytes':len(combined)}))
