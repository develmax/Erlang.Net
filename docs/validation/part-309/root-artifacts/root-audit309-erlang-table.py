from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd();a=r/'artifacts';p=r/'docs/ERLANG_COMPATIBILITY.md';b=p.read_bytes();text=b.decode();h=lambda b:hashlib.sha256(b).hexdigest().upper()
registered={f"{x['Function']}/{x['Arity']}" for x in json.loads((r/'docs/supported-mfas.json').read_bytes()) if x['Module']=='erlang'}
rows=re.findall(r'^\| `([^`]+)` \| ([^\n]+)',text,re.M);missing={name for name,status in rows if status.startswith('Missing;')}
stale=missing&registered;assert stale=={'dist_get_stat/1','load_nif/2','process_info/1'},stale
notes={'dist_get_stat/1':'Partially compatible; registered generation-aware counters and implementation-dependent queued-storage charge. Original statistics observations are retained; complete connected protocol and physical ERTS storage parity remain incomplete.','load_nif/2':'Partially compatible; registered exact indirect BIF bad_lib error for apply/3 and external fun. Successful literal native loading, the default native instruction handler and general NIF ABI remain incomplete.','process_info/1':'Partially compatible; registered logical default subset with twelve fields, or thirteen when registered. Four pinned physical heap/stack/GC fields remain omitted; see supported-mfas.json for limits.'}
for name,note in notes.items():
 old=f'| `{name}` | Missing; real implementation/runtime prerequisites required |';assert text.count(old)==1;text=text.replace(old,f'| `{name}` | {note} |')
remaining={name for name,status in re.findall(r'^\| `([^`]+)` \| ([^\n]+)',text,re.M) if status.startswith('Missing;')}
assert remaining=={'alloc_info/1','alloc_sizes/1','memory/0','memory/1'}
out=a/'root-part309-erlang-table-candidate';assert not out.exists();out.mkdir();data=text.encode();(out/'ERLANG_COMPATIBILITY.md').write_bytes(data)
v={'Status':'THREE_STALE_MISSING_ROWS_CORRECTED_AGAINST_ACTUAL505_REGISTRY','ParentHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'ParentPath':'docs/ERLANG_COMPATIBILITY.md','ParentSha256':h(b),'ResultSha256':h(data),'ErlangRegisteredMfas':len(registered),'CorrectedRows':notes,'RemainingUnregistered':sorted(remaining),'RuntimeChanged':False,'TestsExecuted':False}
(out/'audit.json').write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8');print(json.dumps(v))
