from pathlib import Path
import hashlib,json,re,sys
r=Path.cwd().resolve();d=r/'artifacts/root-part317-main1';control=r/'artifacts/root-part317-docs1';out=r/'docs/validation/part-317'
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
full=read(d/'full-guard.json');native_path=r/'artifacts/root-part317-main-windows-native1/guard.json';native=read(native_path)
assert full['Passed']==6667 and full['Failed']==0 and full['IntegrationResults']==11 and full['Registry']==509 and full['ErlangMfas']==351
assert native['FullPassed']==6667 and native['SourceFreezeSha256']==h((d/'source-freeze.json').read_bytes()) and native['SourceFilesRehashed']==2260
assert h((d/'source-freeze.json').read_bytes())=='EE1516A086A0DE54C8A1CF12656DB45A13C157807349739B07BDEDB500DE1CD8'
assert h((r/'docs/changelog/0047.md').read_bytes())=='E4DDE5A0BA2BB6F8CDF6807391307E3AB708D9B8917D1A368108D91A3B27FF74'
for e in read(d/'source-resource-closure.json')['Files']:assert h((r/e['Path']).read_bytes())==e['Sha256'],e['Path']
note='Part317: Public memory/0,1 report the measured CLR SDK-observed payload profile within its loaded type universe: unique weakly tracked atom-string/binary/code-array backing identities, actual process-slot/mailbox payloads and joined native backing-request extents. Both APIs preserve ordered nine-key selection, duplicates and pinned error precedence. No invented GC/physical partition is used; ETS has no storage implementation, and CLR headers, unrelated application heaps, JIT and ERTS allocator-byte parity remain outside this profile. Actual Main outside-sandbox validation passed6667 tests and eleven integration/package checks; five isolated HOST groups and eleven generated SDK observations passed. The restricted first full6651/6667 preserved16 existing native-port EACCES failures; same-DLL probes prove both named-pipe modes fail with Win32 error5 inside this sandbox and pass outside it, with no source/expectation change for rerun. A first CS1673 build failure is preserved; an explicit ordered loop corrects readonly-struct lambda capture. Registry509 default/511 optionalIO and erlang351/351 represent API presence only. Full semantics, connected distribution, native ABI/upstream/performance remain incomplete. Current317 original/remote checks are pending publication; the whole erlang goal continues.'
scope_path=r/'docs/module-readiness-scope.json';scope=read(scope_path);runtime=next(e for e in scope['Components'] if e['Id']=='Runtime')
if sys.argv[1]=='scope':
 assert not control.exists() and scope['ComparisonTestReport']=='validation/part-315/final-main/tests.json' and note not in runtime['Implemented']
 control.mkdir();(control/'runtime-note.txt').write_bytes((note+'\n').encode());(control/'scope-before.json').write_bytes(scope_path.read_bytes())
 runtime['Implemented'].append(note);scope['ComparisonTestReport']='validation/part-316/final-main/tests.json';write(scope_path,scope)
 write(control/'scope-command.json',{'BeforeSha256':h((control/'scope-before.json').read_bytes()),'AfterSha256':h(scope_path.read_bytes()),'NoteSha256':h((control/'runtime-note.txt').read_bytes())});print('Scope317 updated; actual readiness CLI required.');sys.exit(0)
assert sys.argv[1]=='finalize' and not out.exists() and runtime['Implemented'].count(note)==1
assert read(d/'readiness-exit.json')['ExitCode']==0
ready=read(r/'docs/module-readiness.json');assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'],ready['ComparisonTestReport'])==(6667,509,507,2,'validation/part-316/final-main/tests.json')
entry_path=control/'english-entry.md';entry=entry_path.read_text(encoding='utf-8').strip();assert entry.startswith('## Part 317') and len(entry.encode())<=32768 and len(entry.splitlines())<=400
files=[];targets=set()
def add(source,destination):
 source=(r/source).resolve();target=(out/destination).resolve();assert source.is_relative_to(r) and target.is_relative_to(out) and target not in targets and source.is_file()
 raw=source.read_bytes();assert len(raw)<12*1024*1024;targets.add(target);files.append({'SourcePath':source.relative_to(r).as_posix(),'Destination':target.relative_to(r).as_posix(),'Bytes':len(raw),'Sha256':h(raw)})
