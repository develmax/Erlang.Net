"""Shared byte-exact evidence operations. No VM is started by this module."""
import hashlib, importlib.util, json, os
from pathlib import Path
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
OWNED = 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py'
INVENTORY = 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/bounded_inventory.py'
def require(condition, message):
    if not condition: raise RuntimeError(message)
def sha(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
    return h.hexdigest().upper()
def save(path, value):
    Path(path).write_bytes((json.dumps(value,indent=2)+'\n').encode())
def verified(path, expected):
    path=Path(path).resolve();require(path.is_file() and sha(path)==expected.upper(),'Hash differs: '+str(path));return path
def load(name,path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def row(path):
    path=Path(path).resolve();return {'Path':str(path),'Bytes':path.stat().st_size,'Sha256':sha(path)}
def files(root, excludes=()):
    root=Path(root).resolve();result=[]
    for directory,dirs,names in os.walk(root,followlinks=False):
        for name in [*dirs,*names]:require(not (Path(directory)/name).is_symlink(),'Symlink not admitted: '+str(Path(directory)/name))
        for name in sorted(names):
            p=Path(directory)/name;relative=p.relative_to(root).as_posix()
            if relative not in excludes:result.append({'Path':relative,'Bytes':p.stat().st_size,'Sha256':sha(p)})
    return sorted(result,key=lambda r:r['Path'])
def verify_rows(rows):
    for r in rows:
        p=verified(r['Path'],r['Sha256']);require(p.stat().st_size==r['Bytes'],'Size differs: '+str(p))
def package(repo, here, approved):
    plan_path=verified(here/'source-plan.json',approved)
    plan=json.loads(plan_path.read_bytes())
    require(plan['PinnedCommit']==PIN and plan['Platform']=='windows-x64','Wrong source plan profile')
    helper=load('private557_source_helper',repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py')
    helper.check_inputs(repo,here,plan)
    return [plan_path,*[(here if r['Scope']=='Tool' else repo)/r['Path'] for r in plan['Inputs']]]

def state_inputs(paths):return [row(p) for p in sorted(set(Path(x).resolve() for x in paths))]
def fresh_output(repo, output):
    repo=Path(repo).resolve();output=Path(output).resolve()
    require(output.is_relative_to(repo/'artifacts/nif-private-data557') and not output.exists(),
        'Fresh contained native private-data output required')
    output.mkdir(parents=True)
    return output

def process_ok(r):
    return isinstance(r,dict) and r.get('Settled') is True and r.get('TimedOut') is False and r.get('ExitCode')==0 and r.get('Failures')==[]
def error(phase, exc):
    import traceback
    return {'Phase':phase,'Error':repr(exc),'Traceback':traceback.format_exc()}
def original_admission(completion_path, approved):
    path=verified(completion_path,approved);root=path.parent;c=json.loads(path.read_bytes())
    require(c['SubjectKind']=='GENUINE_LOAD_PRIVATE557' and c['Status']=='LOAD_PRIVATE557_ORIGINAL_V2' and c['Pin']==PIN and c['Passed'] is True and c['OriginalExecuted'] is True and c['WholeContractsClosed']==0 and c.get('RawEtfRewritten') is False and c.get('NormalizationPerformed') is False,'Original receipt profile refused')
    require(process_ok(c['Process']) and c['Errors']==[],'Original process/cleanup failed')
    require(c['InputInventory']['Path']=='sealed-inputs.json' and c['OutputInventory']['Path']=='output-inventory.json','Original inventory roles differ')
    require(json.loads((root/'original2.exit.json').read_bytes())==c['Process'],'Original process receipt detached from actual exit artifact')
    require(len(c['Sources'])==3 and {Path(r['Path']).name for r in c['Sources']}=={'fixture.c','load_private557.erl','load_private_observer557.erl'},'Original source set differs')
    inv=verified(root/c['OutputInventory']['Path'],c['OutputInventory']['Sha256']);rows=json.loads(inv.read_bytes())
    require(files(root,('completion.json','output-inventory.json'))==rows,'Original complete output inventory differs')
    require(c['ObservedSha256']==sha(root/'original/observed.etf') and c['DeterministicSha256']==sha(root/'original/observed-deterministic.etf'),'Original reference detached from execution receipt')
    verify_rows(c['Sources'])
    inputs=verified(root/c['InputInventory']['Path'],c['InputInventory']['Sha256']);verify_rows(json.loads(inputs.read_bytes()))
    verify_rows([c['NativeLibrary'],c['BuildReceipt'],c['OfficialAdmission'],c['ProviderExecutionManifest']])
    br=json.loads(Path(c['BuildReceipt']['Path']).read_bytes());require(br['Pin']==PIN and br['LibrarySha256']==c['NativeLibrary']['Sha256'],'Original native build provenance differs')
    before=verified(root/'provider-before.json',c['ProviderBeforeSha256']);after=verified(root/'provider-after.json',c['ProviderAfterSha256'])
    require(json.loads(before.read_bytes())==json.loads(after.read_bytes()),'Provider before/after mismatch')
    expected={r['Path'].replace('\\','/'):r['Sha256'] for r in json.loads(Path(c['ProviderExecutionManifest']['Path']).read_bytes())}
    require(len(expected)==6427 and json.loads(before.read_bytes())==expected,'Original provider manifest not admitted')
    require({r['Module'] for r in c['CompiledImages']}=={'load_private557','load_private_observer557'},'Original complete compiled-image set differs')
    for r in c['CompiledImages']:
        q=verified(root/r['Path'],r['Sha256']);require(q.stat().st_size==r['Bytes'],'Compiled image size mismatch')
    admitted=[path,inv,inputs,*[(root/r['Path']).resolve() for r in rows],*[Path(r['Path']) for r in json.loads(inputs.read_bytes())]]
    return c,root,state_inputs(admitted)
