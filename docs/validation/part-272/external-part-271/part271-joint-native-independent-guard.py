from pathlib import Path
from importlib.machinery import SourceFileLoader
import json,hashlib,zipfile,subprocess
a=Path('artifacts');j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda b:hashlib.sha256(b).hexdigest().upper()
decode=SourceFileLoader('decode271','artifacts/part269-decode-node.py').load_module().decode
head=j(a/'part-271-pinned-distribution-packet-metadata.json')['Run']['head_sha'];assert head==Path('artifacts/part-271-frozen-head.txt').read_text().strip()
pin='ad05823719d77c8faee87348ea39513d4e2f99c5';summaries=[]
for artifact,folder,project,probe,report_name,expected_id in [('pinned-distribution-packet','distribution-packet','Erlang.DistributionPacketContrasts','distribution_packet_probe','distribution-packet.json',11915611565),('pinned-mixed-map-identity','mixed-map-identity','Erlang.MixedMapIdentityContrasts','mixed_map_identity_probe','mixed-map-identity.json',11915661445)]:
 base=a/('part-271-'+artifact);root=base/folder;m=j(a/('part-271-'+artifact+'-metadata.json'));assert m['Run']['id']==38604609255 and m['Run']['head_sha']==head and m['Run']['conclusion']=='failure'
 assert m['Artifact']['id']==expected_id and m['Artifact']['workflow_run']['head_sha']==head and not m['Artifact']['expired']
 job=next(x for x in m['Jobs'] if x['id']==115992171586);assert job['conclusion']=='failure' and job['labels']==['ubuntu-latest']
 assert sha((a/('part-271-'+artifact+'.zip')).read_bytes())==m['ZipSHA256']==m['Artifact']['digest'][7:].upper()
 with zipfile.ZipFile(a/('part-271-'+artifact+'.zip')) as z:
  for name in z.namelist():
   if not name.endswith('/'):assert z.read(name)==(base/name).read_bytes()
 ev=j(root/'source-build-evidence.json');assert ev['SourceCommit']==pin and ev['SourceTag']=='OTP-29.1.1' and ev['SourceBuildCompleted'] and ev['HarnessCommit']==head and ev['RunId']=='38604609255' and ev['RunAttempt']=='1'
 assert sha((root/'source-build.log').read_bytes())==ev['SourceBuildLogSha256']
 assert decode((root/'runtime.etf').read_bytes())==['29','17.1','jit',ev['InstalledOtpRoot']]
 blob=subprocess.check_output(['git','show',head+':tools/'+project+'/'+probe+'.erl']);assert blob==(root/(probe+'.erl')).read_bytes() and b'\r' not in blob
 plan=j(root/'source-plan.json');report=j(root/report_name);assert report['Pin']==plan['Pin']==pin and report['SourceSha256']==plan['SourceSha256']==sha(blob)
 assert not report['SemanticCompatibilityClaim'] and not report['SdkExpectedUsed']
 for record in report['Files']:
  data=(root/record['Path']).read_bytes();assert len(data)==record['Length'] and sha(data)==record['Sha256']
 for prefix in ['runtime','compile','disassemble']:
  assert (root/(prefix+'.exit')).read_text().strip()=='0' and (root/(prefix+'.raw-complete')).read_text().strip()=='true'
 assert probe in (root/(probe+'disassembly.txt')).read_text()
 summaries.append({'Artifact':artifact,'Id':expected_id,'ZipSha256':m['ZipSHA256'],'Captured':report['Captured'],'DeclaredFilesVerified':len(report['Files'])})
