from pathlib import Path
import json,hashlib,subprocess,ast
R=Path.cwd();A=R/'artifacts';OLD=A/'part351-remote-new-review';F=A/'part352-ci-review/correction-final';H='4c9cb16e7c52bc10e54ceaf652797d100def488b';P='547a2dd266586589a90c7883dac88d09a5a1de20';INITIAL='c09a075b7921b3875f526ea0da9d011c263e3bad';RUN=38991296407;sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());enc=lambda x:(json.dumps(x,indent=2)+'\n').encode()
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==H
# Source-only actual published136 manifest, independently bound to initial131 staged bytes +exact7 correction paths.
initial=read(A/'root-part352-promotion/stage-guard.json');correction=read(A/'root-part352-attrs-correction/files.json');assert len(initial['Files'])==131 and len(correction['Files'])==7
get=lambda head,p:subprocess.check_output(['git','show',head+':'+p])
for row in initial['Files']:assert sha(get(INITIAL,row['Path']))==row['StagedSha256']
diff=subprocess.check_output(['git','diff','--name-only',P,H],text=True).splitlines();assert len(diff)==len(set(diff))==136
assert set(diff)=={r['Path'] for r in initial['Files']}|{r['Path'] for r in correction['Files']}
corrected=[]
for row in correction['Files']:
 b=get(H,row['Path']);working=(R/row['Path']).read_bytes();assert sha(working)==row['Sha256']
 mode='RawExact'
 if b!=working:
  assert row['Path'] in ['.gitattributes','docs/changelog/0055.md'] and b==working.replace(b'\r\n',b'\n');mode='ExactTextLF'
 corrected.append(dict(Path=row['Path'],WorkingSha256=row['Sha256'],PublishedSha256=sha(b),ComparisonMode=mode))
manifest=dict(Head=H,Base=P,InitialHead=INITIAL,InitialStageSha256=sha((A/'root-part352-promotion/stage-guard.json').read_bytes()),CorrectionFilesSha256=sha((A/'root-part352-attrs-correction/files.json').read_bytes()),FinalBlobs=[dict(Path=p,Bytes=len(get(H,p)),Sha256=sha(get(H,p))) for p in diff],CorrectionBindings=corrected)
(F/'published-source-manifest.json').write_bytes(enc(manifest))
# Adapt reviewed exact-once GET downloader; no execution and no monitor/new run discovery in this helper.
s=(OLD/'download-terminal.ps1').read_text();start=s.index('param(');end=s.index('$out=')
header='''$ErrorActionPreference='Stop'
$target=Get-Content artifacts/part352-ci-review/correction-final/monitor/mapped-run.json -Raw | ConvertFrom-Json
$Kind='ci';$rid=$target.RunId;$head=$target.Head
if($head -cne '4c9cb16e7c52bc10e54ceaf652797d100def488b' -or $rid -ne 38991296407 -or $target.Path -cne '.github/workflows/validate.yml' -or $target.Event -cne 'push' -or $target.RunAttempt -ne 1){throw 'Published352 corrected subject/run differs'}
'''
s=header+s[end:];s=s.replace('artifacts/part351-remote-new-review/','artifacts/part352-ci-review/correction-final/').replace('Erlang.Net-part351-terminal-readonly','Erlang.Net-part352-corrected-terminal-readonly');(F/'download-terminal.ps1').write_text(s,newline='\n')
# ZIP extraction derives subject only from the known fixed corrected CI mapping; no original/manual workflow.
s=(OLD/'extract-and-freeze.py').read_text();start=s.index('r=Path.cwd()');end=s.index("assert not (d/'download-manifest.json')")
header="""r=Path.cwd();a=r/'artifacts';kind='ci';mappingpath=a/'part352-ci-review/correction-final/monitor/mapped-run.json';target=json.loads(mappingpath.read_bytes());rid=target['RunId'];d=a/'part352-ci-review/correction-final/ci';j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert target['Head']=='4c9cb16e7c52bc10e54ceaf652797d100def488b' and rid==38991296407 and target['Path']=='.github/workflows/validate.yml' and target['Event']=='push' and target['RunAttempt']==1
mapping={'PublishedHead':target['Head']}
"""
s=s[:start]+header+s[end:];s=s.replace("(subject['OrdinaryDispatchAttemptTime'] if kind=='ordinary' else None)","None").replace("(subject['OrdinaryDispatchAcceptedTime'] if kind=='ordinary' else None)","None").replace("h(a/'part351-remote-new-review/monitor/mapped-runs.json')","h(mappingpath)");ast.parse(s);(F/'extract-and-freeze.py').write_text(s,newline='\n')
s=(OLD/'passive-common.py').read_text().replace('part351-remote-new-review','part352-ci-review/correction-final').replace("assert kind in ('ci','ordinary') and head=='547a2dd266586589a90c7883dac88d09a5a1de20' and rid=={'ci':38989239356,'ordinary':38989291376}[kind]","assert kind=='ci' and head=='4c9cb16e7c52bc10e54ceaf652797d100def488b' and rid==38991296407")
ast.parse(s);(F/'passive-common.py').write_text(s,newline='\n')
s=(OLD/'common351.py').read_text().replace('reviewed351','reviewed352 corrected').replace('part351-remote-new-review','part352-ci-review/correction-final').replace('transport351','transport352');s=s.replace('BA2160DB60288DDD0E2C4D18B50A3155DE0156E7CB6A1DA68D40F42242C68C32',sha((F/'passive-common.py').read_bytes()));ast.parse(s);(F/'common352.py').write_text(s,newline='\n')
print(json.dumps(dict(ManifestSha256=sha((F/'published-source-manifest.json').read_bytes()),FinalBlobs=len(diff))))
