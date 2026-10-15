from pathlib import Path
import hashlib,json,runpy,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd(); a=r/'artifacts'; p=a/'nif-actual-beam-caller310-validated1'; sha=lambda b:hashlib.sha256(b).hexdigest().upper()
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='ae4cd999e20e461f7f5917ec346ade31416e2874'
assert not subprocess.check_output(['git','diff','--name-only','-z']) and not subprocess.check_output(['git','diff','--cached','--name-only','-z'])
assert json.loads((a/'root310-independent-source-review.json').read_bytes())['Status']=='ROOT_BOUNDED_SOURCE_REVIEW_PASS'
mp=p/'promotion-map.json'; assert sha(mp.read_bytes())=='79A1571B2C56892FCB3F756E22A676333FE907D3AD4843A221D91D26BDA5CDDC'
rows=json.loads(mp.read_bytes())['Paths']; writes=[]
for row in rows:
 target=r/row['Path']; assert (sha(target.read_bytes()) if target.exists() else None)==row['ParentSha256'],row['Path']
 b=(p/'overlay'/row['Path']).read_bytes(); assert len(b)==row['ResultBytes'] and sha(b)==row['ResultSha256'];writes.append((target,b))
for target,b in writes:target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b)
attrs=r/'.gitattributes'; b=attrs.read_bytes(); assert b'docs/validation/part-307/** binary' not in b;attrs.write_bytes(b+b'docs/validation/part-307/** binary\n')
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'; prior=plan.read_bytes();assert sha(prior)=='6EE3EFCFDC24A8A78AD3D3276AD514070748AFCA996A4EB89FC41E3649A9ACC0'
v=json.loads(prior); before={x['Path']:x for x in v['Files']}; helper=runpy.run_path(str(plan.parent/'run-reviewed.py')); inputs=helper['reviewed_inputs'](r,plan)
assert len(inputs)==1013 and inputs-before.keys()=={'src/Erlang.Runtime/BeamNifNames.cs'} and not before.keys()-inputs
v['Files']=[]
for name in sorted(inputs):
 x=helper['record'](r/name,r,before.get(name,{}).get('HashMode','TextLF'));x['Path']=name;v['Files'].append(x)
b=(json.dumps(v,indent=2)+'\n').encode();workflow=r/'.github/workflows/nif-pending-native.yml'; wb=workflow.read_bytes();assert wb.count(sha(prior).encode())==1; plan.write_bytes(b);workflow.write_bytes(wb.replace(sha(prior).encode(),sha(b).encode()))
names={x['Path'] for x in rows}|{'.gitattributes',workflow.relative_to(r).as_posix(),plan.relative_to(r).as_posix()};assert len(names)==12
files=[]
for name in sorted(names):
 data=(r/name).read_bytes(); old=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 files.append({'Path':name,'Bytes':len(data),'Sha256':sha(data),'ParentGitSha256':sha(old.stdout) if old.returncode==0 else None})
record={'Status':'EXACT_SOURCE_FROZEN_FULL_PENDING','ParentHead':'ae4cd999e20e461f7f5917ec346ade31416e2874','SourcePaths':12,'CSharpPaths':8,'TokensExactForCSharp':True,'ReviewedSourceInputs':1013,'ReviewedSourcePlanSha256':sha(b),'ExpectedFullPassed':6627,'ExpectedRegistry':504,'ExpectedErlangPresence':346,'AddedHostNames':[],'ExtendedExistingHost':'runtime/source-load-nif-direct-caller-capability','PublicMfaAdded':False,'PublicLoadNifActivated':False,'Files':files}
out=a/'root-part307-final-source-freeze.json';assert not out.exists();out.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8');print(json.dumps({'FreezeSha256':sha(out.read_bytes()),'NativePlanSha256':sha(b),'SourcePaths':12}))
