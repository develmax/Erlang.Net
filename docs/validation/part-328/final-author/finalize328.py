"""Root-only scope/finalize. No Git, build, SDK, VM or network invocation."""
from pathlib import Path
import argparse, copy, hashlib, json, re

ROOT = Path.cwd().resolve()
HERE = Path(__file__).resolve().parent
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
encoded = lambda value: (json.dumps(value, indent=2) + '\n').encode('utf-8')

def exact(path, digest):
    assert re.fullmatch('[A-Fa-f0-9]{64}', digest)
    raw = path.read_bytes()
    assert sha(raw) == digest.upper(), str(path)
    return raw

def bound(item):
    path = (ROOT / item['Path']).resolve()
    assert path.is_relative_to(ROOT)
    raw = exact(path, item['Sha256'])
    if 'Bytes' in item:
        assert len(raw) == item['Bytes']
    return path

def snapshot(name):
    path = ROOT / name
    return dict(Path=name, Exists=path.exists(), Bytes=path.stat().st_size if path.exists() else None,
                Sha256=sha(path.read_bytes()) if path.exists() else None)

def row(path, target):
    raw = path.read_bytes()
    return dict(SourcePath=path.resolve().relative_to(ROOT).as_posix(),
                TargetPath='docs/validation/part-328/' + target, Bytes=len(raw), Sha256=sha(raw))

parser = argparse.ArgumentParser()
parser.add_argument('phase', choices=['scope', 'finalize'])
for role in ('main-source-freeze', 'full-guard', 'native-guard'):
    parser.add_argument('--' + role, type=Path, required=True)
    parser.add_argument('--' + role + '-sha256', required=True)
for role in ('closure', 'author-final-freeze', 'source-peer'):
    parser.add_argument('--' + role + '-sha256', required=True)
parser.add_argument('--full-guard-source', type=Path, required=True)
parser.add_argument('--full-guard-source-sha256', required=True)
parser.add_argument('--finalizer-source-peer', type=Path, required=True)
parser.add_argument('--finalizer-source-peer-sha256', required=True)
parser.add_argument('--readiness-sha256')
parser.add_argument('--readiness-exit-sha256')
args = parser.parse_args()
for item in read(HERE / 'proposal-freeze.json')['Files']:
    assert len(exact(HERE / item['Path'], item['Sha256'])) == item['Bytes']
cfg = read(HERE / 'configuration.json')
finalizer_peer_path = args.finalizer_source_peer.resolve()
assert finalizer_peer_path.is_relative_to(ROOT / 'artifacts')
exact(finalizer_peer_path, args.finalizer_source_peer_sha256)
finalizer_peer = read(finalizer_peer_path)
assert str(finalizer_peer['Status']).startswith('PASS')
assert sha((HERE / 'proposal-freeze.json').read_bytes()) in json.dumps(finalizer_peer)

fp, fullpath, nativepath = [p.resolve() for p in (args.main_source_freeze, args.full_guard, args.native_guard)]
assert fp.parent == fullpath.parent and fp.name == 'source-freeze.json'
assert fp.is_relative_to(ROOT / 'artifacts') and fp != ROOT / 'artifacts/root-part328-main1/source-freeze.json'
assert args.main_source_freeze_sha256.upper() != cfg['FixedFailedMain1FreezeSha256']
assert nativepath.is_relative_to(ROOT / 'artifacts') and nativepath.name == 'guard.json'
exact(fp, args.main_source_freeze_sha256)
exact(fullpath, args.full_guard_sha256)
exact(nativepath, args.native_guard_sha256)
source, full, native = read(fp), read(fullpath), read(nativepath)
closure = fp.parent / 'source-resource-closure.json'
exact(closure, args.closure_sha256)
assert source['FinalSourceClosureSha256'] == args.closure_sha256.upper()
assert source['ParentHead'] == cfg['ParentHead']
assert (source['SourcePaths'], source['CSharpPaths'], source['SourceClosurePaths'], source['ExpectedFullPassed']) == (29, 24, 2393, 6738)
assert source['AuthorFinalFreeze']['Sha256'] == args.author_final_freeze_sha256.upper()
assert source['IndependentPeer']['Sha256'] == args.source_peer_sha256.upper()
author = read(bound(source['AuthorFinalFreeze'])); peer = read(bound(source['IndependentPeer']))
assert author['SourceFactoryDeferredLegacyHostConstructionRestored'] is True
assert author['FailedMain1UpgradePreflight']['ReplacedMain1FreezeSha256'] == cfg['FixedFailedMain1FreezeSha256']
assert author['FailedMain1UpgradePreflight']['FailedMain1CaptureSha256'] == 'C1252335323D98761C5B548C07F4ED498EECEF60E51087A0438C68D3AA8A0065'
assert str(peer['Status']).startswith('PASS')
assert peer.get('FinalSourceFreezeSha256', peer.get('SourceFreezeSha256')) == args.author_final_freeze_sha256.upper()
model = author['FailedMain1UpgradePreflight']['RegistryCanonicalGitModel']
assert model['RawCaptureEqualityStillRequired'] is True
assert model['ExpectedTrackedDirtyCount'] == len(author['FailedMain1UpgradePreflight']['ExactTrackedDirtyPaths'])
assert model['CanonicalDiffersPublished'] == (model['GitCleanObjectId'] != model['PublishedObjectId'])

