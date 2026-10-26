"""Seal only existing source bytes; never compiles or starts an Erlang VM."""
import argparse
import hashlib
import json
from pathlib import Path

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.ResourceControls504Observations'


def row(root, path):
    data = (root / path).read_bytes().replace(b'\r\n', b'\n')
    return dict(Path=path, HashMode='TextLF', Bytes=len(data), Sha256=hashlib.sha256(data).hexdigest().upper())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', required=True)
    args = parser.parse_args()
    root = Path(args.repository).resolve()
    tool = Path(__file__).resolve().parent
    if tool != root / TOOL:
        raise RuntimeError('source root mismatch')
    dependency = 'tools/Distribution/Erlang.GuestConnectedSendObservations/'
    source_tool = 'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/'
    native_stages = ['held', 'queued', 'after_action', 'retired']
    cases = [dict(Name='debug_flags', Native=False, Stages=[]),
             dict(Name='normal_queue', Native=False, Stages=['normal_runnable', 'normal_retired'])]
    cases += [dict(Name=name, Native=True, Stages=native_stages)
              for name in ['held_queues', 'cancel_queued', 'cancel_running']]
    paths = ['README.md', 'collect.py', 'seal-plan.py',
             'fixtures/queue_statistics504.c', 'fixtures/queue_statistics504.erl']
    plan = dict(Status='UNEXECUTED_ORIGINAL_SOURCE_PLAN', OriginalCommit=PIN,
        Flags=['+S', '1:1', '+SDcpu', '1:1', '+SDio', '1', '+A', '1'],
        QueueKeys=['run_queue', 'run_queue_lengths', 'run_queue_lengths_all',
            'total_run_queue_lengths', 'total_run_queue_lengths_all', 'active_tasks',
            'active_tasks_all', 'total_active_tasks', 'total_active_tasks_all', 'run_queues'],
        Cases=cases, DebugFlagArguments=15, OwnedProcess=dependency + 'owned_process.py',
        Decoder=dependency + 'passive_etf.py', SourceIdentity=source_tool + 'source_inputs.py',
        BuildManifest=source_tool + 'manifest.json',
        Files=[row(root, TOOL + '/' + name) for name in paths],
        Dependencies=[row(root, dependency + name) for name in ['owned_process.py', 'passive_etf.py']]
            + [row(root, source_tool + name) for name in ['source_inputs.py', 'manifest.json']],
        SdkCompared=False, WholeApiPromoted=False)
    raw = (json.dumps(plan, indent=2) + '\n').encode('utf-8')
    (tool / 'source-plan.json').write_bytes(raw)
    print(hashlib.sha256(raw).hexdigest().upper())


if __name__ == '__main__':
    main()
