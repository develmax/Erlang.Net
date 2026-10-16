"""Generate successor Root delivery helper drafts; never run their mutation actions."""
from pathlib import Path
import hashlib,json,ast,subprocess,sys
R=Path.cwd();A=R/'artifacts/part345-delivery-helpers-draft';O=R/'artifacts/part348-delivery-helpers-draft';O.mkdir(exist_ok=True);sha=lambda b:hashlib.sha256(b).hexdigest().upper()
assert sha((A/'source-freeze.json').read_bytes())=='00CDA4C6D5944C09161373EE342C14E48060B2D608CEB4AAEC6B5DDE03C0D4C3'
source='08A9020739E9E8C3ADA1741FDBB099779B02D41ECF3D114C24E691DF3A2A488B';closure='A76624B096F08F6F6781EF2ACE25D1D268A04E39376DEC4F5416FDD05372FDFA';parent='2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b'
for name in ['common345.py','plan345.py','promote345.py','finalize345.py','readiness345.py','stage345.py']:
 s=(A/name).read_text(encoding='utf-8').replace('345','348').replace('9c7949f87348223a61c65d37d859b02841036374',parent).replace('92CA87634B19A5CB999C9BA0C94F2DA638E022A1325EF3D3B1A7E4C90A5E3825',source).replace('3E624D130B22232E40E47982D4449D6482469D2BABF0815F3E5521E293E67805',closure)
 s=s.replace('7259','7268').replace('7224','7259').replace('2751','2764').replace('Owned34','Owned25')
 s=s.replace("'Known differences':17,'Needs verification':565","'Known differences':19,'Needs verification':563").replace('(5,17,565)','(5,19,563)')
 s=s.replace('len(owned)==34','len(owned)==25').replace("len(ledger['VerifiedSourceFiles'])==30","len(ledger['VerifiedSourceFiles'])==35").replace("len(added['Names'])==35","len(added['Names'])==9")
 if name=='common345.py':
  s=s.replace(' return g,owned,closure', ''' children=read(full/'bounded-child-controls.json')['Files'];assert len(children)==12
 expected={(family,operation,32768) for family in ['tuple','list-head','map-value','fun-environment','native-record'] for operation in ['compare','hash']}|{('tuple','shallow',6),('tuple','snapshot',0)}
 assert {(r['Value']['Family'],r['Value']['Operation'],r['Value']['Depth']) for r in children}==expected
 for row in children:
  receipt=checked(candidate,row['Path']);assert sha(receipt)==row['ReceiptSha256'];v=read(receipt);assert v==row['Value'] and v['Started'] and v['Settled'] and not v['TimedOut'] and v['ExitCode']==0 and not v['LifecycleFailures']
  assert v['StandardOutputComplete'] and v['StandardErrorComplete'] and sha(receipt.with_name('stdout.txt'))==v['StandardOutputSha256'] and sha(receipt.with_name('stderr.txt'))==v['StandardErrorSha256']
 return g,owned,closure''')
 ast.parse(s);(O/name.replace('345','348')).write_text(s,encoding='utf-8')
readme='''# Part348 conditional Root delivery helpers

These are source-only successor drafts of reviewed345 helpers. No promotion, registry restoration, Tool action or staging is executed while the Root-owned canonical full run is active. All actions refuse until the actual full and independent guard settle at7268/0, eleven integration outcomes and two packages, with exact25/2764/6inputs and35current compatibility bindings. The12 new bounded-child receipts must also settle successfully, retain complete streams and match the expected family/operation/depth matrix.

Fixed actual parent347 is2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b; source08A902 and closureA766 are bound in common348.py. Supply the actual independent guard path/hash and all plan/tool hashes through explicit CLI arguments. If the actual immutable guard has a different schema, preserve this draft and adapt only against its real fields; do not invent flags to satisfy the draft.

Root sequence after actual completion/review: registry restoration once (Root owns it; do not duplicate); passive prospective plan; reviewed exact25 source promotion; bounded existing Tool readiness generation; final artifact-only docs overlay; explicit readiness/docs application; exact pathspec staging. No helper commits, pushes, dispatches a workflow or builds SDK projects. Preserve all587 registry rows, order and custom Limits,35source bindings and5/19/563 statuses.

Keep actual original3456290/fd8 and corrected346real-peer8 pending until their raw results are independently verified. Private old tuple32768 compare/hash child failures and bounded green observations are local evidence, not native equivalence or performance results. Preserve initial CS1061 harness build failure, HOST lifecycle-capture source finding and package-resource raw-byte preflight refusal. The active full run is Root-owned; these scripts must not launch another validation.
''';(O/'README.md').write_text(readme,encoding='utf-8')
controls=[]
for name in ['plan348.py','promote348.py','finalize348.py','readiness348.py','stage348.py']:
 p=subprocess.run([sys.executable,'-B',str(O/name),'--help'],capture_output=True,timeout=15);assert p.returncode==0 and b'--independent-guard-sha256' in p.stdout;controls.append({'Script':name,'HelpExit':0,'HelpSha256':sha(p.stdout)})
(O/'source-controls.json').write_text(json.dumps({'Status':'PASS_AST_AND_CLI_HELP_ONLY_NO_DELIVERY_ACTION','Controls':controls,'FullPassClaimed':False,'MainModified':False,'SdkExecuted':False,'NetworkUsed':False},indent=2)+'\n',encoding='utf-8')
files=[]
for p in sorted(O.iterdir()):
 if p.is_file() and p.name!='source-freeze.json':files.append({'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())})
f=O/'source-freeze.json';assert not f.exists();f.write_text(json.dumps({'Status':'SOURCE_ONLY_CONDITIONAL348_DELIVERY_HELPERS_FULL_PENDING','ParentHead':parent,'SourceFreezeSha256':source,'SourceClosureSha256':closure,'Files':files,'DeliveryActionsExecuted':False,'FullPassClaimed':False,'MainModified':False},indent=2)+'\n',encoding='utf-8');print(sha(f.read_bytes()))
