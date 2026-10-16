from pathlib import Path
import importlib.util,json,hashlib
R=Path.cwd();p=R/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py';assert hashlib.sha256(p.read_bytes()).hexdigest().upper()=='A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
spec=importlib.util.spec_from_file_location('trusted_local_passive_decoder',p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
root=R/'artifacts/part346-remote-new-review/real-peer/artifact-12079855433/capture';rows=[]
def raw(p):
 b=p.read_bytes();v,n=m.decode_one(b);assert n==len(b);return v
A=lambda s:('atom',s)
for case in ['fun_spawn','fun_spawn_link','fun_spawn_opt']:
 d=root/case;info=dict(x[1] for x in raw(d/'origin-fun.etf')[1]);state=dict(raw(d/'child-state.etf')[1]);capture=dict(state[A('captured')][1]);env=info[A('env')]
 rows.append({'Case':case,'Env':repr(env),'ExpectedSingleMapEnv':repr(('list',(state[A('captured')],))),'EnvMatches':env==('list',(state[A('captured')],)),'CreatorPid':repr(info[A('pid')]),'CaptureParentPid':repr(capture[A('parent')]),'CreatorMatchesParent':info[A('pid')]==capture[A('parent')],'OriginInfoRawSha256':hashlib.sha256((d/'origin-fun.etf').read_bytes()).hexdigest().upper()})
out=R/'artifacts/part346-real-peer-independent/origin-inspection.json';assert not out.exists();out.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print(json.dumps(rows))
