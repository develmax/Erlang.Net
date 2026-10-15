from pathlib import Path
import hashlib,json,runpy,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';d=a/'root-part311-combined-isolated';c=d/'candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
head='79bfaecaa7d89a03e2248149ae55c3b5bf03e6a8';assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==head
assert not subprocess.check_output(['git','diff','HEAD','--name-only','-z']) and not subprocess.check_output(['git','diff','--cached','--name-only','-z'])
bp=d/'baseline-snapshot.json';assert sha(bp.read_bytes())=='C3FC24A745C63C3C0F749F823C7DD8E66C43B0F744973661520D281CAC6D5CB8';baseline=read(bp)
cp=d/'pre-format-composition.json';assert sha(cp.read_bytes())=='B4319E99620C65681925D8972AFBED17C31993FAA82938EA091F6111513DE9A0';composition=read(cp)
fg=d/'format-token-guard.json';formatting=read(fg);assert formatting['Status']=='EIGHT_CSHARP_TOKEN_STREAMS_EXACT_AFTER_FORMAT' and formatting['CSharpPaths']==8
paths={x['Path'] for x in composition['Files']};assert len(paths)==8
for row in baseline['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
 if row['Path'] not in paths:
  b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
for row in formatting['Files']:
 b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
oldpaths={x['Path'] for x in baseline['Files']};assert all(not (r/name).exists() for name in paths-oldpaths)
plan=c/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';prior=plan.read_bytes();assert sha(prior)=='BFC52C82190D6BE2CAC9A9396E0F9B87C662A06B85249C76F910450352494CCB'
v=json.loads(prior);before={x['Path']:x for x in v['Files']};helper=runpy.run_path(str(plan.parent/'run-reviewed.py'));inputs=helper['reviewed_inputs'](c,plan);assert set(before)==inputs and len(inputs)==1028
rows=[]
for name in sorted(inputs):
 row=helper['record'](c/name,c,before[name]['HashMode']);row['Path']=name;rows.append(row)
changed={x['Path'] for x in rows if x!=before[x['Path']]};assert changed=={'src/Erlang.Runtime/NifNativeAllocationOwner.cs','src/Erlang.Runtime/NifNativeDiagnostics.cs'},changed
v['Files']=rows;b=(json.dumps(v,indent=2)+'\n').encode();workflow=c/'.github/workflows/nif-pending-native.yml';wb=workflow.read_bytes();assert wb.count(sha(prior).encode())==1
attrs=c/'.gitattributes';ab=attrs.read_bytes();assert b'docs/validation/part-311/** binary' not in ab
plan.write_bytes(b);workflow.write_bytes(wb.replace(sha(prior).encode(),sha(b).encode()));attrs.write_bytes(ab+b'docs/validation/part-311/** binary\n')
paths|={plan.relative_to(c).as_posix(),workflow.relative_to(c).as_posix(),'.gitattributes'};assert len(paths)==11 and sum(x.endswith('.cs') for x in paths)==8
closure=[]
for name in sorted(oldpaths|paths):
 b=(c/name).read_bytes();closure.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
assert len(closure)==2154
sourceclosure=c/'final-source-resource-closure.json';assert not sourceclosure.exists();sourceclosure.write_text(json.dumps({'Status':'FINAL_AUTHORED_SOURCE_RESOURCE_CONFIGURATION_CLOSURE_BEFORE_ACTUAL_MAIN_FULL','SourceRoot':'.','Files':closure,'MainFullExecuted':False},indent=2)+'\n',encoding='utf-8')
files=[]
for name in sorted(paths):
 b=(c/name).read_bytes();target=r/name;old=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 files.append({'Path':name,'Bytes':len(b),'Sha256':sha(b),'ParentWorkingSha256':sha(target.read_bytes()) if target.exists() else None,'ParentGitSha256':sha(old.stdout) if old.returncode==0 else None})
for row in files:
 target=r/row['Path'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((c/row['Path']).read_bytes())
mainplan=r/plan.relative_to(c);mh=runpy.run_path(str(mainplan.parent/'run-reviewed.py'));assert mh['reviewed_inputs'](r,mainplan)==inputs
result={'Status':'EXACT_11_SOURCE_8_CSHARP_FROZEN_PENDING_ACTUAL_MAIN_FULL','ParentHead':head,'SourcePaths':11,'CSharpPaths':8,'TokensExactForCSharp':True,'ReviewedSourceInputs':1028,'ReviewedSourcePlanSha256':sha(mainplan.read_bytes()),'ExpectedFullPassed':6639,'ExpectedRegistry':505,'ExpectedOptionalIoRegistry':507,'ExpectedErlangPresence':347,'AddedHostNames':['runtime/nif-real-payload-reallocation-extent-history'],'NewPublicMfa':False,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'FinalSourceClosureSha256':sha(sourceclosure.read_bytes()),'SourceClosurePaths':2154,'PreviousKnownEofStyleWarningCorrected':True,'ReallocationPromotionMapSha256':composition['ReallocationMapSha256'],'EofPromotionMapSha256':composition['EofMapSha256'],'Files':files}
out=a/'root-part311-final-source-freeze.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps({'SourcePaths':11,'CSharp':8,'NativeInputs':1028,'SourceClosurePaths':2154,'NativePlanSha256':sha(mainplan.read_bytes()),'SourceFreezeSha256':sha(out.read_bytes()),'ClosureSha256':sha(sourceclosure.read_bytes())}))