inputs = read(closure)['Files']
assert len(inputs) == len({x['Path'] for x in inputs}) == 2393
for item in inputs:
    bound(item)
assert full['Status'] == 'PASS_ACTUAL_MAIN328_6738_FULL_AND_ELEVEN_INTEGRATIONS'
assert (full['Passed'], full['Failed'], full['IntegrationResults'], full['Registry'], full['ErlangMfas']) == (6738, 0, 11, 509, 351)
assert full['SourceFreezeSha256'] == sha(fp.read_bytes()) and full['SourceClosureSha256'] == sha(closure.read_bytes())
assert full['FullExecutionContext'] == 'Outside sandbox' and full['PublicMfaDelta'] == 0
for key in ('All6729PreviousOrderedNamesPreserved', 'ThirtyPriorCustomLimitsRestoredBeforeAppend',
            'OnlyProcessFlag2ScopeAppended', 'TwentyNineOtherCustomLimitsExact', 'UnrelatedMfaRowsExactParent'):
    assert full[key] is True, key
assert args.full_guard_source.resolve().is_relative_to(ROOT / 'artifacts')
exact(args.full_guard_source.resolve(), args.full_guard_source_sha256)
assert full['GuardSourceSha256'] == args.full_guard_source_sha256.upper()
assert read(fp.parent / 'full-exit.json')['ExitCode'] == 0
tests = read(ROOT / 'artifacts/tests.json')
old = read(ROOT / 'docs/validation/part-327/final-main/tests.json')
assert tests['Passed'] == len(tests['Tests']) == 6738 and tests['Failed'] == 0
assert all(x['Status'] == 'Passed' for x in tests['Tests'])
added = set(full['AddedTestNames']); assert len(added) == 9
assert [x['Name'] for x in tests['Tests'] if x['Name'] not in added] == [x['Name'] for x in old['Tests']]
assert len(read(ROOT / 'artifacts/integration-results.json')) == 11
assert native['Status'] == 'PASS_ACTUAL_MAIN328_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE'
assert (native['FullPassed'], native['SourceFilesRehashed'], native['OriginalExecuted'], native['PhysicalMemoryCompared']) == (6738, 2393, False, False)
assert native['SourceFreezeSha256'] == sha(fp.read_bytes()) and native['ActualFullGuard']['Sha256'] == sha(fullpath.read_bytes())
assert native['ActualTrustedCQueryExecuted'] is True and native['MainExecution'] is True
assert native['EveryRecursiveToolDllJsonBoundAcrossAllThreeAttempts'] is True
assert len(native['Roles']) == 3 and {x['Role'] for x in native['Roles']} == {'legacy', 'provider-query', 'reallocation'}
for item in native['Roles']:
    assert item['ExitCode'] == 0
    bound(item['Attempt']); bound(item['Process'])
    process = read(ROOT / item['Process']['Path'])
    assert process['ExitCode'] == 0 and process['TimedOut'] is False
