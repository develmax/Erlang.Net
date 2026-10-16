from pathlib import Path
import hashlib,importlib.util,json,sys
sys.dont_write_bytecode=True
import argparse
parser = argparse.ArgumentParser(description='Reviewed post-full Main Windows trusted probes with explicit source/count authority; no build or original VM.')
parser.add_argument('--main-proof', required=True, type=Path)
parser.add_argument('--part', required=True, type=int)
parser.add_argument('--expected-full-passed', required=True, type=int)
parser.add_argument('--expected-integration-results', required=True, type=int)
parser.add_argument('--legacy-input-count', required=True, type=int)
parser.add_argument('--reallocation-input-count', required=True, type=int)
parser.add_argument('--main-freeze-sha256', required=True)
parser.add_argument('--full-guard-sha256', required=True)
parser.add_argument('--source-peer', required=True, type=Path)
parser.add_argument('--source-peer-sha256', required=True)
parser.add_argument('--tool-sha256', required=True)
parser.add_argument('--runtime-sha256', required=True)
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--tested-freeze', required=True, type=Path)
parser.add_argument('--tested-freeze-sha256',required=True)
parser.add_argument('--tested-actual-guard', required=True, type=Path)
parser.add_argument('--tested-actual-guard-sha256',required=True)
parser.add_argument('--main-composition-peer', required=True, type=Path)
parser.add_argument('--main-composition-peer-sha256',required=True)
parser.add_argument('--closure-sha256',required=True)
args = parser.parse_args()

R=Path.cwd().resolve();D=args.output.resolve();C=R;P=args.main_proof.resolve();F=P/'source-freeze.json'
assert D.is_relative_to(R/'artifacts') and D!=P and D.is_dir() and P.is_relative_to(R/'artifacts')
assert args.part >= 331 and args.expected_full_passed > 0 and args.expected_integration_results > 0
peer=args.source_peer.resolve();assert peer.is_relative_to(R/'artifacts')
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
load=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))

