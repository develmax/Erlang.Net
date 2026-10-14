from pathlib import Path
import json,re,subprocess,hashlib,sys
r=Path(__file__).resolve().parents[1];a=r/'artifacts';head='783088ff0b6095c1e984e913b385a1cfceae6d7c';pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert subprocess.check_output(['git','-C',str(r.parent/'otp'),'rev-parse','OTP-29.1.1^{commit}']).decode().strip()==pin
blobs={name:subprocess.check_output(['git','-C',str(r.parent/'otp'),'show','OTP-29.1.1:'+name]) for name in ['erts/preloaded/src/erlang.erl','erts/emulator/beam/bif.tab']}
source=re.sub(r'"""[\s\S]*?"""|"(?:\\.|[^"\\])*"|%[^\n]*',lambda m: '\n'*m.group().count('\n'),blobs['erts/preloaded/src/erlang.erl'].decode())
reference=set()
for block in re.findall(r'-export\s*\(\s*\[(.*?)\]\s*\)',source,re.S):
 for name,arity in re.findall(r"('(?:\\.|[^'])*'|[a-z][\w@]*)\s*/\s*([0-9]+)",block):
  reference.add((name[1:-1] if name.startswith("'") else name,int(arity)))
reference.update((name,int(arity)) for name,arity in re.findall(r'(?m)^\s*(?:bif|ubif|hbif)\s+erlang:([a-z_0-9]+)/([0-9]+)',blobs['erts/emulator/beam/bif.tab'].decode()))
supported=json.loads((r/'docs/supported-mfas.json').read_text(encoding='utf-8-sig'))
registered={(row['Function'],row['Arity']) for row in supported if row['Module']=='erlang'}
assert len(supported)==493 and len(reference)==351 and len(registered)==335 and registered<=reference
path=r/'docs/ERLANG_COMPATIBILITY.md';old=path.read_text(encoding='utf-8-sig')
rows=re.findall(r'^\| `(.+)/(\d+)` \| (.+) \|$',old,re.M)
assert len(rows)==351 and {(name,int(arity)) for name,arity,_ in rows}==reference
changes=[]
def replace(m):
 name,arity=m.group(1),int(m.group(2));state='Registered; bounded functional evidence, see supported-mfas.json' if (name,arity) in registered else 'Missing; real implementation/runtime prerequisites required'
 if state!=m.group(3):changes.append({'Mfa':name+'/'+str(arity),'Before':m.group(3),'After':state})
 return '| `'+name+'/'+str(arity)+'` | '+state+' |'
new=re.sub(r'^\| `(.+)/(\d+)` \| (.+) \|$',replace,old,flags=re.M)
stale='Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5.351 explicit erlang.erl/bif.tab union exports.274 registered,77 missing,total424 direct MFAs.78.1% API presence is not semantic completion.'
assert stale in new
current='Current API presence: pinned OTP-29.1.1 (`'+pin+'`) declares 351 erlang exports in the explicit erlang.erl/bif.tab union. The current supported-mfas.json registers 335 of these exports (95.4%); 16 are missing. The full registry contains 493 direct MFAs. Presence does not establish complete semantic compatibility. Historical part notes above retain their original counts and outcomes.'
new=new.replace(stale,current)
(a/'part270-ERLANG_COMPATIBILITY.candidate.md').write_bytes(new.encode('utf-8'))
receipt={'Status':'SourceAndRegistryPresenceVerified','OriginalPin':pin,'ReferenceExports':351,'RegisteredErlang':335,'MissingErlang':16,'AllRegistered':493,'ChangedRows':changes,'MissingMfas':[name+'/'+str(arity) for name,arity in sorted(reference-registered)],'PinnedSourceSha256':{name:hashlib.sha256(blob).hexdigest().upper() for name,blob in blobs.items()},'BeforeSha256':hashlib.sha256(path.read_bytes()).hexdigest().upper(),'CandidateSha256':hashlib.sha256(new.encode()).hexdigest().upper(),'SemanticCompletionClaim':False}
(a/'part270-compatibility-refresh-guard.json').write_text(json.dumps(receipt,indent=2)+'\n')
if '--apply' in sys.argv:path.write_bytes(new.encode('utf-8'))
print('335/351 presence verified; changed rows:',len(changes),'applied:', '--apply' in sys.argv)