bound(native['ActualTool']); bound(native['ActualRuntime'])
for plan in source['Plans']:
    path = ROOT / plan['Path']; raw = exact(path, plan['Sha256'])
    assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
    assert len(read(path)['Files']) == plan['Inputs']
    for item in read(path)['Files']:
        value = (ROOT / item['Path']).read_bytes()
        assert item['HashMode'] in ('TextLF', 'BinaryExact')
        if item['HashMode'] == 'TextLF': value = value.replace(b'\r\n', b'\n')
        assert len(value) == item['Bytes'] and sha(value) == item['Sha256']
    workflow = (ROOT / plan['Workflow']).read_text()
    assert re.findall(r'^\s*' + plan['Pin'] + r': ([A-F0-9]{64})$', workflow, re.M) == [plan['Sha256']]
baseline = read(fp.parent / 'registry-before-full.json')
registry = read(ROOT / 'docs/supported-mfas.json')
assert len(registry) == len(baseline) == 509 and sum(x['Module'] == 'erlang' for x in registry) == 351
key = lambda x: (x['Module'], x['Function'], x['Arity'])
assert [key(x) for x in registry] == [key(x) for x in baseline]
for before, after in zip(baseline, registry):
    if key(after) == ('erlang', 'process_flag', 2):
        assert {k:v for k,v in after.items() if k != 'Limits'} == {k:v for k,v in before.items() if k != 'Limits'}
        assert after['Limits'] == before['Limits'].replace('undefined_lambda/3, ', '') + cfg['FlagScopeSuffix']
    else: assert after == before
for item in cfg['ClosedVolumes']: bound(item)
static = read(ROOT / cfg['StaticRowsPath'])
exact(ROOT / cfg['StaticRowsPath'], cfg['StaticRowsSha256'])
assert len(static) == 941
for item in static + cfg['ExtraRows']:
    assert len(exact(ROOT / item['SourcePath'], item['Sha256'])) == item['Bytes']
# Both actual focused guards and every declared raw binding are replayed independently of labels.
for name in ('undefined-lambda328-green-actual-guard3.json', 'undefined-lambda328-red-actual-guard4.json'):
    report = read(ROOT / 'artifacts' / name)
    assert report['Passed'] is True and report['TargetBuildPassed'] is True
    for item in report['Bindings']: bound(item)
green = read(ROOT / 'artifacts/undefined-lambda328-green-actual-guard3.json')
red = read(ROOT / 'artifacts/undefined-lambda328-red-actual-guard4.json')
assert (green['GeneratedCasesPassed'], green['HostGroupsPassed']) == (8, 12)
assert red['ActualFailure'] == 'public_restored_local_fun_undef' and red['OwnedProcessSettled'] is True
assert red['HistoricalGeneratedUndefConfirmed'] == 1 and red['HistoricalGeneratedActual'] == '{error,error,undef}'
ledger = read(bound(source['ActualCaseLedger']))
assert ledger['Passed'] is True and len(ledger['HostActuals']) == 12
managed = read(bound(ledger['ManagedActual']))
assert managed['RecipePassed'] is True and (managed['Executed'], managed['FixturePassed'], managed['FixtureFailed']) == (8, 8, 0)
for item in ledger['HostActuals']:
    assert read(bound(item))['Status'] == 'PASS'
control = ROOT / 'artifacts/root-part328-docs1'
output = ROOT / 'docs/validation/part-328'
assert not output.exists()
scopepath = ROOT / 'docs/module-readiness-scope.json'
note = (HERE / 'runtime-note.txt').read_text().strip()
scope = read(scopepath)
runtime = next(x for x in scope['Components'] if x['Id'] == 'Runtime')
if args.phase == 'scope':
    assert not control.exists() and not (ROOT / 'docs/changelog/0051.md').exists()
    assert note not in runtime['Implemented']
    before = [snapshot(x) for x in cfg['MetadataFiles']]
    original = scopepath.read_bytes()
    assert scope['ComparisonTestReport'] == 'validation/part-326/final-main/tests.json'
    runtime['Implemented'].append(note)
    scope['ComparisonTestReport'] = 'validation/part-327/final-main/tests.json'
    updated = encoded(scope)
    command = dict(MetadataBefore=before, ScopeBeforeSha256=sha(original), ScopeAfterSha256=sha(updated),
                   MainFreezeSha256=sha(fp.read_bytes()), FullGuardSha256=sha(fullpath.read_bytes()),
                   NativeGuardSha256=sha(nativepath.read_bytes()), ScriptSha256=sha(Path(__file__).read_bytes()),
                   ConfigurationSha256=sha((HERE / 'configuration.json').read_bytes()))
    control.mkdir()
    (control / 'scope-before.json').write_bytes(original)
    (control / 'scope-command.json').write_bytes(encoded(command))
    scopepath.write_bytes(updated)
    print('Scope328 applied; run actual readiness CLI, then finalize with exact readiness/exit SHA.')
    raise SystemExit(0)
