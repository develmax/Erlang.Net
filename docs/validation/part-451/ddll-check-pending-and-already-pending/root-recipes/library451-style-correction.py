from pathlib import Path
import hashlib,importlib.util,json,os,subprocess
r=Path.cwd();out=r/'artifacts/library451-style-correction1';assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
old=json.loads((r/'artifacts/library451-gates1/completion.json').read_bytes());assert not old['Passed'] and len(old['Stages'])==5 and all(x['Passed'] for x in old['Stages'][:4]) and not old['Stages'][4]['Passed']
p=r/'tests/Erlang.Tests/Program.cs';before=p.read_bytes();base=subprocess.check_output(['git','show','HEAD:tests/Erlang.Tests/Program.cs']);needle=b'Test(Erlang.Tests.ConnectedCandidateHostNames450.Test, Erlang.Tests.ConnectedCandidateHostChecks450.Run);';after=base.replace(needle,needle+b'\nTest(Erlang.Tests.PendingCandidateHostNames451.Test, Erlang.Tests.PendingCandidateHostChecks451.Run);')
assert before.replace(b'\r\n',b'\n')==after and before!=after
beforehash=sha(p);p.write_bytes(after)
bindings={str(f):sha(f) for f in [r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll',r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll']}
helper=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned451style',helper);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
m.owned(['C:/Program Files/dotnet/dotnet.exe','tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',180,dict(os.environ),str(r))
assert all(sha(f)==h for f,h in bindings.items())
receipt={'Passed':True,'PriorFunctionalGatesPassed':True,'InitialStyleFailureRetained':True,'OnlyCorrection':'Program.cs CRLF normalized to existing LF convention; complete source bytes otherwise exact including one new registration','WholeNormalizedBytesIdentical':True,'BeforeSha256':beforehash,'AfterSha256':sha(p),'BinariesUnchanged':bindings,'NoBehaviorChange':True,'FunctionalRerunRequired':False,'ExactFinalSourceFullCiStillRequired':True}
(out/'completion.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
