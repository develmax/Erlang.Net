"""Passive original real-peer8 reader; imports only exact locally reviewed ETF decoder, never downloaded code."""
from pathlib import Path,PurePosixPath,PureWindowsPath
import argparse,hashlib,json,importlib.util,sys,re,zipfile
sys.dont_write_bytecode=True
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5';MANIFEST='C1B3C8BADDCC519B48E557628522E42E5EB31C2CF4055DC343CCBD436EEECD16'
DECODER='A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
CASES=('mfa_spawn','mfa_spawn_link','mfa_spawn_opt','fun_spawn','fun_spawn_link','fun_spawn_opt','labels_visible','labels_hidden')
A=lambda s:('atom',s)
T=lambda *v:('tuple',v)
L=lambda *v:('list',v)
def read(p):return json.loads(Path(p).read_bytes())
def digest(b):return hashlib.sha256(b).hexdigest().upper()
def sha(p):return digest(Path(p).read_bytes())
def need(v,m):
 if not v:raise ValueError(m)
def path(root,name):
 need(isinstance(name,str) and not PurePosixPath(name).is_absolute() and not PureWindowsPath(name).is_absolute() and '\\' not in name and ':' not in name and all(x not in ('','.','..') for x in name.split('/')),'relative capture path')
 q=(root/name).resolve(strict=True);need(q.is_relative_to(root) and q.is_file(),'capture path escape');return q
def text(v):
 need(v[0]=='list' and all(x[0]=='integer' for x in v[1]),'charlist');return ''.join(chr(x[1]) for x in v[1])
def fields(v):need(v[0]=='map','map');return dict(v[1])
def sequence(v,kind='tuple'):need(v[0]==kind and len(v)==2,'typed sequence');return v[1]
def receipt(q):
 e=read(q);need(e['ExitCode']==0 and e['ParentReaped'] is True and e['StreamsClosed'] is True and e['CleanupFailure'] is None and e['TerminationRequested'] is False and e['RemainingGroupKillRequested'] is False,'non-normal owned process');return e

def transport(d,expected_head,run_id,artifact_id):
 mapping=read(d/'run-map.json');run=read(d/'run.json');jobs=read(d/'jobs.json')['jobs'];artifacts=read(d/'artifacts.json')['artifacts']
 need(mapping['Head']==run['head_sha']==expected_head and mapping['Run']==run['id']==run_id and mapping['Attempt']==run['run_attempt']==1,'exact mapped head/run/attempt')
 need(run['path']=='.github/workflows/proc-lib-real-peer-original.yml' and run['event']=='workflow_dispatch' and run['head_branch']=='main' and run['status']=='completed','workflow identity')
 need(len(jobs)==1 and jobs[0]['id']==mapping['Job'] and jobs[0]['head_sha']==expected_head and jobs[0]['run_id']==run_id and jobs[0]['run_attempt']==1 and jobs[0]['status']=='completed','job identity')
 need(len(artifacts)==1 and artifacts[0]['id']==artifact_id and not artifacts[0]['expired'] and artifacts[0]['workflow_run']['head_sha']==expected_head and artifacts[0]['workflow_run']['id']==run_id and artifacts[0]['name']=='proc-lib-real-peer-original-eight','artifact identity')
 rows=read(d/'download-manifest.json');need(len(rows)==len({x['Path'].replace('\\','/') for x in rows}),'download manifest duplicates')
 need({q.relative_to(d).as_posix() for q in d.rglob('*') if q.is_file() and q.name not in ('run-map.json','download-manifest.json')}=={x['Path'].replace('\\','/') for x in rows},'exact raw download closure')
 for x in rows:
  q=path(d,x['Path'].replace('\\','/'));need(q.stat().st_size==x['Bytes'] and sha(q)==x['Sha256'],'download drift')
 item=artifacts[0];zp=d/f'artifact-{artifact_id}.zip';ex=d/f'artifact-{artifact_id}';need(item['digest'].startswith('sha256:') and sha(zp)==item['digest'][7:].upper() and zp.stat().st_size==item['size_in_bytes'],'API ZIP digest')
 with zipfile.ZipFile(zp) as z:
  names=z.namelist();need(len(names)==len(set(names)),'ZIP duplicates')
  need({p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}=={n for n in names if not n.endswith('/')},'exact extracted ZIP fileset')
  for name in names:
   need(not PurePosixPath(name).is_absolute() and not PureWindowsPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and '..' not in PureWindowsPath(name).parts,'ZIP path traversal')
   if not name.endswith('/'):need(z.read(name)==path(ex,name).read_bytes(),'extracted ZIP bytes')
 # Preserve terminal failure separately; do not reclassify VM exit0 as successful acquisition.
 need(run['conclusion']==jobs[0]['conclusion']=='success','terminal workflow failed; raw retained')
 return ex,mapping

