"""Pure source/schema controls only; no VM/compiler/native process."""
from pathlib import Path
import ast,hashlib,importlib.util,json,sys
P=Path(__file__).resolve().parent;R=P.parents[1];T=P/'source/tools/Erlang.NifEntryOriginalObservations'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def load(path,name):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
m=J(T/'manifest.json')
for x in m['Files']:
 p=T/x['Path'];assert p.stat().st_size==x['Bytes'] and H(p)==x['Sha256']
for x in m['Dependencies']:
 p=R/x['Path'];assert p.stat().st_size==x['Bytes'] and H(p)==x['Sha256']
assert H(P/'source'/m['Workflow']['Path'])==m['Workflow']['Sha256']
for p in T.glob('*.py'):ast.parse(p.read_bytes())
ancestor=R/'artifacts/native-entry355-fixtures-proposal/source'
for folder in ['native','original']:
 for old in (ancestor/folder).rglob('*'):
  if not old.is_file():continue
  now=T/old.relative_to(ancestor)
  if old.name=='entry_observer.erl':
   before=old.read_text();after=now.read_text();after=after.replace('    case code:is_loaded(Module) of\n        false -> {module, Module} = code:load_file(Module);\n        {file, _} -> ok\n    end,','    {module, Module} = code:load_file(Module),',1);assert after==before
  else:assert now.read_bytes()==old.read_bytes()
cases=J(T/'cases.json')['Cases'];assert len(cases)==len({x['Name'] for x in cases})==11
assert len({x['NativeLeaf'] for x in cases})==m['NativeImageCount']==8
assert len({x['Module'] for x in cases})==3
identity=load(R/'tools/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','entry_source_control_identity')
env=identity.sanitized_environment({'ERL_ROOTDIR':'foreign','ERL_FLAGS':'foreign','KEEP':'ok'});assert 'ERL_ROOTDIR' not in env and 'ERL_FLAGS' not in env and env['KEEP']=='ok'
good={'ExitCode':0,'ParentReaped':True,'StreamsClosed':True,'TerminationRequested':False,'RemainingGroupKillRequested':False,'CleanupFailure':None};identity.require_normal_receipt(good)
negative=0
for key,val in [('TerminationRequested',True),('RemainingGroupKillRequested',True),('ExitCode',1),('ParentReaped',False),('StreamsClosed',False),('CleanupFailure','failure')]:
 try:identity.require_normal_receipt(dict(good,**{key:val}))
 except AssertionError:negative+=1
 else:raise AssertionError('Bad process admission accepted '+key)
assert negative==6
sys.path.insert(0,str(T));common=load(T/'common.py','entry_pure_common')
case=next(x for x in cases if x['Name']=='alpha_duplicate');A=common.A;V=common.T
returned=V(V(A('entry_case'),A(case['Name']),A(case['Module']),V(A('error'),V(A('bad_lib'),('list',()))),V(A('stub_after_failure'),A('not_loaded'))),V(A('cleanup'),A('deleted_and_purged')))
common.verify_return(returned,case)
try:common.verify_return(V(returned[1][0],V(A('cleanup'),A('forced'))),case)
except AssertionError:negative+=1
else:raise AssertionError('Forced module cleanup accepted')
for p in (P/'source').rglob('*'):
 if p.is_file():assert b'\r' not in p.read_bytes() and not p.read_bytes().startswith(b'\xef\xbb\xbf')
print(json.dumps({'Status':'PASS_SOURCE_SCHEMA_CONTROLS_ONLY','ManifestSha256':H(T/'manifest.json'),'Cases':11,'Images':8,'ActualModuleNames':3,'NegativeReceiptAndCleanupRefusals':negative,'FixtureBodiesUnchangedExceptConditionalNoReloadObserver':True,'CompilerVmNativeSdkExecuted':False}))
