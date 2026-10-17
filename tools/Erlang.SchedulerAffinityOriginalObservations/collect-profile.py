"""Unexecuted Linux-only original OTP scheduler profile; separate from ordinary corpus."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import threading
import time
from source_inputs import require_normal_receipt, sanitized_environment, source_identity, verify

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
PROFILE = bytes.fromhex('83680377117363686564756c65725f70726f66696c6561026102')
DONE = bytes.fromhex('837704646f6e65')
COMPILED = bytes.fromhex('83680277026f6b7704646f6e65')


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write(path, value):
    path.write_bytes((json.dumps(value, indent=2) + '\n').encode())


def main():
    assert sys.platform.startswith('linux'), 'owned process-group profile requires Linux'
    parser = argparse.ArgumentParser()
    for name in ['repository', 'otp-source', 'otp-install', 'output', 'manifest-sha256', 'expected-head', 'build-receipt']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    tool = Path(__file__).resolve().parent
    repo, tool, manifest = verify(args.repository, args.manifest_sha256, args.expected_head)
    manifest_path = tool / 'manifest.json'
    otp_source = Path(args.otp_source).resolve()
    assert subprocess.check_output(['git', '-C', str(otp_source), 'rev-parse', 'HEAD'], text=True, timeout=10).strip() == PIN
    install = Path(args.otp_install).resolve()
    versions = list((install / 'releases').glob('*/OTP_VERSION'))
    assert any(p.read_text().strip() == '29.1.1' for p in versions)
    erl = (install / 'bin/erl').resolve()
    beam = list(install.glob('erts-*/bin/beam.smp'))
    assert erl.is_file() and len(beam) == 1 and beam[0].is_file()
    build_path = Path(args.build_receipt).resolve()
    build = json.loads(build_path.read_bytes())
    assert build['Status'] == 'PASS_OWNED_PINNED_ORIGINAL_BUILD'
    assert build['Commit'] == PIN and build['SubjectHead'] == args.expected_head
    assert build['SourceManifestSha256'] == args.manifest_sha256.upper()
    assert Path(build['RuntimeRoot']).resolve() == install
    assert Path(build['Prefix']).resolve() / 'lib/erlang' == install
    assert Path(build['SourceRoot']).resolve() == otp_source
    assert digest(build_path.parent / 'source-before.json') == build['SourceBeforeSha256']
    assert digest(build_path.parent / 'source-after.json') == build['SourceAfterSha256']
    current_source = source_identity(otp_source)
    assert current_source == json.loads((build_path.parent / 'source-before.json').read_bytes())
    assert current_source == json.loads((build_path.parent / 'source-after.json').read_bytes())
    assert [stage['Name'] for stage in build['Stages']] == ['configure', 'make', 'install']
    for stage in build['Stages']:
        receipt_path = Path(stage['ReceiptPath'])
        assert digest(receipt_path) == stage['ReceiptSha256']
        assert digest(receipt_path.parent / (stage['Name'] + '.stdout')) == stage['StdoutSha256']
        assert digest(receipt_path.parent / (stage['Name'] + '.stderr')) == stage['StderrSha256']
        receipt = json.loads(receipt_path.read_bytes())
        require_normal_receipt(receipt)
    assert len(build['InstalledFiles']) >= 11
    for row in build['InstalledFiles']:
        assert digest(install / row['Path']) == row['Sha256'], row
    spec = importlib.util.spec_from_file_location('profile_owned', repo / manifest['Dependencies'][0]['Path'])
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    decoder_spec = importlib.util.spec_from_file_location('profile_decoder', repo / manifest['Dependencies'][1]['Path'])
    decoder = importlib.util.module_from_spec(decoder_spec)
    decoder_spec.loader.exec_module(decoder)
    output = Path(args.output).resolve()
    assert not output.exists(), 'fresh output required'
    output.mkdir(parents=True)
    write(output / 'inputs.json', {'ManifestSha256': digest(manifest_path), 'OriginalCommit': PIN,
          'ErlPath': str(erl), 'ErlSha256': digest(erl), 'BeamSha256': digest(beam[0]), 'BuildReceiptSha256': digest(build_path),
          'SubjectHead': args.expected_head, 'WorkflowRunId': os.environ.get('GITHUB_RUN_ID'),
          'WorkflowRunAttempt': os.environ.get('GITHUB_RUN_ATTEMPT'),
          'ExplicitFlags': ['+S', '2:2'], 'OrdinaryCorpusModified': False})
    outcomes = []
    for case in manifest['Cases']:
        directory = output / case['Module']
        directory.mkdir()
        source = directory / (case['Module'] + '.erl')
        source.write_bytes((tool / case['Source']).read_bytes())
        environment = sanitized_environment(os.environ)
        environment.update(SCHEDULER_PROFILE_DIR=str(directory), SCHEDULER_PROFILE_RUNTIME_ROOT=str(install),
                           ERL_CRASH_DUMP=str(directory / 'erl_crash.dump'))
        # All dynamic paths are read inside the VM from a private environment variable.
        expression = (
            'D=os:getenv("SCHEDULER_PROFILE_DIR"), '
            'W=fun(N,V)->ok=file:write_file(filename:join(D,N),term_to_binary(V,[{minor_version,2}])) end, '
            'ExpectedRoot=os:getenv("SCHEDULER_PROFILE_RUNTIME_ROOT"), ActualRoot=filename:absname(code:root_dir()), '
            'W("runtime-root.etf",{runtime_root,list_to_binary(ExpectedRoot),list_to_binary(ActualRoot)}), ExpectedRoot=ActualRoot, '
            'P={scheduler_profile,erlang:system_info(schedulers),erlang:system_info(schedulers_online)}, '
            'W("profile.etf",P), {scheduler_profile,2,2}=P, '
            f'{{ok,{case["Module"]}}}=compile:file(filename:join(D,"{case["Module"]}.erl"),[{{outdir,D}},return_errors]), '
            f'{{module,{case["Module"]}}}=code:load_abs(filename:join(D,"{case["Module"]}")), '
            f'{{ok,{{{case["Module"]},CodeMd5}}}}=beam_lib:md5(filename:join(D,"{case["Module"]}.beam")), '
            f'CodeMd5={case["Module"]}:module_info(md5), '
            f'W("image.etf",{{{case["Module"]},code:which({case["Module"]}),CodeMd5}}), '
            f'R=try {case["Module"]}:run() of V->W("returned.etf",V),W("compiled-protocol.etf",{{ok,V}}),{{returned,V}} '
            'catch C:E:S->W("raised.etf",{C,E,S}),{raised,C,E,S} end, '
            'W("outcome.etf",R), case R of {returned,done}->halt(0);_->halt(1) end.'
        )
        arguments = [str(erl), '+S', '2:2', '-noshell', '-noinput', '-boot', 'start_clean', '-eval', expression]
        write(directory / 'attempt.json', {'Arguments': arguments, 'SourceSha256': digest(source), 'TimeoutSeconds': 45})
        process = None
        failure = None
        try:
            process = module.OwnedProcess(arguments, directory, environment, directory / 'vm')
            process.wait(time.monotonic() + 45, threading.Event().is_set)
        except BaseException as error:
            failure = repr(error)
        finally:
            if process is not None:
                try:
                    process.close()
                except BaseException as error:
                    failure = (failure or '') + '; cleanup: ' + repr(error)
            write(directory / 'capture-status.json', {'Failure': failure, 'Started': process is not None})
        if failure:
            raise RuntimeError(f'{case["Module"]}: {failure}')
        receipt = json.loads((directory / 'vm.process.json').read_bytes())
        require_normal_receipt(receipt)
        actual_root = decoder.decode_exact((directory / 'runtime-root.etf').read_bytes())
        assert actual_root == ('tuple', (('atom', 'runtime_root'), ('binary', os.fsencode(str(install))),
                                         ('binary', os.fsencode(str(install)))))
        assert (directory / 'profile.etf').read_bytes() == PROFILE
        assert (directory / 'returned.etf').read_bytes() == DONE
        assert (directory / 'compiled-protocol.etf').read_bytes() == COMPILED
        expected_outcome = bytes.fromhex('836802770872657475726e6564') + DONE[1:]
        assert (directory / 'outcome.etf').read_bytes() == expected_outcome
        assert (directory / 'image.etf').stat().st_size > 0
        outcomes.append({'Module': case['Module'], 'Configured': 2, 'OnlineAtEntry': 2,
                         'SourceSha256': digest(source), 'OutcomeSha256': digest(directory / 'outcome.etf'),
                         'ProcessReceiptSha256': digest(directory / 'vm.process.json'), 'Passed': True})
        write(output / 'observations.json', outcomes)
    assert len(outcomes) == 5
    write(output / 'completion.json', {'Status': 'PASS_ORIGINAL_EXPLICIT_TWO_LANE_PROFILE', 'Cases': outcomes,
          'OrdinaryCorpusModified': False, 'ManagedInteropExecuted': False})


if __name__ == '__main__':
    main()
