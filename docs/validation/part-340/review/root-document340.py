from pathlib import Path
import argparse,hashlib,json,shutil,subprocess
p=argparse.ArgumentParser();p.add_argument('--full-dir',required=True);p.add_argument('--guard-sha256',required=True);args=p.parse_args()
r=Path.cwd();a=r/'artifacts';d=r/args.full_dir;c=a/'connected-remote-completion-candidate';b=r/'docs/validation/part-340'
j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
receipt_path=a/'root-part340-documentation.json';assert not receipt_path.exists()
assert h(d/'completion-guard.json')==args.guard_sha256.upper()
g=j(d/'completion-guard.json');t=j(d/'tests.json');fin=j(a/'root-part340-finalized.json');prom=j(a/'root-part340-promotion.json')
assert g['Failed']==t['Failed']==0 and g['Passed']==t['Passed']==prom['Passed']==fin['Passed']
assert fin['Registry']==prom['Registry']==587 and prom['AllOwnedRawExact'] and prom['WholeClosureRawIdentical']
assert fin['FullGuardSha256']==prom['FullGuardSha256']==args.guard_sha256.upper()
assert g['SingleUninterruptedFullPass'] and g['InitialLaunchPreSdkFailure'] and g['PreviousLaunchExit']==1
assert g['SecondLaunchBuildFailure'] and g['PreviousBuildExit']==1
assert g['ThirdTestFailure'] and g['PreviousTestExit']==1 and g['PreviousTestPassed']==7191 and g['PreviousTestFailed']==2
assert not b.exists()
native=j(a/'part338-remote-new-review/ordinary-semantic-independent-guard.json');assert (native['Passed'],native['FdPassed'])==(6216,8)
assert h(a/'part339-connected-sdk-crosswalk2/crosswalk.json')=='914B07E77BF3E95EA9E9137AF2C8BE518BE26D5F86C861CD62123EE060E2C853'
passed=t['Passed']
summary=(f'Part 340 implements all 39 registered proc_lib exports, including six node-qualified spawn/spawn_link/spawn_opt overloads and real remote process labels through rpc/erpc. '
 f'Actual validation passes {passed}/{passed} tests and 11 integration/package outcomes after the recorded failed launches. Retain the initial Windows PowerShell preflight exit 1, the second build failure caused by an omitted immutable build/Erlang.Net.targets input, and the third result of 7191 passed/2 failed. The first two did not reach guest tests; all 88 new tests passed in the third. Two historical negative protocol fixtures are explicitly migrated because their previously unsupported spawn/alias packets are now implemented, preserving malformed-frame and missing-feature refusals. The corrected pipeline uses PowerShell 7 and the complete tracked input set outside documentation. '
 'All 7105 previous ordered test names and 576 registry rows/custom Limits remain exact. Registry: 587 APIs across 14 modules; proc_lib 39/39, rpc 2/35, erpc 3/33; erlang 351 unchanged. '
 'Connected spawn/TT/link/monitor/priority/alias33/34, visible/hidden/current-generation nodes and apply/3 native1023-argument traversal are implemented. '
 'The new 44 portable cases, 11 direct contracts, 23 connected HOST checks and 11 RPC/lifecycle HOST checks pass. Original part 339 supplies 12 independently audited observations, with a precise contract crosswalk and explicit unmatched whole-trace fields. '
 'Published part 338 original 6216/fd8 and CI 7105/11 plus part 339 CI 7105/11 are complete; the old predeclared map stdout-order strict refusal remains separate. '
 'Current part 340 CI/original comparison awaits publication. The receiver must still resolve anonymous/captured source funs when its matching module is loaded but the closure constructor has never run there; current connected fun tests use external functions. This concrete gap is next, with an independent real two-node original observer. API presence is not whole-module compatibility: inherited native tuning, physical heap/stack/scheduler values, broad logger/RPC exports, physical TCP interoperability and performance parity remain partial.')
outputs={}
def write(path,text):outputs[path]=text
def prepend(path,text):
 first,rest=(r/path).read_text(encoding='utf-8').split('\n',1);write(path,first+'\n\n'+text+'\n\n'+rest.lstrip('\n'))
