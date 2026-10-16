"""Part325-only source proposal: passive validation shared by Root's two explicit entry points."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = Path.cwd().resolve()
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
encoded = lambda value: (json.dumps(value, indent=2) + '\n').encode('utf-8')


def exact(path, digest):
    raw = path.read_bytes()
    assert len(digest) == 64 and sha(raw) == digest.upper(), str(path)
    return raw


def arguments(parser):
    for name in ('main-source-freeze', 'full-guard', 'native-guard'):
        parser.add_argument('--' + name, required=True, type=Path)
        parser.add_argument('--' + name + '-sha256', required=True)


def observed(args):
    for item in read(HERE / 'proposal-freeze.json')['Files']:
        assert len(exact(HERE / item['Path'], item['Sha256'])) == item['Bytes']
    cfg = read(HERE / 'configuration.json')
    paths = [args.main_source_freeze.resolve(), args.full_guard.resolve(), args.native_guard.resolve()]
    assert all(path.is_relative_to(ROOT / 'artifacts') for path in paths)
    fp, fullpath, nativepath = paths
    assert fp.parent == fullpath.parent and fp.name == 'source-freeze.json'
    assert args.main_source_freeze_sha256.upper() == cfg['ActualMainFreezeSha256']
    exact(fp, args.main_source_freeze_sha256)
    exact(fullpath, args.full_guard_sha256)
    exact(nativepath, args.native_guard_sha256)
    source, full, native = read(fp), read(fullpath), read(nativepath)
    closure = fp.parent / 'source-resource-closure.json'
    exact(closure, cfg['FinalClosureSha256'])
    assert source['FinalSourceClosureSha256'] == cfg['FinalClosureSha256']
    assert source['AuthorFinalFreeze']['Sha256'] == cfg['AuthorFinalFreezeSha256']
    exact(ROOT / source['AuthorFinalFreeze']['Path'], cfg['AuthorFinalFreezeSha256'])
    assert source['IndependentPeer']['Sha256'] == cfg['FinalCompositionPeerSha256']
    exact(ROOT / source['IndependentPeer']['Path'], cfg['FinalCompositionPeerSha256'])
    assert source['ParentHead'] == cfg['ParentHead']
    assert (source['SourcePaths'], source['CSharpPaths'], source['SourceClosurePaths']) == (23, 7, 2366)
    rows = read(closure)['Files']
    assert len(rows) == 2366
    for item in rows:
        assert len(exact(ROOT / item['Path'], item['Sha256'])) == item['Bytes']
    assert full['Status'] == 'PASS_ACTUAL_MAIN325_6693_FULL_AND_ELEVEN_INTEGRATIONS'
    assert (full['Passed'], full['Failed'], full['IntegrationResults'], full['Registry'], full['ErlangMfas']) == (6693, 0, 11, 509, 351)
    assert full['SourceFreezeSha256'] == sha(fp.read_bytes()) and full['SourceClosureSha256'] == sha(closure.read_bytes())
    assert full['FullExecutionContext'] == 'Outside sandbox' and full['PublicMfaDelta'] == 0
    assert full['All6692PreviousOrderedNamesPreserved'] and full['TwentyTwoPriorCustomLimitsRestoredBeforeUpdate']
    assert full['ProcessInfo1DefaultCatalogPreserved'] and full['ProcessFlag2And3ExactParent'] and full['Unlink1ExactParent']
    assert full['ConnectedLinkAcceptedOwnershipAndAsyncScopeUpdated'] and full['ObsoleteProcessInfo2Followup325TextRemovedOnly']
    assert full['UnrelatedMfaRowsExactParent'] and full['PendingAutoconnectFullPriorityLivePeersNativeParityStillPartial']
    assert sorted(full['UpdatedScopeMfas']) == [['erlang', 'link', 1], ['erlang', 'link', 2], ['erlang', 'process_info', 2]]
    assert full['GuardSourceSha256'] == cfg['FullGuardSource']['Sha256']
    assert len(exact(ROOT / cfg['FullGuardSource']['Path'], cfg['FullGuardSource']['Sha256'])) == cfg['FullGuardSource']['Bytes']
    assert read(fp.parent / 'full-exit.json')['ExitCode'] == 0
    assert read(fp.parent / 'full-exit.json')['Context'] == 'Outside sandbox'
    assert nativepath.parent == ROOT / 'artifacts/root-part325-main-windows-native1'
    assert len(exact(ROOT / cfg['NativeGuardSource']['Path'], cfg['NativeGuardSource']['Sha256'])) == cfg['NativeGuardSource']['Bytes']
    assert native['Status'] == 'PASS_ACTUAL_MAIN325_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE'
    assert (native['FullPassed'], native['SourceFilesRehashed'], native['OriginalExecuted']) == (6693, 2366, False)
    assert native['SourceFreezeSha256'] == sha(fp.read_bytes())
    assert native['ActualFullGuard']['Sha256'] == sha(fullpath.read_bytes())
    assert native['ActualTrustedCQueryExecuted'] is True and native['PhysicalMemoryCompared'] is False
    assert {item['Role'] for item in native['Roles']} == {'legacy', 'provider-query', 'reallocation'}
    assert len(native['Roles']) == 3 and all(item['ExitCode'] == 0 for item in native['Roles'])
    tests = read(ROOT / 'artifacts/tests.json')
    assert tests['Passed'] == len(tests['Tests']) == 6693 and tests['Failed'] == 0
    assert all(item['Status'] == 'Passed' for item in tests['Tests'])
    assert len(read(ROOT / 'artifacts/integration-results.json')) == 11
    assert sha((fp.parent / 'raw-supported-mfas.json').read_bytes()) == full['RawRegistrySha256']
    # Restore-before-update is performed by the full guard. Independently require every unrelated parent row exact.
    baseline = read(fp.parent / 'registry-before-full.json')
    registry = read(ROOT / 'docs/supported-mfas.json')
    assert len(baseline) == len(registry) == 509 and sum(item['Module'] == 'erlang' for item in registry) == 351
    key = lambda item: (item['Module'], item['Function'], item['Arity'])
    assert [key(item) for item in registry] == [key(item) for item in baseline]
    for before, after in zip(baseline, registry):
        if key(after) in {('erlang', 'link', 1), ('erlang', 'link', 2)}:
            assert {k: v for k, v in after.items() if k != 'Limits'} == {k: v for k, v in before.items() if k != 'Limits'}
            assert after['Limits'] == before['Limits'].split(' Already-connected guest remote PID links publish', 1)[0] + cfg['LinkLimitsSuffix']
        elif key(after) == ('erlang', 'process_info', 2):
            assert {k: v for k, v in after.items() if k != 'Limits'} == {k: v for k, v in before.items() if k != 'Limits'}
            needle = 'LINK phase/retention follow-up325, '
            assert before['Limits'].count(needle) == 1 and after['Limits'] == before['Limits'].replace(needle, '')
        else:
            assert after == before, key(after)
    closed = cfg['Closed0049']
    assert len(exact(ROOT / closed['Path'], closed['Sha256'])) == closed['Bytes']
    static = read(HERE / 'existing-copy-rows.json')['Rows']
    for item in static:
        assert len(exact(ROOT / item['SourcePath'], item['Sha256'])) == item['Bytes']
    return cfg, fp, closure, fullpath, nativepath, static


def readiness(args, main):
    assert args.readiness_sha256 and args.readiness_exit_sha256
    ready_path, exit_path = ROOT / 'docs/module-readiness.json', main / 'readiness-exit.json'
    exact(ready_path, args.readiness_sha256)
    exact(exit_path, args.readiness_exit_sha256)
    assert read(exit_path)['ExitCode'] == 0
    ready = read(ready_path)
    required = {'LocalPassedTests': 6693, 'RegisteredMfas': 509, 'PreviousRegisteredMfas': 509,
                'RegisteredMfaDelta': 0, 'ComparisonTestReport': 'validation/part-324/final-main/tests.json'}
    for key, value in required.items():
        assert ready[key] == value, key
    return ready


def copy_rows(cfg, fp, fullpath, nativepath, static):
    rows = list(static)
    additions = [(fullpath, 'final-main/full-guard.json'), (nativepath, 'final-native/guard.json')]
    for name in ('full-exit.json', 'full-validation.log', 'raw-supported-mfas.json', 'registry-before-full.json',
                 'readiness-exit.json', 'readiness-cli.log', 'source-freeze.json', 'source-resource-closure.json'):
        additions.append((fp.parent / name, 'final-main/' + name))
    for source, name in [('artifacts/tests.json', 'final-main/tests.json'),
                         ('artifacts/integration-results.json', 'final-main/integration-results.json'),
                         ('docs/module-readiness.json', 'module-readiness.json')]:
        additions.append((ROOT / source, name))
    for role in ('legacy', 'provider-query', 'reallocation'):
        for suffix in ('attempt.json', 'process.json', 'stdout', 'stderr'):
            name = role + '.' + suffix
            additions.append((nativepath.parent / name, 'final-native/' + name))
    additions += [(HERE / name, 'final-author/' + name) for name in
                  ('finalize325.py', 'prepare-final-texts.py', 'gates325.py', 'configuration.json', 'proposal-freeze.json')]
    for source, target in additions:
        raw = source.read_bytes()
        rows.append({'SourcePath': source.relative_to(ROOT).as_posix(),
                     'TargetPath': 'docs/validation/part-325/' + target, 'Bytes': len(raw), 'Sha256': sha(raw)})
    return rows
