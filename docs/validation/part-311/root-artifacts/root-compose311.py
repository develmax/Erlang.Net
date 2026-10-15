from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';d=a/'root-part311-combined-isolated';c=d/'candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
baseline=d/'baseline-snapshot.json';assert sha(baseline.read_bytes())=='C3FC24A745C63C3C0F749F823C7DD8E66C43B0F744973661520D281CAC6D5CB8'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='79bfaecaa7d89a03e2248149ae55c3b5bf03e6a8'
for row in read(baseline)['Files']:
 b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
p=a/'nif-reallocation309-validated1';f=p/'final-freeze.json';assert sha(f.read_bytes())=='9F08A60320809B859CB02C3762101F734BF5C42F439AF47EF2115CF2EE0B157E'
for row in read(f)['Files']:
 b=(p/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
mp=p/'promotion-map.json';assert sha(mp.read_bytes())=='E15B6783CB535CF5FF8E55C756569346A8E377BBA562A6C10B387DA70C1F39EA'
rows=read(mp)['Files'];assert len(rows)==7 and all(x['Path'].endswith('.cs') for x in rows)
paths=set()
for row in rows:
 name=row['Path'];main=r/name;b=(p/'overlay'/name).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],name
 assert (sha(main.read_bytes()) if main.exists() else None)==row['ParentSha256'],name
 if row['Operation']=='InsertSingleRowAfterDomain':
  old=(c/name).read_bytes();anchor=b'Test(Erlang.Tests.NifAllocationDomainHostNames.Test, Erlang.Tests.NifAllocationDomainHostChecks.Run);';new=b'Test(Erlang.Tests.NifReallocationHostNames.Test, Erlang.Tests.NifReallocationHostChecks.Run);'
  assert old.count(anchor)==1 and new not in old
  pos=old.index(anchor);start=old.rfind(b'\n',0,pos)+1;indent=old[start:pos];assert not indent.strip();end=pos+len(anchor);nl=b'\r\n' if old[end:end+2]==b'\r\n' else b'\n';result=old[:end]+nl+indent+new+old[end:];assert result==b
 target=c/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b);paths.add(name)
eof=a/'part311-eof-layout-candidate1';ep=eof/'promotion-map.json';assert sha(ep.read_bytes())=='68E9E4D988FDCCC5E525E79E605E1C84C91421DB366AEF7253220FFA306421A9'
for row in read(ep)['Files']:
 name=row['Path'];b=(eof/'overlay'/name).read_bytes();assert name not in paths and len(b)==row['ResultBytes'] and sha(b)==row['ResultSha256'] and sha((r/name).read_bytes())==row['ParentSha256'];(c/name).write_bytes(b);paths.add(name)
assert len(paths)==8
files=[]
for name in sorted(paths):
 b=(c/name).read_bytes();files.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
out=d/'pre-format-composition.json';assert not out.exists();out.write_text(json.dumps({'Status':'REALLOCATION_AND_ONE_EOF_LAYOUT_CORRECTION_COMPOSED_PENDING_NORMAL_MAIN_FULL','ParentHead':'79bfaecaa7d89a03e2248149ae55c3b5bf03e6a8','ReallocationMapSha256':sha(mp.read_bytes()),'EofMapSha256':sha(ep.read_bytes()),'Files':files,'MainEdited':False,'BuildExecuted':False},indent=2)+'\n',encoding='utf-8')
(d/'pre-format-token-inputs.json').write_text(json.dumps([str(c/x['Path']) for x in files]),encoding='utf-8');print(json.dumps({'Paths':8,'CSharp':8,'CompositionSha256':sha(out.read_bytes())}))