assert args.readiness_sha256 and args.readiness_exit_sha256
ready_path = ROOT / 'docs/module-readiness.json'
exit_path = fp.parent / 'readiness-exit.json'
exact(ready_path, args.readiness_sha256); exact(exit_path, args.readiness_exit_sha256)
assert read(exit_path)['ExitCode'] == 0
ready = read(ready_path)
for key,value in dict(LocalPassedTests=6738, RegisteredMfas=509, PreviousRegisteredMfas=509, RegisteredMfaDelta=0,
                      ComparisonTestReport='validation/part-327/final-main/tests.json').items(): assert ready[key] == value
command = read(control / 'scope-command.json')
assert command['ScriptSha256'] == sha(Path(__file__).read_bytes()) and command['ConfigurationSha256'] == sha((HERE / 'configuration.json').read_bytes())
assert command['MainFreezeSha256'] == sha(fp.read_bytes()) and command['FullGuardSha256'] == sha(fullpath.read_bytes()) and command['NativeGuardSha256'] == sha(nativepath.read_bytes())
assert command['ScopeBeforeSha256'] == sha((control / 'scope-before.json').read_bytes())
assert command['ScopeAfterSha256'] == sha(scopepath.read_bytes())
expected_scope = read(control / 'scope-before.json')
next(x for x in expected_scope['Components'] if x['Id'] == 'Runtime')['Implemented'].append(note)
expected_scope['ComparisonTestReport'] = 'validation/part-327/final-main/tests.json'
assert scope == expected_scope
assert next(x for x in ready['Components'] if x['Id'] == 'Runtime')['Implemented'].count(note) == 1
for item in command['MetadataBefore']:
    if item['Path'] in cfg['HumanFiles'] + ['CHANGELOG.md', 'docs/supported-mfas.json']:
        assert snapshot(item['Path']) == item
