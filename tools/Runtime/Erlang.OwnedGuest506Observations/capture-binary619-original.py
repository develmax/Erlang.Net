"""Fresh original execution of existing31-API LibraryBinary390 fixture; no new cases or projection.

Proposal only. Reuses provider admission and owned-child execution from collect.py.
No managed VM/build/CI orchestration, native introspection or debugger is added.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.OwnedGuest506Observations'


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def require(value, message):
    if not value:
        raise ValueError(message)


def save(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)


def erl_string(value):
    return '"' + ''.join('\\' + ch if ch in '\\"' else ch if 32 <= ord(ch) < 127
                         else '\\x{' + format(ord(ch), 'x') + '}' for ch in value) + '"'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--official-admission', type=Path, required=True)
    parser.add_argument('--admission-sha256', required=True)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    repo, output = args.repository.resolve(), args.output.resolve()
    require(os.name == 'nt' and output.is_relative_to(repo / 'artifacts') and not output.exists(),
            'Fresh confined Windows original output required')
    require(digest(args.plan) == args.plan_sha256.upper(), 'Reviewed reuse plan differs')
    plan = json.loads(args.plan.read_bytes())
    require(plan['Pin'] == PIN and plan['NoNewCases'] and not plan['FrameProjection'], 'Reuse policy differs')
    for row in plan['Inputs']:
        path = (repo / row['Path']).resolve()
        require(path.is_relative_to(repo) and digest(path) == row['Sha256'], 'Fixture/helper input differs: ' + row['Path'])
    spec = importlib.util.spec_from_file_location('binary619_collect', repo / TOOL / 'collect.py')
    collect = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(collect)
    args.build_receipt, args.otp_source = None, None
    installed, executable, before, after = collect.provider(args, repo)
    libraries = []
    for row in plan['OriginalLibraries']:
        found = list((installed / 'lib').glob('*/ebin/' + row['Module'] + '.beam'))
        require(len(found) == 1 and digest(found[0]) == row['Sha256'], 'Genuine stock binary dependency differs')
        libraries.append((row['Module'], found[0].resolve()))
    pairs = '[' + ','.join('{' + module + ',' + erl_string(str(path).replace('\\', '/')) + '}'
                           for module, path in libraries) + ']'
    owners = ('(fun()->[begin {ok,{M,ExpectedMd5}}=beam_lib:md5(P),ExpectedMd5=M:module_info(md5),'
              'Which=code:which(M),case Which of preloaded->true=lists:member(M,erlang:pre_loaded());'
              '_->ExpectedPath=filename:absname(P),ExpectedPath=filename:absname(Which) end,'
              '{M,Which,ExpectedMd5,M:module_info(compile),M:module_info(attributes),M:module_info(exports)} '
              'end||{M,P}<-' + pairs + '] end)()')
    output.mkdir(parents=True)
    save(output / 'provider-before.json', before)
    errors, runs = [], []
    environment = dict(os.environ, CONTRACT506_ORIGINAL_ROOT=str(installed), PYTHONDONTWRITEBYTECODE='1')
    for key in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS',
                'ERLC_EMULATOR', 'ERLC_USE_SERVER', 'ERLC_SERVER_ID', 'ERLC_CONFIGURATION', 'ERL_ROOTDIR']:
        environment.pop(key, None)
    try:
        for row in plan['Modes']:
            directory = output / row['Name']
            directory.mkdir()
            result = dict(Name=row['Name'], Passed=False, Error=None)
            try:
                fixture = repo / row['FixtureDirectory']
                for name in row['Copies']:
                    shutil.copyfile(fixture / name, directory / name)
                # Keep the exact cached compiled observer used by the existing
                # managed suite, including original child-root/frame metadata.
                # This is fresh original EXECUTION, not fresh compiler evidence.
                expression = ('W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
                              'Expected=filename:absname(os:getenv("CONTRACT506_ORIGINAL_ROOT")),'
                              'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
                              'W("profile.etf",{erlang:system_info(wordsize),erlang:system_info(schedulers),'
                              'erlang:system_info(schedulers_online),erlang:system_info(debug_compiled)}),'
                              + 'Observer=' + row['Observer'] + ',Observer:module_info(),'
                              'ObserverPath=filename:absname("' + row['Observer'] + '.beam"),'
                              'ObserverPath=filename:absname(code:which(Observer)),'
                              '{ok,{Observer,ObserverMd5}}=beam_lib:md5(ObserverPath),ObserverMd5=Observer:module_info(md5),'
                              'W("observer-owner.etf",{Observer,ObserverPath,ObserverMd5,Observer:module_info(compile)}),'
                              'W("stock-owners-before.etf",' + owners + '),'
                              + row['Expression'] + 'W("stock-owners-after.etf",' + owners + '),halt(0).')
                child_environment = dict(environment, ERL_CRASH_DUMP=str(directory / 'erl_crash.dump'))
                result['Process'] = collect.execute(repo, [str(executable), '+S', '1:1', '+A', '1', '-noshell',
                    '-noinput', '-boot', 'no_dot_erlang', '-pa', str(directory), '-eval', expression], directory, child_environment)
                actual, golden = directory / 'observed.etf', fixture / row['Golden']
                require(actual.is_file(), 'Original result absent')
                require((directory / 'stock-owners-before.etf').read_bytes()
                        == (directory / 'stock-owners-after.etf').read_bytes(), 'Original stock owner changed')
                result.update(ActualSha256=digest(actual), GoldenSha256=digest(golden),
                              WholeRawEqual=actual.read_bytes() == golden.read_bytes(),
                              FullErrorsAndStacksRetained=True, RawEtfRewritten=False)
                require(result['WholeRawEqual'], 'Whole unprojected existing observation differs')
                result['Passed'] = True
            except Exception as error:
                result['Error'] = repr(error)
                errors.append(row['Name'] + ': ' + repr(error))
            runs.append(result)
    finally:
        try:
            current = after()
            save(output / 'provider-after.json', dict(ProviderEqual=current == before['Inventory'], Inventory=current))
            require(current == before['Inventory'], 'Provider changed')
            require(digest(args.plan) == args.plan_sha256.upper(), 'Reuse plan changed')
            for row in plan['Inputs']:
                require(digest(repo / row['Path']) == row['Sha256'], 'Fixture/helper input changed: ' + row['Path'])
        except Exception as error:
            errors.append('postflight: ' + repr(error))
    complete = dict(Passed=not errors and len(runs) == len(plan['Modes']), SourcePin=PIN,
                    Modes=runs, Errors=errors, FreshOriginalExecution=True, ObserverRecompiled=False,
                    NewCases=0, RawEtfRewritten=False, FrameProjection=False, WholeContractsClosed=0,
                    PhysicalHeaderExecutionClaim=False, ManagedReplayPerformed=False)
    save(output / 'completion.json', complete)
    if not complete['Passed']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
