from pathlib import Path
import json,hashlib,ast
r=Path.cwd();d=r/'artifacts/part331-publication-controls-new';s=(r/'artifacts/part329-publication-authority-proposal1/dispatch329-once.ps1').read_text().replace('329','331').replace('$freeze.SourcePaths -ne 12','$freeze.SourcePaths -ne 19').replace('$freeze.CSharpPaths -ne 7','$freeze.CSharpPaths -ne 15').replace('$freeze.SourceClosurePaths -ne 2397','$freeze.SourceClosurePaths -ne 2417').replace('$freeze.ExpectedFullPassed -ne 6747','$freeze.ExpectedTests -ne 6775').replace('$legacyPlans[0].Inputs -ne 1120','$legacyPlans[0].Inputs -ne 1123').replace('$ownPlans[0].Inputs -ne 1145','$ownPlans[0].Inputs -ne 1148')
(d/'dispatch-once.ps1').write_bytes(s.encode());ast.parse((d/'bind-authority.py').read_text())
readme='''# Part331 source-only publication controls

UNEXECUTED. No network, POST, stage, commit, or Main source edit by author.

Root supplies an artifact-local JSON manifest with RequiredBindings, exactly nine unique roles: SourceFreeze, FullGuard, NativeGuard, IndependentActualPeer, StageGuard, IndependentStagePeer, EvidenceReceipt, Readiness, DispatcherSourcePeer. Each entry has repository-relative Path and uppercase Sha256. Actual and stage peer can use the same independently reviewed file, with SourceFreezeSha256, EvidenceReceiptSha256, StageGuardSha256, FullGuardSha256, NativeGuardSha256, and CachedBeamAndProvenanceBinaryAttributesAndBytesExact=true. Dispatcher source peer requires PASS Status and DispatcherSha256.

Stage guard schema uses SourcePaths=19, CSharpPaths=15, Records[{Path,StagedBytes,StagedSha256}], StagedPaths, SourceFreezeSha256, EvidenceReceiptSha256, ProofsBinaryExact/AllCSharpBinaryExact/DefaultGitWhitespaceCheckPassed=true, LicenseLocalStaged/CacheStaged=false. Four ActualStagedRawSourcePlanSeals and two ActualStagedEmbeddedBeamAndProvenanceResources have the same exact schemas as329. All published paths/counts are derived, never future guesses. Receipt SourceFreezeSha256/SourceClosureSha256/ActualFullGuardSha256/ActualNativeGuardSha256/ActualReadinessSha256/EvidenceIndexSha256/Files use standard331 receipt fields.

After actual finalization, stage review, commit, push, and remote head verification: python -B bind-authority.py --published-head ACTUAL40 --verified-remote-head ACTUAL40 --bindings artifacts/ACTUAL.json --bindings-sha256 ACTUAL64. Exact source559184/closure9E6119/full8914 and parentb748 are fixed; future stage/peer/receipt/readiness/committed head values are read from actual CLI bindings. Binder exclusively creates artifacts/root-part331-post-publication-dispatch-authority.json after checks.

Root obtains independent actual authority review, then invokes dispatch-once.ps1 -Kind ordinary|native|reallocation|distribution -ExpectedHead ACTUAL40 -ReviewedAuthorityPath ACTUAL -ExpectedAuthoritySha256 ACTUAL64. Dispatcher validates local and remote heads plus all bindings, exclusively writes attempt before its sole POST, never retries ambiguous outcomes. Only reallocation has expected_head workflow input. CI runs on push. Read-only331 monitor controls are separate. Do not execute before Root authorization/publication.
'''
(d/'README.md').write_bytes(readme.encode());rows=[]
for n in ['bind-authority.py','dispatch-once.ps1','README.md']:
 p=d/n;rows.append(dict(Path=n,Bytes=p.stat().st_size,Sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()))
p=d/'source-freeze.json';p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_331_POST_PUBLICATION_BINDER_AND_EXACT_ONCE_DISPATCHER_UNEXECUTED',ParentHead='b7484cb0dacc13c04ac818a3c58e4ff4f6afe344',SourcePaths=19,CSharpPaths=15,SourceClosurePaths=2417,FullPassed=6775,FutureActualHeadAndProofBindingsRequired=True,ExecutionPerformed=False,Files=rows),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
