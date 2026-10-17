from pathlib import Path
import json,hashlib,re,difflib
R=Path(__file__).resolve().parents[2]
P=R/'artifacts/spawn-priority351-preparation'
C=R/'artifacts/spawn-priority351-candidate'
O=Path(__file__).resolve().parent
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
freeze=P/'runtime-styled-source-freeze.json'
assert h(freeze)=='53A8C9186D0C62E151174902FB7D3C079FC882B58F5F4F654FBC3488ED15390A'
rows=json.loads(freeze.read_bytes())['Files'];assert len(rows)==20
owned={x['Path'] for x in rows};changes=[]
for x in rows:
 old=R/'artifacts/spawn-priority351-proposal3/source'/x['Path'];snapshot=P/'runtime-styled-source'/x['Path'];actual=C/x['Path']
 assert h(old)==x['Source3Sha256'] and h(snapshot)==h(actual)==x['Sha256'] and actual.stat().st_size==x['Bytes']
 a=old.read_text();b=actual.read_text()
 assert re.sub(r'\s+','',a)==re.sub(r'\s+','',b),x['Path']
 if a!=b:changes.append(dict(Path=x['Path'],Diff=''.join(difflib.unified_diff(a.splitlines(True),b.splitlines(True),n=2))))
closure=P/'runtime-build-closure.json'
assert h(closure)=='D479BCE0E94257F01917E3C6BE5E73235D8237ECF7319F3FFFA6FD5D4CD4FED2'
cr=json.loads(closure.read_bytes())['Files'];assert len(cr)==2780
for x in cr:
 a=C/x['Path'];assert a.stat().st_size==x['Bytes'] and h(a)==x['Sha256']
baseline=json.loads((P/'baseline-current-main-raw.json').read_bytes())['Files'];assert len(baseline)==2768
unowned=[x for x in baseline if x['Path'] not in owned];assert len(unowned)==2760
for x in unowned:assert h(C/x['Path'])==h(R/x['Path'])==x['Sha256']
v=dict(Status='PASS_INDEPENDENT_STYLED_PRIORITY351_PRODUCTION20_AND_BUILD_CLOSURE_SOURCE_REVIEW',StyledFreezeSha256=h(freeze),Source3FreezeSha256='652DD4A9646BCF0A14FC6CA76C7DC88BEE72291E3FC8F453E691EC1023B6F140',Source3PeerSha256='8D96FAA8337AD4EEBE2CE0104A8E3D170A51F750FD442DDA15B5BBA94383D0FB',ProductionRows=20,BuildClosureRows=2780,BuildClosureSha256=h(closure),BaselineMainRows=2768,UnownedMainRawRows=2760,UnownedRawVariances=0,ComparisonMethod='All20 current candidate/snapshot/frozen hashes rechecked; whitespace-removed decoded text exact. All8 changed-file unified diffs independently inspected: only blank lines, indentation and comma-delimited call-argument wraps; no literal/comment/operator/identifier or token-boundary edits. Concrete equivalent whitespace diff review; no Roslyn tokenizer execution claimed.',ChangedFiles=changes,BoundedProductionCompileAdmissible=True,FocusedHarnessGate='Separate immutable runner/target/source handoff still pending. This receipt binds Root build inputs, not unreviewed focused harness/results.',InitialPassiveReviewPreconditionCorrection='Initial local inline review assumed baseline manifest contained only2760unowned rows; actual schema contains2768Main rows including8existing owned parents. Corrected by excluding exact20owned path set before asserting2760. No production/code/runtime failure.',MainModified=False,SdkExecuted=False,NetworkUsed=False,OriginalExecuted=False,CompatibilityStatusesChanged=False)
f=O/'report.json';assert not f.exists();f.write_text(json.dumps(v,indent=2)+'\n',newline='\n');print(h(f))
