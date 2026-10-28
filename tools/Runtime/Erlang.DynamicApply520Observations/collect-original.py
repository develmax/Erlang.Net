"""Source-bound fixture acquisition using existing provider/process helpers."""
import argparse
import hashlib
import importlib.util
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

def source_digest(path, row):
    data = path.read_bytes()
    mode = row.get('HashMode', 'Raw')
    require(mode in ('Raw', 'TextLF'), 'Unknown source hash mode')
    if mode == 'TextLF':
        data = data.replace(b'\r\n', b'\n')
    return hashlib.sha256(data).hexdigest().upper()

def load(name, path):
    specification = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module

def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

def require(condition, message):
    if not condition:
        raise ValueError(message)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--execute-original', action='store_true')
    parser.add_argument('--expected-head', required=True)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--official-admission', type=Path)
    group.add_argument('--build-receipt', type=Path)
    parser.add_argument('--admission-sha256', required=True)
    parser.add_argument('--otp-source', type=Path)

    arguments = parser.parse_args()
    repository = arguments.repository.resolve(strict=True)
    tool_directory = Path(__file__).resolve().parent
    output = arguments.output.resolve()
    plan_path = tool_directory / 'source-plan.json'

    require(sha(plan_path) == arguments.plan_sha256.upper(), 'Reviewed source plan SHA differs')
    source_plan = json.loads(plan_path.read_bytes())
    require(source_plan['Pin'] == PIN, 'Pin differs')
    for row in source_plan['Inputs']:
        file_path = (repository / row['Path']).resolve(strict=True)
        require(file_path.is_relative_to(repository) and source_digest(file_path, row) == row['Sha256'], 'Source input differs: ' + row['Path'])
    require(subprocess.check_output(['git', '-C', str(repository), 'rev-parse', 'HEAD'], text=True).strip() == arguments.expected_head, 'Actual collector head differs')
    require(output.is_relative_to(repository / 'artifacts') and (not output.exists()), 'Fresh owned artifact output required')
    require(arguments.execute_original, 'No execution without explicit --execute-original')

    shared = load('dynamic520_shared', repository / 'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py')
    installed, erl, provider, after = shared.provider(arguments, repository)
    output.mkdir(parents=True)
    save(output / 'provider-before.json', provider)
    phases = {}
    errors = []
    images = []
    passed = False
    native_metadata = None

    for file_path in sorted((tool_directory / 'fixtures').glob('*.erl')):
        shutil.copyfile(file_path, output / file_path.name)

    odbc = repository / 'tests/Erlang.Tests/Resources/ApplyOriginal477/odbc.beam'
    require(sha(odbc) == '7830D857641C35AD0491C89212AB124003281C27103D280FD151BA9460EEA8BC', 'Existing genuine477 ODBC image differs')
    shutil.copyfile(odbc, output / 'odbc.beam')
    env = dict(os.environ, ERL_CRASH_DUMP=str(output / 'erl_crash.dump'), ERL_CRASH_DUMP_SECONDS='15')

    try:
        metadata_helper = load('dynamic520_native_metadata527', tool_directory / 'metadata-image527.py')
        native_metadata = metadata_helper.acquire(installed, output)

        compile_directory = output / 'compile'
        compile_directory.mkdir()
        for file_path in output.glob('*.erl'):
            shutil.copyfile(file_path, compile_directory / file_path.name)
        expression = (
            'Names=[dynamic_apply_caller520,dynamic_apply_target520,acquire520],'
            'lists:foreach(fun(M)->{ok,M}=compile:file(atom_to_list(M)++".erl",[debug_info,return_errors]),ok end,Names),'
            'I=[begin {ok,{M,[{imports,Rows}]}}=beam_lib:chunks(atom_to_list(M)++".beam",[imports]),{M,Rows} end||M<-[dynamic_apply_caller520,dynamic_apply_target520]],'
            'ok=file:write_file("static-imports.etf",term_to_binary(I)),halt().'
        )
        phases['Compile'] = shared.execute(repository, [str(erl), '+S', '2:2', '-noshell', '-noinput', '-eval', expression], compile_directory, env)

        decoder = load('dynamic520_etf', repository / 'tools/Native/Erlang.NifMapFamily504Observations/diagnostic_etf.py')
        tree = decoder.decode_exact((compile_directory / 'static-imports.etf').read_bytes())
        require(tree[0] == 'list', 'Import list')
        for entry in tree[1]:
            require(entry[0] == 'tuple' and len(entry[1]) == 2, 'Import module row')
            module, imports = entry[1]
            require(module[0] == 'atom' and imports[0] == 'list', 'Module imports shape')
            modules = set()
            for imp in imports[1]:
                require(imp[0] == 'tuple' and len(imp[1]) == 3, 'Import MFA')
                modules.add(imp[1][0][1])
            require(modules == {'erlang'}, 'Caller/callee must have only genuine erlang static imports, never dynamic target')
            beam_path = compile_directory / (module[1] + '.beam')
            shutil.copyfile(beam_path, output / beam_path.name)
            images.append(dict(Module=module[1], Name=beam_path.name, Sha256=sha(beam_path), StaticModules=sorted(modules)))
        shutil.copyfile(compile_directory / 'acquire520.beam', output / 'acquire520.beam')

        observer_expression = (
            '{ok,{erlang,NativeMd5}}=beam_lib:md5("metadata-effective-erlang.beam"),'
            'NativeMd5=erlang:module_info(md5),'
            'ok=file:write_file("native-metadata-executing.etf",'
            'term_to_binary({erlang,code:which(erlang),NativeMd5,erlang:module_info(compile)})),'
            'ok=acquire520:run(),halt().'
        )
        phases['Observer'] = shared.execute(
            repository,
            [str(erl), '+S', '2:2', '-noshell', '-noinput', '-pa', str(output), '-eval', observer_expression],
            output,
            env,
        )
        require(all(((output / n).is_file() for n in ['observed.etf', 'observed-deterministic.etf', 'worker-witness.etf', 'owners.etf', 'native-metadata-executing.etf'])), 'Full original outputs missing')
        passed = True
    except Exception as error:
        errors.append(repr(error))

    finally:
        try:
            current = after()
            require(current == provider['Inventory'], 'Provider closure changed')
            save(output / 'provider-after.json', dict(ProviderEqual=True, Inventory=current))
        except Exception as error:
            errors.append('postflight: ' + repr(error))
            passed = False
        inventory = []
        for file_path in sorted(output.rglob('*')):
            if not file_path.is_file() or file_path == output / 'completion.json':
                continue
            inventory.append(dict(
                Path=file_path.relative_to(output).as_posix(),
                Bytes=file_path.stat().st_size,
                Sha256=sha(file_path),
            ))
        completion = dict(
            Pin=PIN,
            OriginalOnly=True,
            AcquisitionPassed=passed and not errors,
            SourcePlanSha256=sha(plan_path),
            ProviderAdmissionSha256=arguments.admission_sha256.upper(),
            Files=inventory,
            Images=images,
            NativeMetadataImage=native_metadata,
            LoaderImage=dict(
                Module='odbc',
                Name='odbc.beam',
                Sha256=sha(output / 'odbc.beam'),
            ),
            Phases=phases,
            Errors=errors,
            WholeContractsClosed=0,
            SourceInputs=source_plan['Inputs'],
        )
        save(output / 'completion.json', completion)
    require(passed and (not errors), 'Original acquisition failed; retain all errors/cleanup: ' + repr(errors))
if __name__ == '__main__':
    main()
