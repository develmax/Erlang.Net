from pathlib import Path
import ast,hashlib,json
r=Path.cwd();d=r/'artifacts/connected-guest-links321-original-candidate4';old=r/'artifacts/connected-guest-links321-original-candidate3';t=d/'overlay/tools/Erlang.GuestConnectedLinkObservations';ot=old/'overlay/tools/Erlang.GuestConnectedLinkObservations';out=Path(__file__).resolve().parent
sha=lambda z:hashlib.sha256(z).hexdigest().upper();load=lambda p:json.loads(p.read_bytes())
assert sha((d/'source-freeze.json').read_bytes())=='5B8F78E6D0266CFCBCC91C3D6912FCB8B3C6EA2FC460700DE2B9527D9FD7DAFC'
f=load(d/'source-freeze.json')
for row in f['Files']:
 z=(d/'overlay'/row['Path']).read_bytes();assert len(z)==row['Bytes'] and sha(z)==row['Sha256']
for row in f['EvidenceBindings']:
 z=(d/row['Path']).read_bytes();assert len(z)==row['Bytes'] and sha(z)==row['Sha256']
plan=load(t/'source-plan.json');assert sha((t/'source-plan.json').read_bytes())==f['SourcePlanSha256']=='5106A602867DA8CB30AF0F42E6384E543B576148D25BAF8FC4E685BDD6C76602'
for row in plan['Sources']:
 z=(t/row['Path']).read_bytes();assert len(z)==row['Bytes'] and sha(z)==row['Sha256']
 if row['Path'].endswith('.py'):ast.parse(z)
assert plan['PinnedSources']==load(ot/'source-plan.json')['PinnedSources']
for name in ['owned_process.py','process_capture.py','passive_etf.py']:assert (t/name).read_bytes()==(ot/name).read_bytes()
probe=(t/'guest_connected_links321.erl').read_text();oldprobe=(ot/'guest_connected_links321.erl').read_text()
assert probe.split('observe(Remote, Phase,',1)[0]==oldprobe.split('observe(Remote, Phase,',1)[0]
expected=oldprobe.replace('    Remote ! {probe, self(), Reference, Phase},','    Message = {probe, self(), Reference, Phase},\n    Message = guest_connected_bang322:bang(Remote, Message),').replace('            Owner ! {observed, Reference, Phase, Links},','            Message = {observed, Reference, Phase, Links},\n            Message = guest_connected_bang322:bang(Owner, Message),')
assert probe==expected
minimal='-module(guest_connected_bang322).\n-export([bang/2]).\n\nbang(Pid, Message) -> Pid ! Message.\n';assert (t/'guest_connected_bang322.erl').read_text()==minimal
collector=(t/'collect.py').read_text();assert "str(output / 'sources/guest_connected_bang322.erl')" in collector
assert "GUEST_CONNECTED_LINK_SOURCE_PLAN_SHA256: "+f['SourcePlanSha256'] in (d/'overlay/.github/workflows/distribution-signal-observations.yml').read_text()
# Pure selected metadata validation only. Sentinel marks arrival AFTER both metadata checks.
module=ast.parse(collector);env={'Path':Path,'hashlib':hashlib,'MODULE':'guest_connected_links321','DRIVER':'guest_connected_links321_driver','VERSION':'29.1.1'}
selected=ast.Module(body=[x for x in module.body if isinstance(x,ast.FunctionDef) and x.name in {'require','seq','atom','text','verify_actual'}],type_ignores=[]);exec(compile(selected,'pure-source-validator-only','exec'),env)
a=lambda s:('atom',s);ls=lambda x:('list',tuple(x));tu=lambda *x:('tuple',tuple(x));text=lambda s:ls([('integer',ord(c)) for c in s]);names={'server':'synthetic_server@127.0.0.1','client':'synthetic_client@127.0.0.1'}
beams={name:b'synthetic-pure-only-'+name.encode() for name in ['guest_connected_links321','guest_connected_links321_driver','guest_connected_bang322']};md5=[('binary',hashlib.md5(x).digest()) for x in beams.values()]
values={role+'/runtime.etf':tu(a('guest_connected_links321_runtime'),a(node),text('29'),text('29.1.1'),text('/synthetic-otp'),*md5,text('synthetic'),text('synthetic')) for role,node in names.items()}
class MetadataPassed(Exception):pass
class MemoryPath:
 def __init__(self,s=''):self.s=s
 def __truediv__(self,s):return MemoryPath(self.s+'/'+s if self.s else s)
 def read_bytes(self):
  if self.s.startswith('ebin/'):return beams[self.s[5:-5]]
  if self.s=='client/connection.etf':raise MetadataPassed()
  return self.s.encode()
class Decoder:
 def __init__(self,v):self.v=v
 def decode_one(self,b):return self.v[b.decode()],len(b)
def accepted(v):
 try:env['verify_actual'](MemoryPath(),Decoder(v),Path('/synthetic-otp').resolve(),names)
 except MetadataPassed:return True
 except RuntimeError:return False
 raise AssertionError('unexpected completion')
assert accepted(values);controls=[]
for role in names:
 key=role+'/runtime.etf';items=list(values[key][1]);items[7]=('binary',b'wrong-third-object-md5')
 assert not accepted(dict(values,**{key:tu(*items)}));controls.append(role+'-wrong-third-md5')
 items=list(values[key][1]);del items[7]
 assert not accepted(dict(values,**{key:tu(*items)}));controls.append(role+'-legacy-nine-fields')
report={'Status':'PASS_INDEPENDENT_SOURCE_ONLY_ORIGINAL321_COLLECTOR_REVISION4','SourceFreezeSha256':sha((d/'source-freeze.json').read_bytes()),'SourcePlanSha256':f['SourcePlanSha256'],'ReviewerContribution':'Reviewer previously proposed minimal bang source and diagnosed prior full-module failure; did not author collector/probe/driver/plan mutations. This review independently checks those changes.','PriorRevision3Peer':'artifacts/connected-guest-links321-original-allocator309-source-peer1.json','SourceFilesRehashed':len(f['Files']),'EvidenceBindingsRehashed':len(f['EvidenceBindings']),'PlanSourcesRehashed':len(plan['Sources']),'PythonAstParsed':4,'PinnedErlcCommandContainsThreeAuthoredSourceInputs':True,'BothRuntimeMetadataExactlyTenFieldsThreeActualObjectMd5':True,'BothOwnerAndWorkerCallMinimalHelperAndMatchReturnedMessage':True,'SevenContractsNinePhasesCleanupCardinalityByteExactExceptTwoSendSubstitutions':True,'ThreeProcessHelpersByteExactReviewed319':True,'FreshMonitorUnlinkKillDownPrimaryFaultCleanupPreserved':True,'FullPlanWorkflowSealVerified':True,'SyntheticMetadataPositiveOnly':1,'SyntheticMetadataRefusals':controls,'FullCollectorOrOriginalExecutionClaimed':False,'NewBeamBytesOrSupportedOpcodePredictionClaimed':False,'HistoricalA8dBindingCorrectedToSeparate5521And6380':True,'SdkVmCompilerNativeNetworkMainGitMutationExecuted':False,'NoBlockingSourceFindings':True}
(out/'review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Path':str((out/'review.json').relative_to(r)),'Sha256':sha((out/'review.json').read_bytes()),'Sources':len(f['Files'])}))
