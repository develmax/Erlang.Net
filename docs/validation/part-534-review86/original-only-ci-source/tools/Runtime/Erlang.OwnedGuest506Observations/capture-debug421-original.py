import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import threading
import time

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + '\n')


parser = argparse.ArgumentParser()
for option in ['repository', 'expected-head', 'manifest-sha256', 'otp-source', 'output']:
    parser.add_argument('--' + option, required=True)
args = parser.parse_args()
repo = Path(args.repository).resolve()
source = Path(args.otp_source).resolve()
out = Path(args.output).resolve()
manifest = repo / 'tools/Runtime/Erlang.OwnedGuest506Observations/debug421-original-inputs.json'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip() == args.expected_head
assert sha(manifest) == args.manifest_sha256.upper()
plan = json.loads(manifest.read_bytes())
assert plan['Pin'] == PIN
for row in plan['Files']:
    assert sha(repo / row['Path']) == row['Sha256'], row['Path']
assert not out.exists()
out.mkdir(parents=True)
sys.path.insert(0, str(repo / 'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations'))
from source_inputs import source_identity, sanitized_environment, require_normal_receipt

owned_path = repo / 'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py'
spec = importlib.util.spec_from_file_location('debug421_owned', owned_path)
owned = importlib.util.module_from_spec(spec)
spec.loader.exec_module(owned)
before = source_identity(source)
save(out / 'source-before.json', before)
environment = sanitized_environment(os.environ)
environment['ERLC_USE_SERVER'] = 'false'
environment['ERL_TOP'] = str(source)


def run(name, command, timeout):
    process = owned.OwnedProcess(command, source, environment, out / name)
    try:
        process.wait(time.monotonic() + timeout, threading.Event().is_set)
    finally:
        process.close()
    receipt = json.loads((out / (name + '.process.json')).read_bytes())
    require_normal_receipt(receipt)
    return receipt


run('make-debug', ['make', '-j2', 'TYPE=debug'], 2100)
fixture = repo / 'tests/Erlang.Tests/Fixtures/CountersOriginal421'
for name in ['source.erl', 'beam_counters421.beam', 'authority.json', 'manifest.json', 'original-completion.json']:
    shutil.copyfile(fixture / name, out / name)
expression = '''
true=erlang:system_info(debug_compiled),
debug=erlang:system_info(build_type),
8=erlang:system_info(wordsize),
1=erlang:system_info(schedulers),
1=erlang:system_info(schedulers_online),
{ok,Exe}=file:read_link("/proc/self/exe"),
Profile=#{debug_compiled=>erlang:system_info(debug_compiled),
 build_type=>atom_to_binary(erlang:system_info(build_type)),
 wordsize=>erlang:system_info(wordsize),schedulers=>erlang:system_info(schedulers),
 schedulers_online=>erlang:system_info(schedulers_online),
 dirty_cpu_schedulers=>erlang:system_info(dirty_cpu_schedulers),
 dirty_io_schedulers=>erlang:system_info(dirty_io_schedulers),
 architecture=>list_to_binary(erlang:system_info(system_architecture)),
 otp_release=>list_to_binary(erlang:system_info(otp_release)),
 executable=>list_to_binary(Exe),
 worker_md5=>binary:encode_hex(beam_counters421:module_info(md5)),
 internal_md5=>binary:encode_hex(erts_internal:module_info(md5))},
Values=beam_counters421:run_child(),Stacks=beam_counters421:run_stacks_child(),
32=length(Values),5=length(Stacks),
true=lists:all(fun({_,_,Rows})->length(Rows)=:=32 end,Stacks),
ok=file:write_file("OUT/profile.json",json:encode(Profile)),
ok=file:write_file("OUT/profile.etf",term_to_binary(Profile,[deterministic])),
ok=file:write_file("OUT/values.etf",term_to_binary(Values,[deterministic])),
ok=file:write_file("OUT/stacks.etf",term_to_binary(Stacks,[deterministic])),
io:format("DEBUG421 192 complete rows captured~n"),halt(0).
'''.replace('OUT', out.as_posix())
receipt = run('debug-observer', [str(source / 'bin/erl'), '-emu_type', 'debug', '+S', '1:1',
    '+SDcpu', '1', '+SDio', '1', '-noshell', '-noinput', '-pa', str(out), '-eval', expression], 180)
profile = json.loads((out / 'profile.json').read_bytes())
assert profile['debug_compiled'] is True and profile['build_type'] == 'debug'
assert profile['wordsize'] == 8 and profile['schedulers'] == profile['schedulers_online'] == 1
executable = Path(profile['executable']).resolve()
assert executable.is_relative_to(source) and 'debug' in executable.name
after = source_identity(source)
assert before == after
save(out / 'source-after.json', after)
save(out / 'completion.json', dict(Passed=True, OriginalOnly=True, Pin=PIN,
    Head=args.expected_head, WholeRows=192, Profiles=6, DebugProfile=profile,
    ActualExecutableSha256=sha(executable), ActualExecutable=str(executable),
    SourceBeforeAfterEqual=True, RawEtfRewritten=False, Projection=False,
    ManagedExecution=False, WholeContractsClosed=0, ProcessReceipt=receipt,
    Files=[dict(Path=p.name, Bytes=p.stat().st_size, Sha256=sha(p))
           for p in sorted(out.iterdir()) if p.is_file()]))
print('PASS genuine pinned DEBUG 421 original: 192 complete rows; physical process settled')
