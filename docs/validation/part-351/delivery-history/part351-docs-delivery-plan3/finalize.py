"""Artifact-only final351 docs overlay. Requires actual settled full and independent guard."""
from pathlib import Path
import argparse,json,hashlib,subprocess
R=Path.cwd();O=R/'artifacts/part351-docs-delivery-plan3';F=R/'artifacts/spawn-priority351-full-actual2';S=R/'artifacts/spawn-priority351-full-source4';C=R/'artifacts/spawn-priority351-full-corrected-candidate2'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
p=argparse.ArgumentParser();p.add_argument('--independent-guard',type=Path,required=True);p.add_argument('--independent-guard-sha256',required=True);a=p.parse_args();guardpath=a.independent_guard.resolve()
assert sha(guardpath.read_bytes())==a.independent_guard_sha256.upper();peer=read(guardpath);g=read(F/'completion-guard.json');e=read(F/'full-exit.json');inventory=read(O/'proof-copy-plan.json')
assert peer['Status'].startswith('PASS') and peer['Passed']==7297 and peer['Failed']==0
assert peer['SourceFreezeSha256']==g['SourceFreezeSha256']==inventory['SourceFreezeSha256'] and peer['SourceClosureSha256']==g['SourceClosureSha256']==inventory['SourceClosureSha256']
assert g['Status']=='PASS_ACTUAL_ISOLATED351_FULL' and g['SingleUninterruptedFullPass'] and g['Passed']==7297 and g['Failed']==0
assert g['PackageOutputs']==2 and g['BoundedChildControls']==12 and len(g['IntegrationResults'])==11 and len(g['AddedTestNames'])==11
assert g['CompatibilityCounts']=={'Verified compatible':10,'Known differences':24,'Needs verification':553} and g['CurrentRuntimeSourceBindings']==74 and g['Registry']==587 and g['OwnedPaths']==49 and g['SourceClosureRows']==3006
assert e['ExitCode']==0 and e['Settled'] and not e['TimedOut'] and not e['LifecycleFailures']
head=inventory['ParentHead'];assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==head
pending={};origins={};overlay=O/'final-overlay';assert not overlay.exists()
def add(target,b,origin):assert target not in pending;pending[target]=b;origins[target]=origin
def proof(p,target):add('docs/validation/part-351/'+target,p.read_bytes(),p.relative_to(R).as_posix())
for row in inventory['Files']:
 b=(R/row['SourcePath']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'];add(row['TargetPath'],b,row['SourcePath'])
for q in sorted(F.iterdir()):
 if q.is_file() and q.suffix in ['.json','.log','.txt']:proof(q,'full/'+q.name)
proof(guardpath,'independent-full/guard.json')
for row in read(F/'bounded-child-controls.json')['Files']:
 q=C/row['Path'];assert sha(q.read_bytes())==row['ReceiptSha256']
 for raw in [q,q.with_name('stdout.txt'),q.with_name('stderr.txt')]:proof(raw,'full-child-raw/'+q.parent.name+'/'+raw.name)
# Archive the reviewed delivery controls themselves, without treating their execution as test evidence.
for name in ['prepare.py','finalize.py','source-freeze.json','proof-copy-plan.json','changelog-draft.md','reporting350-ci-subset.json']:proof(O/name,'delivery-source/'+name)
summary=('Part351 implements process scheduling priority and owner admission, with21 production files, eight new HOST checks and three portable scenarios. Actual canonical local validation passes **7297 tests/11 integration outcomes/two packages**, preserving7286 earlier ordered names and twelve bounded child controls. Registry587; compatibility **10 Verified/24 Known differences/553 pending**, with74 current runtime-source guards. Scheduling priority now works; heap/GC/MQD/affinity options remain unsupported. statistics/1 has a source-proven valid-key refusal. Published350 CI passes7286/11 and original passes6313/fd8; its predeclared map stdout variance retains separate strict refusal. Current-head351 CI and new priority original comparisons remain pending publication. [Evidence](validation/part-351/).')
prefixes={'AGENTS.md':summary.replace('(validation/','(docs/validation/'),'docs/PROGRESS.md':summary,'docs/COMPATIBILITY.md':summary,'docs/semantic-differences.md':summary+' Physical BEAM scheduling/performance and whole spawn_opt parity are not claimed.','docs/NEXT_STEPS.md':'Part351: publish the exact tested priority implementation, verify same-head CI and three new priority scenarios against pinned OTP. Keep10/24/553 classifications until a later complete contract audit and current-source proof justify promotion. Continue real statistics garbage-collection accounting without invented counts.','docs/DECISIONS.md':'Part351 — A per-owner TaskScheduler, actual executing-turn marker and logical epochs implement priority admission. Preserve selected source leases and cancellation/retirement cleanup across asynchronous waits. Keep scheduling priority distinct from priority message delivery and physical BEAM scheduler parity.','docs/dependency-decisions.md':'Part351 — Reuse runtime/BCL scheduling and existing tests without packages. Preserve all older Verified evidence, bind74 current runtime dependencies, and refine unsupported spawn options truthfully. Archive exact source and declared raw proof subsets; no SDK/runtime/package binaries.'}
for path,prefix in prefixes.items():
 old=(R/path).read_text(encoding='utf-8');heading,tail=old.split('\n',1)
 if path=='docs/PROGRESS.md':heading='# Progress checkpoint — 2026-10-17'
 add(path,(heading+'\n\n'+prefix+'\n\n'+tail.lstrip('\n')).encode(),'new current351 checkpoint; historical body retained')
entry=(O/'changelog-draft.md').read_text(encoding='utf-8-sig').replace('FULL_RESULT_PENDING.','Corrected canonical full2 validation exits0 with7297 passed,0failed,eleven integration outcomes,two package outputs and twelve settled bounded child controls; the independent passive guard binds the exact frozen source, closure, registry, tests, streams and package receipts.')
assert 'PENDING' not in entry
index=(R/'CHANGELOG.md').read_text(encoding='utf-8');old=(R/'docs/changelog/0055.md').read_text(encoding='utf-8');log=old.rstrip()+'\n\n'+entry.rstrip()+'\n';volume='0055'
assert len(log.encode())<=32768 and len(log.splitlines())<=400
assert 'part350 reporting clarification (active)' in index
index=index.replace('part350 reporting clarification (active)','part350 reporting clarification and part351 (active)')
add('docs/changelog/0055.md',log.encode(),'append-only English351 entry preserving existing reporting350 clarification; closed0054 untouched')
add('CHANGELOG.md',index.encode(),'new volume index')
rows=[]
for target,b in sorted(pending.items()):
 q=overlay/target;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(b);rows.append(dict(SourcePath=q.relative_to(R).as_posix(),TargetPath=target,Bytes=len(b),Sha256=sha(b),ParentSha256=sha((R/target).read_bytes()) if (R/target).exists() else None,Origin=origins[target]))
plan=dict(Status='PREPARED_CORRECTED_ACTUAL351_DOCS_PROOF_OVERLAY_ROOT_REVIEW_REQUIRED',ParentHead=head,Files=rows,FilesCount=len(rows),Bytes=sum(x['Bytes'] for x in rows),ActualPassed=7297,Integrations=11,Packages=2,CompatibilityCounts=[10,24,553],RuntimeSourceBindings=74,IndependentGuardSha256=sha(guardpath.read_bytes()),SourceFreezeSha256=g['SourceFreezeSha256'],SourceClosureSha256=g['SourceClosureSha256'],ChangelogVolume=volume,ChangelogBytes=len(log.encode()),ChangelogLines=len(log.splitlines()),FutureSixPredicatePromotionsExcluded=True,MainModified=False,CandidateModified=False,SdkExecuted=False,Network=False)
out=O/'final-plan.json';assert not out.exists();out.write_text(json.dumps(plan,indent=2)+'\n',newline='\n');print(json.dumps(dict(Files=len(rows),Bytes=plan['Bytes'],ChangelogVolume=volume,ChangelogBytes=plan['ChangelogBytes'],ChangelogLines=plan['ChangelogLines'],Sha256=sha(out.read_bytes()))))