assert not (ROOT / 'docs/changelog/0051.md').exists()
# Bind the actual outcomes into the immutable English source template; no guessed final hashes.
draft = exact(ROOT / cfg['EnglishDraftPath'], cfg['EnglishDraftSha256']).decode()
entry = draft.split('\n\n', 1)[1]
entry = entry.replace('The candidate defers restored local-fun dispatch until actual invocation.', 'The candidate defers compiler-source and unresolved restored local-fun dispatch until actual invocation; matching legacy public HOST factories retain their existing construction contract.')
entry = entry.replace('The actual executing ProcessContext.Runtime owns both local-entry/factory resolution and loaded-code/handler decisions.', 'For deferred compiler-source and unresolved wrappers, the actual executing ProcessContext.Runtime owns both local-entry/factory resolution and loaded-code/handler decisions.')
entry = entry.replace('Status: DRAFT; final Main, focused revision2, historical, native and readiness gates are pending. This entry must be rebound to actual receipts before publication.', 'Status: completed bounded part; actual final source, focused/historical, Main, native and readiness receipts are bound below.')
entry = entry.replace('These are authored SDK controls until actual revision2 execution is bound: __ACTUAL_FOCUS_AND_HISTORICAL_RESULT__.', 'Actual repaired revision3 passed all twelve HOST selectors (seven feature controls and five unchanged legacy regression groups) and all eight generated fixtures; identical updated HOST helpers over exact327 production produced a settled public undef historical control. The same updated historical composition also built and ran the generated discriminator, capturing {error,error,undef}; guard4 binds both actual historical results, and the legacy repair changes no ordinary source or expected outcome. The original pending-purge scheduler protocol is not inferred from these SDK controls.')
entry = entry.replace('__ACTUAL_MAIN_FULL_RESULT__', 'actual Main6738/6738 passed with eleven integration/package results and all6729 previous ordered test names preserved')
entry = entry.replace('The planned count is6738 (all6729 previous ordered tests plus eight ordinary cases and one HOST row), with eleven integrations required; this arithmetic is not an observed pass.', 'The observed increase is exactly eight appended ordinary cases and one HOST row.')
entry = entry.replace('__ACTUAL_POST_FULL_NATIVE_RESULT__', 'three fresh separate post-full trusted Windows processes passed legacy, same-runtime five-phase payload query and reallocation controls; these are SDK checks, not original Erlang or physical-memory comparisons')
entry = entry.replace('__ACTUAL_READINESS_CLI_RESULT__', 'actual readiness CLI exit0')
entry = entry.replace('Registry509/previous509/delta0 and351erlang remain expected presence totals pending actual inventory.', 'Actual registry509/previous509/delta0 and351erlang report API presence, not semantic completion.')
entry = entry.replace('__ACTUAL_FINAL_SOURCE_BINDINGS__', 'Main freeze '+sha(fp.read_bytes())+', closure '+sha(closure.read_bytes())+';29 owned paths/24 C# files/2393 complete inputs and both canonicalLF native-plan workflow seals')
entry = entry.replace('These targets are reserved and do not exist as proof merely because this draft links them.', 'The five linked targets are populated from the actual checked receipts.')
entry += '\n\nThe first actual Main full run failed6733/6738 with five unchanged legacy factory-construction regressions. The same captured failed Runtime reproduced all five failures. These are real regressions, not infrastructure failures. The repair restores only matching public Register HOST-factory construction at its existing selected decode or owner-preparation points: capture publication under Functions gate, construct outside it, return the exact legacy FunctionTerm. Compiler source factories remain deferred and never construct during decode. Legacy public HOST factories retain their existing selected decode or owner-preparation timing outside gates; no new ingress path is introduced. Old tests remain unchanged, and twelve focused groups include those five regression groups. The earlier failed Main1 reports/logs/registry/exit and same-DLL RED stay preserved; only fresh corrected Main full/native/readiness gates authorize this entry.\n\nThe recorded fatal first-fixture case also leaves generic non-tail CLR stack safety as a known remaining limitation; changing the fixture helper does not repair or establish production stack safety. Current original328 execution remains pending.\n'
assert '__ACTUAL_' not in entry and 'Status: DRAFT' not in entry and entry.startswith('## Part 328')
newvolume = ('# Detailed work changelog — volume 0051\n\n' + entry.strip() + '\n').encode()
assert len(newvolume) <= 32768 and len(newvolume.splitlines()) <= 400
changes = {ROOT / 'docs/changelog/0051.md': newvolume}
for name in cfg['HumanFiles']:
    raw = (ROOT / name).read_bytes(); assert note.encode() not in raw
    newline = b'\r\n' if b'\r\n' in raw else b'\n'
    at = raw.index(newline * 2) + 2 * len(newline)
    changes[ROOT / name] = raw[:at] + note.encode() + newline * 2 + raw[at:]
index = (ROOT / 'CHANGELOG.md').read_bytes()
oldline = '- [0050.md](docs/changelog/0050.md) — parts324–327 (active)'.encode()
assert index.count(oldline) == 1
nl = b'\r\n' if b'\r\n' in index else b'\n'
newlines = '- [0050.md](docs/changelog/0050.md) — parts324–327 (closed)\n- [0051.md](docs/changelog/0051.md) — part328 (active)'.encode().replace(b'\n', nl)
changes[ROOT / 'CHANGELOG.md'] = index.replace(oldline, newlines)
rows = static + cfg['ExtraRows']
additions = [(fullpath, 'final-main/full-guard.json'), (nativepath, 'final-native/guard.json'), (ready_path, 'module-readiness.json')]
additions += [(bound(source['AuthorFinalFreeze']), 'final-source/author-final-freeze.json'),
              (bound(source['IndependentPeer']), 'final-source/independent-composition-peer.json'),
              (bound(source['ActualCaseLedger']), 'final-source/actual-case-ledger.json'),
              (finalizer_peer_path, 'final-author/finalizer-source-peer.json')]

additions += [(ROOT / item['Path'], 'final-source/owned/' + item['Path']) for item in source['Files']]
for name in ('full-exit.json','full-attempt.json','full-validation.log','raw-supported-mfas.json','registry-before-full.json','readiness-exit.json','readiness-cli.log','source-freeze.json','source-resource-closure.json'):
    additions.append((fp.parent / name, 'final-main/' + name))
