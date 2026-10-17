"""UNEXECUTED passive ordinary354 adaptation; no VM/SDK/network or downloaded code execution."""
import base64
import json
import re
import subprocess
from pathlib import Path
import common as m

BASE_HEAD = 'a9ec41acf2eb5727f60c613ba8146c470f82fbb0'
BASE_REPORT = 'artifacts/root-part353-ordinary-remote/capture-12087319277/artifact-12087319277/differential.json'
BASE_SHA = '489542F91DADBA7964ED522E66263E107A3615855A74E5D1F766E79724F86280'


def main(args):
    extracted, run, job, log = m.transport(args, 'ordinary')
    plan, promoted, stage_count = m.publication(args)
    old_path = args.repository / BASE_REPORT
    m.need(m.sha(old_path) == BASE_SHA, 'actual original353 baseline')
    old, current = m.read(old_path), m.read(extracted / 'differential.json')
    changes = []
    for value, count in ((old, 6321), (current, 6344)):
        m.need(value['Planned'] == value['Executed'] == value['Passed'] == count and value['Failed'] == 0
               and value['Complete'] is True and value['VersionVerified'] is True and value['Baseline'] == 'OTP-29.1.1'
               and value['InvestigationOnly'] is False and value['ReferenceCoverageComplete'] is False
               and all(value[k] is None for k in ('AbortedSource', 'InfrastructureError', 'AbortedNativeEvidence')), 'complete ordinary counts')
    for key, count, added in (('Results', 778, 0), ('ModuleResults', 5374, 23), ('DiagnosticResults', 169, 0)):
        m.need(len(old[key]) == count and len(current[key]) == count + added and all(r['Passed'] for r in current[key]), 'track counts')
        m.need([r.get('Name', r.get('Source')) for r in old[key]]
               == [r.get('Name', r.get('Source')) for r in current[key][:count]], 'old ordered contracts')
        changes.extend({'Track': key, 'Index': i, 'Old': a, 'New': b}
                       for i, (a, b) in enumerate(zip(old[key], current[key])) if a != b)
    m.need(len(changes) <= 1, 'only predeclared map stdout may differ')
    canonical, alternative = '237B613D3E7B307D2C623D3E2241227D0D0A', '237B623D3E2241222C613D3E7B307D7D0D0A'
    for change in changes:
        m.need(change['Track'] == 'ModuleResults' and change['Index'] == 3605, 'exact historical map row')
        a, b = change['Old'], change['New']
        m.need(a['Name'] == b['Name'] == 'erlang-term-display-map'
               and {k for k in set(a) | set(b) if a.get(k) != b.get(k)}
               <= {'NativeStandardOutputHex', 'NativeAndSdkStandardOutputExactlyMatch'}, 'no arbitrary old row normalization')
        for row in (a, b):
            m.need(row['ExpectedStandardOutputHex'] == row['SdkStandardOutputHex'] == canonical
                   and row['ExpectedStandardOutputAlternatives'] == [alternative]
                   and row['NativeStandardOutputHex'] in (canonical, alternative)
                   and row['NativeAndSdkStandardOutputExactlyMatch'] == (row['NativeStandardOutputHex'] == canonical)
                   and row['StandardStreamsMatch'] is True and row['Expected'] == row['FixtureExpected'] == row['Actual'] == '{ok,done}', 'predeclared map policy')
    prefix = 'tools/Erlang.Differential/'
    paths = [prefix + 'SizeError' + suffix + '.cs' for suffix in ('CaseNames', 'CaseSources', 'Cases')]
    paths += [prefix + n for n in ('CompiledModuleCases.cs', 'OracleOutcomeTags.cs', 'TermDisplayCases.cs', 'CompiledStandardOutputContract.cs')]
    paths += ['reference-baseline.json', '.github/workflows/differential.yml']
    sources = m.blobs(args.repository, args.expected_head, paths)
    baseline = json.loads(sources['reference-baseline.json'])
    m.need(baseline['commit'] == m.PIN and baseline['tag'] == 'OTP-29.1.1', 'pinned reference')
    names = m.constants(sources[prefix + 'SizeErrorCaseNames.cs'].decode())
    texts = m.constants(sources[prefix + 'SizeErrorCaseSources.cs'].decode())
    catalog = sources[prefix + 'SizeErrorCases.cs'].decode()
    pairs = re.findall(r'new\(\s*SizeErrorCaseNames\.(\w+),\s*SizeErrorCaseSources\.(\w+),', catalog)
    m.need(len(pairs) == 23 and names['Done'] == 'done'
           and len(re.findall(r'Term.Tuple\(Term.A\(OracleOutcomeTags.Success\), Term.A\(SizeErrorCaseNames.Done\)\)', catalog)) == 23
           and m.constants(sources[prefix + 'OracleOutcomeTags.cs'].decode())['Success'] == 'ok', 'source-derived expectations')
    contracts = [{'Name': names[n], 'Source': texts[s]} for n, s in pairs]
    m.need(len({c['Name'] for c in contracts}) == 23, '23 unique contracts')
    for row, contract in zip(current['ModuleResults'][5374:], contracts):
        m.need(row['Name'] == contract['Name'] and row['Source'] == contract['Source']
               and row['SourceSha256'] == m.digest(contract['Source'].encode())
               and row['Expected'] == row['FixtureExpected'] == row['Actual'] == '{ok,done}'
               and row['CallerContext'] == 'UntrappedFresh' and row['CompilerPolicy'] == 'Default'
               and row['Passed'] is True and row['StandardStreamsMatch'] is True
               and all(row.get(k) in (None, '') for k in ('NativeStandardOutputHex', 'NativeStandardErrorHex', 'SdkStandardOutputHex', 'SdkStandardErrorHex')), 'actual five source/golden/caller contracts')
    listing = subprocess.check_output(['git', '-C', str(args.repository), 'ls-tree', '-r', '--name-only', BASE_HEAD, '--', prefix], timeout=30).decode().splitlines()
    old_paths = [p for p in listing if p.endswith('.cs')]
    old_sources = m.blobs(args.repository, BASE_HEAD, old_paths)
    new_sources = m.blobs(args.repository, args.expected_head, old_paths)
    for path in old_paths:
        before, after = old_sources[path], new_sources[path]
        if path == prefix + 'CompiledModuleCases.cs':
            m.need(after == before.replace(b'.. SchedulerAffinityCases.Modules', b'.. SchedulerAffinityCases.Modules,\n        .. SizeErrorCases.Modules'), 'single additive catalog spread')
        else:
            m.need(before == after, 'old oracle source preserved ' + path)
    for key in ('OriginalOtpDefectExclusions', 'SpawnContextDiagnostics', 'NativeCompilationInvestigation', 'ExpressionTrack', 'CompilerDiagnosticTrack'):
        m.need(current[key] == old[key], 'old policy/exclusions unchanged')
    track, prior = current['CompiledModuleTrack'], old['CompiledModuleTrack']
    m.need(track['Planned'] == track['Executed'] == track['Passed'] == 5397 and track['Failed'] == 0
           and {k: v for k, v in track.items() if k not in ('Planned', 'Executed', 'Passed')}
           == {k: v for k, v in prior.items() if k not in ('Planned', 'Executed', 'Passed')}, 'compiled track')
    for row in current['ModuleResults'] + current['DiagnosticResults']:
        m.need(m.digest(row['Source'].encode()) == row['SourceSha256'], 'every module/diagnostic source hash')
    fd = m.read(extracted / 'fd-differential.json')
    old_fd = m.read(old_path.parent / 'fd-differential.json')
    m.need(fd == old_fd and fd['Passed'] == len(fd['Results']) == 8 and fd['Failed'] == 0, 'fd8 raw unchanged')
    for row in fd['Results']:
        markers = re.findall(r'ERLANG_NET_ETF_BEGIN:([A-Za-z0-9+/=]+):ERLANG_NET_ETF_END', row['StandardOutput'])
        m.need(row['Passed'] is True and row['ExitCode'] == 0 and row['StandardError'] == ''
               and row['Native'] == row['Sdk'] == '{ok,done}' and row['ActualOutputHex'] == row['OutputHex']
               and len(markers) == 1 and base64.b64decode(markers[0], validate=True) == bytes.fromhex('83680277026F6B7704646F6E65'), 'fd raw markers/streams')
    workflow = sources['.github/workflows/differential.yml']
    m.need(workflow == m.blobs(args.repository, BASE_HEAD, ['.github/workflows/differential.yml'])['.github/workflows/differential.yml'], 'ordinary default workflow flags unchanged')
    m.need(m.PIN in log and 'checking OTP version... 29.1.1' in log, 'actual pinned build log')
    expected_pass = [r['Source'] for r in current['Results']] + ['module ' + r['Name'] for r in current['ModuleResults']] + ['diagnostic ' + r['Name'] for r in current['DiagnosticResults']]
    m.need(re.findall(r'^\S+ PASS (.*)$', log, re.M) == expected_pass, 'exact job PASS order6344')
    variants = [{'Index': i, 'Name': r['Name']} for i, r in enumerate(current['ModuleResults']) if not r['NativeAndSdkStandardOutputExactlyMatch']]
    m.need(all(v == {'Index': 3605, 'Name': 'erlang-term-display-map'} for v in variants), 'no undeclared stdout variant')
    report = {'Status': 'PASS_PASSIVE_PUBLISHED354_ORIGINAL6344_FD8_SEMANTIC_CONTRACTS',
        'Head': args.expected_head, 'Run': args.run, 'Job': job['id'], 'Artifact': args.artifact,
        'Passed': 6344, 'Failed': 0, 'FdPassed': 8, 'Previous6321OrderedContractsPreserved': True,
        'PublishedStageBlobs': stage_count, 'SourceDerivedNewContracts': contracts,
        'SelectedNewRawRows': current['ModuleResults'][5374:], 'OldPrefixRawChanges': changes,
        'CurrentNativeSdkRawMapVariants': variants, 'StrictRawEqualityPassed': not changes and not variants,
        'ReportSha256': m.sha(extracted / 'differential.json'), 'PriorReportSha256': BASE_SHA,
        'SdkOrNativeExecutedByReviewer': False, 'NetworkUsed': False, 'WholeSchedulerCompatibilityClaimed': False}
    m.fresh(args.report, report)
    m.fresh(args.strict_report, {'Status': 'PASS_STRICT_RAW_EQUALITY' if report['StrictRawEqualityPassed'] else
        'REFUSED_STRICT_RAW_EQUALITY_PREDECLARED_MAP_STDOUT_VARIANCE', 'SemanticGuardSha256': m.sha(args.report),
        'HistoricalChanges': changes, 'CurrentVariants': variants, 'RawNormalization': False})


if __name__ == '__main__':
    parser = m.parser()
    parser.add_argument('--strict-report', type=Path, required=True)
    main(parser.parse_args())
