from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';d=a/'connected-remote-completion-full-actual4';c=a/'connected-remote-completion-candidate'
j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert h(d/'completion-guard.json')=='F57E4C5F051190D83EC031DB417FAA8292DB9B8834404EA63C89E21098033C37'
g=j(d/'completion-guard.json');t=j(d/'tests.json');e=j(d/'full-exit.json')
assert e['ExitCode']==0 and e['Settled'] and not e['TimedOut'] and t['Passed']==7193 and t['Failed']==0
for key,path in [('FullExitSha256','full-exit.json'),('TestReportSha256','tests.json'),('SourceFreezeSha256','source-freeze.json'),('SourceClosureSha256','source-resource-closure.json'),('PackageResourceManifestSha256','package-resources.json'),('RawRegistrySha256','raw-supported-mfas.json'),('PackageOutputManifestSha256','packages.json')]:assert g[key]==h(d/path)
for label,folder,keys in [('initial','connected-remote-completion-full',('PreviousLaunchExitSha256','PreviousLaunchLogSha256')),('build','connected-remote-completion-full-actual2',('PreviousBuildExitSha256','PreviousBuildLogSha256')),('test','connected-remote-completion-full-actual3',('PreviousTestExitSha256','PreviousTestLogSha256'))]:
 old=a/folder;assert g[keys[0]]==h(old/'full-exit.json') and g[keys[1]]==h(old/'full-validation.log')
 exit=j(old/'full-exit.json');assert exit['ExitCode']==1 and exit['Settled'] and not exit['TimedOut']
third=a/'connected-remote-completion-full-actual3';prior=j(third/'tests.json')
assert (prior['Passed'],prior['Failed'])==(7191,2) and h(third/'tests.json')==g['PreviousTestReportSha256']
names=lambda v:[x['Name'] for x in v['Tests']]
assert names(t)==names(prior) and len(set(names(t)))==7193
failed={x['Name'] for x in prior['Tests'] if x['Status']!='Passed'}
proposal=j(a/'connected-negative-fixtures340-proposal2/source-freeze.json')
assert failed==set(proposal['OldNamesPreserved'])
before={x['Path']:x for x in j(third/'source-resource-closure.json')['Files']};after={x['Path']:x for x in j(d/'source-resource-closure.json')['Files']}
assert before.keys()==after.keys() and len(after)==2691
changes={p for p in before if before[p]!=after[p]};assert changes=={x['Path'] for x in proposal['Files']}
assert all(not p.startswith('src/') for p in changes)
old=j(r/'docs/validation/part-338/final-candidate/tests.json');oldset=set(names(old));assert [n for n in names(t) if n in oldset]==names(old)
assert len(oldset)==7105 and len(set(names(t))-oldset)==88
audit=j(d/'transitive-input-audit.json');assert h(d/'transitive-input-audit.json')==g['TransitiveInputAuditSha256']
assert audit['MissingBefore']==['build/Erlang.Net.targets'] and audit['MissingAfter']==[] and audit['GuestFullTestsHadNotStarted']
added=audit['AddedInput'];assert h(r/added['Path'])==h(c/added['Path'])==added['Sha256']
packages=j(d/'packages.json');assert len(packages)==2
for p in packages:assert (c/p['Path']).stat().st_size==p['Bytes'] and h(c/p['Path'])==p['Sha256']
out=a/'root-part340-full-independent-guard.json';assert not out.exists()
out.write_text(json.dumps(dict(Status='PASS_INDEPENDENT340_FULL_AND_ALL_PRIOR_FAILURE_HISTORY',FullGuardSha256=h(d/'completion-guard.json'),Passed=7193,Failed=0,Previous7105OrderedNamesPreserved=True,New88Pass=True,TwoHistoricalNegativeNamesMigrated=sorted(failed),OnlyChangedClosureFilesSinceThird=sorted(changes),ProductionUnchangedSinceFailedThird=True,PreviousThreeFailuresRetained=True,PackagesRehashed=2,MainEdited=False,SdkExecuted=False),indent=2)+'\n');print(h(out))