additions += [(ROOT / 'artifacts/tests.json','final-main/tests.json'),(ROOT / 'artifacts/integration-results.json','final-main/integration-results.json'),(args.full_guard_source.resolve(),'final-main/full-guard-source.py')]
for role in ('legacy','provider-query','reallocation'):
    for suffix in ('attempt.json','process.json','stdout','stderr'): additions.append((nativepath.parent / (role+'.'+suffix),'final-native/'+role+'.'+suffix))
for name in ('scope-before.json','scope-command.json'): additions.append((control / name,'final-author/'+name))
for name in ('finalize328.py','configuration.json','runtime-note.txt','proposal-freeze.json','README.md'): additions.append((HERE / name,'final-author/'+name))
rows += [row(p,t) for p,t in additions]
copies = {}; ordered = []
for item in rows:
    sourcepath = (ROOT / item['SourcePath']).resolve(); target = (ROOT / item['TargetPath']).resolve()
    assert sourcepath.is_relative_to(ROOT) and target.is_relative_to(output)
    assert sourcepath.suffix.lower() not in ('.dll','.zip','.tar','.gz','.so','.exe','.pdb')
    assert target.name not in ('evidence-index.json','evidence-receipt.json')
    raw = exact(sourcepath,item['Sha256']); assert len(raw) == item['Bytes'] and len(raw) < 12*1024*1024
    if target in copies:
        assert copies[target] == raw and next(x for x in ordered if x['TargetPath'] == item['TargetPath']) == item
        continue
    copies[target] = raw; ordered.append(item)
for link in re.findall(r'\[[^\]]+\]\(([^)]+)\)', entry):
    if link.startswith('../validation/part-328/'):
        target = (ROOT / 'docs/changelog' / link).resolve()
        assert target in copies or target == output / 'evidence-receipt.json'
# All preconditions, proof bytes and metadata renders settle before final writes. Output existing refuses replay.
output.mkdir(parents=True)
for path,raw in copies.items(): path.parent.mkdir(parents=True,exist_ok=True); path.write_bytes(raw)
for path,raw in changes.items(): path.write_bytes(raw)
(output / 'evidence-index.json').write_bytes(encoded(ordered))
receipt = dict(Status='PASS_ACTUAL_MAIN328_UNDEFINED_LAMBDA_FULL_NATIVE_READINESS',Passed=6738,Failed=0,IntegrationResults=11,
               All6729PreviousOrderedNamesPreserved=True,Registry=509,PreviousRegistry=509,RegistryDelta=0,ErlangPresence=351,
               SourcePaths=29,CSharpPaths=24,SourceClosurePaths=2393,SourceFreezeSha256=sha(fp.read_bytes()),SourceClosureSha256=sha(closure.read_bytes()),
               ActualFullGuardSha256=sha(fullpath.read_bytes()),ActualNativeGuardSha256=sha(nativepath.read_bytes()),ActualReadinessSha256=args.readiness_sha256.upper(),
               ActualFocusedSdkCases=8,ActualFocusedHostSelectors=12,LegacyFiveRegressionGroupsIncluded=True,FailedFirstFull6733Of6738Preserved=True,PublicLegacyHostFactoryConstructionRestoredSourceFactoriesDeferred=True,Current328OriginalExecuted=False,InitialFixtureFatalStackOverflowPreserved=True,
               GenericNonTailClrStackSafetyRemaining=True,Closed0050Sha256=sha((ROOT / 'docs/changelog/0050.md').read_bytes()),ActiveVolume='docs/changelog/0051.md',
               EvidenceIndexSha256=sha((output / 'evidence-index.json').read_bytes()),FinalizerSha256=sha(Path(__file__).read_bytes()),Files=ordered)
(output / 'evidence-receipt.json').write_bytes(encoded(receipt))
for item in cfg['ClosedVolumes']: bound(item)
stage = cfg['MetadataFiles'] + [x['Path'] for x in source['Files']] + [x['TargetPath'] for x in ordered]
stage += ['docs/validation/part-328/evidence-index.json','docs/validation/part-328/evidence-receipt.json']
(control / 'explicit-stage-files.json').write_bytes(encoded(dict(Files=list(dict.fromkeys(stage)),GitExecuted=False)))
print(encoded(dict(CopiedProofRows=len(ordered),ActiveVolumeBytes=len(newvolume),ReceiptSha256=sha((output / 'evidence-receipt.json').read_bytes()))).decode())
