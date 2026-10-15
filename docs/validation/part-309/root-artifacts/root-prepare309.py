from pathlib import Path
import hashlib,json,runpy,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';p=a/'beam-load-nif-opcodes313-validated1';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
head='e1d8a999faac113d98ebf2d4471f80ef30daf84a'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==head
assert not subprocess.check_output(['git','diff','--name-only','-z']) and not subprocess.check_output(['git','diff','--cached','--name-only','-z'])
mp=p/'promotion-map.json';assert sha(mp.read_bytes())==sys.argv[1]
rows=read(mp);rows=rows['Paths'] if isinstance(rows,dict) else rows
assert len(rows)==7 and sum(x['Path'].endswith('.cs') for x in rows)==3
writes=[]
for row in rows:
 name=row['Path'];target=r/name;data=(p/'overlay'/name).read_bytes()
 assert len(data)==row['ResultBytes'] and sha(data)==row['ResultSha256'],name
 assert (sha(target.read_bytes()) if target.exists() else None)==row['ParentSha256'],name
 assert name.startswith('tools/Erlang.BeamFixtures/') or name=='.github/workflows/beam-fixtures.yml'
 writes.append((target,data))
attrs=r/'.gitattributes';attrbytes=attrs.read_bytes();assert b'docs/validation/part-309/** binary' not in attrbytes
doc=a/'root-part309-erlang-table-candidate';audit=read(doc/'audit.json');docpath=r/audit['ParentPath'];docbytes=(doc/'ERLANG_COMPATIBILITY.md').read_bytes()
assert sha(docpath.read_bytes())==audit['ParentSha256'] and sha(docbytes)==audit['ResultSha256']
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';planhash=sha(plan.read_bytes());assert planhash=='29FC1D27A9A11D03C9928C50DDB6A900D5F235ADE731FE3E3934F66181E7CFA0'
helper=runpy.run_path(str(plan.parent/'run-reviewed.py'));pv=read(plan);assert len(pv['Files'])==1020 and helper['reviewed_inputs'](r,plan)=={x['Path'] for x in pv['Files']}
for row in pv['Files']:
 actual=helper['record'](r/row['Path'],r,row['HashMode']);actual['Path']=row['Path'];assert actual==row,row['Path']
for target,data in writes:target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
attrs.write_bytes(attrbytes+b'docs/validation/part-309/** binary\n');docpath.write_bytes(docbytes)
names={x['Path'] for x in rows}|{'.gitattributes'};files=[]
for name in sorted(names):
 data=(r/name).read_bytes();old=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 files.append({'Path':name,'Bytes':len(data),'Sha256':sha(data),'ParentGitSha256':sha(old.stdout) if old.returncode==0 else None})
result={'Status':'EXACT_STANDALONE_TOOLING_SOURCE_FROZEN_REUSING_PUBLISHED308_RUNTIME_VALIDATION','ParentHead':head,'SourcePaths':8,'CSharpPaths':3,'TokensExactForCSharp':True,'PromotionMapSha256':sha(mp.read_bytes()),'ReusedFullHead':head,'ReusedFullPassed':6637,'NewFullValidationExecuted':False,'RuntimeCompilerAndTestSourcesChanged':False,'Registry':505,'OptionalIoRegistry':507,'ErlangPresence':347,'ReviewedSourceInputs':1020,'ReviewedSourcePlanSha256':planhash,'NativeSourceInputsUnchanged':True,'NewOriginalCompilerExecuted':False,'NewOpcodeShapesVerified':False,'NewNativeExecuted':False,'NewPublicMfa':False,'Files':files}
out=a/'root-part309-final-source-freeze.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps({'SourcePaths':8,'CSharpPaths':3,'FreezeSha256':sha(out.read_bytes()),'NewFullValidationExecuted':False}))
