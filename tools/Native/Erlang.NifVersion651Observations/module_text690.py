"""Four valid Latin1 calling-module contracts using the already admitted N1 image/provider."""
import json
import shutil
from pathlib import Path
from common import checked, estr, need, row, save, sha

NATIVE_CASE = 'v2_18_opts1'
OBSERVER = 'module_observer690'
ADMISSION = 'APPROVED_MODULE690_EXECUTION'
EXPECTED = {
    'ascii': 'nif_ascii690',
    'latin1_unquoted': 'nif_é690',
    'latin1_quoted': 'Nif_é690',
    'latin1_distinct': 'nif_ø690',
}


def assert_expected(term, case):
    atom = lambda value: ('atom', value)
    tuple_ = lambda *values: ('tuple', tuple(values))
    need(term[0] == 'tuple' and len(term[1]) == 3, 'Module690 needs whole three outcomes')
    load, first, second = term[1]
    text = ('list', tuple(('integer', value) for value in case['ExpectedTextBytes']))
    need(load == tuple_(atom('value'), tuple_(atom('error'), tuple_(atom('bad_lib'), text))),
         'Module690 returned byte-charlist differs from pinned formatter')
    for stub in [first, second]:
        need(stub[0] == 'tuple' and len(stub[1]) == 4
             and stub[1][0:3] == (atom('raised'), atom('error'), atom('not_loaded'))
             and stub[1][3][0] == 'list' and bool(stub[1][3][1]),
             'Module690 stub lost complete error stack')


def expression(specs, cases, installed, library):
    tuples = ','.join('{' + ','.join(estr(value) for value in spec) + '}' for spec in specs)
    entries = ','.join('{' + estr(case['Id']) + ',list_to_atom('
                       + str(case['Codepoints']) + '),' + str(case['ExpectedTextBytes']) + '}' for case in cases)
    stem = Path(library).with_suffix('').as_posix()
    return (
        'W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
        'Expected=filename:absname(' + estr(installed) + '),Expected=filename:absname(code:root_dir()),'
        '8=erlang:system_info(wordsize),'
        'Compile=fun({Id,S,D})->Result=try '
        '{ok,F}=epp:parse_file(S,[],[]),C=compile:forms(F,[binary,debug_info,return_errors,return_warnings]),'
        'W(Id++"-compile.etf",C),{M,B}=case C of {ok,M0,B0}->{M0,B0};{ok,M0,B0,_}->{M0,B0} end,'
        'ok=file:write_file(D,B),P=filename:absname(D),{module,M}=code:load_binary(M,P,B),'
        'P=filename:absname(code:which(M)),{ok,{M,Md5}}=beam_lib:md5(D),Md5=M:module_info(md5),'
        '{ok,M,P,Md5,M:module_info(compile)} catch Class:Reason:Stack->{failed,Class,Reason,Stack} end,'
        'W(Id++"-preparation.etf",Result),{Id,Result} end,'
        'Prepared=[Compile(Spec)||Spec<-[' + tuples + ']],W("image-owners.etf",Prepared),'
        'Observe=fun({Id,M,ExpectedText})->T=try module_observer690:capture(M,' + estr(stem) + ') '
        'catch Class:Reason:Stack->{probe_failed,Class,Reason,Stack} end,W(Id++".etf",T),'
        '{Id,case T of {{value,{error,{bad_lib,Text}}},_,_}->Text=:=ExpectedText;_->false end} end,'
        'Checks=[Observe(Case)||Case<-[' + entries + ']],W("checks.etf",Checks),'
        'Ready=lists:all(fun({_,{ok,_,_,_,_}})->true;(_)->false end,Prepared),'
        'case Ready andalso lists:all(fun({_,true})->true;(_)->false end,Checks) of '
        'true->halt(0);false->halt(79) end.'
    )


