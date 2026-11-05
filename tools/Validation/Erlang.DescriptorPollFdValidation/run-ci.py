"""Execute existing descriptor fixtures on an exact committed production tree."""
import argparse
import datetime
import hashlib
import json
import os
import pathlib
import re
import signal
import subprocess
import time

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
VERSION = '29.1.1'
RUNTIME = '10.0.11'
TOOL = pathlib.Path(__file__).resolve().parent
ROOT = TOOL.parents[2]
EXPECTED = ['stream', 'packet-one', 'packet-two', 'packet-four', 'line', 'list', 'reject-exit-status', 'eof-closes']


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def dump(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()


def admission(expected_head):
    assert re.fullmatch('[0-9a-f]{40}', expected_head), 'Exact expected Git head required'
    assert git('rev-parse', 'HEAD') == expected_head, 'Wrong committed head'
    assert not git('diff', '--name-only', 'HEAD'), 'Tracked working source mutation'
    raw = (TOOL / 'source-plan.json').read_bytes()
    plan = json.loads(raw)
    assert plan['OtpSourcePin'] == PIN
    for row in plan['Inputs']:
        path = ROOT / row['Path']
        assert path.is_file(), row['Path']
        data = path.read_bytes()
        assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
        assert git('ls-files', '--error-unmatch', '--', row['Path']) == row['Path']
    return {'Head': expected_head, 'SourcePlanSha256': sha(raw), 'InputCount': len(plan['Inputs']),
            'InputsStable': True, 'SourceTree': git('rev-parse', 'HEAD:src')}


def run(out, rows, label, command, seconds=300, cwd=ROOT):
    start = time.monotonic()
    result = {'Name': label, 'Command': [str(x) for x in command], 'ExitCode': None,
              'TimedOut': False, 'ForcedCleanup': False, 'ProcessGroupAliveAfterNaturalExit': False}
    try:
        with (out / (label + '.stdout.log')).open('wb') as stdout, (out / (label + '.stderr.log')).open('wb') as stderr:
            process = subprocess.Popen(result['Command'], cwd=cwd, stdout=stdout, stderr=stderr, start_new_session=True)
            try:
                process.wait(timeout=seconds)
            except subprocess.TimeoutExpired:
                result['TimedOut'] = result['ForcedCleanup'] = True
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait(timeout=5)
            result['ExitCode'] = process.returncode
            try:
                os.killpg(process.pid, 0)
                result['ProcessGroupAliveAfterNaturalExit'] = True
                result['ForcedCleanup'] = True
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
    except Exception as error:
        result['InfrastructureError'] = repr(error)
    result['ElapsedSeconds'] = round(time.monotonic() - start, 3)
    rows.append(result)
    dump(out / (label + '.exit.json'), result)
    return result


def clean(row):
    return row['ExitCode'] == 0 and not any(row[k] for k in ['TimedOut', 'ForcedCleanup', 'ProcessGroupAliveAfterNaturalExit']) and 'InfrastructureError' not in row


def inventory(prefix):
    rows = []
    for path in sorted(prefix.rglob('*')):
        relative = path.relative_to(prefix).as_posix()
        if path.is_symlink():
            assert path.resolve().is_relative_to(prefix.resolve()), 'Provider symlink escapes admitted root'
            rows.append({'Path': relative, 'Link': os.readlink(path)})
        elif path.is_file():
            data = path.read_bytes()
            rows.append({'Path': relative, 'Bytes': len(data), 'Sha256': sha(data)})
    return rows


def admit_provider(prefix, receipt):
    assert receipt['SourceCommit'] == PIN and receipt['SourceTag'] == 'OTP-29.1.1'
    assert receipt['SourceBuildCompleted'] is True and receipt['InstalledOtpVersion'] == VERSION
    assert receipt['InstalledPrefix'] == str(prefix), 'Receipt path differs; executable root relocation needs separate admission'
    assert receipt['InstalledFiles'] == inventory(prefix), 'Provider extracted bytes or membership changed'
    assert (prefix / 'lib/erlang/releases/29/OTP_VERSION').read_text().strip() == VERSION
    assert (prefix / 'bin/erl').is_file()


def prepare_provider(args, out, rows, provider):
    prefix = pathlib.Path(args.provider_prefix).resolve() if args.provider_prefix else out / 'provider/install'
    if args.provider_receipt:
        raw = pathlib.Path(args.provider_receipt).read_bytes()
        assert args.provider_receipt_sha256 and sha(raw) == args.provider_receipt_sha256.upper(), 'Unapproved provider receipt'
        receipt = json.loads(raw)
        admit_provider(prefix, receipt)
        receipt_path = out / 'provider-reused-receipt.json'
        dump(receipt_path, receipt)
    else:
        assert not args.provider_prefix, '--provider-prefix requires an approved existing receipt'
        source = out / 'provider/source'
        source.parent.mkdir(parents=True, exist_ok=True)
        assert not source.exists() and not prefix.exists(), 'Fresh provider paths required; retain prior attempts'
        steps = [
            ('provider-clone', ['git', 'clone', '--depth', '1', '--branch', 'OTP-29.1.1', 'https://github.com/erlang/otp.git', source], 900, ROOT),
        ]
        for label, command, seconds, cwd in steps:
            assert clean(run(out, rows, label, command, seconds, cwd)), label + ' failed'
        actual = subprocess.check_output(['git', '-C', str(source), 'rev-parse', 'HEAD'], text=True).strip()
        assert actual == PIN, 'Original source pin differs'
        assert not subprocess.check_output(['git', '-C', str(source), 'diff', '--name-only', 'HEAD'], text=True).strip()
        configure = ['./configure', '--prefix=' + str(prefix), '--without-javac', '--without-wx', '--without-odbc', '--without-debugger', '--without-observer', '--without-et']
        for label, command, seconds in [('provider-configure', configure, 600), ('provider-make', ['make', '-j2'], 2400), ('provider-install', ['make', 'install'], 600)]:
            assert clean(run(out, rows, label, command, seconds, source)), label + ' failed; dependent provider preparation skipped'
        assert (prefix / 'lib/erlang/releases/29/OTP_VERSION').read_text().strip() == VERSION
        receipt = {'SourceCommit': PIN, 'SourceTag': 'OTP-29.1.1', 'SourceBuildCompleted': True,
                   'InstalledOtpVersion': VERSION, 'InstalledPrefix': str(prefix),
                   'HarnessHead': args.expected_head, 'RunId': os.environ.get('GITHUB_RUN_ID'),
                   'ConfigureOptions': configure[1:], 'InstalledFiles': inventory(prefix),
                   'PreparationLogs': [{'Path': name, 'Sha256': sha((out / name).read_bytes())}
                                       for name in ['provider-clone.stdout.log', 'provider-clone.stderr.log', 'provider-configure.stdout.log', 'provider-configure.stderr.log', 'provider-make.stdout.log', 'provider-make.stderr.log', 'provider-install.stdout.log', 'provider-install.stderr.log']]}
        admit_provider(prefix, receipt)
        receipt_path = out / 'provider-built-receipt.json'
        dump(receipt_path, receipt)
    # Retain the exact admitted receipt BEFORE any executable consumer.
    # This state remains available if version checking or later execution fails.
    receipt_raw = receipt_path.read_bytes()
    assert json.loads(receipt_raw) == receipt
    provider.update({'Prefix': str(prefix), 'ReceiptPath': str(receipt_path),
                     'ReceiptSha256': sha(receipt_raw), 'SourceCommit': PIN,
                     'InstalledInventorySha256': sha(json.dumps(receipt['InstalledFiles'], sort_keys=True).encode()),
                     'InstalledEntryCount': len(receipt['InstalledFiles'])})
    check = run(out, rows, 'provider-executable-version', [prefix / 'bin/erl', '-noshell', '-noinput', '-eval', 'io:put_chars(erlang:system_info(otp_release)),halt().'], 60)
    assert clean(check) and (out / 'provider-executable-version.stdout.log').read_text().strip() == '29'
    return prefix / 'bin/erl'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--provider-prefix')
    parser.add_argument('--provider-receipt')
    parser.add_argument('--provider-receipt-sha256')
    args = parser.parse_args()
    assert os.name == 'posix' and os.uname().sysname == 'Linux', 'Linux execution required'
    out = pathlib.Path(args.output).resolve()
    out.mkdir(parents=True, exist_ok=False)
    rows, errors, observations = [], [], []
    before = after = None
    provider_before = {}
    provider_after = None
    try:
        before = admission(args.expected_head)
        dump(out / 'admission-before.json', before)
        oracle = None
        try:
            oracle = prepare_provider(args, out, rows, provider_before)
        except Exception as error:
            errors.append('original provider rejected: ' + repr(error))
        for profile in ['Debug', 'Release']:
            binary = out / 'bin' / profile
            build = run(out, rows, profile + '-build', ['dotnet', 'build', TOOL / 'Erlang.DescriptorPollFdValidation.csproj', '-c', profile, '--output', binary, '-m:1', '--disable-build-servers', '--nologo'], 900)
            if not clean(build):
                errors.append(profile + ' preparation failed; dependent executions skipped')
                continue
            for mode in ['compare', 'cancel']:
                if mode == 'compare' and oracle is None:
                    errors.append(profile + ' original comparisons skipped after rejected provider preparation')
                    continue
                label = profile + '-' + mode
                report = out / (label + '.json')
                runrow = run(out, rows, label, ['dotnet', 'exec', '--fx-version', RUNTIME, '--roll-forward', 'Disable', binary / 'Erlang.DescriptorPollFdValidation.dll', mode, oracle or '-', report], 300)
                try:
                    result = json.loads(report.read_text())
                    assert clean(runrow) and result['Passed'] is True and result['Mode'] == mode
                    assert result['Runtime'] == RUNTIME and result['Architecture'] == 'X64'
                    if mode == 'compare':
                        detail = json.loads(pathlib.Path(result['DetailsPath']).read_text())
                        assert result['OriginalComparisonCount'] == 8 and detail['Passed'] == 8 and detail['Failed'] == 0
                        assert [c['Name'] for c in detail['Results']] == EXPECTED
                        assert all(c['Passed'] and c['ExitCode'] == 0 and c['ActualOutputHex'] == c['OutputHex'] for c in detail['Results'])
                    else:
                        assert result['ExistingCancellationCase'] == 'runtime/port-fd-io/cancel-idle-borrowed-reader'
                    observations.append({'Profile': profile, 'Mode': mode, 'Passed': True})
                except Exception as error:
                    errors.append(label + ' actual outcome rejected: ' + repr(error))
    except Exception as error:
        errors.append('collector: ' + repr(error))
    finally:
        # Independent of source admission and outcome handling. Recheck even if
        # a later native consumer/build/report/version check has failed.
        if provider_before:
            try:
                receipt_path = pathlib.Path(provider_before['ReceiptPath'])
                receipt_raw = receipt_path.read_bytes()
                assert sha(receipt_raw) == provider_before['ReceiptSha256'], 'Admitted provider receipt changed'
                receipt = json.loads(receipt_raw)
                admit_provider(pathlib.Path(provider_before['Prefix']), receipt)
                provider_after = dict(provider_before)
                assert provider_after['SourceCommit'] == receipt['SourceCommit'] == PIN
            except Exception as error:
                provider_after = {'Failed': repr(error)}
                errors.append('provider final stability: ' + repr(error))
        try:
            after = admission(args.expected_head)
            assert before == after
        except Exception as error:
            errors.append('source stability: ' + repr(error))
        summary = {'Passed': not errors and len(observations) == 4 and bool(provider_before) and provider_before == provider_after, 'ExpectedHead': args.expected_head,
                   'Rows': rows, 'Errors': errors, 'Observations': observations, 'SourceBefore': before, 'SourceAfter': after,
                   'ExpectedOriginalComparisonsPerProfile': 8, 'ExpectedExistingIdleRegressionsPerProfile': 1,
                   'ProviderBefore': provider_before or None, 'ProviderAfter': provider_after,
                   'ProviderStable': bool(provider_before) and provider_before == provider_after,
                   'Scope': 'Existing original OTP descriptor fixtures plus existing SDK idle borrowed-reader regression; no full statistics or arbitrary external FD reuse closure.'}
        dump(out / 'summary.json', summary)
        print(json.dumps({'Passed': summary['Passed'], 'Errors': errors, 'Observations': observations}))
    return 0 if summary['Passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
