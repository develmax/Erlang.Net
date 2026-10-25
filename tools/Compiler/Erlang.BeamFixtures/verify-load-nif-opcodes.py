from pathlib import Path, PurePosixPath, PureWindowsPath
import argparse
import hashlib
import importlib.util
import json


def require(value, reason):
    if not value:
        raise ValueError(reason)


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def atom(value):
    return ('atom', value)


def integer(value):
    return ('integer', value)


def tuple_term(*items):
    return ('tuple', items)


def erlang_string(value):
    return '"' + str(value).replace('\\', '\\\\').replace('"', '\\"').replace('\r', '\\r').replace('\n', '\\n') + '"'


def instruction(value, name):
    return value[0] == 'tuple' and value[1] and value[1][0] == atom(name)


def verify(output, expected_head, expected_run, expected_attempt):
    output = output.resolve()
    root = output / 'load-nif-opcodes'
    result_path = root / 'load-nif-opcode-verifier-results.json'
    require(not result_path.exists(), 'Fresh verifier result required; earlier proof cannot be overwritten')
    contract = read_json(Path(__file__).with_name('load-nif-opcode-contract.json'))
    parent = read_json(output / 'manifest.json')
    proof = read_json(output / 'source-build-evidence.json')
    observed = read_json(root / 'load-nif-opcode-manifest.json')
    require(len(parent['Fixtures']) == 6 and parent['OriginalFixturesExecuted'] == 5, 'Retained baseline six/five contract')
    for value in (parent, observed):
        require(value['SourcePin'] == contract['SourcePin'] and value['SourceTag'] == contract['SourceTag'] and value['InstalledVersion'] == '29.1.1', 'Pinned compiler identity')
    require(proof['SourceCommit'] == contract['SourcePin'] and proof['SourceTag'] == contract['SourceTag'] and proof['SourceBuildCompleted'] is True, 'Completed pinned source build')
    require(proof['HarnessCommit'] == expected_head and str(proof['RunId']) == str(expected_run) and str(proof['RunAttempt']) == str(expected_attempt), 'Original head/run/attempt binding')
    require(observed['InstalledOtpRoot'] == parent['InstalledOtpRoot'] == proof['InstalledOtpRoot'], 'Same installed compiler root')
    require(observed['ParentManifestSha256'] == digest((output / 'manifest.json').read_bytes()), 'Retained parent manifest binding')
    require(observed['SourceBuildEvidenceSha256'] == parent['SourceBuildEvidenceSha256'] == digest((output / 'source-build-evidence.json').read_bytes()), 'Build evidence binding')
    require(observed['SourceBuildLogSha256'] == parent['SourceBuildLogSha256'] == proof['SourceBuildLogSha256'].upper() == digest((output / 'source-build.log').read_bytes()), 'Build log binding')
    for key in ('CompilerExecutableSha256', 'OracleExecutableSha256'):
        require(observed[key] == parent[key], 'Same attested compiler/oracle bytes')
    require(observed['OriginalCompilationPerformed'] is True and observed['FileOnlyDisassemblyPerformed'] is True, 'Compilation/disassembly roles')
    for key in ('OriginalModuleLoadPerformed', 'OriginalBodyExecutionPerformed', 'NativeLoadingExecutionPerformed', 'OpcodeShapesVerified'):
        require(observed[key] is False, 'Exporter must retain compile-only/unverified-shape flags')
    require(observed['ExpectedRoles'] == contract['ExpectedRoles'] and observed['Module'] == contract['Module'], 'Static three-role declaration')
    require(observed['Metadata']['FileOnlyQuery'] is True and observed['Metadata']['ModuleLoadPerformed'] is False and observed['Metadata']['ExitCode'] == 0, 'File-only original metadata')
    files = {}
    for row in observed['Files']:
        name = row['File']
        require(name and Path(name).name == name and name not in files, 'Safe unique evidence filename')
        data = (root / name).read_bytes()
        require(len(data) == row['Bytes'] and digest(data) == row['Sha256'], 'Captured file bytes/hash')
        files[name] = data
    actual_files = list(root.iterdir())
    require(all(path.is_file() for path in actual_files), 'Flat evidence directory closure')
    require(set(files) == {path.name for path in actual_files} - {'load-nif-opcode-manifest.json'}, 'Exact captured output file closure')
    source_name = contract['Module'] + '.erl'
    beam_name = contract['Module'] + '.beam'
    require(digest(files[source_name]) == contract['SourceSha256'], 'Independent source declaration')
    declared = json.loads(files['predeclared-contract.json'].decode('utf-8-sig'))
    require(declared['SourceSha256'] == contract['SourceSha256'] and declared['ExpectedRoles'] == contract['ExpectedRoles'], 'Predeclared source/roles')
    for key in ('OriginalCompilationPerformed', 'OriginalBodyExecutionPerformed', 'NativeLoadingExecutionPerformed'):
        require(declared[key] is False, 'Declaration predates compilation/execution')
    for prefix in ('compile', 'disassemble', contract['Module'] + '-metadata'):
        require(files[prefix + '.exit-code.txt'].decode().strip() == '0', 'Original process succeeded')
        require(prefix + '.stdout.txt' in files and prefix + '.stderr.txt' in files, 'Retained process capture')
    require(len(observed['CompilerArguments']) == 5 and observed['CompilerArguments'][:3] == ['-Werror', '+debug_info', '-o'], 'Exactly authored compiler flags/arguments')
    original_output = PurePosixPath(observed['CompilerArguments'][3]) if observed['CompilerArguments'][3].startswith('/') else PureWindowsPath(observed['CompilerArguments'][3])
    original_source = type(original_output)(observed['CompilerArguments'][4])
    require(original_output.is_absolute() and original_source.is_absolute(), 'Absolute original compile paths')
    require(original_output.name == 'load-nif-opcodes' and original_source.name == source_name, 'Actual compile input/output paths')
    require(original_source.parent == original_output, 'Source belongs to original output directory')
    require('disassembly.term' in files and 'disassembly.eval.erl' in files, 'Raw original disassembly and evaluation')
    evaluation = ('D=beam_disasm:file(' + erlang_string(original_output / beam_name)
                  + '),{beam_file,beam_fixture_load_nif_opcodes,_,_,_,Functions}=D,ok=file:write_file('
                  + erlang_string(original_output / 'functions.etf')
                  + ',term_to_binary({load_nif_opcode_functions,beam_fixture_load_nif_opcodes,Functions})),ok=file:write_file('
                  + erlang_string(original_output / 'disassembly.term')
                  + ',unicode:characters_to_binary(io_lib:format("~tp.~n",[D]))),halt(0).\n')
    require(files['disassembly.eval.erl'] == evaluation.encode('utf-8'), 'Exact authored file-only disassembly evaluation and paths')
    metadata = observed['Metadata']
    for file_key, hash_key in [('EtfFile', 'EtfSha256'), ('TextFile', 'TextSha256'), ('EvaluationFile', 'EvaluationSha256')]:
        require(metadata[file_key] in files and digest(files[metadata[file_key]]) == metadata[hash_key], 'Metadata file reference/hash capture')
    prefix = contract['Module'] + '-metadata'
    require(digest(files[prefix + '.stdout.txt']) == metadata['StandardOutputSha256'] and digest(files[prefix + '.stderr.txt']) == metadata['StandardErrorSha256'], 'Metadata process stdout/stderr binding')
    require(b'code:load' not in files[metadata['EvaluationFile']] and ('beam_lib:md5(' + erlang_string(original_output / beam_name)).encode('utf-8') in files[metadata['EvaluationFile']], 'Metadata remains file-only query of compiled BEAM')
    decoder_path = Path(__file__).with_name('verify-metadata-mutations.py')
    spec = importlib.util.spec_from_file_location('load_nif_etf_decoder', decoder_path)
    decoder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(decoder)
    actual_chunks = decoder.chunks(files[beam_name])
    require([(name, len(data), digest(data)) for name, data in actual_chunks] == [(row['Id'], row['PayloadBytes'], row['PayloadSha256']) for row in observed['Chunks']], 'Actual raw BEAM chunk inventory')
    functions_term = decoder.decode_exact(files['functions.etf'])
    require(functions_term[0] == 'tuple' and len(functions_term[1]) == 3 and functions_term[1][:2] == (atom('load_nif_opcode_functions'), atom(contract['Module'])), 'Original function disassembly envelope')
    functions = functions_term[1][2]
    require(functions[0] == 'list', 'Actual function list')
    load_import = tuple_term(atom('extfunc'), atom('erlang'), atom('load_nif'), integer(2))
    put_import = tuple_term(atom('extfunc'), atom('erlang'), atom('put'), integer(2))
    roles = []
    for name, arity, opcode in [('non_tail', 3, 'call_ext'), ('tail_last', 3, 'call_ext_last'), ('tail_only', 2, 'call_ext_only')]:
        found = [value[1] for value in functions[1] if instruction(value, 'function') and value[1][1:3] == (atom(name), integer(arity))]
        require(len(found) == 1 and len(found[0]) == 5 and found[0][4][0] == 'list', 'Unique actual function/arity')
        code = found[0][4][1]
        load_calls = [(index, value[1]) for index, value in enumerate(code) if value[0] == 'tuple' and len(value[1]) >= 3 and value[1][2] == load_import]
        require(len(load_calls) == 1, 'Exactly one literal load import in each role')
        position, call = load_calls[0]
        require(call[:3] == (atom(opcode), integer(2), load_import), 'Required original compiler opcode hypothesis')
        slots = None
        if name == 'tail_last':
            require(len(call) == 4 and call[3][0] == 'integer' and call[3][1] > 0, 'Positive tail-last Y frame')
            slots = call[3][1]
            allocations = [value[1] for value in code[:position] if instruction(value, 'allocate') or instruction(value, 'allocate_heap')]
            require(len(allocations) == 1 and allocations[0][1] == integer(slots), 'Allocated/deallocated slot match')
            puts = [index for index, value in enumerate(code[:position]) if value == tuple_term(atom('call_ext'), integer(2), put_import)]
            require(len(puts) == 1, 'Observable put/2 call before tail transfer')
            saved = []
            for register in (0, 1):
                saves = [value[1][2] for value in code[:puts[0]] if instruction(value, 'move') and len(value[1]) == 3 and value[1][1] == tuple_term(atom('x'), integer(register)) and instruction(value[1][2], 'y')]
                require(len(saves) == 1 and saves[0][1][1][0] == 'integer' and 0 <= saves[0][1][1][1] < slots, 'Path/Info retained in allocated Y slots')
                require(tuple_term(atom('move'), saves[0], tuple_term(atom('x'), integer(register))) in code[puts[0] + 1:position], 'Evaluated arguments restored after observable call')
                saved.append(saves[0])
            require(saved[0] != saved[1], 'Distinct retained Path/Info slots')
        elif name == 'tail_only':
            require(len(call) == 3 and not any(instruction(value, 'allocate') or instruction(value, 'allocate_heap') for value in code), 'No-frame tail-only control')
        else:
            require(len(call) == 3 and any(instruction(value, 'put_tuple2') or instruction(value, 'put_tuple') for value in code[position + 1:]), 'Non-tail tuple continuation')
        roles.append({'Function': name, 'Arity': arity, 'ActualOpcode': opcode, 'FrameSlots': slots})
    result = {'Status': 'ACTUAL_ORIGINAL_COMPILE_DISASSEMBLY_THREE_ROLES_PASS', 'SourcePin': contract['SourcePin'], 'HarnessCommit': expected_head, 'RunId': str(expected_run), 'RunAttempt': str(expected_attempt), 'BeamSha256': digest(files[beam_name]), 'Roles': roles, 'OriginalCompilationPerformed': True, 'OpcodeShapesVerified': True, 'OriginalBodyExecutionPerformed': False, 'NativeLoadingExecutionPerformed': False, 'RetainedCompilerFixtures': 6, 'RetainedBodyExecutions': 5}
    with result_path.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print('PASS actual original load_nif call_ext/call_ext_last/call_ext_only; compile/disassembly only; retained baseline6/5')
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--expected-run', required=True)
    parser.add_argument('--expected-attempt', required=True)
    args = parser.parse_args()
    verify(args.output, args.expected_head, args.expected_run, args.expected_attempt)


if __name__ == '__main__':
    main()
