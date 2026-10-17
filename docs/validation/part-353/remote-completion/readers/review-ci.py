"""UNEXECUTED passive published353 CI7313/11 reader; source adapted from corrected352."""
import collections
import json
from pathlib import Path
import re
import common as m

BASE_CI = 'artifacts/part352-ci-review/correction-final/ci/artifact-12084534509/tests.json'
BASE_SHA = '2445669ADE27293907E4C348B8A0FDAA7797D2552E1FF196781E18601A6A76EC'
FULL = 'docs/validation/part-353/full/'
RAW = FULL + 'raw/'
PEER_SHA = 'AC6719076493E263AD89A55311A5B2BE79E7DB0F78946BE89EB0AE92CF260703'
PACKAGE_PATHS = {'README.md', 'docs/DOTNET-LICENSE.txt', 'docs/DOTNET-ThirdPartyNotices.txt',
                 'docs/OTP-LICENSE.txt', 'docs/ROSLYN-LICENSE.txt', 'docs/OTP-PORTS.md'}
UNCHANGED_PARENT_LICENSES = {'docs/DOTNET-LICENSE.txt', 'docs/DOTNET-ThirdPartyNotices.txt', 'docs/OTP-LICENSE.txt'}
CLOSURE_INVENTORY = 'artifacts/root-part353-closure-publication-differences.json'
CLOSURE_INVENTORY_SHA = 'B1CBDAD8BBBE706E43F29D9EF1AFDBF5B4D531FB5A818C4290037C5D22F6E893'
STAGED_CLOSURE_DIFFERENCES = {'.gitattributes', 'CHANGELOG.md', 'docs/API_COMPATIBILITY.md',
    'docs/MODULE_READINESS.md', 'docs/OTP-PORTS.md', 'docs/changelog/0055.md',
    'docs/module-readiness.json', 'tools/Erlang.Differential/SchedulerAffinityCaseSources.cs'}


