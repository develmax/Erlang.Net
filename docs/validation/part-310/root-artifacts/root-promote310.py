from pathlib import Path
import hashlib,json,runpy,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';d=a/'root-part310-combined-isolated';c=d/'candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='e829c53ef4323ea8cdd0948cd77c243efa787abd'
assert not subprocess.check_output(['git','diff','HEAD','--name-only','-z']) and not subprocess.check_output(['git','diff','--cached','--name-only','-z'])
baseline=read(d/'baseline-snapshot.json');composition=read(d/'pre-format-composition.json');seal=read(d/'native-plan-seal.json');formatting=read(d/'format-token-guard.json')
assert formatting['Status']=='TWENTY_CSHARP_TOKEN_STREAMS_EXACT_AFTER_FORMAT' and formatting['CSharpPaths']==20
for row in baseline['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
paths={x['Path'] for x in composition['Files']}|{x['Path'] for x in seal['Files']};assert len(paths)==26 and sum(x.endswith('.cs') for x in paths)==20
for row in composition['Files']:
 if not row['Path'].endswith('.cs'):
  b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
for row in seal['Files']:
 b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
for row in formatting['Files']:
 assert sha((c/row['Path']).read_bytes())==row['Sha256'],row['Path']
oldpaths={x['Path'] for x in baseline['Files']};assert all(not (r/name).exists() for name in paths-oldpaths)
for row in baseline['Files']:
 if row['Path'] not in paths:
  b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
closure=[]
for name in sorted(oldpaths|paths):
 b=(c/name).read_bytes();closure.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
assert len(closure)==2149
sourceclosure=c/'final-source-resource-closure.json';assert not sourceclosure.exists();sourceclosure.write_text(json.dumps({'Status':'FINAL_AUTHORED_SOURCE_RESOURCE_CONFIGURATION_CLOSURE_BEFORE_ACTUAL_MAIN_FULL','SourceRoot':'.','Files':closure,'MainFullExecuted':False},indent=2)+'\n',encoding='utf-8')
files=[]
for name in sorted(paths):
 b=(c/name).read_bytes();target=r/name;old=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 files.append({'Path':name,'Bytes':len(b),'Sha256':sha(b),'ParentWorkingSha256':sha(target.read_bytes()) if target.exists() else None,'ParentGitSha256':sha(old.stdout) if old.returncode==0 else None})
for row in files:
 target=r/row['Path'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((c/row['Path']).read_bytes())
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';helper=runpy.run_path(str(plan.parent/'run-reviewed.py'));v=read(plan)
assert len(v['Files'])==1028 and helper['reviewed_inputs'](r,plan)=={x['Path'] for x in v['Files']}
for x in v['Files']:
 actual=helper['record'](r/x['Path'],r,x['HashMode']);actual['Path']=x['Path'];assert actual==x,x['Path']
result={'Status':'EXACT_26_SOURCE_20_CSHARP_FROZEN_PENDING_ACTUAL_MAIN_FULL','ParentHead':'e829c53ef4323ea8cdd0948cd77c243efa787abd','SourcePaths':26,'CSharpPaths':20,'TokensExactForCSharp':True,'ReviewedSourceInputs':1028,'ReviewedSourcePlanSha256':sha(plan.read_bytes()),'ExpectedFullPassed':6638,'ExpectedRegistry':505,'ExpectedOptionalIoRegistry':507,'ExpectedErlangPresence':347,'AddedHostNames':['runtime/nif-runtime-native-payload-domain-lifecycle'],'NewPublicMfa':False,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'FinalSourceClosureSha256':sha(sourceclosure.read_bytes()),'Files':files}
out=a/'root-part310-final-source-freeze.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps({'SourcePaths':26,'CSharp':20,'NativeInputs':1028,'SourceClosurePaths':2149,'SourceFreezeSha256':sha(out.read_bytes()),'ClosureSha256':sha(sourceclosure.read_bytes())}))
