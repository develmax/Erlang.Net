from pathlib import Path
import subprocess,json,hashlib,shutil
r=Path.cwd();p=r/'artifacts/atom-name-validation-prototype';before=json.loads((r/'artifacts/atom-name-validation-source-freeze-revision3.json').read_text());owned={x['Path'] for x in before['Files']};oldsource={x['Path']: (p/x['Path']).read_bytes() for x in before['Files']};head=subprocess.check_output(['git','rev-parse','HEAD']).decode().strip();assert head=='2c1f6dadcf97b7301b60ea153dd6895a6a1a4687';seed=[]
for name in subprocess.check_output(['git','ls-files','-z']).decode().split('\0'):
 if not name or not(name.startswith(('src/','tools/','tests/','benchmarks/','examples/')) or '/' not in name):continue
 source=r/name
 if not source.is_file():continue
 seed.append({'Path':name,'Sha256':hashlib.sha256(source.read_bytes()).hexdigest().upper()})
 if name in owned:continue
 target=p/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
program=(r/'tests/Erlang.Tests/Program.cs').read_text();newprogram=program.replace('int failed = 0;','Test(Erlang.Tests.AtomNameValidationTestNames.Test, Erlang.Tests.AtomNameValidationChecks.Run);\n\nint failed = 0;');assert newprogram==oldsource['tests/Erlang.Tests/Program.cs'].decode().replace('\r\n','\n');(p/'tests/Erlang.Tests/Program.cs').write_bytes(oldsource['tests/Erlang.Tests/Program.cs'])
compiled=(r/'tools/Erlang.Differential/CompiledModuleCases.cs').read_text();newcompiled=compiled.replace('.. IdentifierOrderCases.Modules','.. IdentifierOrderCases.Modules, .. AtomScalarValidationCases.Modules');assert newcompiled==oldsource['tools/Erlang.Differential/CompiledModuleCases.cs'].decode().replace('\r\n','\n');(p/'tools/Erlang.Differential/CompiledModuleCases.cs').write_bytes(oldsource['tools/Erlang.Differential/CompiledModuleCases.cs'])
rows=[]
for name in sorted(owned):
 raw=(p/name).read_bytes();original=r/name;rows.append({'Path':name,'BeforeSha256':hashlib.sha256(original.read_bytes()).hexdigest().upper() if original.exists() else None,'Sha256':hashlib.sha256(raw).hexdigest().upper(),'BeforeSource':original.read_text() if original.exists() else None,'SourceContent':raw.decode()})
receipt={'Stage':'Published271SourceExactRebaseBeforePromotion','PublishedBase':head,'Files':rows,'AllOwned11ContentsUnchangedAfterRebase':all((p/n).read_bytes()==v for n,v in oldsource.items()),'NineSourceExpectedCasesUnchanged':True,'PriorPacketHostAppendRetained':True,'OtherPublishedSeedFilesExact':len([x for x in seed if x['Path'] not in owned]),'Seed':seed,'NativeExecuted':False,'MainFullExpected':6507,'MainFullExecuted':False}
f=r/'artifacts/atom-name-validation-source-freeze-rebased271.json';f.write_text(json.dumps(receipt,indent=2));print('Rebased271freezeSHA',hashlib.sha256(f.read_bytes()).hexdigest().upper());print('Ownedsourcecontents unchanged',receipt['AllOwned11ContentsUnchangedAfterRebase'],'otherpublishedseed',receipt['OtherPublishedSeedFilesExact'])
