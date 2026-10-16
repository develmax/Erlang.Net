"""Root-reviewable helpers; no execution until an actual independent full guard exists."""
from pathlib import Path
import hashlib,json,subprocess
PARENT='2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b'
SOURCE='08A9020739E9E8C3ADA1741FDBB099779B02D41ECF3D114C24E691DF3A2A488B'
CLOSURE='A76624B096F08F6F6781EF2ACE25D1D268A04E39376DEC4F5416FDD05372FDFA'
COUNTS={'Verified compatible':5,'Known differences':19,'Needs verification':563}
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
 assert independent['Status'].startswith('PASS') and independent['Passed']==7268 and independent['Failed']==0 and independent['SourceFreezeSha256']==SOURCE and independent['SourceClosureSha256']==CLOSURE
 assert g['Status']=='PASS_ACTUAL_ISOLATED348_FULL' and g['Passed']==7268 and g['Failed']==0 and g['SingleUninterruptedFullPass'] and g['ParentHead']==PARENT
 assert exit['ExitCode']==0 and exit['Settled'] and not exit['TimedOut']
 assert sha(full/'source-freeze.json')==SOURCE and sha(full/'source-resource-closure.json')==CLOSURE
 owned=exact(candidate,full/'source-freeze.json');closure=exact(candidate,full/'source-resource-closure.json');packages=exact(candidate,full/'package-resources.json')
 assert len(owned)==25 and len(closure)==2764 and len(packages)==6
 t=read(full/'tests.json');old=read(full/'previous-tests.json');names=[r['Name'] for r in t['Tests']];previous=[r['Name'] for r in old['Tests']]
 assert t['Passed']==len(names)==len(set(names))==7268 and t['Failed']==0 and all(r['Status']=='Passed' for r in t['Tests'])
 assert old['Passed']==len(previous)==7259 and old['Failed']==0 and [n for n in names if n in set(previous)]==previous
 added=read(full/'added-tests.json');assert [n for n in names if n not in set(previous)]==added['Names']==g['AddedTestNames'] and len(added['Names'])==9
 assert read(full/'integration-results.json')==g['IntegrationResults'] and len(g['IntegrationResults'])==11 and len(read(full/'packages.json')['Files'])==2
 ledger=read(candidate/'docs/mfa-compatibility.json');assert {s:sum(e['Status']==s for e in ledger['Entries']) for s in COUNTS}==COUNTS and len(ledger['Entries'])==587
 assert len(ledger['VerifiedSourceFiles'])==35
 for row in ledger['VerifiedSourceFiles']:assert sha(checked(candidate,row['Path']))==row['Sha256']
 children=read(full/'bounded-child-controls.json')['Files'];assert len(children)==12
 expected={(family,operation,32768) for family in ['tuple','list-head','map-value','fun-environment','native-record'] for operation in ['compare','hash']}|{('tuple','shallow',6),('tuple','snapshot',0)}
 assert {(r['Value']['Family'],r['Value']['Operation'],r['Value']['Depth']) for r in children}==expected
 for row in children:
  receipt=checked(candidate,row['Path']);assert sha(receipt)==row['ReceiptSha256'];v=read(receipt);assert v==row['Value'] and v['Started'] and v['Settled'] and not v['TimedOut'] and v['ExitCode']==0 and not v['LifecycleFailures']
  assert v['StandardOutputComplete'] and v['StandardErrorComplete'] and sha(receipt.with_name('stdout.txt'))==v['StandardOutputSha256'] and sha(receipt.with_name('stderr.txt'))==v['StandardErrorSha256']
 return g,owned,closure
