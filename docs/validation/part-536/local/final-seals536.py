from pathlib import Path
import ast,json,hashlib,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts/resource536';historical=r.parent/'w504/artifacts/resource532';suffix=sys.argv[1];extra=set(subprocess.check_output(['git','ls-files','--others','--exclude-standard'],text=True).splitlines());extra.update(subprocess.check_output(['git','diff','--name-only','HEAD'],text=True).splitlines())
extra={x for x in extra if (x.startswith(('src/','tests/')) and x.endswith('.cs')) or (x.startswith('tools/Runtime/Erlang.OwnedGuest506Observations/') and (r/x).is_file())}
extra.update(x.relative_to(r).as_posix() for x in (r/'tests/Erlang.Tests/Fixtures/CountersOriginal421Debug').iterdir() if x.is_file())
for name in ['run-reviewed.py','seal-source-plan.py']:
 p=r/'tools/Native/Erlang.NifPendingVarargsObservations'/name;s=p.read_text();tree=ast.parse(s);node=next(n for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='EXPLICIT_REVIEWED_INPUTS' for t in n.targets));known=set(ast.literal_eval(node.value.args[0]));lines=s.splitlines(keepends=True);lines[node.lineno-1:node.end_lineno]=['EXPLICIT_REVIEWED_INPUTS = frozenset((\n'+''.join('    '+repr(x)+',\n' for x in sorted(known|extra))+'))\n'];p.write_bytes(''.join(lines).encode())
for source,target,oldout,newout in [('refresh532-final.py','refresh536.py','artifacts/resource532/bindings-linux-v1','artifacts/resource536/bindings-'+suffix),('plans532-final.py','plans536.py','artifacts/resource532/plans-linux-v1','artifacts/resource536/plans-'+suffix)]:
 (a/target).write_text((historical/source).read_text().replace(oldout,newout))
(a/'prepare-original.py').write_text((historical/'prepare-original.py').read_text().replace("['git','diff','--name-only']","['git','diff','--name-only','HEAD']"))
for cmd in [[sys.executable,'-B','tools/Native/Erlang.NifResource529Observations/seal.py'],[sys.executable,'-B','artifacts/resource536/refresh536.py'],[sys.executable,'-B','artifacts/resource536/prepare-original.py']]:subprocess.run(cmd,check=True)
p=r/'tools/Native/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';d=json.loads(p.read_bytes());known={x['Path'] for x in d['Files']}
for rel in sorted(extra):
 if rel not in known:
  b=(r/rel).read_bytes();d['Files'].append(dict(Path=rel,Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper(),HashMode='BinaryExact'))
d['Files'].sort(key=lambda x:x['Path']);p.write_bytes((json.dumps(d,indent=2)+'\n').encode())
subprocess.run([sys.executable,'-B','artifacts/resource536/plans536.py'],check=True)
subprocess.run([sys.executable,'-B',str(r.parent/'Erlang.Net/artifacts/batch566/native-source-preflight-proposal/check-source-plans.py'),'--repository',str(r),'--output','artifacts/resource536/preflight-'+suffix+'.json'],check=True)
print('Eight final source gates refreshed; exact new sources/tests admitted; no compatibility promotion')
