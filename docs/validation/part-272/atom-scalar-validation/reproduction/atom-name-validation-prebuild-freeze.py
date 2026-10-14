from pathlib import Path
import json,hashlib,shutil
r=Path.cwd();p=r/'artifacts/atom-name-validation-prototype';e=r/'artifacts/atom-name-validation-evidence';owned=['src/Erlang.Terms/AtomNameRegistry.cs','src/Erlang.Terms/AtomNameLimits.cs','src/Erlang.Terms/AtomNameValidation.cs','tests/Erlang.Tests/AtomNameValidationChecks.cs','tests/Erlang.Tests/AtomNameValidationTestNames.cs','tests/Erlang.Tests/AtomNameValidationTestParameters.cs','tests/Erlang.Tests/Program.cs','tools/Erlang.Differential/AtomScalarValidationCases.cs','tools/Erlang.Differential/CompiledModuleCases.cs'];rows=[]
for name in owned:
 raw=(p/name).read_bytes();original=r/name;rows.append({'Path':name,'Sha256':hashlib.sha256(raw).hexdigest().upper(),'BeforeSha256':hashlib.sha256(original.read_bytes()).hexdigest().upper() if original.exists() else None,'BeforeSource':original.read_text() if original.exists() else None,'SourceContent':raw.decode()})
oldneg=json.loads((e/'old-negative.json').read_text(encoding='utf-8-sig'));assert oldneg['InvalidNameAccepted'] and oldneg['Equal'] and not oldneg['SameHash'] and oldneg['MapEntries']==2 and oldneg['RegistryGrowth']==3 and oldneg['TooLongAccepted'];assert len(oldneg['Documents'])==4 and all(x['Exact'] for x in oldneg['Documents'])
f=r/'artifacts/atom-name-validation-source-freeze-initial.json';f.write_text(json.dumps({'Stage':'PreBuildAfterActualOldNegative','Files':rows,'ActualOldNegative':oldneg,'NativeExecuted':False,'ProductionFiles':3,'NewCompiledCases':8,'NewHostGroups':1,'CoreRegistrationsAdded':0},indent=2));print('Prebuildfreeze SHA',hashlib.sha256(f.read_bytes()).hexdigest().upper())
style=e/'owned-style-root'
for directory in ['src','tools','tests','benchmarks','examples']:(style/directory).mkdir(parents=True,exist_ok=True)
for name in owned:
 dest=style/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p/name,dest)
