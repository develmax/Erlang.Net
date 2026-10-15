from pathlib import Path
import ast,hashlib,json,runpy,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';tool=r/'tools/Erlang.NifPendingVarargsObservations';mp=tool/'reviewed-source-plan.json';plan=json.loads(mp.read_bytes())
ns=runpy.run_path(str(tool/'run-reviewed.py'))
assert ns['reviewed_inputs'](r.resolve(),mp.resolve())=={x['Path'] for x in plan['Files']}
for x in plan['Files']:
 b=(r/x['Path']).read_bytes()
 if x['HashMode']=='TextLF':b=b.replace(b'\r\n',b'\n')
 else:assert x['HashMode']=='BinaryExact'
 assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256'],x['Path']
for p in tool.rglob('*.py'):ast.parse(p.read_bytes(),filename=str(p))
original=json.loads((tool/'original/source-plan.json').read_bytes())
assert hashlib.sha256((tool/'original/source-plan.json').read_bytes()).hexdigest().upper()=='EAC7F22640E3233268FD81EC22568A08666BAD5A190A81748DC1A49A1D5B20C0'
for x in original['Sources']:assert hashlib.sha256((tool/'original'/x['Name']).read_bytes()).hexdigest().upper()==x['Sha256']
assert "METADATA_ARITY = 8" in (tool/'verify-original13.py').read_text()
prefixes=['src/Erlang.Runtime/NifNative','src/Erlang.Runtime/ModuleRegistry.NifTaints.cs','tools/Erlang.NifNativeBackend/','tools/Erlang.NifPendingVarargsObservations/']
paths=set(subprocess.check_output(['git','diff','HEAD','--name-only','-z'],cwd=r).decode().split('\0'))-{''}
paths.discard('.gitattributes')
for p in subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z'],cwd=r).decode().split('\0'):
 if p and any(p.startswith(prefix) for prefix in prefixes):paths.add(p)
paths.add('.github/workflows/nif-pending-native.yml')
assert {'Erlang.Net.slnx','tools/validate.ps1'}.issubset(paths)
assert len([p for p in paths if p.endswith('.cs')])==31
assert not any('LICENSE.local' in p or '__pycache__' in p or '/bin/' in p or '/obj/' in p for p in paths)
files=[]
for p in sorted(paths):
 b=(r/p).read_bytes();previous=None
 try:previous=hashlib.sha256(subprocess.check_output(['git','show','HEAD:'+p],cwd=r,stderr=subprocess.DEVNULL)).hexdigest().upper()
 except subprocess.CalledProcessError:pass
 files.append({'Path':p,'Bytes':len(b),'Sha256':hashlib.sha256(b).hexdigest().upper(),'PreviousGitSha256':previous})
base=a/'root-nif-native299-isolated'
assert json.loads((base/'tokens31-before.json').read_bytes())==json.loads((base/'tokens31-after.json').read_bytes())
head=subprocess.check_output(['git','rev-parse','HEAD']).decode().strip();assert head=='e037631759438f944eda934005432ca04fb516e5'
f={'ParentHead':head,'SourcePaths':len(files),'CSharpPaths':31,'TokensExactForCSharp':True,'PublicLoadNifRegistered':False,'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedSourceNativeRoutePassedBeforeFinalFormatting':True,'ReviewedSourceInputs':len(plan['Files']),'ReviewedSourcePlanSha256':hashlib.sha256(mp.read_bytes()).hexdigest().upper(),'Files':files}
(a/'root-part299-final-source-freeze.json').write_text(json.dumps(f,indent=2)+'\n')
print(json.dumps({k:v for k,v in f.items() if k!='Files'}))