# Fixed review inputs are passive files; no imported capture runner can execute.
import re
CONFIGURATION = Path(__file__).resolve().parent / 'configuration.json'
assert sha(CONFIGURATION.read_bytes()) == '8742307F6361D8ABD53B29FA0E23E9979B0BA767DB6EE4703FB70F357A239857'
CONFIG = load(CONFIGURATION)
assert re.fullmatch('[A-Fa-f0-9]{64}',args.tested_freeze_sha256) and re.fullmatch('[A-Fa-f0-9]{64}',args.closure_sha256)
CONFIG['TestedFreezeSha256']=args.tested_freeze_sha256.upper()
CONFIG['MainClosureSha256']=args.closure_sha256.upper()
MAIN_FREEZE=load(F)
MAIN_PLANS=MAIN_FREEZE['Plans']
assert len(MAIN_PLANS)==2 and {x['Path'] for x in MAIN_PLANS}=={'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
EXPECTED_INPUTS={'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json':args.legacy_input_count,'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json':args.reallocation_input_count}
assert all(value > 0 for value in EXPECTED_INPUTS.values())
assert {item['Path']:item['Inputs'] for item in MAIN_PLANS}==EXPECTED_INPUTS

assert re.fullmatch('[A-Fa-f0-9]{64}',args.main_freeze_sha256) and re.fullmatch('[A-Fa-f0-9]{64}',args.source_peer_sha256)
tested_freeze = args.tested_freeze.resolve()
tested_guard = args.tested_actual_guard.resolve()
composition_peer = args.main_composition_peer.resolve()
for authority in (tested_freeze, tested_guard, composition_peer):
    assert authority.is_relative_to(R/'artifacts')

def independent_inputs_exact():
    assert sha(tested_freeze.read_bytes()) == args.tested_freeze_sha256.upper() == MAIN_FREEZE['TestedFreezeSha256']
    assert sha(tested_guard.read_bytes()) == args.tested_actual_guard_sha256.upper() == MAIN_FREEZE['TestedActualGuardSha256']
    assert sha(peer.read_bytes()) == args.source_peer_sha256.upper() == MAIN_FREEZE['IndependentSourcePeerSha256']
    assert sha(composition_peer.read_bytes()) == args.main_composition_peer_sha256.upper()
    composition = load(composition_peer)
    assert composition['Status'] == 'PASS_INDEPENDENT_ACTUAL_MAIN331_COMPOSITION_AND_CORRECTED_FULL_GUARD_SOURCE'
    assert composition['SourceClosureRowsRehashed'] == MAIN_FREEZE['SourceClosurePaths']
    assert composition['OwnedSourcePaths'] == MAIN_FREEZE['SourcePaths']
    assert composition['OwnedCSharpFiles'] == MAIN_FREEZE['CSharpPaths']
    assert composition['All15MainCSharpRowsExactActuallyTestedFreeze'] is True
    assert composition['TestedFreezeSha256'] == MAIN_FREEZE['TestedFreezeSha256']
    assert composition['TestedActualGuardSha256'] == MAIN_FREEZE['TestedActualGuardSha256']
    assert composition['OriginalSourcePeerSha256'] == MAIN_FREEZE['IndependentSourcePeerSha256']
    assert composition['SourceFreezeSha256'] == args.main_freeze_sha256.upper()
    assert composition['SourceClosureSha256'] == args.closure_sha256.upper() == MAIN_FREEZE['SourceClosureSha256']
    implementation = load(peer)
    assert str(implementation['Status']).startswith('PASS') and implementation['SourceFreezeSha256'] == MAIN_FREEZE['TestedFreezeSha256']
    actual = load(tested_guard)
    assert actual['Passed'] == MAIN_FREEZE['ExpectedTests'] == args.expected_full_passed and actual['Failed'] == 0
    assert actual['FormattedSourceFreezeSha256'] == MAIN_FREEZE['TestedFreezeSha256'] and actual['All6747PreviousOrderedTestNamesRetained'] is True
    assert actual['NewCompiledScenarios'] == 21 and actual['NewDirectMfaContracts'] == 6 and actual['NewHostGroups'] == 1
    tested = load(tested_freeze)
    assert len(tested['Files']) == MAIN_FREEZE['CSharpPaths'] == 15
    frozen_main = {row['Path']:row for row in MAIN_FREEZE['Files']}
    for row in tested['Files']:
        assert frozen_main[row['Path']] == row
        raw = (R/row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']

def references_exact():
    independent_inputs_exact()
    for item in CONFIG['References']:
        raw = (R / item['Path']).read_bytes()
        assert len(raw) == item['Bytes'] and sha(raw) == item['Sha256'], item['Path']
    for item in MAIN_PLANS:
        assert len(json.loads((R/item['Path']).read_bytes())['Files'])==EXPECTED_INPUTS[item['Path']]
        raw = (R / item['Path']).read_bytes()
        assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf') and sha(raw) == item['Sha256']
        workflow = (R / item['Workflow']).read_bytes().replace(b'\r\n', b'\n')
        pins = re.findall(rb'(?m)^\s*' + item['Pin'].encode() + rb': ([A-F0-9]{64})$', workflow)
        assert pins == [item['Sha256'].encode()]
references_exact()
assert CONFIG['IndependentPeerRole']=='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW'
def bind(p):return {'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())}
assert sha(F.read_bytes())==args.main_freeze_sha256.upper()
assert args.source_peer_sha256.upper() == load(F)['IndependentSourcePeerSha256']
cl=P/'source-resource-closure.json';assert sha(cl.read_bytes())==args.closure_sha256.upper()
rows=load(cl)['Files'];assert len(rows)==load(F)['SourceClosurePaths']
assert load(F)['SourcePaths']==len(load(F)['Files']) and load(F)['CSharpPaths']==sum(x['Path'].endswith('.cs') for x in load(F)['Files'])
assert sha(peer.read_bytes())==args.source_peer_sha256.upper()
independent_inputs_exact()
for r in rows:
 b=(C/r['Path']).read_bytes();assert len(b)==r['Bytes'] and sha(b)==r['Sha256'],r['Path']
tool=C/'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll';runtime=tool.with_name('Erlang.Runtime.dll')
assert tool.is_file() and sha(tool.read_bytes())==args.tool_sha256.upper()
assert runtime.is_file() and sha(runtime.read_bytes())==args.runtime_sha256.upper()
assert runtime.read_bytes()==(C/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
binaryset={p.relative_to(R).as_posix() for p in tool.parent.rglob('*') if p.is_file() and p.suffix in {'.dll','.json'}}
roles=[];common=None
for role in ['legacy','provider-query','reallocation']:
 a=load(D/(role+'.attempt.json'));p=load(D/(role+'.process.json'));out=(D/(role+'.stdout')).read_bytes();err=(D/(role+'.stderr')).read_bytes()
 assert a['MainExecution'] is True and a['IndependentPeerRole']==CONFIG['IndependentPeerRole']
 assert a['ActualFullGuardSha256']==sha((P/'full-guard.json').read_bytes())==args.full_guard_sha256.upper()
 assert a['Role']==p['Role']==role and a['NativeExecutionAuthorized'] is True and a['AutomaticRetryAllowed'] is False
 assert a['SourceFreezeSha256']==sha(F.read_bytes()) and a['SourceClosureSha256']==sha(cl.read_bytes()) and a['SourcePeerSha256']==args.source_peer_sha256.upper()==sha(peer.read_bytes())
 assert type(p['ExitCode']) is int and p['ExitCode']==0 and p['TimedOut'] is False and type(p['ElapsedSeconds']) in (int,float) and 0<=p['ElapsedSeconds']<120 and not err
 assert p['StdoutSha256']==sha(out) and p['StderrSha256']==sha(err) and p['NativeProbeInvoked'] is True and p['NativeExecutionValidated'] is False and p['OriginalExecuted'] is False and p['MainExecution'] is True
 bi={x['Path']:x['Sha256'] for x in a['Bindings']};assert len(bi)==len(a['Bindings']) and binaryset.issubset(bi)
 for name,h in bi.items():assert sha((R/name).read_bytes())==h,name
 assert len(bi)==len(binaryset)+10
 if common is None:common=bi
 else:assert bi==common
 for b in p['ActualSdkAssemblies']:assert sha((R/b['Path']).read_bytes())==b['Sha256']
 command=a['Command'];assert command[:2]==['dotnet',str(tool)]
 if role=='legacy':assert len(command)==4
 elif role=='provider-query':assert command[2]=='--native-payload-provider-query' and len(command)==5
 else:assert command[2]=='--reallocation-probe' and len(command)==9
 native_arguments=command[3:] if role!='legacy' else command[2:]
 for name,h in zip(native_arguments[::2],native_arguments[1::2]):assert sha(Path(name).read_bytes())==h
 roles.append({'Role':role,'StdoutSha256':sha(out),'ExitCode':0,'ElapsedSeconds':p['ElapsedSeconds'],'RecursiveDllJsonPins':len(binaryset),'Attempt':bind(D/(role+'.attempt.json')),'Process':bind(D/(role+'.process.json'))})
# Both actual entrypoint runtime assets and localized resources must be present and bound.
deps=load(tool.with_suffix('.deps.json'));assets=[]
for target in deps['targets'].values():
 for library in target.values():
  for kind in ['runtime','resources','native']:
   for name in library.get(kind,{}):
    path=tool.parent/name;assert path.is_file(),name;assert path.relative_to(R).as_posix() in common;assets.append(name)
assert assets
for role in ['legacy','reallocation']:assert (D/(role+'.stdout')).read_bytes()==(R/'artifacts/root-part317-main-windows-native1'/ (role+'.stdout')).read_bytes()
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
assert typed['LoadedRequestedBytes']==typed['LoadedBackingRequestBytes'] and typed['LoadedRequestedBytes']>0
full=load(P/'full-guard.json');assert str(full['Status']).startswith(f'PASS_ACTUAL_MAIN{args.part}_')
assert full['Passed']==load(F)['ExpectedTests'] and full['Passed']==args.expected_full_passed and full['Failed']==0 and full['IntegrationResults']==args.expected_integration_results
assert full['SourceFreezeSha256']==sha(F.read_bytes()) and full['SourceClosureSha256']==sha(cl.read_bytes())
full_exit=load(P/'full-exit.json')
assert full_exit['ExitCode']==0 and full_exit['TimedOut'] is False and full_exit['Settled'] is True
report={'Status':f'PASS_ACTUAL_MAIN{args.part}_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE','SourceFreeze':bind(F),'SourceClosure':bind(cl),'SourceRowsRehashed':len(rows),'ActualTool':bind(tool),'ActualRuntime':bind(runtime),'Roles':roles,'RuntimeDependencyAssetsVerified':len(assets),'EveryRecursiveToolDllJsonBoundAcrossAllThreeAttempts':True,'FullPassed':load(F)['ExpectedTests'],'SourceFreezeSha256':sha(F.read_bytes()),'SourceFilesRehashed':len(rows),'ActualFullGuard':bind(P/'full-guard.json'),'ImplementationSourcePeer':bind(peer),'MainCompositionPeer':bind(composition_peer),'TestedSourceFreeze':bind(tested_freeze),'TestedActualGuard':bind(tested_guard),'ManualReader':bind(reader),'IndependentTypedReplayMatchesRoot':True,'QueryPhases':typed['Completion']['Phases'],'RuntimeOrdinals':[q['RuntimeOrdinal'] for q in typed['QueryObservations']],'JoinedOwners':[q['JoinedOwners'] for q in typed['QueryObservations']],'RequestedAndBackingPairs':[q['QueryResult'] for q in typed['QueryObservations']],'LoadedOwnerLedgerEqualsActualQuery':True,'UnloadedCurrentZeroAndHistoryRetained':True,'Scope':typed['Scope'],'TrustedBridgeSha256':None,'LegacyAndReallocationRawByteExactPreviousActual317':True,'OriginalExecuted':False,'PhysicalMemoryCompared':False,'NewCCompiled':False,'ReviewerExecutedNativeVmSdkGitOrMainMutation':False,'ReviewedInterpretation':'Actual trusted Windows C callback/owner lifecycle queried through the same live SDK runtime; requested payload ledger scope only. No original Erlang comparison or physical allocator footprint claim.'}
references_exact()
out=D/'guard.json';assert not out.exists();report['IndependentPeerRole']=CONFIG['IndependentPeerRole'];report['MainExecution']=True;report['SourcePaths']=load(F)['SourcePaths'];report['CSharpPaths']=load(F)['CSharpPaths'];report['ActualTrustedCQueryExecuted']=True;report['MainSourceFreezeSha256']=sha(F.read_bytes());out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Path':out.relative_to(R).as_posix(),'Sha256':sha(out.read_bytes()),'Status':report['Status']}))