for name in ['source-freeze.json','source-resource-closure.json','full-guard.json','full-exit.json','full-validation.log','raw-supported-mfas.json','readiness-exit.json','readiness-cli.log']:
 add('artifacts/root-part317-main1/'+name,'final-main/'+name)
for name in ['tests-first-failed.json','full-validation-first-failed.log','full-exit-first-failed.json','raw-supported-mfas-first-failed.json','full-outside-sandbox-attempt.json']:
 add('artifacts/root-part317-main1/'+name,'first-restricted-full/'+name)
for name in ['PipeDiagnostic.csproj','Program.cs','build.log','default-result.json','outside-sandbox-result.json']:
 add('artifacts/root-part317-pipe-diagnostic/'+name,'pipe-environment-diagnosis/'+name)
add('artifacts/tests.json','final-main/tests.json');add('artifacts/integration-results.json','final-main/integration-results.json')
for path in sorted(native_path.parent.iterdir()):
 if path.is_file() and path.suffix in {'.json','.stdout','.stderr'}:add(path.relative_to(r),'final-native/'+path.name)
add('docs/module-readiness.json','module-readiness.json')
for name in ['source-freeze.json','source-resource-closure.json','HANDOFF.md','published-parent-binding.json']:
 add('artifacts/clr-memory-public317-candidate1/'+name,'runtime-author/'+name)
for name in ['source-freeze.json','case-manifest.json']:
 add('artifacts/clr-memory-public317-oracle1/'+name,'oracle-author/'+name)
for name in ['design-freeze.json','DESIGN.md']:
 path=r/'artifacts/clr-memory-public317-design1'/name
 if path.exists():add(path.relative_to(r),'design/'+name)
for name in ['focus-source-freeze.json','focus-source-resource-closure.json','target-build-first-failed.log','target-build-corrected.log','actual-focus.json','managed-focus-build.log','managed-actual-results.json','managed-process.json','managed-stdout.log','managed-stderr.log']:
 add('artifacts/root-part317-composed1/'+name,'isolated-focus/'+name)
for name in ['final-source-freeze.json','final-source-resource-closure.json']:
 add('artifacts/root-part317-composed2/'+name,'final-candidate/'+name)
add('artifacts/root-part317-reseal1/receipt.json','reseal/receipt.json')
for name in ['proposal-freeze.json','configuration.json','reseal.py','source-closure-audit.json','explicit-all-source-peer.json']:
 add('artifacts/clr-memory-public317-plan-reseal-proposal1/'+name,'reseal/'+name)
for name in ['clr-memory-public317-beam310-source-peer.json','clr-memory-public317-oracle-beam310-source-peer.json','root-part317-final-beam310-source-peer.json','clr-memory-public317-beam310-pinned-relations.txt']:
 add('artifacts/'+name,'peer/'+name)
add('artifacts/part317-main-native-beam310-source-peer.json','peer/native-proposal-review.json')
add('artifacts/part317-finalization-source-peer2/review.json','peer/finalizer-source-review2.json')
for name in ['proposal-freeze.json','configuration.json','run317-main-native.py','guard317-main-native.py']:
 add('artifacts/part317-main-native-proposal1/'+name,'native-controls/'+name)
for name in ['root-compose317-runtime.py','root-compose317-oracle.py','root-compose317-final.py','root-freeze317-focus.py','root-freeze317-formatted.py','root-freeze317-resealed.py','root-run317-managed-focus.py','root-promote317.py','root-guard317-full.py','root-finalize317.py','root-run317-full-outside-sandbox.ps1','root-stage317.py']:
 add('artifacts/'+name,'root-controls/'+name)
