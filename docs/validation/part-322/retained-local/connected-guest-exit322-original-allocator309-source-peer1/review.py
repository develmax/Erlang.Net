from pathlib import Path
import ast,hashlib,json,copy,subprocess
r=Path.cwd();d=r/'artifacts/connected-guest-exit322-original-candidate2';t=d/'overlay/tools/Erlang.GuestConnectedExitObservations';prior=r/'artifacts/connected-guest-links321-original-candidate4/overlay/tools/Erlang.GuestConnectedLinkObservations';out=Path(__file__).resolve().parent;sha=lambda z:hashlib.sha256(z).hexdigest().upper();load=lambda p:json.loads(p.read_bytes())
assert sha((d/'source-freeze.json').read_bytes())=='1F7CD50EAC19C3CD5AB066D36B67151F763573D38240BE5A7E8E100B3246B628'
f=load(d/'source-freeze.json')
for x in f['Files']:
 z=(d/'overlay'/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
assert sha((d/'author.py').read_bytes())==f['AuthorSourceSha256']
plan=load(t/'source-plan.json');assert sha((t/'source-plan.json').read_bytes())==f['SourcePlanSha256']=='F40BA9404BF5BE58BE58108365AB85C6B24E9528DA870355FDFE0126DBD3576B';assert b'\r' not in (t/'source-plan.json').read_bytes()
for x in plan['Sources']:
 z=(t/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
 if x['Path'].endswith('.py'):ast.parse(z)
for name in ['owned_process.py','process_capture.py','passive_etf.py','guest_connected_bang322.erl']:assert (t/name).read_bytes()==(prior/name).read_bytes()
pins=[]
for x in plan['PinnedSources']:
 z=subprocess.check_output(['git','--no-optional-locks','-C',str(r.parent/'otp'),'show',plan['PinnedCommit']+':'+x['Path']]);assert sha(z)==x['Sha256'];pins.append(x)
assert 'GUEST_CONNECTED_EXIT_SOURCE_PLAN_SHA256: '+f['SourcePlanSha256'] in (d/'overlay/.github/workflows/distribution-signal-observations.yml').read_text()
probe=(t/'guest_connected_exit322.erl').read_text();assert probe.count('Before = seq_trace:get_token(),')==2 and probe.count('After = seq_trace:get_token(),')==2
observe=probe.split('observe(Remote, Label, Operation, Kind) ->',1)[1].split('observe_kill(Remote) ->',1)[0];assert observe.index('After = seq_trace:get_token(),')<observe.index('receive')
# Pure validator only; no collector/process helpers imported or invoked.
source=(t/'collect.py').read_text();mod=ast.parse(source);env={'Path':Path,'hashlib':hashlib,'MODULE':'guest_connected_exit322','DRIVER':'guest_connected_exit322_driver','VERSION':'29.1.1'};selected=ast.Module(body=[x for x in mod.body if isinstance(x,ast.FunctionDef) and x.name in {'require','seq','atom','text','verify_actual'}],type_ignores=[]);exec(compile(selected,'source-only-pure-validator','exec'),env)
a=lambda s:('atom',s);ls=lambda v:('list',tuple(v));tu=lambda *v:('tuple',tuple(v));integer=lambda n:('integer',n);text=lambda s:ls([integer(ord(c)) for c in s]);names={'server':'synthetic_server@127.0.0.1','client':'synthetic_client@127.0.0.1'};owner=('pid',a(names['client']),1,0,0);remote=('pid',a(names['server']),2,0,0);ref=('reference',a(names['client']),(1,2,3));token=tu(integer(1),integer(42),integer(8),owner,integer(4));labels=['exit_two','exit_three','exit_signal_two','exit_signal_three','duplicate_priority','unknown_option','improper_options','kill_trapped_worker']
beams={name:b'synthetic-passive-only-'+name.encode() for name in ['guest_connected_exit322','guest_connected_exit322_driver','guest_connected_bang322']};md5=[('binary',hashlib.md5(z).digest()) for z in beams.values()];base={role+'/runtime.etf':tu(a('guest_connected_exit322_runtime'),a(node),text('29'),text('29.1.1'),text('/synthetic-otp'),*md5,text('synthetic'),text('synthetic')) for role,node in names.items()};base['client/connection.etf']=tu(a(names['client']),a(names['server']),ls([a(names['server'])]));rows=[]
for label in labels:
 if label in ['unknown_option','improper_options']:result=tu(a('exception'),a('error'),a('badarg'),ls([]));obs=tu(a('no_exit'),ref);history=ls([])
 elif label=='kill_trapped_worker':result=tu(a('return'),a('true'));obs=tu(a('down'),ref,a('process'),remote,a('killed'));history=ls([])
 else:
  result=tu(a('return'),a('true'));reason=tu(a(label),('binary',bytes([1,2,3])));exit=tu(a('EXIT'),owner,reason);obs=tu(a('observed'),ref,exit,ls([]));history=ls([exit])
 rows.append(tu(a('case_result'),a(label),token,token,result,obs,history))
def withrows(v,rr):v['client/outcome.etf']=tu(a('return'),tu(a('guest_connected_exit322'),ls([tu(a(n),a('true')) for n in labels]),tu(a('raw'),owner,remote,ls(rr))));return v
base=withrows(base,rows);base['client/worker-cleanup.etf']=tu(a('return'),tu(a('worker_joined'),a('noproc')));base['client/server-cleanup.etf']=base['server/shutdown.etf']=tu(a('return'),a('server_joined_protocol'))
class MemoryPath:
 def __init__(self,s=''):self.s=s
 def __truediv__(self,s):return MemoryPath(self.s+'/'+s if self.s else s)
 def read_bytes(self):return beams[self.s[5:-5]] if self.s.startswith('ebin/') else self.s.encode()
class Decoder:
 def __init__(self,v):self.v=v
 def decode_one(self,b):return self.v[b.decode()],len(b)
def check(v):return env['verify_actual'](MemoryPath(),Decoder(v),Path('/synthetic-otp').resolve(),names)
check(base);controls=[]
def refuse(name,v):
 try:check(v)
 except RuntimeError:controls.append(name);return
 raise AssertionError('unexpected accepted '+name)
def changedrow(name,index,column,value):
 rr=copy.deepcopy(rows);row=list(rr[index][1]);row[column]=value;rr[index]=tu(*row);refuse(name,withrows(copy.deepcopy(base),rr))
changedrow('after-token-mutated',0,3,tu(integer(1),integer(42),integer(9),owner,integer(4)))
changedrow('valid-exit-false-return',0,4,tu(a('return'),a('false')))
changedrow('duplicate-priority-two-effects',4,6,ls([rows[4][1][6][1][0]]*2))
changedrow('invalid-unknown-remote-effect',5,6,rows[0][1][6])
changedrow('invalid-improper-remote-effect',6,6,rows[0][1][6])
changedrow('remote-exit-token-present',0,5,tu(a('observed'),ref,rows[0][1][5][1][2],ls([integer(1)])))
changedrow('kill-must-be-killed-not-noproc',7,5,tu(a('down'),ref,a('process'),remote,a('noproc')))
refuse('final-cleanup-must-be-noproc',dict(base,**{'client/worker-cleanup.etf':tu(a('return'),tu(a('worker_joined'),a('killed')))}))
refuse('missing-eighth-case',withrows(copy.deepcopy(base),rows[:-1]));refuse('wrong-case-order',withrows(copy.deepcopy(base),[rows[1],rows[0],*rows[2:]]))
for role in names:
 key=role+'/runtime.etf';v=copy.deepcopy(base);item=list(v[key][1]);item[7]=('binary',b'wrong-third-md5');v[key]=tu(*item);refuse(role+'-third-md5-mismatch',v)
 v=copy.deepcopy(base);item=list(v[key][1]);del item[7];v[key]=tu(*item);refuse(role+'-old-nine-fields',v)
report={'Status':'PASS_INDEPENDENT_SOURCE_ONLY_ORIGINAL322_EXIT_COLLECTOR_REVISION2','SourceFreezeSha256':sha((d/'source-freeze.json').read_bytes()),'SourcePlanSha256':f['SourcePlanSha256'],'SourceFilesRehashed':len(f['Files']),'PlanSourcesRehashed':len(plan['Sources']),'AuthorSourceRehashed':True,'PythonAstParsed':4,'PinnedGitObjectsReadOnlyVerified':pins,'PrivateForegroundTwoLoopbackVmOwnershipAndCleanupReviewed':True,'ThreeImagesAndBothTenFieldActualMetadataRequired':True,'FourPublicFormsEightContractsAndDuplicatePriority':True,'BeforeAfterFullTokenSnapshotsBeforeReceiveAndInspect':True,'InvalidOptionHistoryBarrierShowsNoRemoteEffect':True,'KillFreshDownKilledDistinctFinalFreshMonitorNoproc':True,'PrimaryFailureWinsWithRawCleanupAndOutcomesRetained':True,'SyntheticPureFullValidatorPositive':1,'SyntheticRefusals':controls,'NoCollectorVmCompilerSdkNativeNetworkMainGitMutationExecuted':True,'ReadOnlyGitShowOnly':True,'PriorityOrderPressureWireEqualityNotClaimed':True,'NoBlockingSourceFindings':True}
(out/'review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'ReportSha256':sha((out/'review.json').read_bytes()),'Refusals':len(controls)}))
