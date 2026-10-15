from pathlib import Path
import hashlib,json,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();p=a/'root-part303-final-source-freeze.json';f=read(p);assert f['SourcePaths']==len(f['Files'])==13 and f['CSharpPaths']==11
for row in f['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256'],row['Path']
planpath=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';plan=read(planpath);assert len(plan['Files'])==1007 and h(planpath.read_bytes())==f['ReviewedSourcePlanSha256'];ns=runpy.run_path(str(planpath.parent/'run-reviewed.py'));assert ns['reviewed_inputs'](r,planpath)=={x['Path'] for x in plan['Files']}
for row in plan['Files']:
 b=(r/row['Path']).read_bytes();b=b.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else b;assert len(b)==row['Bytes'] and h(b)==row['Sha256'],row['Path']
iso=a/'root-nif-source-caller303-isolated';assert read(iso/'tokens11-folder2-before.json')==read(iso/'tokens11-folder2-after.json');assert read(a/'root-part303-main-focus13-exit.json')['ExitCode']==0
result=dict(Status='PASS_ACTUAL_MAIN13_SOURCE_11_TOKENS_1007_INPUTS_AND_13_FOCUS',SourcePaths=13,CSharpPaths=11,ReviewedInputs=1007,SourceFreezeSha256=h(p.read_bytes()),MainFocusedGroupsPassed=13,PublicLoadNifActivated=False,OriginalNewCasesExecuted=False)
(a/'root-part303-source-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
