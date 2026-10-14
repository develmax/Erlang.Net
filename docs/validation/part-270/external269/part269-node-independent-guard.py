from pathlib import Path
import json,hashlib,subprocess,zipfile
from importlib.machinery import SourceFileLoader
decode=SourceFileLoader('node_decoder','artifacts/part269-decode-node.py').load_module().decode
a=Path('artifacts');root=a/'part-269-pinned-node-identity/node-identity'
j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
head='783088ff0b6095c1e984e913b385a1cfceae6d7c';pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
m=j(a/'part-269-pinned-node-identity-metadata.json');p=j(a/'part-269-node4-latest-provenance.json')
assert m['Run']['head_sha']==p['Run']['head_sha']==head
assert m['Run']['id']==p['Run']['id']==38582865798 and p['Job']['id']==115917049900
assert p['Job']['labels']==['ubuntu-latest']
assert m['Run']['conclusion']==p['Job']['conclusion']=='success'
assert all(x['status']=='completed' and x['conclusion']=='success' for x in p['Job']['steps'])
assert sha((a/'part-269-pinned-node-identity.zip').read_bytes())==m['ZipSHA256']==m['Artifact']['digest'][7:].upper()
with zipfile.ZipFile(a/'part-269-pinned-node-identity.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(a/'part-269-pinned-node-identity'/name).read_bytes()
report=j(root/'node-identity.json');plan=j(root/'source-plan.json');ev=j(root/'source-build-evidence.json')
assert report['Pin']==plan['Pin']==ev['SourceCommit']==pin
assert ev['SourceBuildCompleted'] and ev['SourceTag']=='OTP-29.1.1' and ev['HarnessCommit']==head and ev['RunId']=='38582865798' and ev['RunAttempt']=='1'
assert sha((root/'source-build.log').read_bytes())==ev['SourceBuildLogSha256']
assert decode((root/'runtime.etf').read_bytes())==['29','17.1','jit',ev['InstalledOtpRoot']]
for path,field in [('node_identity_probe.erl','SourceSha256'),('distribution_pending_probe.erl','PendingSourceSha256')]:
 blob=subprocess.check_output(['git','show',head+':tools/Erlang.NodeIdentityContrasts/'+path])
 assert blob==(root/path).read_bytes() and b'\r' not in blob and not blob.startswith(b'\xef\xbb\xbf')
 assert sha(blob)==plan[field]==report[field]
 assert path.split('.')[0] in (root/(path.split('.')[0]+'disassembly.txt')).read_text()
for path,expected in plan['PinnedSources'].items():
 assert sha(subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:'+path]))==expected
for row in report['Files']:
 blob=(root/row['Path']).read_bytes();assert len(blob)==row['Length'] and sha(blob)==row['Sha256']
assert report['Planned']==report['Captured']==4 and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim']
assert [r['Name'] for r in report['Rows']]==['start','start-stop','start-stop-restart','pending-handle']
for row in report['Rows']:
 assert row['Captured'] and row['RawComplete'] and not row['TimedOut'] and row['ExitCode']==0 and row['Stage']=='complete'
 d=root/row['Name'];assert (d/'stage').read_bytes()==b'complete' and (d/'process.exit').read_text().strip()=='0' and (d/'process.raw-complete').read_text().strip()=='true'
summaries=[]
for case,phases in [('start',['before','started']),('start-stop',['before','started','stopped']),('start-stop-restart',['before','started','stopped','restarted'])]:
 d=root/case;facts={ph:decode((d/(ph+'-facts.etf')).read_bytes()) for ph in phases};before=facts['before'];local=(d/'before-local.etf').read_bytes();standard=(d/'before-standard.etf').read_bytes()
 assert before['node']=='nonode@nohost' and before['creation']==0 and before['alive']=='false'
 assert decode((d/'cleanup-child.etf').read_bytes())==['graceful','normal']
 for ph,f in facts.items():
  assert f['owner_same']=='true' and f['owner_lookup']==['ok','owner'] and f['child_lookup']==['ok','child'] and f['reference_lookup']==['ok','reference']
  assert f['hashes']==before['hashes'] and f['pid_node']==f['child_node']==f['reference_node']==f['node'] and f['child_ping']==['pong',f['node']]
  assert (d/(ph+'-local.etf')).read_bytes()==local
  ld=f['local_decode'];assert ld[0]=='result' and ld[1]['same']=='true' and ld[1]['nodes']==[f['node']]*3 and ld[1]['owner_alive']==ld[1]['child_alive']==['result','true']
  sd=f['standard_decode'];assert sd[0]=='result' and sd[1]['nodes']==['nonode@nohost']*3
  if ph in ('started','restarted'):
   assert f['alive']=='true' and f['creation']>0 and f['node']!='nonode@nohost' and sd[1]['same']=='false'
   for k in ('owner_alive','child_alive'):
    assert sd[1][k][:3]==['exception','error','badarg'] and sd[1][k][3][0][:2]==['erlang','is_process_alive']
   assert (d/(ph+'-standard.etf')).read_bytes()!=standard
  else:
   assert f['alive']=='false' and f['creation']==0 and sd[1]['same']=='true' and sd[1]['owner_alive']==sd[1]['child_alive']==['result','true']
   assert (d/(ph+'-standard.etf')).read_bytes()==standard
 if 'restarted' in facts:assert facts['started']['node']==facts['restarted']['node'] and facts['started']['creation']!=facts['restarted']['creation']
 summaries.append({'Case':case,'Phases':[{'Phase':ph,'Node':f['node'],'Creation':f['creation'],'Hashes':f['hashes']} for ph,f in facts.items()]})
pending=decode((root/'pending-handle/actual.etf').read_bytes())
assert pending==['ok','flat_typed_handle','same_pending_identity','next_connection_id',['ok',['ok',0,0,0]],['ok',['ok',0,0,0]],['error','badarg'],['error','badarg'],['error','badarg'],['ok',['ok',0,0,0]]]
assert (root/'pending-handle/process.stderr').read_bytes()==b'pending_stat={ok,0,0,0}\n'
log=a/'part-269-node4-job.log';assert sha(log.read_bytes())==j(Path(str(log)+'.digest.json'))['SHA256']
assert pin in log.read_text(encoding='utf-8-sig')
result={'Status':'FourNativeNodeObservationsIndependentlyVerified','Head':head,'Run':38582865798,'Job':115917049900,'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'SourceBuildVerified':True,'CommittedSourceBytesExact':True,'RawFilesVerified':len(report['Files']),'Runtime':['29','17.1','jit'],'LocalEncodingStable':True,'IntrinsicHashesAndMapKeysStable':True,'StandardEncodingTracksCurrentWireIdentity':True,'OldNonodeStandardDecodeExternalWhileStarted':True,'NativePendingActual':pending,'Transitions':summaries,'SdkCompatibilityClaim':False,'NoOutcomeNormalization':True}
(a/'part269-node-independent-guard.json').write_text(json.dumps(result,indent=2)+'\n')
print('PASS four native node observations, committed source/pinned build/runtime/raw ETF/disassembly/ZIP/API/log')
