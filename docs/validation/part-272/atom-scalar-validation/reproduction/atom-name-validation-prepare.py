import pathlib,subprocess,json,hashlib,shutil
r=pathlib.Path.cwd();p=r/'artifacts/atom-name-validation-prototype';p.mkdir(exist_ok=True)
seed=[]
for name in subprocess.check_output(['git','ls-files','-z']).decode().split('\0'):
 if not name or not (name.startswith(('src/','tools/','tests/','benchmarks/','examples/')) or '/' not in name):continue
 source=r/name
 if not source.is_file():continue
 target=p/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target);seed.append({'Path':name,'Sha256':hashlib.sha256(source.read_bytes()).hexdigest().upper()})
e=r/'artifacts/atom-name-validation-evidence';e.mkdir(exist_ok=True)
old=e/'old-main-terms';old.mkdir(exist_ok=True)
for name in ['Erlang.Terms.dll','Erlang.Terms.pdb']:
 source=r/'src/Erlang.Terms/bin/Debug/net10.0'/name;shutil.copyfile(source,old/name)
(e/'old-main-terms-binding.json').write_text(json.dumps({'MainHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'MainWorkingPacketAndMixed33Imported':True,'AssemblyFiles':[{'Name':f.name,'Sha256':hashlib.sha256(f.read_bytes()).hexdigest().upper()} for f in old.glob('*')],'RelevantUnchangedSource':[{'Path':'src/Erlang.Terms/'+name,'Sha256':hashlib.sha256((r/'src/Erlang.Terms'/name).read_bytes()).hexdigest().upper()} for name in ['Atom.cs','AtomNameRegistry.cs','AtomTableLimits.cs','AtomCache.cs','CommonAtomNames.cs','ErlangPrimordialAtomNames.cs','TermOrder.cs']]},indent=2))
(r/'artifacts/atom-name-validation-seed.json').write_text(json.dumps({'Head':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'Working33PacketCollectorImported':True,'Files':seed},indent=2));print('Candidateprepared seed',len(seed),'old actualMain TermsDLL/PDB copied without executing/building')
