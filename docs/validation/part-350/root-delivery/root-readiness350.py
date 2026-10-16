from pathlib import Path
import hashlib,json,subprocess,time
r=Path.cwd();o=r/'artifacts/root-part350-readiness1';assert not o.exists();o.mkdir()
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
dll=r/'artifacts/additional-type-predicate-candidate/tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll';assert sha(dll.read_bytes())=='AE3AB09B4790A00C61C1873813C91A69926A4EF7F369F4F6B7CE2E63547BE341'
report=r/'artifacts/additional-type-predicate350-full-actual1/tests.json';assert sha(report.read_bytes())=='4D8F1EDACF32E26B882EF4E3D9F16F0F613C6C3DCDDADC4A27A0043C59A8F4F4'
assert (read(report)['Passed'],read(report)['Failed'])==(7286,0)
ledger=read(r/'docs/mfa-compatibility.json');assert len(ledger['VerifiedSourceFiles'])==59
for row in ledger['VerifiedSourceFiles']:assert sha((r/row['Path']).read_bytes())==row['Sha256']
registry=r/'docs/supported-mfas.json';assert sha(registry.read_bytes())=='3B2591EFD44F896F4F12ACC1A73428C4AD90FFFE1723E76B38CFDB5722157F29'
cmd=['dotnet',str(dll),'readiness',str(r/'docs'),str(report),str(o/'generated')];start=time.monotonic()
with (o/'stdout.txt').open('xb') as stdout,(o/'stderr.txt').open('xb') as stderr:
    p=subprocess.run(cmd,cwd=r,stdout=stdout,stderr=stderr,timeout=60)
receipt=dict(Command=cmd,ExitCode=p.returncode,Settled=True,TimedOut=False,Seconds=time.monotonic()-start,ToolDllSha256=sha(dll.read_bytes()),InputReportSha256=sha(report.read_bytes()),LedgerSha256=sha((r/'docs/mfa-compatibility.json').read_bytes()))
(o/'exit.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');assert p.returncode==0
v=read(o/'generated/module-readiness.json');assert (v['VerifiedCompatibleMfas'],v['KnownDifferenceMfas'],v['NeedsVerificationMfas'],v['LocalPassedTests'],v['RegisteredMfas'])==(10,23,554,7286,587)
rows=[];normalizations=[]
for name in ['module-readiness.json','MODULE_READINESS.md','API_COMPATIBILITY.md']:
    src=o/'generated'/name;b=src.read_bytes();rawsha=sha(b)
    if name.endswith('.md'):
        b=b.rstrip(b'\r\n')+b'\n';src=o/'normalized'/name;src.parent.mkdir(exist_ok=True);src.write_bytes(b)
    target='docs/'+name;rows.append(dict(SourcePath=str(src),TargetPath=target,Bytes=len(b),Sha256=sha(b)));normalizations.append(dict(Path=name,RawCliSha256=rawsha,FinalSha256=sha(b),OnlyTrailingMarkdownBlankLinesRemoved=name.endswith('.md')))
(o/'output-bindings.json').write_text(json.dumps(normalizations,indent=2)+'\n',encoding='utf-8')
for name in ['exit.json','stdout.txt','stderr.txt','output-bindings.json']:
    src=o/name;b=src.read_bytes();rows.append(dict(SourcePath=str(src),TargetPath='docs/validation/part-350/root-readiness/'+name,Bytes=len(b),Sha256=sha(b)))
plan=dict(Status='ACTUAL350_CLI_READINESS_TEN_VERIFIED',Files=rows,Counts=[10,23,554],LocalPassed=7286,NewSdkBuild=False)
(o/'copy-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
for row in rows:
    dst=r/row['TargetPath'];dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(Path(row['SourcePath']).read_bytes())
print(json.dumps(dict(Counts=[10,23,554],Rows=len(rows),CopyPlanSha256=sha((o/'copy-plan.json').read_bytes()))))

