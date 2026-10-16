from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();h=lambda b:hashlib.sha256(b).hexdigest().upper();p=r/'artifacts/root-part329-post-publication-dispatch-authority.json';a=json.loads(p.read_bytes())
assert h(p.read_bytes())=='3E424ECF7AF97FF1F8E92A01D86FEBEFBFE895FBA69A10693DEB623425243E3A'
assert a['PublishedHead']==a['RemoteHeadReportedVerifiedByRoot']=='d83ed875d0c77939e20049afcb46fa297fee82dd'
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=r).decode().strip()==a['PublishedHead']
assert len(a['RequiredBindings'])==len({x['Role'] for x in a['RequiredBindings']})==9
for e in a['RequiredBindings']:
 q=(r/e['Path']).resolve();assert q.is_relative_to(r/'artifacts') or q.is_relative_to(r/'docs');assert h(q.read_bytes())==e['Sha256']
for flag in ['RootExecutionApproved','FreshFullPassed','IntegrationAndPackagesPassed','FreshPostFullWindowsLegacyPassed','FreshPostFullWindowsReallocationPassed','FreshPostFullWindowsProviderFivePhasesPassed','ActualStagedCanonicalSourcePlansPassed','DefaultGitWhitespaceCheckPassed']:assert a[flag] is True
assert a['PublishedCommitBlobsRehashed']==1170 and len(a['ActualChangedMetadataPaths'])==13
out=r/'artifacts/part329-new-publication-review/actual-authority-peer.json';out.write_bytes((json.dumps(dict(Status='PASS_INDEPENDENT_ACTUAL_AUTHORITY_NINE_BINDINGS',AuthoritySha256=h(p.read_bytes()),PublishedHead=a['PublishedHead'],BindingsRehashed=9,PublishedStagePaths=1170,NoNetworkOrDispatchExecuted=True),indent=2)+'\n').encode());print(h(out.read_bytes()))
