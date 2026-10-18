"""Parent-executed passive Git/blob and producer input review. No captured code is executed."""
from pathlib import Path
import argparse,hashlib,json,subprocess
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def dump(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
def blob(head,path):return subprocess.check_output(['git','-C',str(ROOT),'show',head+':'+path],timeout=45)

def main():
 p=argparse.ArgumentParser()
 for key in ['artifact','tools-root','bindings','output']:p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--expected-head',required=True);a=p.parse_args()
 out=a.output.resolve();assert out.is_relative_to(HERE) and not out.exists()
 reviewed=J(HERE/'reviewed-source-packet.json');packet=a.tools_root.resolve()
 assert packet==ROOT/reviewed['SourcePacket'] and H(packet/'source-seal.json')==reviewed['SourceSealSha256']
 for r in J(packet/'source-seal.json')['Files']:
  q=(packet/r['Path']).resolve();assert q.is_relative_to(packet) and q.stat().st_size==r['Bytes'] and H(q)==r['Sha256']
 bindings=J(a.bindings);assert bindings['Head']==a.expected_head
 expected={}
 def add(path,row):
  assert not Path(path).is_absolute() and '..' not in Path(path).parts
  if path in expected:assert expected[path]['Sha256']==row['Sha256']
  expected[path]=row
 for cohort in reviewed['Cohorts']:
  folder=packet/cohort['Id'];prefix='tools/'+cohort['Tool']+'/'
  assert H(folder/'profile.json')==cohort['ProfileSha256'] and H(folder/'source-inputs.corrected.json')==cohort['SourceInputsSha256']==cohort['ManifestSha256']
  plan=J(folder/'source-inputs.corrected.json');assert plan['PinnedOtpCommit']==PIN
  for row in plan['RepositoryInputs']:add(row['Path'],row)
  for row in plan['ToolInputs']:
   local=(folder/row['Path']).resolve();assert local.is_relative_to(folder) and local.stat().st_size==row['Bytes'] and H(local)==row['Sha256']
   add(prefix+row['Path'],row)
  manifest=folder/'source-inputs.corrected.json';add(prefix+'source-inputs.corrected.json',{'Bytes':manifest.stat().st_size,'Sha256':H(manifest)})
  sharedpath='tools/Erlang.SchedulerAffinityOriginalObservations/manifest.json';raw=blob(a.expected_head,sharedpath)
  assert hashlib.sha256(raw).hexdigest().upper()==plan['ExistingBuildManifestSha256']
  shared=json.loads(raw)
  for row in shared['Files']:add('tools/Erlang.SchedulerAffinityOriginalObservations/'+row['Path'],row)
  for row in shared['Dependencies']+[shared['Workflow']]:add(row['Path'],row)
 for row in bindings['Bindings']:add(row['Path'],row)
 out.mkdir();snapshot=out/'producer-snapshot';snapshot.mkdir();checked=[]
 for path,row in sorted(expected.items()):
  raw=blob(a.expected_head,path);assert len(raw)==row['Bytes'] and hashlib.sha256(raw).hexdigest().upper()==row['Sha256'],path
  q=(snapshot/path).resolve();assert q.is_relative_to(snapshot);q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(raw)
  checked.append({'Path':path,'Bytes':len(raw),'Sha256':row['Sha256'],'ExactProducerGitBlob':True})
 sourcefiles=list(a.artifact.rglob('source-before.json'));assert len(sourcefiles)==1
 source=J(sourcefiles[0]);assert source['Commit']==PIN
 rows=source['TrackedFiles'];assert len(rows)==len({r['Path'] for r in rows})==12008
 tree=subprocess.check_output(['git','-C',str(ROOT.parent/'otp'),'ls-tree','-r','-z','--full-tree',PIN])
 blobs={}
 for entry in tree.split(b'\0'):
  if not entry:continue
  meta,path=entry.split(b'\t',1);mode,kind,oid=meta.split(b' ');assert kind==b'blob'
  blobs[path.decode()]=(mode.decode(),oid.decode())
 assert set(blobs)=={r['Path'] for r in rows}
 process=subprocess.Popen(['git','-C',str(ROOT.parent/'otp'),'cat-file','--batch'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 verified=[]
 try:
  for row in rows:
   mode,oid=blobs[row['Path']];assert mode==row['Mode']
   process.stdin.write((oid+'\n').encode());process.stdin.flush();header=process.stdout.readline().decode().split()
   assert header[:2]==[oid,'blob'] and int(header[2])==row['Bytes']
   remaining=row['Bytes'];digest=hashlib.sha256()
   while remaining:
    data=process.stdout.read(min(remaining,1048576));assert data;digest.update(data);remaining-=len(data)
   assert process.stdout.read(1)==b'\n' and digest.hexdigest().upper()==row['Sha256']
   verified.append(dict(row,Blob=oid))
  process.stdin.close();assert process.wait(timeout=15)==0 and not process.stderr.read()
 finally:
  if process.poll() is None:process.kill();process.wait(timeout=15)
 dump(out/'pinned-blobs.json',{'Status':'PASS_ALL12008_PINNED_GIT_BLOBS_ONCE_FOR_THREE_COHORTS','Commit':PIN,'Files':verified})
 dump(out/'producer-authority.json',{'Status':'PASS_COMMITTED_PRODUCER_INPUTS_THREE_COHORTS','Head':a.expected_head,'Files':checked,'SourceBeforeSha256':H(sourcefiles[0]),'NoCapturedCodeExecuted':True})
 dump(out/'acceptance.json',{'Status':'PASS_SHARED_SOURCE_AND_PRODUCER_ADMISSION','Head':a.expected_head,'PinnedBlobs':len(verified),'SourceBeforeSha256':H(sourcefiles[0]),'ProducerSnapshotFiles':len(checked),'ProducerSnapshot':str(snapshot),'PinnedBlobsSha256':H(out/'pinned-blobs.json'),'ProducerAuthoritySha256':H(out/'producer-authority.json'),'DownloadedCodeExecuted':False})
if __name__=='__main__':main()