def collect(
    tool,
    output,
    image,
    native_receipt,
    host,
    installed,
    erl,
    bridge_admission,
    phase,
    decoder,
    environment,
    framework,
    linux,
):
    root = output / 'module-text690'
    root.mkdir()
    fixtures = tool / 'fixtures/module690'
    cases = json.loads((fixtures / 'cases.json').read_bytes())
    need(len(cases) == 4 and {case['Id']: case['Module'] for case in cases} == EXPECTED,
         'Exact four valid Latin1 module cases required')
    source_inputs = [row(path) for path in sorted(fixtures.iterdir()) if path.is_file()]
    need(len(source_inputs) == 6, 'Exact five source files and cases manifest required')
    rows = []
    errors = []
    original_result = dict(Passed=False, SkippedDependent='Provider or native image unavailable')
    beams = {}
    prep = root / 'original'
    prep.mkdir()
    for case in cases:
        need(Path(case['Source']).name == case['Source'] and Path(case['Beam']).name == case['Beam'], 'Confined aliases')
        need(case['Codepoints'] == [ord(value) for value in case['Module']]
             and all(value <= 255 for value in case['Codepoints']), 'Original module-name Latin1 domain')
        expected_display = "'" + case['Module'] + "'" if case['Id'] == 'latin1_quoted' else case['Module']
        text = "Library module name 'nif_version651' does not match calling module '" + expected_display + "'"
        need(case['ExpectedTextBytes'] == list(text.encode('utf-8')), 'Pinned byte-charlist expectation differs')
    try:
        for source in fixtures.glob('*.erl'):
            shutil.copyfile(source, prep / source.name)
            need(sha(prep / source.name) == sha(source), 'Module690 copied source differs')
        if installed is not None and erl is not None and image is not None:
            library = checked(image['Library']['Path'], image['Library']['Sha256'])
            need(all(ord(value) < 128 for value in str(prep) + str(installed) + str(library)),
                 'NonASCII provider/native/source directory paths are a separate protocol')
            specs = [(case['Id'], case['Source'], case['Beam']) for case in cases]
            specs.append(('observer', OBSERVER + '.erl', OBSERVER + '.beam'))
            source = expression(specs, cases, installed, library)
            (prep / 'invocation.eval').write_bytes(source.encode('utf-8'))
            environment['ERL_CRASH_DUMP'] = str(prep / 'erl_crash.dump')
            original_result = phase([erl, '+S', '1:1', '+A', '1', '-noshell', '-noinput', '-boot',
                                     'no_dot_erlang', *(['-start_epmd', 'false'] if linux else []),
                                     '-eval', source], prep, 'observe', 180)
            for case in cases:
                path = prep / case['Beam']
                if path.is_file():
                    beams[case['Id']] = row(path)
            path = prep / (OBSERVER + '.beam')
            if path.is_file():
                beams['observer'] = row(path)
        else:
            errors.append(dict(Phase='original-preparation', Error='Missing admitted provider/native image'))
        save(prep / 'compiled-images.json', beams)
        for case in cases:
            directory = root / case['Id']
            directory.mkdir()
            item = dict(Id=case['Id'], CallingModule=case['Module'], Routes={}, Comparisons={})
            original_file = prep / (case['Id'] + '.etf')
            item['Routes']['original'] = dict(original_result)
            if original_file.is_file():
                try:
                    assert_expected(decoder.decode_exact(original_file.read_bytes()), case)
                except Exception as error:
                    item['Routes']['original']['Passed'] = False
                    errors.append(dict(Case=case['Id'], Route='original', Error=repr(error)))
            else:
                item['Routes']['original']['Passed'] = False
                errors.append(dict(Case=case['Id'], Route='original', Error='Missing raw outcome'))
            for route in ['reviewed', 'ordinary']:
                route_dir = directory / route
                route_dir.mkdir()
                if host is None or image is None or native_receipt is None or case['Id'] not in beams or 'observer' not in beams:
                    item['Routes'][route] = dict(Passed=False, SkippedDependent='Missing host/native or target+observer BEAM preparation')
                    continue
                try:
                    library = checked(image['Library']['Path'], image['Library']['Sha256'])
                    selected_beams = [beams[case['Id']], beams['observer']]
                    for beam in selected_beams:
                        checked(beam['Path'], beam['Sha256'])
                    admission = route_dir / 'execution-admission.json'
                    save(admission, dict(Status=ADMISSION, Pin='ad05823719d77c8faee87348ea39513d4e2f99c5',
                         Id=NATIVE_CASE, ModuleTextCase690=case['Id'], CallingModule690=case['Module'], Route=route,
                         LibraryPath=str(library), LibrarySha256=image['Library']['Sha256'],
                         BuildReceiptPath=str(native_receipt), BuildReceiptSha256=sha(native_receipt),
                         Beams=selected_beams, **bridge_admission))
                    environment['ERL_CRASH_DUMP'] = str(route_dir / 'unused-managed.crash.dump')
                    captured = phase(['dotnet', 'exec', '--fx-version', framework, host, '--version651-case',
                                      admission, sha(admission), route_dir / 'capture'], route_dir, 'managed', 180)
                    receipt_path = route_dir / 'capture/completion.json'
                    if receipt_path.is_file():
                        receipt = json.loads(receipt_path.read_bytes())
                        captured['CaptureReceipt'] = receipt
                        if not (receipt.get('Passed') and receipt.get('RuntimeClosed') and receipt.get('ImageRetired')
                                and receipt.get('OriginalBeamExecution') and receipt.get('ModuleText690')
                                and receipt.get('CallingModule') == case['Module'] and receipt.get('Route') == route):
                            captured['Passed'] = False
                            errors.append(dict(Case=case['Id'], Route=route, Error='Wrong protocol/owner or incomplete capture/cleanup'))
                    else:
                        captured['Passed'] = False
                        errors.append(dict(Case=case['Id'], Route=route, Error='Missing managed capture/cleanup receipt'))
                    item['Routes'][route] = captured
                    managed_file = route_dir / 'capture/observed.etf'
                    if original_file.is_file() and managed_file.is_file():
                        left, right = original_file.read_bytes(), managed_file.read_bytes()
                        a, b = decoder.decode_exact(left), decoder.decode_exact(right)
                        comparison = dict(WholeTypedEquality=a == b, RawByteEquality=left == right,
                                          OriginalSha256=sha(original_file), ManagedSha256=sha(managed_file))
                        item['Comparisons'][route] = comparison
                        assert_expected(a, case)
                        assert_expected(b, case)
                        if not comparison['WholeTypedEquality'] or not comparison['RawByteEquality']:
                            errors.append(dict(Case=case['Id'], Route=route, Error='Complete outcome/stack differs; no rewriting'))
                except Exception as error:
                    failed = item['Routes'].setdefault(route, {})
                    failed['Passed'] = False
                    failed.setdefault('Errors', []).append(repr(error))
                    errors.append(dict(Case=case['Id'], Route=route, Error=repr(error)))
            rows.append(item)
            save(directory / 'completion.json', item)
    except Exception as error:
        errors.append(dict(Phase='module-collection', Error=repr(error)))
    finally:
        try:
            for source in source_inputs:
                checked(source['Path'], source['Sha256'])
            for source in fixtures.glob('*.erl'):
                if (prep / source.name).is_file():
                    need(sha(prep / source.name) == sha(source), 'Module690 source changed')
            for beam in beams.values():
                checked(beam['Path'], beam['Sha256'])
        except Exception as error:
            errors.append(dict(Phase='module-inputs-after', Error=repr(error)))
    complete = len(rows) == 4 and len(beams) == 5 and original_result.get('Passed') and all(
        set(item['Routes']) == {'original', 'reviewed', 'ordinary'}
        and all(route.get('Passed') for route in item['Routes'].values())
        and set(item['Comparisons']) == {'reviewed', 'ordinary'}
        and all(value['WholeTypedEquality'] and value['RawByteEquality'] for value in item['Comparisons'].values())
        for item in rows)
    result = dict(Passed=bool(complete and not errors), Count=4, ExpectedComparisons=8, Rows=rows, Errors=errors,
                  FreshOriginalCompilation=len(beams) == 5, OriginalProcesses=1 if 'Receipt' in original_result else 0,
                  ReusedNativeCase=NATIVE_CASE, NewNativeBuilds=0, FramesProjected=False, RawOutcomesRewritten=False,
                  ApiDelta=0, FullContractDelta=0)
    save(root / 'completion.json', result)
    return result
