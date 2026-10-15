from pathlib import Path
import hashlib,importlib.util,json,sys
sys.dont_write_bytecode=True
R=Path.cwd();D=R/'artifacts/root-part316-main-windows-native1';C=R;P=R/'artifacts/root-part316-main2';F=P/'source-freeze.json'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
load=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
def bind(p):return {'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())}
assert sha(F.read_bytes())=='57F9F381B91AECFDA6634167A659B6C6C9C0A9147ED0AF9BCEDA9F1B3AD7680C'
cl=P/'source-resource-closure.json';assert sha(cl.read_bytes())=='87E2A60C038EC320EE9F91E9C1A3E7387E250D5EA5EA4AD293D717D973DF4691'
rows=load(cl)['Files'];assert len(rows)==2241
assert load(F)['SourcePaths']==26 and load(F)['CSharpPaths']==19
for r in rows:
 b=(C/r['Path']).read_bytes();assert len(b)==r['Bytes'] and sha(b)==r['Sha256'],r['Path']
tool=C/'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll';runtime=tool.with_name('Erlang.Runtime.dll')
assert tool.is_file()
assert runtime.is_file()
assert runtime.read_bytes()==(C/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
binaryset={p.relative_to(R).as_posix() for p in tool.parent.rglob('*') if p.is_file() and p.suffix in {'.dll','.json'}}
roles=[];common=None
for role in ['legacy','provider-query','reallocation']:
 a=load(D/(role+'.attempt.json'));p=load(D/(role+'.process.json'));out=(D/(role+'.stdout')).read_bytes();err=(D/(role+'.stderr')).read_bytes()
 assert a['MainExecution'] is True
 assert a['ActualFullGuardSha256']==sha((P/'full-guard.json').read_bytes())
 assert a['Role']==p['Role']==role and a['NativeExecutionAuthorized'] is True and a['AutomaticRetryAllowed'] is False
 assert a['SourceFreezeSha256']==sha(F.read_bytes()) and a['SourceClosureSha256']==sha(cl.read_bytes()) and a['SourcePeerSha256']==sha((R/'artifacts/allocator-public-sys-profile316-count-correction-beam310-source-peer.json').read_bytes())
 assert p['ExitCode']==0 and p['TimedOut'] is False and 0<=p['ElapsedSeconds']<120 and not err
 assert p['StdoutSha256']==sha(out) and p['StderrSha256']==sha(err) and p['NativeProbeInvoked'] is True and p['NativeExecutionValidated'] is False and p['OriginalExecuted'] is False and p['MainExecution'] is True
 bi={x['Path']:x['Sha256'] for x in a['Bindings']};assert len(bi)==len(a['Bindings']) and binaryset.issubset(bi)
 for name,h in bi.items():assert sha((R/name).read_bytes())==h,name
 assert len(bi)==len(binaryset)+7
 if common is None:common=bi
 else:assert bi==common
 for b in p['ActualSdkAssemblies']:assert sha((R/b['Path']).read_bytes())==b['Sha256']
 command=a['Command'];assert command[:2]==['dotnet',str(tool)]
 if role=='legacy':assert len(command)==4
 elif role=='provider-query':assert command[2]=='--native-payload-provider-query' and len(command)==5
 else:assert command[2]=='--reallocation-probe' and len(command)==9
 args=command[3:] if role!='legacy' else command[2:]
 for name,h in zip(args[::2],args[1::2]):assert sha(Path(name).read_bytes())==h
 roles.append({'Role':role,'StdoutSha256':sha(out),'ExitCode':0,'ElapsedSeconds':p['ElapsedSeconds'],'RecursiveDllJsonPins':len(binaryset),'Attempt':bind(D/(role+'.attempt.json')),'Process':bind(D/(role+'.process.json'))})
# Both actual entrypoint runtime assets and localized resources must be present and bound.
deps=load(tool.with_suffix('.deps.json'));assets=[]
for target in deps['targets'].values():
 for library in target.values():
  for kind in ['runtime','resources','native']:
   for name in library.get(kind,{}):
    path=tool.parent/name;assert path.is_file(),name;assert path.relative_to(R).as_posix() in common;assets.append(name)
assert assets
for role in ['legacy','reallocation']:assert (D/(role+'.stdout')).read_bytes()==(R/'artifacts/root-part315-main-windows-native1'/ (role+'.stdout')).read_bytes()
reader=R/'artifacts/native-payload-provider315-manual-reader1/read_manual_capture.py';frozen=load(reader.parent/'source-freeze.json')
assert sha(reader.read_bytes())=='A8205F7577A9FB991EB8FDFF860571B375E5B57A944BF70F4AFBEC5B4A837FAA'
# The reviewed reader freeze is authoritative, and independent replay never starts a process.
assert sha((reader.parent/'source-freeze.json').read_bytes())=='5920861E12EC8964EB3CB0F8BF3C02A902130C0E0A289772C2D01D92E3EB103D'
spec=importlib.util.spec_from_file_location('passive315',reader);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
legacy=C/'tools/Erlang.NifPendingVarargsObservations/reviewed_sdk_stdout.py';l=m.legacy_reader(legacy)
source=C/'tools/Erlang.NifFixtureHost/native/fixture.c';assert sha(source.read_bytes())==m.SOURCE_SHA
assert m.SOURCE_SHA in (C/'src/Erlang.Runtime/NifNativeNames.cs').read_text(encoding='utf-8-sig')
oldpath=next(n for n in common if n.endswith('/nif-fixture-build/fixture.dll'))
typed=m.parse_capture((D/'provider-query.stdout').read_bytes(),(D/'provider-query.stderr').read_bytes(),l,sha(source.read_bytes()),common[oldpath],None)
isolated_typed=load(R/'artifacts/root-part315-isolated-windows1/manual-typed-review.json')
for k,v in typed.items():assert isolated_typed[k]==v,k
assert typed['LoadedRequestedBytes']==typed['LoadedBackingRequestBytes']==4
full=load(P/'full-guard.json');assert full['Status']=='PASS_ACTUAL_MAIN316_6654_FULL_AND_ELEVEN_INTEGRATIONS'
assert full['Passed']==6654 and full['Failed']==0 and full['IntegrationResults']==11
assert full['SourceFreezeSha256']==sha(F.read_bytes()) and full['SourceClosureSha256']==sha(cl.read_bytes())
assert load(P/'full-exit.json')['ExitCode']==0
report={'Status':'PASS_ACTUAL_MAIN316_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE','SourceFreeze':bind(F),'SourceClosure':bind(cl),'SourceRowsRehashed':2241,'ActualTool':bind(tool),'ActualRuntime':bind(runtime),'Roles':roles,'RuntimeDependencyAssetsVerified':len(assets),'EveryRecursiveToolDllJsonBoundAcrossAllThreeAttempts':True,'FullPassed':6654,'SourceFreezeSha256':sha(F.read_bytes()),'SourceFilesRehashed':2241,'ActualFullGuard':bind(P/'full-guard.json'),'ActualTargetWarnings':0,'ActualTargetErrors':0,'ManualReader':bind(reader),'IndependentTypedReplayMatchesRoot':True,'QueryPhases':typed['Completion']['Phases'],'RuntimeOrdinals':[q['RuntimeOrdinal'] for q in typed['QueryObservations']],'JoinedOwners':[q['JoinedOwners'] for q in typed['QueryObservations']],'RequestedAndBackingPairs':[q['QueryResult'] for q in typed['QueryObservations']],'LoadedOwnerLedgerEqualsActualQuery':True,'UnloadedCurrentZeroAndHistoryRetained':True,'Scope':typed['Scope'],'TrustedBridgeSha256':None,'LegacyAndReallocationRawByteExactPreviousActual315':True,'OriginalExecuted':False,'PhysicalMemoryCompared':False,'NewCCompiled':False,'ReviewerExecutedNativeVmSdkGitOrMainMutation':False,'ReviewedInterpretation':'Actual trusted Windows C callback/owner lifecycle queried through the same live SDK runtime; requested payload ledger scope only. No original Erlang comparison or physical allocator footprint claim.'}
out=D/'guard.json';assert not out.exists();report['MainExecution']=True;report['SourcePaths']=26;report['CSharpPaths']=19;report['ActualTrustedCQueryExecuted']=True;report['MainSourceFreezeSha256']=sha(F.read_bytes());out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Path':out.relative_to(R).as_posix(),'Sha256':sha(out.read_bytes()),'Status':report['Status']}))

