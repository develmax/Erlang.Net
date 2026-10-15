from pathlib import Path
import json,hashlib,shutil,subprocess,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';p=a/'root-nif-source-caller303-isolated/promotion-recipe';read=lambda x:json.loads(x.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();parent='ce3636a9d17a4d1e1c979c57b4df3d0800b72134'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==parent
f=read(p/'recipe-freeze.json');assert h((p/'recipe-freeze.json').read_bytes())=='F4DFC425BC429530C33BC0F586F98391ED3386BD2065AB1D1DCC3AD5E77E3F35'
for row in f['Files']:
 data=(p/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'],row['Path']
m=read(p/'promotion-map.json');rows=m['SourceFiles']+[m['NativePlan'],m['NativeWorkflow']];assert len(rows)==13
for row in rows:
 target=r/row['Path'];assert (h(target.read_bytes()) if target.exists() else None)==row['ParentSha256'],row['Path'];data=(p/'source-overlay'/row['Path']).read_bytes();assert h(data)==row['Sha256'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
planpath=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';plan=read(planpath);ns=runpy.run_path(str(planpath.parent/'run-reviewed.py'));assert len(plan['Files'])==1007 and ns['reviewed_inputs'](r,planpath)=={row['Path'] for row in plan['Files']}
for row in plan['Files']:
 data=(r/row['Path']).read_bytes();data=data.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else data;assert len(data)==row['Bytes'] and h(data)==row['Sha256'],row['Path']
freeze=dict(ParentHead=parent,SourcePaths=13,CSharpPaths=11,TokensExactForCSharp=True,ReviewedSourceInputs=1007,ReviewedSourcePlanSha256=h(planpath.read_bytes()),IsolatedManagedFocusGroups=13,ExpectedFullPassed=6622,ExpectedRegistry=503,ExpectedErlangPresence=345,PublicMfaAdded=False,PublicLoadNifActivated=False,OriginalNewCasesExecuted=False,GeneralNifAbiComplete=False,Files=[dict(Path=row['Path'],Sha256=h((r/row['Path']).read_bytes()),Bytes=(r/row['Path']).stat().st_size,PreviousGitSha256=row['ParentSha256']) for row in rows])
output=a/'root-part303-final-source-freeze.json';assert not output.exists();output.write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8');print(h(output.read_bytes()))
