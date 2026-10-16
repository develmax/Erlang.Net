"""Root-reviewable helpers; no execution until an actual independent full guard exists."""
from pathlib import Path
import hashlib,json,subprocess
PARENT='9c7949f87348223a61c65d37d859b02841036374'
SOURCE='92CA87634B19A5CB999C9BA0C94F2DA638E022A1325EF3D3B1A7E4C90A5E3825'
CLOSURE='3E624D130B22232E40E47982D4449D6482469D2BABF0815F3E5521E293E67805'
COUNTS={'Verified compatible':5,'Known differences':17,'Needs verification':565}
read=lambda p:json.loads(Path(p).read_bytes())
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,value):
 with Path(p).open('x',encoding='utf-8',newline='\n') as f:json.dump(value,f,indent=2);f.write('\n')
def checked(root,name):
 assert isinstance(name,str) and not Path(name).is_absolute() and ':' not in name and '\\' not in name and all(x not in ('','.','..') for x in name.split('/'))
 p=(root/name).resolve(strict=True);assert p.is_relative_to(root) and p.is_file();return p
def exact(root,manifest):
 rows=read(manifest)['Files'];assert len(rows)==len({r['Path'] for r in rows})
 for row in rows:
  p=checked(root,row['Path']);assert p.stat().st_size==row['Bytes'] and sha(p)==row['Sha256'],row['Path']
 return {r['Path'] for r in rows}
def key(row):return row['Module'],row['Function'],row['Arity']
def authority(candidate,full,peer,peer_sha):
 assert sha(peer)==peer_sha.upper()
 independent=read(peer);g=read(full/'completion-guard.json');exit=read(full/'full-exit.json')
 assert independent['Status'].startswith('PASS') and independent['Passed']==7259 and independent['Failed']==0 and independent['SourceFreezeSha256']==SOURCE and independent['SourceClosureSha256']==CLOSURE
 assert g['Status']=='PASS_ACTUAL_ISOLATED345_FULL' and g['Passed']==7259 and g['Failed']==0 and g['SingleUninterruptedFullPass'] and g['ParentHead']==PARENT
 assert exit['ExitCode']==0 and exit['Settled'] and not exit['TimedOut']
 assert sha(full/'source-freeze.json')==SOURCE and sha(full/'source-resource-closure.json')==CLOSURE
 owned=exact(candidate,full/'source-freeze.json');closure=exact(candidate,full/'source-resource-closure.json');packages=exact(candidate,full/'package-resources.json')
 assert len(owned)==34 and len(closure)==2751 and len(packages)==6
 t=read(full/'tests.json');old=read(full/'previous-tests.json');names=[r['Name'] for r in t['Tests']];previous=[r['Name'] for r in old['Tests']]
 assert t['Passed']==len(names)==len(set(names))==7259 and t['Failed']==0 and all(r['Status']=='Passed' for r in t['Tests'])
 assert old['Passed']==len(previous)==7224 and old['Failed']==0 and [n for n in names if n in set(previous)]==previous
 added=read(full/'added-tests.json');assert [n for n in names if n not in set(previous)]==added['Names']==g['AddedTestNames'] and len(added['Names'])==35
 assert read(full/'integration-results.json')==g['IntegrationResults'] and len(g['IntegrationResults'])==11 and len(read(full/'packages.json')['Files'])==2
 ledger=read(candidate/'docs/mfa-compatibility.json');assert {s:sum(e['Status']==s for e in ledger['Entries']) for s in COUNTS}==COUNTS and len(ledger['Entries'])==587
 assert len(ledger['VerifiedSourceFiles'])==30
 for row in ledger['VerifiedSourceFiles']:assert sha(checked(candidate,row['Path']))==row['Sha256']
 return g,owned,closure