def verify_stable_source(args, composition, published):
    inventory_path = args.repository / CLOSURE_INVENTORY
    m.need(m.sha(inventory_path) == CLOSURE_INVENTORY_SHA, 'closed actual3106 publication inventory')
    inventory = m.read(inventory_path)
    m.need(inventory['Status'] == 'ACTUAL_FULL2_VS_PUBLISHED353_SOURCE_REPRESENTATION_INVENTORY_NO_RUNTIME_CLAIM'
           and inventory['Head'] == args.expected_head == '50e756f73437eb8d2882a19f729bccb09af74f3d'
           and inventory['Parent'] == '0e739220c2d5d7afc66b6fb94c1f2b15d156bd90', 'exact inventory subject')
    rows = inventory['Differences']
    m.need(len(rows) == len({r['Path'] for r in rows}) == 142, 'closed142 differences')
    differences = {r['Path']: r for r in rows}
    unchanged = {r['Path'] for r in rows if r['Staged'] is False}
    staged_set = {r['Path'] for r in rows if r['Staged'] is True}
    m.need(len(unchanged) == 134 and staged_set == STAGED_CLOSURE_DIFFERENCES
           and len(staged_set) == 8, 'closed134 unchanged and8 already admitted stage differences')
    stage = m.read(args.stage_guard)
    m.need(m.sha(args.stage_guard) == '54ED0A88E11466C8BC7BB347ECE214E01734DF7ECFB2D901F15A7D0F91CE3B1A', 'actual634 stage')
    records = {r['Path']: r for r in stage['Records']}
    m.need(not unchanged & set(records), 'unchanged allowance absent changed-stage records')
    actuals = m.blobs(args.repository, args.expected_head, [r['Path'] for r in composition['StableClosure']])
    parents = m.blobs(args.repository, inventory['Parent'], unchanged)
    observed, report = set(), []
    for row in composition['StableClosure']:
        path, data = row['Path'], actuals[row['Path']]
        if m.digest(data) == row['Sha256'] and len(data) == row['Bytes']:
            m.need(path not in differences, 'no unused mismatch permission')
            continue
        m.need(path in differences, 'unexpected frozen source mismatch ' + path)
        item = differences[path]
        m.need(item['FrozenSha256'] == row['Sha256'] and item['PublishedSha256'] == m.digest(data)
               and item['SourcePath'] == row['SourcePath'] and item['Staged'] == (path in records), 'exact mismatch row identities')
        if path in unchanged:
            m.need(not path.lower().endswith('.cs') and item['RawFrozenToLfExact'] is True
                   and item['PublishedRawSameParent'] is True and data == parents[path], 'nonCSharp unchanged parent raw equality')
            source = (args.repository / row['SourcePath']).read_bytes()
            m.need(len(source) == row['Bytes'] and m.digest(source) == row['Sha256']
                   and b'\x00' not in source and b'\x00' not in data, 'frozen text source identity')
            source.decode('utf-8', errors='strict')
            data.decode('utf-8', errors='strict')
            m.need(source.replace(b'\r\n', b'\n') == data, 'closed exact CRLF to LF only')
            reason = 'Closed134UnchangedUtf8TextLfParentRawExact'
        elif path == 'tools/Erlang.Differential/SchedulerAffinityCaseSources.cs':
            original = published['docs/validation/part-353/tested-source/' + path]
            m.need(m.digest(original) == row['Sha256'], 'retained tested fixture bytes')
            m.exact_five_fixture_blank_lines(original, data)
            reason = 'ExactFiveBlankLinesCompiledValuesIdentical'
        elif path in ('.gitattributes', 'docs/OTP-PORTS.md'):
            source = (args.repository / row['SourcePath']).read_bytes()
            record = records[path]
            m.need(record['ComparisonMode'] == 'TextLF' and record['WorkingSha256'] == row['Sha256']
                   and record['StagedSha256'] == m.digest(data) and m.digest(source) == row['Sha256']
                   and source.replace(b'\r\n', b'\n') == data, 'previously closed actual-stage TextLF')
            reason = 'ActualStageTextLF'
        else:
            m.need(path in STAGED_CLOSURE_DIFFERENCES and (path.startswith('docs/') or path == 'CHANGELOG.md')
                   and records[path]['StagedSha256'] == m.digest(data), 'only closed staged postfull documentation')
            reason = 'ClosedActualStagedPostFullDocumentation'
        observed.add(path)
        report.append({'Path': path, 'FrozenSha256': row['Sha256'], 'PublishedSha256': m.digest(data), 'Reason': reason})
    m.need(observed == set(differences), 'all and only142 inventory differences observed')
    return report


def verify_package_inputs(args, package_rows):
    m.need(m.sha(args.stage_guard) == '54ED0A88E11466C8BC7BB347ECE214E01734DF7ECFB2D901F15A7D0F91CE3B1A', 'actual package policy guard')
    stage = m.read(args.stage_guard)
    policies = stage['Guard']['PackageInputs']
    m.need(len(policies) == len(package_rows) == 6 and {r['Path'] for r in policies}
           == {r['Path'] for r in package_rows} == PACKAGE_PATHS, 'exact six package policies')
    policy = {r['Path']: r for r in policies}
    records = {r['Path']: r for r in stage['Records']}
    actuals = m.blobs(args.repository, args.expected_head, PACKAGE_PATHS)
    parents = m.blobs(args.repository, stage['ParentHead'], UNCHANGED_PARENT_LICENSES)
    differences = []
    for row in package_rows:
        path, current, rule = row['Path'], actuals[row['Path']], policy[row['Path']]
        source = (args.repository / row['SourcePath']).read_bytes()
        m.need(m.digest(source) == row['Sha256'] == rule['TestedRawSha256']
               and len(source) == row['Bytes'] and m.digest(current) == rule['ObservedSha256'], 'frozen raw and actual observed package identities')
        if current == source:
            m.need(rule['ComparisonMode'] == 'RawExact' and rule['RawExact'] is True, 'exact raw package policy')
        else:
            m.need(rule['ComparisonMode'] == 'DeclaredPackageTextLF' and rule['RawExact'] is False
                   and source.replace(b'\r\n', b'\n') == current, 'only exact six-policy declared package LF transformation')
            if path in UNCHANGED_PARENT_LICENSES:
                m.need(path not in records and current == parents[path], 'unchanged license raw-identical to parent Git blob')
            else:
                m.need(path == 'docs/OTP-PORTS.md' and path in records
                       and records[path]['ComparisonMode'] == 'TextLF'
                       and records[path]['WorkingSha256'] == row['Sha256']
                       and records[path]['StagedSha256'] == m.digest(current), 'changed OTP-PORTS precise actual-stage TextLF')
            differences.append(path)
    m.need(set(differences) == UNCHANGED_PARENT_LICENSES | {'docs/OTP-PORTS.md'}, 'closed four actual LF representations')
    return differences


