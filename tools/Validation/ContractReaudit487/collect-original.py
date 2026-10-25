"""Acquire unchanged whole BIF outcomes using the existing admitted OTP provider."""
import argparse
import hashlib
import importlib.util
import json
import os
import re
import shutil
from pathlib import Path

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--admission', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--check-golden', action='store_true')
    parser.add_argument('--fixture', type=Path, default=Path('tests/Erlang.Tests/Resources/ContractReaudit487'))
    parser.add_argument('--subject')
    parser.add_argument('--golden-prefix', default='')
    args = parser.parse_args()
    root, out = args.repository.resolve(), args.output.resolve()
    assert not out.exists() and out.is_relative_to(root / 'artifacts')
    helpers = root / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'
    inventory = load('inventory487', helpers / 'bounded_inventory.py')
    owned = load('owned487', helpers / 'owned.py')
    admission = json.loads(args.admission.read_bytes())
    assert admission['SourceCommit'] == PIN and admission['ReleaseTag'] == 'OTP-29.1.1'
    installed = Path(admission['InstalledOtpRoot']).resolve()
    manifest = Path(admission['ExecutionManifestPath'])
    assert inventory.digest(manifest) == admission['ExecutionManifestSha256']
    expected = {row['Path'].replace('\\', '/'): row['Sha256'] for row in json.loads(manifest.read_bytes())}
    assert len(expected) == 6427 and inventory.inventory(installed) == expected
    fixture = (root / args.fixture).resolve()
    assert fixture.is_relative_to(root / 'tests') or fixture.is_relative_to(root / 'artifacts')
    if args.subject:
        assert re.fullmatch('[a-z][a-z0-9_]*', args.subject)
    assert re.fullmatch('[a-z0-9_.]*', args.golden_prefix)
    sources = [fixture / (args.subject + '.erl')] if args.subject else list(fixture.glob('*.erl'))
    assert len(sources) == 1
    source = sources[0]
    subject = source.stem
    assert re.fullmatch('[a-z][a-z0-9_]*', subject)
    assert source.read_text(encoding='utf-8').startswith('-module(' + subject + ').')
    out.mkdir(parents=True)
    shutil.copyfile(source, out / source.name)
    workers = {
        'ordsets_core_support.beam': 'E96720C753F9438E66367C71A90C5D0C1E3AA29091B178799986E36A8438022F',
        'ordsets_core_worker.beam': 'E345AE81943C41F82F3CBCA24B963D2D25CB24C0C15E2A0B21B72A65757D6B3F'
    }
    for name, expected_hash in workers.items():
        worker = root / 'tests/Erlang.Tests/Resources/BeamOrdsets381' / name
        assert inventory.digest(worker) == expected_hash
        shutil.copyfile(worker, out / name)
    env = dict(os.environ)
    for name in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR']:
        env.pop(name, None)
    command = [str(installed / 'bin/erl.exe'), '+S', '1:1', '+A', '1', '-noshell', '-noinput', '-boot', 'no_dot_erlang', '-pa', str(out)]
    expression = ('{ok,' + subject + ',_}=compile:file("' + source.name + '",[return_errors,return_warnings]),'
                  '[ok=prim_file:write_file(File,term_to_binary(ordsets_core_worker:run(' + subject + ',Entry),[{minor_version,2}])) '
                  '|| {File,Entry} <- [{"values.etf",run},{"stacks.etf",run_stacks}]],'
                  '{beam_file,_,_,_,_,Functions}=beam_disasm:file("' + subject + '.beam"),'
                  'Instructions=lists:append([Is || {function,_,_,_,Is} <- Functions]),'
                  'ApplySites=[I || I <- Instructions,is_tuple(I),lists:member(element(1,I),[apply,apply_last])],'
                  'true=length(ApplySites)>0,'
                  'ok=prim_file:write_file("dispatch-proof.etf",term_to_binary({' + subject + ',length(ApplySites),ApplySites})),halt(0).')
    errors = []
    receipt = None
    try:
        receipt = owned.owned(command + ['-eval', expression], out, 'original', 120, env, str(out))
        if args.check_golden:
            for name in ['values.etf', 'stacks.etf']:
                if (out / name).read_bytes() != (fixture / (args.golden_prefix + name)).read_bytes():
                    errors.append('Whole original golden differs: ' + name)
    except Exception as error:
        errors.append(repr(error))
    finally:
        provider_equal = inventory.inventory(installed) == expected
        if not provider_equal:
            errors.append('Provider changed')
        result = {'Passed': not errors, 'Pin': PIN, 'ProviderFiles': len(expected),
                  'ProviderBeforeEqual': True, 'ProviderAfterEqual': provider_equal,
                  'SourceSha256': inventory.digest(source), 'OriginalReceipt': receipt,
                  'GoldenChecked': args.check_golden, 'NormalizedFields': 0, 'Errors': errors,
                  'Files': {p.name: inventory.digest(p) for p in out.iterdir() if p.is_file()}}
        (out / 'completion.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
