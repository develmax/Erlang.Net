from pathlib import Path
import json,hashlib,re,sys
r=Path.cwd().resolve();art=r/'artifacts';p=art/'root-part316-main2';out=r/'docs/validation/part-316';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
config_path=Path(sys.argv[2]).resolve();cfg=read(config_path);assert cfg['Part']==316 and cfg['RootApprovedForExecution'] is True
freeze=p/'source-freeze.json';closure=p/'source-resource-closure.json';assert h(freeze.read_bytes())=='57F9F381B91AECFDA6634167A659B6C6C9C0A9147ED0AF9BCEDA9F1B3AD7680C';assert h(closure.read_bytes())=='87E2A60C038EC320EE9F91E9C1A3E7387E250D5EA5EA4AD293D717D973DF4691'
for e in read(closure)['Files']:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
full=p/'full-guard.json';native=art/'root-part316-main-windows-native1/guard.json';assert h(full.read_bytes())==cfg['ActualFullGuardSha256'] and h(native.read_bytes())==cfg['ActualNativeGuardSha256']
f=read(full);n=read(native);assert f['Passed']==6654 and f['Failed']==0 and f['Registry']==507 and f['ErlangMfas']==349 and f['PublicMfaDelta']==2 and f['IntegrationResults']==11
assert n['FullPassed']==6654 and n['SourceFreezeSha256']==h(freeze.read_bytes()) and n['SourceFilesRehashed']==2241
assert read(p/'full-exit.json')['ExitCode']==0
note_path=r/cfg['RuntimeNotePath'];note=note_path.read_text(encoding='utf-8').strip();assert note.startswith('Part316:') and h(note_path.read_bytes())==cfg['RuntimeNoteSha256']
scope_path=r/'docs/module-readiness-scope.json';scope=read(scope_path);runtime=[e for e in scope['Components'] if e['Id']=='Runtime'];assert len(runtime)==1
assert h((r/'docs/changelog/0046.md').read_bytes())=='CAF1B30E0592EF5502A0642631256E3B4F4A5AD8201D96BBB234DA6F2B163361'
if sys.argv[1]=='scope':
 assert scope['ComparisonTestReport']=='validation/part-314/final-main/tests.json' and note not in runtime[0]['Implemented'] and not (p/'readiness-scope-before.json').exists()
 before=scope_path.read_bytes();runtime[0]['Implemented'].append(note);scope['ComparisonTestReport']='validation/part-315/final-main/tests.json';(p/'readiness-scope-before.json').write_bytes(before);write(scope_path,scope);write(p/'readiness-scope-command.json',{'BeforeSha256':h(before),'AfterSha256':h(scope_path.read_bytes()),'RuntimeNoteSha256':h(note_path.read_bytes())});print('Scope316 updated; actual readiness CLI required.');sys.exit(0)
assert sys.argv[1]=='finalize' and not out.exists()
assert cfg['ActualReadinessExitCode']==read(p/'readiness-exit.json')['ExitCode']==0
marker=read(p/'readiness-scope-command.json');assert marker['AfterSha256']==h(scope_path.read_bytes()) and marker['RuntimeNoteSha256']==h(note_path.read_bytes()) and runtime[0]['Implemented'].count(note)==1
ready=read(r/'docs/module-readiness.json');assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'],ready['ComparisonTestReport'])==(6654,507,505,2,'validation/part-315/final-main/tests.json')
entry_path=r/cfg['EnglishEntryPath'];entry=entry_path.read_text(encoding='utf-8').strip();assert h(entry_path.read_bytes())==cfg['EnglishEntrySha256'] and entry.startswith('## Part 316') and '2026-10-16' in entry and '**Final Main316 gates are pending.' not in entry
assert h((art/'pack315-terminal-independent-summary.json').read_bytes())=='97605D4F5FA324CD42E1E6D0CEE77B7F34F3E2AA079BA3005514737E360E29A4'
files=cfg['Files'];targets=set();copies=[];reserved={out/n for n in ['evidence-index.json','evidence-receipt.json','final-bound-config.json']}
for e in files:
 source=(r/e['SourcePath']).resolve();target=(r/e['Destination']).resolve();assert source.is_relative_to(r) and target.is_relative_to(out) and target not in targets and target not in reserved;raw=source.read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'] and len(raw)<100*1024*1024;targets.add(target);copies.append((target,raw))
