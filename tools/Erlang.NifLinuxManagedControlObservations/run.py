"""Managed-only discriminator. Never loads native images or a bridge."""
from pathlib import Path
import argparse, hashlib, json, os, shutil, subprocess, sys

H = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J = lambda p: json.loads(Path(p).read_bytes())
R = Path(__file__).resolve().parents[2]
PRODUCER_HEAD = 'f7399755673e510c15ddcdc573a376141c1c5965'

def write(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')

def main():
    parser = argparse.ArgumentParser()
    for key in ['admission', 'approved-admission-sha256', 'expected-head', 'focus', 'output']:
        parser.add_argument('--' + key, required=True)
    args = parser.parse_args()
    admission = Path(args.admission).resolve()
    assert H(admission) == args.approved_admission_sha256.upper()
    ad = J(admission)
    assert ad['SubjectHead'] == PRODUCER_HEAD
    actual_head = subprocess.check_output(['git', '--no-optional-locks', '-C', str(R), 'rev-parse', 'HEAD'], text=True, timeout=20).strip()
    assert actual_head == args.expected_head
    assert not os.environ.get('GITHUB_SHA') or os.environ['GITHUB_SHA'] == actual_head
    image = next(row for row in ad['Images'] if row['Module'] == 'map_size_callback_contract')
    for pathkey, hashkey in [('ManifestPath', 'ManifestSha256'), ('ErlangSourcePath', 'ErlangSourceSha256')]:
        assert H(image[pathkey]) == image[hashkey]
    focus = Path(args.focus).resolve()
    assert focus.name == 'Erlang.NifLinuxManagedControlFocus.dll' and focus.is_file()
    dotnet = shutil.which('dotnet')
    assert dotnet
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=False)
    dlls = [dict(Path=str(path), Sha256=H(path)) for path in sorted(focus.parent.glob('*.dll'))]
    assert any(Path(row['Path']).name == 'Erlang.Runtime.dll' for row in dlls)
    command = [dotnet, str(focus), str(admission), H(admission), str(output / 'managed')]
    write(output / 'inputs.json', dict(Command=command, ExecutingHead=actual_head, ProducerHead=PRODUCER_HEAD, AdmissionSha256=H(admission), FocusSha256=H(focus), AdjacentDlls=dlls, ManifestSha256=image['ManifestSha256'], ErlangSourceSha256=image['ErlangSourceSha256']))
    for tool in ['Erlang.NifEntryOriginalObservations', 'Erlang.GuestConnectedSendObservations', 'Erlang.SchedulerAffinityOriginalObservations']:
        sys.path.insert(0, str(R / 'tools' / tool))
    import common, owned_process, source_inputs
    env = source_inputs.sanitized_environment(os.environ)
    for key in ['LD_PRELOAD', 'LD_LIBRARY_PATH', 'DOTNET_STARTUP_HOOKS', 'DOTNET_ADDITIONAL_DEPS', 'DOTNET_SHARED_STORE', 'GH_TOKEN', 'GITHUB_TOKEN']:
        env.pop(key, None)
    env.update(DOTNET_PROCESSOR_COUNT='2', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
    stage = common.stage(owned_process, source_inputs, command, R, env, output / 'managed-process', 180)
    receipt = J(stage['ReceiptPath'])
    source_inputs.require_normal_receipt(receipt)
    result = J(output / 'managed' / 'completion.json')
    assert result['Status'] == 'ACQUIRED_MANAGED_ONLY_TWO_RUNTIME_MAP_CONTROL'
    assert result['AdmissionSha256'] == H(admission) and result['RuntimeDomains'] == 2
    assert result['NativeImagesLoaded'] == result['ForeignWorkersCreated'] == result['NativeEnvironmentsAllocated'] == 0
    assert result['RuntimeDisposalsCompleted']
    for domain in result['Domains']:
        assert [row['Requested'] for row in domain['Rows']] == [0,1,31,32,33,512,8193,8193]
        assert all(row['Actual'] == row['Requested'] and row['Type'] == 'MapTerm' for row in domain['Rows'])
    for row in dlls:
        assert H(row['Path']) == row['Sha256']
    for pathkey, hashkey in [('ManifestPath', 'ManifestSha256'), ('ErlangSourcePath', 'ErlangSourceSha256')]:
        assert H(image[pathkey]) == image[hashkey]
    write(output / 'completion.json', dict(Status='ACQUIRED_MANAGED_ONLY_MAP_CONTROL', ExecutingHead=actual_head, ProducerHead=PRODUCER_HEAD, OwnedStage=stage, ObservationSha256=H(output/'managed'/'completion.json'), OriginalComparisonExecuted=False))

if __name__ == '__main__':
    main()
