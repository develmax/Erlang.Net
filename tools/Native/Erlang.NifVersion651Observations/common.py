"""Version651 source-only evidence utilities; process/provider implementations are existing sealed helpers."""
import hashlib, importlib.util, json, os
from pathlib import Path
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL='tools/Native/Erlang.NifVersion651Observations'
OWNED='tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py'
PROVIDER='tools/Runtime/Erlang.OwnedGuest506Observations/collect.py'
def need(x,text):
    if not x: raise RuntimeError(text)
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,value):
    with Path(p).open('x',encoding='utf-8') as f: json.dump(value,f,indent=2)
def load(name,p):
    s=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def checked(p,h):
    p=Path(p).resolve();need(p.is_file() and sha(p)==h.upper(),'Changed input '+str(p));return p
def guard(repo,tool,approved,filename="inputs.json"):
    need(tool==repo/TOOL,'Integrated sibling tool path required')
    checked(tool/filename,approved);plan=json.loads((tool/filename).read_bytes());need(plan['Pin']==PIN,'Wrong pin')
    for row in plan['Inputs']: checked((tool if row['Scope']=='Tool' else repo)/row['Path'],row['Sha256'])
    return plan
def fresh(repo,p):
    p=Path(p).resolve();need(p.is_relative_to(repo/'artifacts/nif-version651') and not p.exists(),'New confined output required');p.mkdir(parents=True);return p
def environment():
    env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1')
    for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_USE_SERVER','ERLC_SERVER_ID','ERLC_CONFIGURATION','ERL_ROOTDIR']:env.pop(key,None)
    return env
def row(p):
    p=Path(p).resolve();return dict(Path=str(p),Sha256=sha(p),Bytes=p.stat().st_size)
def snapshot(paths): return [row(p) for p in sorted(set(Path(p).resolve() for p in paths))]
def verify_rows(rows):
    for r in rows:need(checked(r['Path'],r['Sha256']).stat().st_size==r['Bytes'],'Input size changed')
def settled(p):
    return p.get('Settled') is True and p.get('ExitCode')==0 and p.get('TimedOut') is False and not p.get('Failures')
def stage(owned,argv,d,name,env,timeout=120):
    result=owned.owned([str(v) for v in argv],d,name,timeout,env,str(d));need(settled(result),'Unsettled '+name);return result
def estr(value):return json.dumps(str(value).replace('\\','/'),ensure_ascii=True)