required={'final-main/tests.json','final-main/full-guard.json','final-main/source-freeze.json','final-main/source-resource-closure.json','final-native/guard.json','module-readiness.json'}
assert {out/n for n in required}<=targets
required_sources={'artifacts/pack315-terminal-independent-summary.json','artifacts/root-part316-main1/tests-first-failed.json','artifacts/root-part316-main1/full-validation.log','artifacts/root316-first-full-halt-count-beam310-review.json','artifacts/allocator-public-sys-profile316-count-correction1/correction.json'}
assert required_sources<={e['SourcePath'] for e in files}
first=read(art/'root-part316-main1/tests-first-failed.json');assert first['Passed']==6653 and first['Failed']==1
for link in re.findall(r'\]\((\.\./validation/part-316/[^)]+)\)',entry):assert (r/'docs/changelog'/link).resolve() in targets|reserved,link
volume=r/'docs/changelog/0047.md';old=volume.read_bytes();assert h(old)==cfg['VolumeBeforeSha256'] and b'## Part 316' not in old;nl=b'\r\n' if b'\r\n' in old else b'\n';combined=old.rstrip(b'\r\n')+nl*2+entry.replace('\r\n','\n').replace('\n',nl.decode()).encode()+nl;assert len(combined)<=32768 and len(combined.splitlines())<=400
changes={volume:combined}
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/OTP-PORTS.md']
for name in metadata:
 path=r/name;raw=path.read_bytes();assert h(raw)==cfg['MetadataBefore'][name] and b'Part316:' not in raw
 if name=='docs/ERLANG_COMPATIBILITY.md':
  for function in ['alloc_info','alloc_sizes']:
   before=('| `'+function+'/1` | Missing; real implementation/runtime prerequisites required |').encode();after=('| `'+function+'/1` | Partial: CLR sys_alloc metadata; unsupported allocator profiles raise notsup; full ERTS sizes/options remain unsupported |').encode();assert raw.count(before)==1;raw=raw.replace(before,after)
 nl=b'\r\n' if b'\r\n' in raw else b'\n';pos=raw.index(nl*2)+len(nl)*2;changes[path]=raw[:pos]+note.encode()+nl*2+raw[pos:]
index=r/'CHANGELOG.md';raw=index.read_bytes();assert h(raw)==cfg['ChangelogBeforeSha256'] and raw.count('parts312–315 (active)'.encode())==1;changes[index]=raw.replace('parts312–315 (active)'.encode(),'parts312–316 (active)'.encode())
for target,raw in copies:target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
for target,raw in changes.items():target.write_bytes(raw)
write(out/'evidence-index.json',files);(out/'final-bound-config.json').write_bytes(config_path.read_bytes())
receipt={'Status':'PASS_ACTUAL_MAIN316_FULL_NATIVE_READINESS_WITH_PUBLIC_CLR_SYS_ALLOC_PROFILE','Passed':6654,'Failed':0,'Registry':507,'RegistryDelta':2,'ErlangPresence':349,'IntegrationResults':11,'SourcePaths':26,'CSharpPaths':19,'SourceClosurePaths':2241,'SourceFreezeSha256':h(freeze.read_bytes()),'SourceClosureSha256':h(closure.read_bytes()),'EvidenceIndexSha256':h((out/'evidence-index.json').read_bytes()),'ConfigSha256':h(config_path.read_bytes()),'ComparisonTestReport':ready['ComparisonTestReport'],'FirstFullFailurePreserved':True,'Current316RemotePending':True,'Previous315AllFourTerminalAudited':True,'Previous315ReallocationImportFailurePreserved':True,'NewOriginalReallocationCompared':False,'NewOriginalPublicAllocatorCasesCompared':False,'DeclaredClrBackendAdaptation':True,'PhysicalMemoryParityClaimed':False,'PortableCompiledBinaryArchiveClaimed':False,'Files':files};write(out/'evidence-receipt.json',receipt);print(json.dumps({'Copies':len(files),'Registry':507,'ErlangPresence':349,'VolumeBytes':len(combined)}))
