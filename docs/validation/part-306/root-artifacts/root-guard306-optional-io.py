from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts'; read=lambda p:json.loads(p.read_bytes()); sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert read(a/'root-part306-full-exit.json')['ExitCode']==0
t=read(a/'tests.json'); name='runtime/halt-dedicated-raw-process-numeric-errors-abort-and-real-dump'
assert t['Passed']==6627 and t['Failed']==0 and sum(x['Name']==name and x['Status']=='Passed' for x in t['Tests'])==1
host=r/'tests/Erlang.Tests/HaltSubprocessHostChecks.cs'; io=r/'tests/Erlang.Tests/IoCharacterHostChecks.cs'; counts=r/'tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs'
assert 'await IoCharacterHostChecks.Run();' in host.read_text()
s=io.read_text(); assert s.index('DefaultCoreAndOtp')<s.index('IoCharacterRequests.Register(runtime)')<s.index('WithOptionalIo')
assert 'DefaultCoreAndOtp = 504;' in counts.read_text() and 'WithOptionalIo = 506;' in counts.read_text()
out=a/'root-part306-optional-io-guard.json'; assert not out.exists()
v={'Status':'PASS_ACTUAL_FULL_HOST_DEFAULT504_OPTIONAL_IO506','DefaultExports':504,'OptionalIoExports':506,'PassedHost':name,'EvidenceMethod':'Passed full HOST awaits IoCharacterHostChecks.Run, which checks actual registry counts before and after optional registration. No separate additional run.','IndependentAdditionalExecution':False,'FullTestsSha256':sha(a/'tests.json'),'Sources':[{'Path':p.relative_to(r).as_posix(),'Sha256':sha(p)} for p in [host,io,counts]]}
out.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8');print(json.dumps(v))
