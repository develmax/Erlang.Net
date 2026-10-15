from pathlib import Path
import hashlib,json,subprocess,sys
r=Path.cwd();a=r/'artifacts';d=a/'root-part310-combined-isolated';c=d/'candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
base=d/'base-composition.json';assert sha(base.read_bytes())=='85EA81B8989069B1CC480F7664A9FB9D94473E81514971024A1B6EB2CE8173E8'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='e829c53ef4323ea8cdd0948cd77c243efa787abd'
for row in read(base)['Files']:
 b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
p=a/sys.argv[1];mp=p/'promotion-map.json';assert sha(mp.read_bytes())==sys.argv[2]
rows=read(mp);rows=rows.get('Files',rows.get('Paths')) if isinstance(rows,dict) else rows
assert len(rows)==5 and sum(x['Path'].endswith('.cs') for x in rows)==4
names={x['Path'] for x in read(base)['Files']};assert not names.intersection(x['Path'] for x in rows)
for row in rows:
 name=row['Path'];main=r/name;b=(p/'overlay'/name).read_bytes()
 assert len(b)==row['ResultBytes'] and sha(b)==row['ResultSha256'],name
 assert (sha(main.read_bytes()) if main.exists() else None)==row['ParentSha256'],name
 assert (c/name).read_bytes()==main.read_bytes() if main.exists() else not (c/name).exists()
 target=c/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b);names.add(name)
assert len(names)==23 and sum(x.endswith('.cs') for x in names)==20
files=[]
for name in sorted(names):
 b=(c/name).read_bytes();files.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
out=d/'pre-format-composition.json';assert not out.exists();out.write_text(json.dumps({'Status':'FOUR_PACKAGES_COMPOSED_PENDING_FINAL_FORMAT_AND_ACTUAL_MAIN_FULL','BaseCompositionSha256':sha(base.read_bytes()),'BeamCandidate':sys.argv[1],'BeamPromotionMapSha256':sha(mp.read_bytes()),'Files':files,'MainEdited':False,'BuildExecuted':False},indent=2)+'\n',encoding='utf-8')
tokens=d/'pre-format-token-inputs.json';tokens.write_text(json.dumps([str(c/x['Path']) for x in files if x['Path'].endswith('.cs')]),encoding='utf-8')
print(json.dumps({'Paths':23,'CSharp':20,'CompositionSha256':sha(out.read_bytes())}))