for path in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/PROC_LIB_COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/semantic-differences.md']:prepend(path,summary)
proc=outputs['docs/PROC_LIB_COMPATIBILITY.md'];write('docs/PROC_LIB_COMPATIBILITY.md',proc.replace('# proc_lib local process foundation','# proc_lib process support',1))
prepend('docs/DECISIONS.md','Part340 — Port proc_lib node overloads through actual connected spawn and preserve erpc reference-correlated DOWN and timeout races. Keep local infinite RPC execution in its caller. Obtain independent original controller observations and assert only the shared checked contracts; do not fabricate physical process metrics or claim full captured-trace/TCP equivalence.')
prepend('docs/dependency-decisions.md','Part340 — Reuse owned distribution controllers, existing process/mailbox/monitor/alias/trace channels, real registry apply dispatch and process-info requests. Add only rpc:call/4,5 and erpc:call/4,5,execute_call/4 needed for remote proc_lib metadata. Node enumeration reads actual connection entries; no fake registry or local substitute for remote labels. No third-party package or actor framework is added.')
prepend('docs/DEPENDENCY_PRIORITIES.md','Part 340 — proc_lib has 39/39 API presence and remote labels. Complete exact-head original/CI audits and the newly identified fresh-receiver captured-function dependency with real two-node original observations. Then continue wider gen/sys lifecycle and state protocols required by gen_server/supervisor. Full logger/RPC modules, physical runtime/tuning/TCP and performance remain separate milestones.')
prepend('docs/NEXT_STEPS.md','Part340: publish the frozen proc_lib/RPC dependency slice and dispatch the enlarged ordinary comparison once. Audit exact-head CI and 6260 planned original cases plus fd8 without treating planned counts as results. Next resolve anonymous/captured source functions against exact receiving code images without requiring prior constructor execution; compare real two-node proc_lib and label behavior with the original. Preserve all 6216 old contracts, explicit physical-report adaptation and raw map stdout refusal. No proc_lib export is absent; inherited native tuning, physical/TCP/whole-module/performance compatibility remains partial.')
scope=j(r/'docs/module-readiness-scope.json');scope['ComparisonTestReport']='validation/part-338/final-candidate/tests.json'
if 'erpc' not in scope['FocusModules']:scope['FocusModules'].append('erpc')
component=next(x for x in scope['Components'] if x['Id']=='proc_lib adapters');component['Implemented'].append(summary)
component['Remaining']=['Resolve fresh receiver anonymous/captured source funs when their matching module is loaded but closure constructor has never executed locally; real two-node original verification','Inherited native spawn heap/scheduler tuning options and physical BEAM heap/stack/resource metrics','Full crash-stack/remote-neighbour and logger background/configuration/overload/formatting parity','Full captured trace/TCP interoperability, VM scheduling and performance parity']
write('docs/module-readiness-scope.json',json.dumps(scope,indent=2)+'\n')
entry=(a/'part340-delivery-helpers-draft/changelog340-template.md').read_text(encoding='utf-8').replace('{{PASSED}}',str(passed)).replace('{{FULL_GUARD_SHA256}}',args.guard_sha256.upper())
entry=entry.replace('## Part 340 — Connected','## Part340 — 2026-10-16 — Connected')
entry=entry.replace('Actual completed local validation:', 'Retain the initial Windows PowerShell preflight exit1 and second build failure caused by an omitted immutable build/Erlang.Net.targets input. Neither reached guest tests. The third PowerShell7 pipeline includes that exact Main input and is uninterrupted. Actual completed local validation:')
entry=entry.replace('The third PowerShell7 pipeline includes that exact Main input and is uninterrupted.', 'The third pipeline passes 7191 tests but fails two historical negative fixtures that still classify newly implemented packets as unsupported. Preserve that raw result. Migrate those two fixtures with source-reviewed malformed-frame and missing-feature checks before the completed full run.')
entry=entry.replace('All7105 previous ordered tests', 'All7105 previous ordered test names')
entry=entry.replace('Prior339 source-built original controller observations remain acquisition evidence until their explicit semantic comparison passes.', 'Original339 provides12 typed observations; the actual23 SDK checks have an independently verified semantic contract crosswalk. Whole trace snapshots, dynamic identities and TCP transport remain outside that equivalence claim.')
entry+='\nAdditional permanent coverage:44 compiled source cases,11 direct MFA contracts and32 newly registered HOST checks; parse all44 before guest execution. Preserve the failed alias20/21 ordering revision, RPC6/10 and10/11 prerequisite revisions, and private-harness build failure. Their corrected focused results remain separate from this full run.\n'
entry+='\nKnown next dependency: a receiving runtime with the matching source module but no locally constructed closure still needs lazy anonymous/captured-fun resolution. Current connected fun checks use external make_fun references. Keep this gap explicit despite 39/39 API presence; implement and check it with exact code-image leases and independent real two-node OTP observations.\n'
for old,new in [('Registry:587','Registry: 587'),('across14','across 14'),('proc_lib39/39','proc_lib 39/39'),('All7105','All 7105'),('and576','and 576'),('Published338','Published part 338'),('original6216','original 6216'),('prior contracts and45','prior contracts and 45'),('Current340','Current part 340'),('Original339','Original part 339'),('provides12','provides 12'),('actual23','actual 23'),('coverage:44','coverage: 44'),('cases,11','cases, 11'),('and32','and 32'),('all44','all 44'),('alias20/21','alias 20/21'),('RPC6/10','RPC 6/10'),('and10/11','and 10/11')]:entry=entry.replace(old,new)
assert '{{' not in entry
volume='docs/changelog/0053.md';combined=(r/volume).read_text(encoding='utf-8')+'\n'+entry
assert len(combined.encode())<=32768 and len(combined.splitlines())<=400
write(volume,combined)
index=(r/'CHANGELOG.md').read_text(encoding='utf-8');assert 'parts338–339 (active)' in index;write('CHANGELOG.md',index.replace('parts338–339 (active)','parts338–340 (active)'))
assert len(j(c/'docs/supported-mfas.json'))==587
assert h(c/'docs/supported-mfas.json')==fin['RestoredRegistrySha256']
assert all((r/path).is_file() for path in outputs)
# Render and check every output before the first Main documentation write.
for path,text in outputs.items():(r/path).write_text(text,encoding='utf-8',newline='\n')
shutil.copyfile(c/'docs/supported-mfas.json',r/'docs/supported-mfas.json');shutil.copyfile(d/'tests.json',a/'tests.json')
b.mkdir(parents=True);(b/'.gitattributes').write_bytes(b'* binary\n')
for source in d.iterdir():
 if source.is_file() and source.suffix in ['.json','.log','.txt']:
  out=b/'final-candidate'/source.name;out.parent.mkdir(exist_ok=True);shutil.copyfile(source,out)
receipt_path.write_text(json.dumps(dict(Status='340_DOCUMENTATION_AFTER_ACTUAL_FULL_AND_PROMOTION',Passed=passed,Registry=587,ProcLib=39,ChangelogVolume=volume,Current340CiVerified=False,Current340OriginalVerified=False,FullGuardSha256=args.guard_sha256.upper()),indent=2)+'\n')
print(json.dumps(dict(Passed=passed,Registry=587,ProcLib=39,ProofDestination=str(b),ChangelogVolume=volume)))
