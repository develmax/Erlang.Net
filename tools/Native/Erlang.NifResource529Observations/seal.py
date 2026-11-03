"""Bind exact final integrated sources; does not build or execute native/guest code."""
import hashlib,json,os,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
TOOL=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def main():
 path=TOOL/'source-plan.json';plan=json.loads(path.read_bytes())
 assert plan['ProtocolSchemaVersion']==2 and plan['OriginalFirstRequired'] is True
 assert set(plan['Variants'])=={'family1','handoff1','handoff2'}
 assert set(plan['Jobs'])=={'family527','fresh_load_after_purge','genuine_upgrade'}
 for variant in plan['Variants'].values():
  variant['NativeSourceSha256']=sha(ROOT/variant['NativeSourcePath'])
  variant['ErlangSourceSha256']=sha(ROOT/variant['ErlangSourcePath'])
 plan['BuildScriptSha256']=sha(TOOL/'build-native.py')
 files={p for p in TOOL.rglob('*') if p.is_file() and p.name!='source-plan.json' and '__pycache__' not in p.parts}
 # Binding final source closure is intentional: runtime, compiler, terms, bridge and child driver.
 for base in [ROOT/'src',ROOT/'tools/Native/Erlang.NifNativeBackend']:
  files.update(p for p in base.rglob('*') if p.is_file() and p.suffix in {'.cs','.csproj','.c','.h','.props','.targets'} and not {'bin','obj','.git'}.intersection(p.parts))
 for name in ['owned.py','bounded_inventory.py']:
  files.add(ROOT/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'/name)
 for name in ['Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json']:
  if (ROOT/name).is_file():files.add(ROOT/name)
 plan['Dependencies']=[{'Path':p.relative_to(ROOT).as_posix(),'Sha256':sha(p)} for p in sorted(files)]
 plan['OriginalGoldenState']='NOT_COLLECTED'
 plan['SealState']='FINAL_SOURCE_BOUND_NOT_EXECUTED'
 payload=(json.dumps(plan,indent=2)+'\n').encode()
 fd,tmp=tempfile.mkstemp(dir=TOOL,prefix='.source-plan-',suffix='.tmp')
 try:
  with os.fdopen(fd,'wb') as stream:stream.write(payload);stream.flush();os.fsync(stream.fileno())
  os.replace(tmp,path)
 finally:
  if os.path.exists(tmp):os.unlink(tmp)
 print(json.dumps({'PlanSha256':sha(path),'Dependencies':len(plan['Dependencies']),'Executed':False}))
if __name__=='__main__':main()
