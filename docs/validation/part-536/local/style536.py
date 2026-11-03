from pathlib import Path
import subprocess,json,hashlib,sys
r=Path.cwd();out=r/'artifacts/resource536'/sys.argv[1];out.mkdir(exist_ok=False)
for base in ['src','tools','tests','benchmarks','examples']:(out/base).mkdir()
changed=set(subprocess.check_output(['git','diff','HEAD','--name-only'],text=True).splitlines());changed.update(subprocess.check_output(['git','ls-files','--others','--exclude-standard'],text=True).splitlines())
paths=sorted(p for p in changed if p.endswith('.cs') and p.split('/')[0] in ['src','tests','tools'])
before={}
for name in paths:
 b=(r/name).read_bytes();before[name]=hashlib.sha256(b).hexdigest().upper();p=out/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
style=r/'tools/Build/Erlang.Style/bin/Release/net10.0/Erlang.Style.dll'
subprocess.run(['dotnet','exec','--fx-version','10.0.11',str(style),'--write',str(out)],check=True)
formatted=[]
for name in paths:
 assert hashlib.sha256((r/name).read_bytes()).hexdigest().upper()==before[name]
 b=(out/name).read_bytes()
 if b!=(r/name).read_bytes():(r/name).write_bytes(b);formatted.append(name)
(out/'application.json').write_text(json.dumps({'Selected':len(paths),'Formatted':formatted,'Preimages':before},indent=2)+'\n')
subprocess.run(['dotnet','exec','--fx-version','10.0.11',str(style),'--check',str(out)],check=True)
print('Changed-source style selection',len(paths),'formatted',len(formatted))

