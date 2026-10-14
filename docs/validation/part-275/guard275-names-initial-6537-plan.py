from pathlib import Path
import hashlib,json,re
root=Path.cwd(); art=root/'artifacts'; oldpath=root/'docs/validation/part-273/tests.json'; newpath=art/'tests.json'
j=lambda p:json.loads(p.read_text(encoding='utf-8-sig')); sha=lambda b:hashlib.sha256(b).hexdigest().upper()
old=j(oldpath); new=j(newpath); assert old['Passed']==6527 and new['Passed']==6537 and new['Failed']==0
oldnames=[x['Name'] for x in old['Tests']]; names=[x['Name'] for x in new['Tests']]
assert len(names)==len(set(names))==6537
added=set(names)-set(oldnames); assert len(added)==10 and [n for n in names if n not in added]==oldnames
compiled={'oracle/compiled-module/'+n for n in ['process-info-logical-small-bump','process-info-logical-bump-after-yield','process-info-logical-bump-error-domain']}
assert {n for n in added if '/compiled-module/' in n}==compiled
files=['RuntimeSetNodeHostNames.cs','RuntimeNodeTextActivationHostNames.cs','ProcessBumpCapHostNames.cs','RuntimeNodeCreationHostNames.cs','DistributionRegisteredHostNames.cs','ConnectedDistributionHostNames.cs','ConnectedIngressHostNames.cs']
hosts=set()
for file in files:
    text=(root/'tests/Erlang.Tests'/file).read_text(); matched=re.search(r'const string Test\s*=\s*"([^"]+)"',text); assert matched,file; hosts.add(matched.group(1))
assert added==compiled|hosts and len(hosts)==7
program=(root/'tests/Erlang.Tests/Program.cs').read_text(); assert program.count('Erlang.Tests.ErlangHashAllocationControlNames.Test')==1
prior=j(art/'part273-test-name-preservation-guard.json')
receipt={'Status':'PASS','OldCount':6527,'NewCount':6537,'NewCompiled':sorted(compiled),'NewHosts':sorted(hosts),'NewNames':sorted(added),'AllOldNamesAndOrderExact':True,'ExactlyDeclaredRemovedNames':prior['ExactlyDeclaredRemovedNames']+sorted(added),'OldReportSha256':sha(oldpath.read_bytes()),'NewReportSha256':sha(newpath.read_bytes()),'CorrectedNativeSourceExplicit':True,'WholeCompatibilityClaim':False}
(art/'part275-test-name-preservation-guard.json').write_text(json.dumps(receipt,indent=2)+'\n'); print('PASS old6527 names/order exact after declared3 compiled+7 HOST additions')
