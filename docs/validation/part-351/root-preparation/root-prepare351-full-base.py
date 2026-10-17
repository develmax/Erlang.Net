from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();s=r/'artifacts/spawn-priority351-permanent-preparation2';p=r/'artifacts/spawn-priority351-permanent-candidate';c=r/'artifacts/spawn-priority351-full-candidate';o=r/'artifacts/spawn-priority351-full-base'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()=='c1f219f31726fc93e537080f400651d300c47d72'
assert sha((s/'source-freeze.json').read_bytes())=='AFE2569C70A92B59C7DF5B3BFFD28FC12530B51B56573C83C5EF59662AC33400'
assert sha((s/'source-resource-closure.json').read_bytes())=='82FE7A6263EFD5BA55A726A4A78159B28A00648FCEAF9B98FBFC88CCB8EC3A04'
peer=r/'artifacts/spawn-priority351-permanent-independent/report.json';assert sha(peer.read_bytes())=='FA4523E17B0C0F72E38144E50C43ADE9109BD1F663B41237F58B51FA459EDB4E'
rows=read(s/'source-resource-closure.json')['Files'];assert len(rows)==2826 and not c.exists() and not o.exists()
for row in rows:assert sha((p/row['Path']).read_bytes())==row['Sha256'],row['Path']
c.mkdir();o.mkdir()
for row in rows:
    target=c/row['Path'];assert not row['Path'].startswith(('artifacts/','.git/')) and '..' not in target.parts
    target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((p/row['Path']).read_bytes())
attrs=c/'.gitattributes';before=attrs.read_bytes();assert b'docs/validation/part-351/** binary' not in before
attrs.write_bytes(before+b'\ndocs/validation/part-351/** binary\n')
scopepath=c/'docs/module-readiness-scope.json';scope=read(scopepath);assert scope['ComparisonTestReport']=='validation/part-348/full/tests.json'
scope['ComparisonTestReport']='validation/part-350/full/tests.json'
runtime=[x for x in scope['Components'] if x['Id']=='Runtime'];assert len(runtime)==1
runtime[0]['Implemented'].append('Part351: max/high/normal/low owner scheduling and logical low8 admission, initial spawn priority, priority flag/query, actual executing-turn borrowing and escaped registered-callback readmission. Isolated ParseAll10 and24 managed controls pass with exact loaded DLL identities; current canonical full/original/CI evidence is reported separately, with no whole-MFA or physical/performance parity claim.')
scopepath.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8',newline='\n')
planpath=c/'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json';plan=read(planpath)
attrrows=[x for x in plan['Files'] if x['Path']=='.gitattributes'];assert len(attrrows)==1
row=attrrows[0];raw=attrs.read_bytes();data=raw.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else raw;row.update(Bytes=len(data),Sha256=sha(data))
planpath.write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
workflow=c/'.github/workflows/nif-reallocation-native.yml';text=workflow.read_text(encoding='utf-8');import re
text,n=re.subn(r'(REVIEWED_REALLOCATION_PLAN_SHA256:\s*)[0-9A-Fa-f]{64}',lambda m:m[1]+sha(planpath.read_bytes()),text);assert n==1
workflow.write_text(text,encoding='utf-8',newline='\n')
changes=[]
for row in rows:
    b=(c/row['Path']).read_bytes()
    if sha(b)!=row['Sha256']:changes.append(dict(Path=row['Path'],BeforeSha256=row['Sha256'],Sha256=sha(b),Bytes=len(b)))
assert {x['Path'] for x in changes}=={'.gitattributes','docs/module-readiness-scope.json','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml'}
receipt=dict(Status='SEPARATE_CANONICAL351_BASE_NOT_READY_FOR_FULL',ParentHead='c1f219f31726fc93e537080f400651d300c47d72',ClonedInputs=2826,PermanentPeerSha256=sha(peer.read_bytes()),Changes=changes,LedgerStillStale59=True,MetadataOverlayRequired=True,StyleAndFinalFreezeRequired=True,FullExecuted=False,MainModified=False)
(o/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(Candidate=c.relative_to(r).as_posix(),Changes=len(changes),Sha256=sha((o/'receipt.json').read_bytes()))))
