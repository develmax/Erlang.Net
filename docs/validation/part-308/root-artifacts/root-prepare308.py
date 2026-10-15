from pathlib import Path
import hashlib,json,runpy,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';p=a/'root-part308-combined-validated1'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='8b83560372b63b4760604ac088802fb74ac1c112'
assert not subprocess.check_output(['git','diff','--name-only','-z']) and not subprocess.check_output(['git','diff','--cached','--name-only','-z'])
# Root supplies exact immutable validated map hash after inspecting handoff.
mp=p/'promotion-map.json';assert sha(mp.read_bytes())==sys.argv[1]
rows=read(mp);rows=rows['Paths'] if isinstance(rows,dict) and 'Paths' in rows else rows
assert isinstance(rows,list) and len(rows)==29 and sum(x['Path'].endswith('.cs') for x in rows)==28
writes=[]
for row in rows:
 name=row['Path'];target=r/name
 assert (sha(target.read_bytes()) if target.exists() else None)==row['ParentSha256'],name
 b=(p/'overlay'/name).read_bytes();assert len(b)==row['ResultBytes'] and sha(b)==row['ResultSha256'],name
 writes.append((target,b))
for target,b in writes:target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b)
attrs=r/'.gitattributes';b=attrs.read_bytes();assert b'docs/validation/part-308/** binary' not in b;attrs.write_bytes(b+b'docs/validation/part-308/** binary\n')
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';prior=plan.read_bytes();assert sha(prior)=='FD758C0CBD4B6AAF29FB3B3A82BDF97473FE1848E2079BB3B3B3C95F0176B299'
v=read(plan);before={x['Path']:x for x in v['Files']};helper=runpy.run_path(str(plan.parent/'run-reviewed.py'));inputs=helper['reviewed_inputs'](r,plan)
assert len(inputs)==1020 and not before.keys()-inputs,(len(inputs),before.keys()-inputs)
assert inputs-before.keys()=={'src/Erlang.Runtime/ModuleRegistry.SourceEntry.cs','src/Erlang.Runtime/SourceCodeAdmissionKind.cs','src/Erlang.Runtime/NifNativeAllocationParameters.cs','src/Erlang.Runtime/NifNativeAllocationSnapshot.cs','src/Erlang.Runtime/ErlangLoadNifNames.cs','src/Erlang.Runtime/ErlangLoadNifRegistrations.cs','src/Erlang.Runtime/ErlangLoadNifTerms.cs'}
v['Files']=[]
for name in sorted(inputs):
 row=helper['record'](r/name,r,before.get(name,{}).get('HashMode','TextLF'));row['Path']=name;v['Files'].append(row)
b=(json.dumps(v,indent=2)+'\n').encode();workflow=r/'.github/workflows/nif-pending-native.yml';wb=workflow.read_bytes();assert wb.count(sha(prior).encode())==1
plan.write_bytes(b);workflow.write_bytes(wb.replace(sha(prior).encode(),sha(b).encode()))
names={x['Path'] for x in rows}|{'.gitattributes',workflow.relative_to(r).as_posix(),plan.relative_to(r).as_posix()};assert len(names)==32
files=[]
for name in sorted(names):
 data=(r/name).read_bytes();old=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 files.append({'Path':name,'Bytes':len(data),'Sha256':sha(data),'ParentGitSha256':sha(old.stdout) if old.returncode==0 else None})
result={'Status':'EXACT_SOURCE_FROZEN_FULL_PENDING','ParentHead':'8b83560372b63b4760604ac088802fb74ac1c112','CompositePromotionMapSha256':sha(mp.read_bytes()),'SourcePaths':32,'CSharpPaths':28,'TokensExactForCSharp':True,'ReviewedSourceInputs':1020,'ReviewedSourcePlanSha256':sha(b),'ExpectedFullPassed':6637,'ExpectedRegistry':505,'ExpectedOptionalIoRegistry':507,'ExpectedErlangPresence':347,'AddedHostNames':['runtime/beam-fun-entry-pending-purge-active-continuation','runtime/nif-native-requested-backing-allocation-ledger'],'AddedCompiledCases':7,'PublicMfaAdded':True,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'Files':files}
out=a/'root-part308-final-source-freeze.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'FreezeSha256':sha(out.read_bytes()),'NativePlanSha256':sha(b),'SourcePaths':32,'CSharp':28}))
