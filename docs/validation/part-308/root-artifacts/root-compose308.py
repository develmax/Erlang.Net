from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd(); a=r/'artifacts'; out=a/'root-part308-combined-isolated'; c=out/'candidate'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='8b83560372b63b4760604ac088802fb74ac1c112'
base=out/'baseline-snapshot2.json'; assert sha(base.read_bytes())=='59F2CCD9F2A49F1224D7F98D9E7FBFB9531158C1C1E81C51E0D24CECE6BA90A2'
for row in read(base)['Files']:
 b=(c/row['Path']).read_bytes(); assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
names=set(); actions=[]
for folder,expected in [('beam-closure-pending-entry311-validated1','AF15EE5DAF203AD021EC74C510F9B555FD7D217CD8CA99EAD3C261B9CC5E61DF'),('indirect-load-nif312-validated2','E10FEB639CBE5176D9E3C8C05FE4409AB767D26ABE3D80C979D53553D26B8EE9')]:
 p=a/folder; mp=p/'promotion-map.json'; assert sha(mp.read_bytes())==expected
 rows=read(mp); rows=rows['Paths'] if isinstance(rows,dict) else rows
 for row in rows:
  name=row['Path']; target=c/name; b=(p/'overlay'/name).read_bytes()
  assert name not in names and sha(b)==row['ResultSha256'] and len(b)==row['ResultBytes'],name
  parent=r/name; assert (sha(parent.read_bytes()) if parent.exists() else None)==row['ParentSha256'],name
  target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b);names.add(name)
 actions.append({'Candidate':folder,'PromotionMapSha256':expected,'Paths':len(rows)})
p=a/'nif-allocation309-ledger-candidate3'; f=p/'source-freeze-sourceonly.json';assert sha(f.read_bytes())=='0A6C8CFC402119A92CCD5F20F395D91D29B6B2568E7B02E32DE49EAB09EE442E'
for row in read(f)['CandidateFiles']:
 name=row['Path']; b=(p/'overlay'/name).read_bytes();assert sha(b)==row['Sha256'] and len(b)==row['Bytes'],name
 target=c/name
 if name=='tests/Erlang.Tests/Program.cs':
  spec=read(a/'nif-allocation309-ledger-candidate2/promotion-map.json')['Files'][-1]
  old=target.read_bytes(); anchor=spec['Anchor'].encode(); registration=spec['Registration'].encode();assert old.count(anchor)==1 and registration not in old
  pos=old.index(anchor); start=old.rfind(b'\n',0,pos)+1; indent=old[start:pos]; assert indent.strip()==b''
  target.write_bytes(old[:start]+indent+registration+b'\r\n'+old[start:])
 else:
  assert name not in names; target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b)
 names.add(name)
actions.append({'Candidate':p.name,'SourceFreezeSha256':sha(f.read_bytes()),'ProgramMerge':'Insert one registration preserving closure row'})
rows=[]
for name in sorted(names):
 b=(c/name).read_bytes(); rows.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
assert len(rows)==29 and sum(x['Path'].endswith('.cs') for x in rows)==28
result={'Status':'THREE_VALIDATED_CANDIDATES_COMPOSED_PENDING_COMPOSITE_VALIDATION','ParentHead':'8b83560372b63b4760604ac088802fb74ac1c112','BaselineSha256':sha(base.read_bytes()),'Actions':actions,'ExpectedFullPassed':6637,'DefaultRegistry':505,'OptionalIoRegistry':507,'ErlangPresence':347,'Files':rows}
(out/'composition.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Paths':len(rows),'CSharp':28,'CompositionSha256':sha((out/'composition.json').read_bytes())}))
