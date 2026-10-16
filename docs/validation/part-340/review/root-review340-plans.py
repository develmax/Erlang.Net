from pathlib import Path
import hashlib,json,re
r=Path.cwd();c=r/'artifacts/connected-remote-completion-candidate'
h=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
report=read(r/'artifacts/connected-remote-completion-plan-source/plan-review.json')
assert report['ParentHead']=='36e6da6dfc2696adce6bff8808ca96bbd46596b6'
records=[]
for row in report['Plans']:
 path=row['Path'];old=read(r/path);new=read(c/path)
 assert {k:v for k,v in old.items() if k!='Files'}=={k:v for k,v in new.items() if k!='Files'}
 oldpaths={v['Path'] for v in old['Files']};newpaths={v['Path'] for v in new['Files']}
 assert len(newpaths)==len(new['Files'])==row['Files'] and oldpaths<newpaths
 assert newpaths-oldpaths==set(row['AddedSourcePaths']) and len(newpaths-oldpaths)==30
 for f in new['Files']:
  b=(c/f['Path']).read_bytes()
  if f['HashMode']=='TextLF':b=b.replace(b'\r\n',b'\n')
  else:assert f['HashMode']=='BinaryExact'
  assert len(b)==f['Bytes'] and h(b)==f['Sha256'],f['Path']
 workflow=row['Workflow'];before=(r/workflow).read_bytes();after=(c/workflow).read_bytes()
 assert h((c/path).read_bytes())==row['Sha256']
 pattern=rb'(REVIEWED_[A-Z_]*PLAN_SHA256: )([0-9A-F]{64})'
 oldseal=re.findall(pattern,before);newseal=re.findall(pattern,after)
 assert len(oldseal)==len(newseal)==1 and oldseal[0][0]==newseal[0][0]
 assert oldseal[0][1].decode()==h((r/path).read_bytes()) and newseal[0][1].decode()==h((c/path).read_bytes())
 assert before.replace(oldseal[0][1],newseal[0][1])==after
 records.append(dict(Path=path,Files=len(newpaths),Added=30,EveryInputRehashed=True,NonFilesFieldsExact=True,WorkflowOnlySealChanged=True,PlanSha256=h((c/path).read_bytes()),WorkflowSha256=h(after)))
out=r/'artifacts/root-340-plans-source-review.json';assert not out.exists();out.write_text(json.dumps(dict(Status='PASS_SOURCE_ONLY340_NATIVE_PLANS',Plans=records,NativeExecuted=False,MainEdited=False),indent=2)+'\n')
print(json.dumps(dict(Plans=len(records),Counts=[p['Files'] for p in records],Sha256=h(out.read_bytes()))))
