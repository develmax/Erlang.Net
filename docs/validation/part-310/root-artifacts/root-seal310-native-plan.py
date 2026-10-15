from pathlib import Path
import hashlib,json,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd();c=r/'artifacts/root-part310-combined-isolated/candidate';d=c.parent
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
plan=c/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';prior=plan.read_bytes();assert sha(prior)=='29FC1D27A9A11D03C9928C50DDB6A900D5F235ADE731FE3E3934F66181E7CFA0'
v=json.loads(prior);before={x['Path']:x for x in v['Files']};helper=runpy.run_path(str(plan.parent/'run-reviewed.py'));inputs=helper['reviewed_inputs'](c,plan)
new={'src/Erlang.Runtime/NifNativeAllocationDomain.cs','src/Erlang.Runtime/NifNativeAllocationDomainChange.cs','src/Erlang.Runtime/NifNativeAllocationDomainSnapshot.cs','src/Erlang.Runtime/NifNativeAllocationDomainDiagnostics.cs','src/Erlang.Runtime/ProcessRuntime.NativeAllocations.cs','tools/Erlang.NifNativeBackend/NifNativeDomainChecks.cs','tools/Erlang.NifNativeBackend/NifNativeDomainNames.cs','tools/Erlang.NifPendingVarargsObservations/reviewed_sdk_stdout.py'}
assert len(before)==1020 and len(inputs)==1028 and inputs-set(before)==new and not set(before)-inputs
v['Files']=[]
for name in sorted(inputs):
 row=helper['record'](c/name,c,before.get(name,{}).get('HashMode','TextLF'));row['Path']=name;v['Files'].append(row)
b=(json.dumps(v,indent=2)+'\n').encode();workflow=c/'.github/workflows/nif-pending-native.yml';wb=workflow.read_bytes();assert wb.count(sha(prior).encode())==1
attrs=c/'.gitattributes';ab=attrs.read_bytes();assert b'docs/validation/part-310/** binary' not in ab
plan.write_bytes(b);workflow.write_bytes(wb.replace(sha(prior).encode(),sha(b).encode()));attrs.write_bytes(ab+b'docs/validation/part-310/** binary\n')
rows=[]
for p in (plan,workflow,attrs):
 data=p.read_bytes();rows.append({'Path':p.relative_to(c).as_posix(),'Bytes':len(data),'Sha256':sha(data)})
out=d/'native-plan-seal.json';assert not out.exists();out.write_text(json.dumps({'Status':'SOURCE_ONLY_1028_INPUTS_RESEALED_PENDING_COMPOSITE_VALIDATION','PreviousPlanSha256':sha(prior),'ReviewedSourcePlanSha256':sha(b),'PreviousInputs':1020,'Inputs':1028,'AddedPaths':sorted(new),'Files':rows,'NativeExecuted':False,'MainEdited':False},indent=2)+'\n',encoding='utf-8');print(json.dumps({'Inputs':1028,'PlanSha256':sha(b),'SealSha256':sha(out.read_bytes())}))