packet=a/'part-271-pinned-distribution-packet/distribution-packet';pd=packet/'pass-through-send-registered';pr=j(packet/'distribution-packet.json')
assert pr['Captured']==0 and pr['Planned']==1 and pr['Rows'][0]['ExitCode']==1 and pr['Rows'][0]['RawComplete'] and not pr['Rows'][0]['TimedOut'] and pr['Rows'][0]['Stage'] is None
outer=decode((pd/'exception.etf').read_bytes());assert outer[:2]==['error','controller_timeout'] and outer[2][0][:3]==['distribution_packet_probe','run',1]
cleanup=decode((pd/'cleanup-controller.etf').read_bytes());assert cleanup[0]=='badarg' and cleanup[1][0][0:2]==['erlang','setnode']
stdout=(pd/'process.stdout').read_bytes();assert b'{badarg,' in stdout and b'{erlang,setnode,[packet_remote@localhost,' in stdout
assert (pd/'process.exit').read_text().strip()=='1' and (pd/'process.raw-complete').read_text().strip()=='true'
missing=[name for name in j(packet/'source-plan.json')['RequiredArtifacts'] if not (pd/name).exists()];assert 'inputs.etf' in missing and 'incoming-send.packet' in missing
mixed=a/'part-271-pinned-mixed-map-identity/mixed-map-identity';md=mixed/'mixed-local-external-map-start-stop';mr=j(mixed/'mixed-map-identity.json')
assert mr['Captured']==mr['Planned']==1 and mr['Rows'][0]['Stage']=='complete' and mr['Rows'][0]['ExitCode']==0 and mr['Rows'][0]['RawComplete']
required=j(mixed/'source-plan.json')['RequiredArtifacts'];assert len(required)==len(set(required))==32 and all((md/name).exists() for name in required)
facts={phase:decode((md/(phase+'-facts.etf')).read_bytes()) for phase in ['before','started','stopped']};assert facts['before']['node']==facts['stopped']['node']=='nonode@nohost' and facts['started']['node'].startswith('0@')
observed=[]
for phase,f in facts.items():
 assert len(f['pairs'])==2
 for row in f['pairs']:
  v=row['map'];assert v['hashes'][0]==v['hashes'][1]
  assert row['external_node']=='a@host' and row['local_node']==f['node']
  active=phase=='started';assert row['identifier_order']==(['true','false','false'] if active else ['false','true','false'])
  assert v['exact_equal']==v['numeric_equal']==('false' if active else 'true') and v['order']==(['false','true'] if active else ['false','false'])
  assert v['rebuilt_map_key_lookup']==(['result','error'] if active else ['result',['ok','retained_'+row['name']+'_map']])
  assert v['retained_binary']!=v['rebuilt_binary'] if active else v['retained_binary']==v['rebuilt_binary']
  observed.append({'Phase':phase,'Kind':row['name'],'IdentifierOrder':row['identifier_order'],'ExactEqual':v['exact_equal'],'MapOrder':v['order'],'Hashes':v['hashes'],'RebuiltMapKeyLookup':v['rebuilt_map_key_lookup']})
 combined=f['combined'];assert combined['hashes'][0]==combined['hashes'][1] and combined['exact_equal']==('false' if phase=='started' else 'true')
for name in ['reference','pid']:
 hashes=[next(r['map']['hashes'][0] for r in f['pairs'] if r['name']==name) for f in facts.values()];assert len(set(hashes))==1
assert len({f['combined']['hashes'][0] for f in facts.values()})==1
(a/'part271-packet-decoded-failure.json').write_bytes((json.dumps({'OuterException':outer,'ControllerDownReason':cleanup,'MissingDeclaredFiles':missing},indent=2)+'\n').encode())
(a/'part271-mixed-decoded-facts.json').write_bytes((json.dumps(facts,indent=2)+'\n').encode())
dist=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:erts/emulator/beam/dist.c']).decode();start=dist.index('setup_connection_distctrl(');assert 'ERTS_DE_STATE_PENDING' in dist[start:]
steps={x['name']:x['conclusion'] for x in job['steps']};assert any('mixed map' in name and result=='success' for name,result in steps.items()) and any('Capture' in name and 'mixed map' not in name and result=='failure' for name,result in steps.items())
rawlog=(a/'part-271-joint-native-job.log').read_bytes();rawmeta=j(a/'part-271-joint-native-job.log.digest.json');assert sha(rawlog)==rawmeta['SHA256']
latest=j(a/'part-271-joint-native-latest-provenance.json');assert latest['Run']['id']==38604609255 and latest['Run']['head_sha']==head and latest['Run']['conclusion']=='failure'
actualjob=latest['Job'];assert actualjob['conclusion']=='failure' and len(actualjob['steps'])==13
assert actualjob['steps']==job['steps']
assert pin.encode() in rawlog and head.encode() in rawlog and b'tools/Erlang.MixedMapIdentityContrasts --no-build' in rawlog
assert b'Process completed with exit code 1' in rawlog and b'pinned-mixed-map-identity' in rawlog
result={'Status':'PacketFailureAndIndependentMixedSuccessPreserved','Head':head,'Run':38604609255,'Job':115992171586,'JobConclusion':'failure','PinnedRuntimeVerified':['29','17.1','jit'],'ArtifactSummaries':summaries,'PacketCaptured':0,'PacketOuterFailure':['error','controller_timeout'],'PacketControllerFailure':'badarg at setnode/3','PacketMissingRequiredFiles':missing,'SourceCause':'Published probe adopts controller on idle dist entry without erts_internal:new_connection; pinned setup_connection_distctrl requires pending state','MixedCaptured':1,'MixedRequiredFiles':32,'MixedObservedNativeCachedMapOrderingDivergence':True,'ObservedPairs':observed,'MixedFullFactsFile':'part271-mixed-decoded-facts.json','CanonicalEqualityTrueAssumptionContradicted':True,'NoSemanticExpectationsOrRowsChanged':True,'NoRetries':True,'RawJobLogVerificationPending':False,'RawJobLogSha256':sha(rawlog),'ActualJobStepsVerified':13}
(a/'part271-joint-native-independent-guard.json').write_bytes((json.dumps(result,indent=2)+'\n').encode());print('Verified immutable271 packetFAIL/rawcontroller+timeout and independentmixed32nativeSUCCESS; actualnativecached-map equalitydivergence retained')