def main(a):
 root=a.repository.resolve(strict=True);d=a.capture.resolve(strict=True);need(re.fullmatch('[0-9a-f]{40}',a.expected_head),'explicit published40SHA')
 decoder=root/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py';need(sha(decoder)==DECODER,'reviewed decoder drift')
 spec=importlib.util.spec_from_file_location('trusted_passive_etf',decoder);etf=importlib.util.module_from_spec(spec);spec.loader.exec_module(etf)
 ex,mapping=transport(d,a.expected_head,a.run,a.artifact)
 matches=list(ex.rglob('observations.json'));need(len(matches)==1,'one original capture summary');out=matches[0].parent;summary=read(matches[0])
 need(summary['Status']=='ORIGINAL_REAL_PEER_OBSERVATIONS_COMPLETE' and summary['PlannedCases']==8 and summary['CollectorHead']==a.expected_head and summary['SourceManifestSha256']==MANIFEST and summary['PinnedOtpCommit']==PIN,'complete original identity')
 need(summary['SdkExecuted'] is False and summary['ExpectedValuesPresent'] is False and tuple(x['Name'] for x in summary['Cases'])==CASES,'exact original8 scope')
 provenance=read(out/'original-provenance.json');need(provenance['PinnedOtpCommit']==PIN and provenance['CollectorHead']==a.expected_head,'original pin/head');installed=PurePosixPath(provenance['ResolvedRoot']);need(installed.is_absolute(),'installed root')
 receipt(out/'runtime-control.process.json');control=(out/'runtime-control.stdout').read_text();need('OTP 29\n' in control and 'PRINTABLE latin1\n' in control and 'ROOT '+str(installed)+'\n' in control,'actual runtime control')
 for key in ('OtpVersion','Erl','Erlc','Beam','Erlexec'):
  row=provenance[key];need(row['Bytes']>0 and re.fullmatch('[A-F0-9]{64}',row['Sha256']),'recorded runtime provenance')
 source_manifest=read(a.source_manifest);need(sha(a.source_manifest)==MANIFEST and source_manifest['Cases']==list(CASES),'reviewed source manifest');source_rows={x['Path']:x for x in source_manifest['Files']}
 peer_provenance=read(out/'peer-epmd-provenance.json');need(peer_provenance['Sources']==source_manifest['PinnedSourceAnchors'],'exact pinned peer/EPMD/checksum source anchors');need(peer_provenance['Epmd']['Bytes']>0 and re.fullmatch('[A-F0-9]{64}',peer_provenance['Epmd']['Sha256']),'recorded exact EPMD binary provenance')
 def raw(case,name):
  q=out/case/name;b=q.read_bytes();need(1<len(b)<=16*1024*1024,'ETF size');v,n=etf.decode_one(b);need(n==len(b),'ETF trailing');return v
 results=[]
 for case,row in zip(CASES,summary['Cases']):
  cd=out/case;need(row['Status']=='OBSERVED_NORMAL_CHILD_PEER_AND_OS_CLEANUP' and read(cd/'observation.json')==row,'normal acquisition row')
  need({x['Path'] for x in row['RawEtf']}=={x.name for x in cd.glob('*.etf')},'complete raw ETF list')
  for x in row['RawEtf']:q=path(cd,x['Path']);need(q.stat().st_size==x['Bytes'] and sha(q)==x['Sha256'],'raw ETF binding')
  for label in ('compile','run','epmd','epmd-names','epmd-stop'):receipt(cd/(label+'.process.json'))
  run_receipt=read(cd/'run.process.json');owner=read(cd/'peer-owned-process.json');cleanup=read(cd/'peer-os-cleanup.json')
  need(owner['OriginOwnedPid']==run_receipt['OwnedPid'] and owner['Pid']>0 and owner['StartTicks']>0,'peer descendant generation receipt')
  need(cleanup['Status']=='PEER_OS_PROCESS_GONE' and cleanup['Forced'] is False,'peer normal OS exit')
  epmd=read(cd/'epmd.process.json');one=read(cd/'epmd-owner.json');last=read(cd/'epmd-owner-before-stop.json')
  need(one['Pid']==last['Pid']==epmd['OwnedPid'] and one['Port']==last['Port']==row['Port'] and one['ListenerAddress']==last['ListenerAddress']=='127.0.0.1' and one['OwnedSocketInodes'] and set(one['OwnedSocketInodes']) & set(last['OwnedSocketInodes']),'same owned EPMD loopback listener')
  need(not re.search(rb'(?m)^name ',(cd/'epmd-names.stdout').read_bytes()),'EPMD has registrations')
  need(raw(case,'outer.etf')==T(A('result'),A('ok')) and raw(case,'stage.etf')==T(A('complete'),A(case)) and raw(case,'scenario.etf')==T(A('observed'),A('ok')),'completed scenario')
  for name in ('child-cleanup.etf','peer-controller-cleanup.etf'):need(raw(case,name)==T(A('observed'),A('normal')),'normal DOWN')
  need(raw(case,'peer-stop.etf')==A('ok'),'peer stop');need(not (cd/'failure-child-cleanup.etf').exists(),'failure-only child cleanup present')
  ids=[];module_codes={}
  for filename in ('origin-identity.etf','peer-identity.etf'):
   identity=fields(raw(case,filename));need(text(identity[A('otp')])=='29' and text(identity[A('root')])==str(installed) and identity[A('printable')]==A('latin1'),'exact native identity');mods=sequence(identity[A('modules')],'list');need(len(mods)==4,'four module identities');seen=set()
   for entry in mods:
    module,loaded_path,object_path,loaded_md5,code_md5,file_md5=sequence(entry);name=module[1];need(module[0]=='atom' and name in ('proc_lib_real_peer_probe','proc_lib_remote_payload','proc_lib','peer') and name not in seen,'unique module');seen.add(name)
    lp=PurePosixPath(text(loaded_path));op=PurePosixPath(text(object_path));need(lp==op and lp.is_absolute() and '..' not in lp.parts,'same loaded/object path')
    for checksum in (loaded_md5,code_md5,file_md5):need(checksum[0]=='binary' and len(checksum[1])==16,'MD5 binary16')
    need(loaded_md5==code_md5,'loaded code checksum mismatch')
    if name in module_codes:need(module_codes[name]==(loaded_md5,file_md5),'origin/peer module image differs')
    else:module_codes[name]=(loaded_md5,file_md5)
    if name in ('proc_lib_real_peer_probe','proc_lib_remote_payload'):
     beam=cd/'beam'/(name+'.beam');need(lp.name==beam.name and lp.parent.name=='beam' and lp.parent.parent.name==case and file_md5==('binary',hashlib.md5(beam.read_bytes()).digest()),'retained compiled BEAM fileMD5')
    else:need(lp.is_relative_to(installed),'installed module root')
   ids.append(identity)
  need(int(text(ids[1][A('os_pid')]))==owner['Pid'],'actual peer PID binding');origin_node=ids[0][A('node')];remote_node=ids[1][A('node')];need(origin_node!=remote_node,'distinct native nodes')
  need(raw(case,'peer-node-cleanup.etf')==T(A('observed'),A('nodedown'),remote_node),'actual remote nodedown')
  for name in ('proc_lib_real_peer_probe','proc_lib_remote_payload'):
   src=cd/'source'/(name+'.erl');need(sha(src)==source_rows[name+'.erl']['Sha256'],'actual compiled source exact')
  result=raw(case,'spawn-result.etf');monitor=case.endswith('_opt');child=sequence(result)[0] if monitor else result;need(child[0]=='pid' and child[1]==remote_node,'real remote child')
  if monitor:need(sequence(result)[1][0]=='reference' and raw(case,'primary-down.etf')==T(A('observed'),A('normal')),'primary monitor normal')
  if case in ('mfa_spawn_link','fun_spawn_link'):need(raw(case,'linked-exit.etf')==T(A('observed'),A('normal')),'linked EXIT normal')
  state=fields(raw(case,'child-state.etf'));capture=fields(state[A('captured')]);parent=capture[A('parent')];marker=capture[A('marker')];label=capture[A('label')]
  need(parent[0]=='pid' and parent[1]==origin_node and state[A('node')]==remote_node,'capture parent/node')
  expected_label=('map',frozenset(((A('marker'),marker),(A('nested'),T(('binary',bytes((0,255,128))),L(('integer',-123456789),T(A('arbitrary'),A('label'))))))))
  need(label==expected_label,'arbitrary nested label exact')
  expected_payload=T(('bit_binary',bytes((224,)),3),('map',frozenset(((A('values'),L(('integer',1),('integer',2),('integer',3))),))),L(*(('integer',ord(x)) for x in 'captured')))
  need(capture[A('payload')]==expected_payload and set(capture)=={A('parent'),A('marker'),A('label'),A('payload')},'entire captured nested payload exact')
  need(state[A('constructor_marker')]==raw(case,'receiver-marker-before.etf')==raw(case,'receiver-marker-after.etf')==A('undefined'),'constructor not run receiver')
  need(state[A('label')]==label and raw(case,'get-label-dead.etf')==A('undefined'),'child/dead label')
  need(raw(case,'get-label-alive.etf')==(A('undefined') if case=='labels_hidden' else label),'visible/hidden label contract')
  need(raw(case,'remote-label-dictionary.etf')==T(T(A('dictionary'),A('$process_label')),label),'independent actual label dictionary')
  need(state[A('ancestors')]==L(marker,A('original_ancestor')),'registered parent ancestry')
  need(state[A('links')]==T(A('links'),L(parent) if case in ('mfa_spawn_link','fun_spawn_link') else L()),'atomic child links')
  need(state[A('group_leader')][0]=='pid' and state[A('group_leader')][1]==origin_node,'inherited group leader origin node')
  initial=sequence(state[A('raw_initial_call')]);logical=sequence(state[A('initial_call')]);need(initial[0]==logical[0]==A('proc_lib_remote_payload') and initial[1]==logical[1],'initial call module/function')
  if case.startswith('fun_'):
   info={}
   for item in sequence(raw(case,'origin-fun.etf'),'list'):
    pair=sequence(item);need(len(pair)==2 and pair[0][0]=='atom' and pair[0] not in info,'unique two-field origin fun_info proplist');info[pair[0]]=pair[1]
   need({A(k) for k in ('module','type','arity','env','pid','name')}<=set(info),'origin fun_info required fields')
   need(info[A('module')]==A('proc_lib_remote_payload') and info[A('type')]==A('local') and info[A('arity')]==('integer',0),'origin anonymous fun metadata')
   need(info[A('env')]==L(state[A('captured')]),'origin complete single-map environment')
   metadata_pid=info[A('pid')]
   need(isinstance(metadata_pid,tuple) and len(metadata_pid)==6 and metadata_pid[0]=='pid' and metadata_pid[1]==origin_node and metadata_pid[4]==parent[4],'origin fun metadata init PID node and creation')
   need(initial[1]==info[A('name')] and initial[2]==('integer',0) and logical[2]==L(),'anonymous initial call')
  else:need(initial[1]==A('child') and initial[2]==('integer',1) and logical[2]==L(A('Argument__1')),'MFA initial call')
  nodes=fields(raw(case,'nodes-before.etf'));need(remote_node in sequence(nodes[A('connected')],'list'),'connected node admission');need((remote_node in sequence(nodes[A('hidden')],'list'))==(case=='labels_hidden'),'hidden node classification');need((remote_node in sequence(nodes[A('visible')],'list'))==(case!='labels_hidden'),'visible node classification')
  results.append(dict(Name=case,Status='PASS_TYPED_ORIGINAL_SHARED_CONTRACTS',OriginNode=origin_node[1],RemoteNode=remote_node[1],RawSourceEtfRehashed=True))
 report=dict(Status='PASS_ORIGINAL_REAL_PEER8_TYPED_CONTRACTS',Head=a.expected_head,Run=a.run,Artifact=a.artifact,SourceManifestSha256=MANIFEST,Cases=results,ActualOriginalCases=8,SdkExecutedByReader=False,SdkSemanticComparisonClaimed=False,LiveNativeSdkInteroperabilityClaimed=False,Limits=['Pinned fun_info(pid) returns the init-process PID, not the creator; exact init PID was not separately captured. Its origin node and creation are checked and the complete raw PID is retained.','Installed binary/fullmodule MD5 values are captured provenance, not independently recomputed absent original installed files.','Saved peer StartTicks and EPMD PID/socket inode ownership receipts are checked; live /proc is not re-observed by passive reader.','Group leader is checked as origin-node PID; exact origin GL was not separately captured.','SDK source assertions/crosswalk remain separate from executed SDK outcomes; whole dynamic trace/packet identity and performance not claimed.'])
 need(not a.output.exists(),'fresh report path');a.output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for k in ('repository','capture','source-manifest','output'):p.add_argument('--'+k,type=Path,required=True)
 p.add_argument('--expected-head',required=True);p.add_argument('--run',type=int,required=True);p.add_argument('--artifact',type=int,required=True);a=p.parse_args()
 try:main(a)
 except Exception as e:
  if not a.output.exists():a.output.write_text(json.dumps(dict(Status='REFUSED_RAW_CAPTURE_RETAINED',Error=repr(e),OriginalPassClaimed=False,SdkPassClaimed=False),indent=2)+'\n',encoding='utf-8')
  raise