previous_manifest=r/'artifacts/pack316-lean-terminal-copy-rows.json';assert h(previous_manifest.read_bytes())=='18571D0BDE8CC27DE04393B4FE9F2921EBD1D2C35A01B8D8E460B4C6645D48D1'
summary=r/'artifacts/pack316-terminal-independent-summary.json';assert h(summary.read_bytes())=='C5A73A25EACDBAD60A4687CBBE83D304E8349BC8F4225D81AC805C8772C60DA6'
previous=read(summary);assert previous['Head']=='6d7a430f55a43e488257f654e72a53b3b7f96ada' and previous['Ordinary']['Passed']==5870 and previous['Ordinary']['FdPassed']==8
for e in read(previous_manifest)['Rows']:
 raw=(r/e['SourcePath']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];target=(r/e['TargetPath']).resolve();assert target.is_relative_to(out);add(e['SourcePath'],target.relative_to(out))
add(previous_manifest.relative_to(r),'previous-head316/lean-copy-rows.json')
for name in ['runtime-note.txt','scope-command.json','scope-before.json','english-entry.md']:
 add('artifacts/root-part317-docs1/'+name,'documentation-controls/'+name)
for link in re.findall(r'\]\((\.\./validation/part-317/[^)]+)\)',entry):assert (r/'docs/changelog'/link).resolve() in targets|{out/'evidence-receipt.json',out/'evidence-index.json'},link
changes={};metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/OTP-PORTS.md']
for name in metadata:
 path=r/name;raw=path.read_bytes();assert b'Part317:' not in raw
 if name=='docs/ERLANG_COMPATIBILITY.md':
  for arity in [0,1]:
   before=f'| `memory/{arity}` | Missing; real implementation/runtime prerequisites required |'.encode();after=f'| `memory/{arity}` | Partial: measured CLR SDK payload profile; excludes ERTS allocator/physical bytes, JIT and absent ETS storage |'.encode();assert raw.count(before)==1;raw=raw.replace(before,after)
 nl=b'\r\n' if b'\r\n' in raw else b'\n';pos=raw.index(nl*2)+len(nl)*2;changes[path]=raw[:pos]+note.encode()+nl*2+raw[pos:]
index=r/'CHANGELOG.md';raw=index.read_bytes();before='- [0047.md](docs/changelog/0047.md) — parts312–316 (active)'.encode();assert raw.count(before)==1
changes[index]=raw.replace(before,'- [0047.md](docs/changelog/0047.md) — parts312–316 (closed)\n- [0048.md](docs/changelog/0048.md) — part317 (active)'.encode())
volume=r/'docs/changelog/0048.md';assert not volume.exists();changes[volume]=(entry+'\n').encode()
for e in files:
 source=r/e['SourcePath'];raw=source.read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];target=r/e['Destination'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
for path,raw in changes.items():path.write_bytes(raw)
write(out/'evidence-index.json',files)
receipt={'Status':'PASS_ACTUAL_MAIN317_FULL_NATIVE_READINESS_WITH_DECLARED_CLR_MEMORY_PROFILE','Passed':6667,'Failed':0,'Registry':509,'RegistryDelta':2,'ErlangPresence':351,'ErlangApiTotal':351,'ApiPresenceIsNotSemanticCompletion':True,'IntegrationResults':11,'SourcePaths':38,'CSharpPaths':33,'SourceClosurePaths':2260,'SourceFreezeSha256':h((d/'source-freeze.json').read_bytes()),'SourceClosureSha256':h((d/'source-resource-closure.json').read_bytes()),'EvidenceIndexSha256':h((out/'evidence-index.json').read_bytes()),'Original317ExecutionClaimed':False,'Current317RemotePending':True,'Previous316CiNativeAndNewReallocationAudited':True,'PhysicalMemoryParityClaimed':False,'PortableCompiledBinaryArchiveClaimed':False,'Closed0047Sha256':'E4DDE5A0BA2BB6F8CDF6807391307E3AB708D9B8917D1A368108D91A3B27FF74','Files':files}
write(out/'evidence-receipt.json',receipt);print(json.dumps({'Copies':len(files),'VolumeBytes':len(changes[volume]),'Registry':509,'ErlangPresence':351,'ReceiptSha256':h((out/'evidence-receipt.json').read_bytes())}))
