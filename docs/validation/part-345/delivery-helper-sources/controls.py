"""Source/CLI help controls only. Never invoke a delivery action or build."""
from pathlib import Path
import ast,json,hashlib,subprocess,sys
O=Path(__file__).resolve().parent;sha=lambda b:hashlib.sha256(b).hexdigest().upper()
files=[];controls=[]
for p in sorted(O.glob('*.py')):ast.parse(p.read_bytes());files.append({'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())})
for name in ['plan345.py','promote345.py','finalize345.py','readiness345.py','stage345.py']:
 p=subprocess.run([sys.executable,'-B',str(O/name),'--help'],capture_output=True,timeout=15);assert p.returncode==0 and b'--independent-guard-sha256' in p.stdout
 controls.append({'Script':name,'HelpExit':p.returncode,'HelpSha256':sha(p.stdout),'NoDeliveryActionExecuted':True})
readme=O/'README.md';files.append({'Path':'README.md','Bytes':readme.stat().st_size,'Sha256':sha(readme.read_bytes())})
report={'Status':'PASS_DELIVERY_HELPER_SOURCE_AST_AND_HELP_ONLY','Controls':controls,'FullPassClaimed':False,'MainModified':False,'SdkExecuted':False,'NetworkUsed':False};p=O/'source-controls.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');files.append({'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())})
freeze={'Status':'SOURCE_ONLY_REVIEWABLE345_DELIVERY_HELPERS_FULL_ACTUAL_GUARD_PENDING','ParentHead':'9c7949f87348223a61c65d37d859b02841036374','SourceFreezeSha256':'92CA87634B19A5CB999C9BA0C94F2DA638E022A1325EF3D3B1A7E4C90A5E3825','SourceClosureSha256':'3E624D130B22232E40E47982D4449D6482469D2BABF0815F3E5521E293E67805','Files':files,'HelperActionsExecuted':False,'MainModified':False,'FullPassClaimed':False}
p=O/'source-freeze.json';assert not p.exists();p.write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8');print(json.dumps({'FreezeSha256':sha(p.read_bytes()),'Files':len(files)}))