def main(args):
    extracted, run, job, log = m.transport(args, 'ci')
    plan, published, stage_count = m.publication(args)
    test_files, integration_files = list(extracted.rglob('tests.json')), list(extracted.rglob('integration-results.json'))
    m.need(len(test_files) == len(integration_files) == 1, 'one remote test/integration report')
    remote, integrations = m.read(test_files[0]), m.read(integration_files[0])
    local = json.loads(published[RAW + 'artifacts/tests.json'])
    previous = m.read(args.repository / BASE_CI)
    m.need(m.sha(args.repository / BASE_CI) == BASE_SHA, 'actual corrected352 baseline7297')
    names, old_names = m.test_names(remote, 7313), [r['Name'] for r in previous['Tests']]
    m.need(previous['Passed'] == len(old_names) == 7297 and previous['Failed'] == 0
           and names[:7297] == old_names and names == [r['Name'] for r in local['Tests']], 'old7297 orderedprefix/local7313 names')
    peer = json.loads(published[FULL + 'independent-report.json'])
    m.need(m.digest(published[FULL + 'independent-report.json']) == PEER_SHA
           and peer['Status'] == 'PASS_INDEPENDENT_ACTUAL_CORRECTED_CANONICAL353_FULL2_RAW_REGISTRY_RESTORATION_PENDING'
           and peer['Passed'] == local['Passed'] == 7313 and peer['Failed'] == local['Failed'] == 0
           and names[7297:] == peer['AddedTestNames'] and len(peer['AddedTestNames']) == 16
           and sum(n.startswith('oracle/compiled-module/') for n in names[7297:]) == 5, 'accepted independent full2/5compiled11HOST')
    m.need(integrations == json.loads(published[RAW + 'artifacts/integration-results.json'])
           == peer['IntegrationResults'] == m.EXPECTED_INTEGRATIONS, 'all11 real outcomes')
    composition = json.loads(published[RAW + 'composition-plan.json'])
    staged_records = {r['Path']: r for r in m.read(args.stage_guard)['Records']}
    m.need(m.digest(published[RAW + 'composition-plan.json']) == peer['CompositionPlanSha256']
           == '3F8C5F95912959D7BE22BDDA3E6F39B696AC644EA98E5461C6B67C64771F06BC', 'actual corrected composition')
    m.need(len(composition['Files']) == 3107 and len(composition['StableClosure']) == 3106
           and composition['RegistryExcludedFromStableClosure'] is True, 'stable closure excludes generated registry')
    metadata_changes = verify_stable_source(args, composition, published)
    registry = m.blobs(args.repository, args.expected_head, ['docs/supported-mfas.json', 'docs/mfa-compatibility.json', 'docs/module-readiness.json'])
    before, raw_registry = json.loads(published[RAW + 'registry-before.json']), json.loads(published[RAW + 'raw-supported-mfas.json'])
    restored = json.loads(registry['docs/supported-mfas.json'])
    m.need(registry['docs/supported-mfas.json'] == published[RAW + 'registry-before.json']
           and len(restored) == len(raw_registry) == 587
           and [{k: v for k, v in r.items() if k != 'Limits'} for r in raw_registry]
           == [{k: v for k, v in r.items() if k != 'Limits'} for r in before], 'raw registry separate from exact customLimits restoration')
    m.need(sum(n.startswith('mfa/') for n in names) == 587 and len({r['Module'] for r in restored}) == 14, 'registry/test count')
    ledger, readiness = json.loads(registry['docs/mfa-compatibility.json']), json.loads(registry['docs/module-readiness.json'])
    counts = dict(collections.Counter(e['Status'] for e in ledger['Entries']))
    m.need(counts == {'Verified compatible': 16, 'Known differences': 24, 'Needs verification': 547}
           and len(ledger['Entries']) == 587 and len(ledger['VerifiedSourceFiles']) == 89, 'compatibility unchanged16/24/547')
    m.need((readiness['RegisteredMfas'], readiness['LocalPassedTests'], readiness['VerifiedCompatibleMfas'],
            readiness['KnownDifferenceMfas'], readiness['NeedsVerificationMfas']) == (587, 7313, 16, 24, 547), 'published readiness')
    guards = m.blobs(args.repository, args.expected_head, [r['Path'] for r in ledger['VerifiedSourceFiles']])
    m.need(len(guards) == 89, 'unique89 sourceguards')
    for row in ledger['VerifiedSourceFiles']:
        m.need(m.digest(guards[row['Path']]) == row['Sha256'], 'current source guard')
    evidence_paths = {'docs/' + r['Path'] for e in ledger['Entries'] for r in e['Evidence']}
    evidence = m.blobs(args.repository, args.expected_head, evidence_paths)
    for entry in ledger['Entries']:
        for row in entry['Evidence']:
            m.need(m.digest(evidence['docs/' + row['Path']]) == row['Sha256'], 'ledger evidence')
    package_rows = composition['PackageResources']
    m.need(len(package_rows) == len(plan['PackageInputs']) == 6, 'six package inputs')
    package_lf = verify_package_inputs(args, package_rows)
    # Nupkg/DLL payloads are intentionally absent from public evidence; retain independent actual package authority.
    m.need(len(peer['Packages']) == 2 and peer['PackageInputs'] == 6
           and all(r['CrcAndSafeUniquePathsVerified'] is True for r in peer['Packages']), 'two actual package receipts')
    child_receipts = [p for p in published if p.startswith(FULL + 'children/') and p.endswith('/receipt.json')]
    m.need(len(child_receipts) == 12, 'twelve retained bounded children')
    matrix = {(family, operation, 32768) for family in ('tuple', 'list-head', 'map-value', 'fun-environment', 'native-record')
              for operation in ('compare', 'hash')} | {('tuple', 'shallow', 6), ('tuple', 'snapshot', 0)}
    actual_matrix = set()
    for path in child_receipts:
        value = json.loads(published[path])
        m.need(value['Started'] is True and value['Settled'] is True and value['TimedOut'] is False
               and value['KillRequested'] is False and value['ExitCode'] == 0 and not value['LifecycleFailures']
               and value['StandardOutputComplete'] is True and value['StandardErrorComplete'] is True
               and value['ProcessId'] > 0 and 0 <= value['ElapsedSeconds'] <= 60, 'normal bounded child')
        parent = path.rsplit('/', 1)[0]
        stdout, stderr = published[parent + '/stdout.txt'], published[parent + '/stderr.txt']
        m.need(m.digest(stdout) == value['StandardOutputSha256'] and m.digest(stderr) == value['StandardErrorSha256']
               and stderr == b'', 'child raw streams')
        lines = stdout.decode('utf-8-sig').splitlines()
        identity = json.loads(lines[0])
        m.need(identity['TermsAssemblySha256'] == value['TermsDllSha256'] == peer['ActualChildTermsIdentity']
               and lines[-1] == 'PASS', 'actual loaded Terms identity')
        if value['Operation'] in ('compare', 'hash'):
            m.need('CONSTRUCTED' in lines, 'constructed before recursion operation')
        actual_matrix.add((value['Family'], value['Operation'], value['Depth']))
    m.need(actual_matrix == matrix, 'exact child matrix')
    plans = []
    for path, workflow, variable, count in (
        ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', '.github/workflows/nif-pending-native.yml', 'REVIEWED_SOURCE_PLAN_SHA256', 1268),
        ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json', '.github/workflows/nif-reallocation-native.yml', 'REVIEWED_REALLOCATION_PLAN_SHA256', 1293)):
        pair = m.blobs(args.repository, args.expected_head, [path, workflow])
        data = pair[path]
        body = json.loads(data)
        m.need(b'\r' not in data and not data.startswith(b'\xef\xbb\xbf') and len(body['Files']) == count, 'LF plan count')
        values = m.blobs(args.repository, args.expected_head, [r['Path'] for r in body['Files']])
        for row in body['Files']:
            raw = values[row['Path']]
            m.need(row['HashMode'] in ('TextLF', 'BinaryExact'), 'plan hash mode')
            if row['HashMode'] == 'TextLF': raw = raw.replace(b'\r\n', b'\n')
            m.need(m.digest(raw) == row['Sha256'] and len(raw) == row['Bytes'], 'current native plan row')
        m.need(re.findall(variable.encode() + rb': ([A-F0-9]{64})', pair[workflow]) == [m.digest(data).encode()], 'exact source plan seal')
        plans.append({'Path': path, 'Rows': count, 'Sha256': m.digest(data)})
    m.need('origin +' + args.expected_head + ':refs/remotes/origin/main' in log
           and len(re.findall(r'^\S+ ' + args.expected_head + r'\s*$', log, re.M)) >= 2, 'actual checkout subject log')
    m.fresh(args.report, {'Status': 'PASS_PASSIVE_PUBLISHED353_CI7313_ELEVEN_INTEGRATIONS',
        'Head': args.expected_head, 'Run': args.run, 'Job': job['id'], 'Artifact': args.artifact,
        'Passed': 7313, 'Failed': 0, 'Integrations': 11, 'Old7297OrderedPrefixPreserved': True,
        'Added16Names': names[7297:], 'Registry': 587, 'RestoredCustomLimitsRawExact': True,
        'RawGeneratedRegistrySha256': m.digest(published[RAW + 'raw-supported-mfas.json']),
        'RestoredRegistrySha256': m.digest(registry['docs/supported-mfas.json']),
        'SourceGuards': 89, 'CompatibilityCounts': counts, 'PublishedStageBlobs': stage_count,
        'StableSourceRows': 3106, 'ExplicitPostFullMetadataDifferences': metadata_changes,
        'ClosedPublicationInventorySha256': CLOSURE_INVENTORY_SHA,
        'UnchangedNonCSharpParentExactTextLfRows': 134,
        'WholeFrozenClosureRawExact': not metadata_changes, 'Packages': 2, 'PackageResources': 6,
        'PublishedPackageResourcesRawExactTested': not package_lf, 'PublishedPackageResourceLfOnlyPaths': package_lf,
        'PackagePayloadRehashedByThisReader': False, 'PackageScope': 'Accepted actual full2 independent receipt; public archive excludes nupkg/DLL payloads.',
        'BoundedChildren': 12, 'NativePlans': plans, 'LedgerEvidenceFiles': len(evidence),
        'ActualRemoteTestReportSha256': m.sha(test_files[0]), 'IndependentFull2Sha256': PEER_SHA,
        'NativeOrSdkExecutedByReviewer': False, 'NetworkUsed': False, 'CompatibilityPromotionClaimed': False})


if __name__ == '__main__':
    main(m.parser().parse_args())
