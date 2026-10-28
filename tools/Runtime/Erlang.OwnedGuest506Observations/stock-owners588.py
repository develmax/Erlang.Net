"""Opt-in stock-image custody for the existing OwnedGuest506 collector.

No VM/collector orchestration here. Invoked only by the explicit atomics job.
"""
import hashlib
import json
from pathlib import Path
import shutil

MODULES = ('atomics', 'counters')
PROFILE = 'GENUINE_FILE_LOADED_STOCK_ATOMICS_COUNTERS588'
SOURCE_BUILT_PROFILE = 'GENUINE_FILE_LOADED_STOCK_ATOMICS_COUNTERS598_SOURCE_BUILT'
SOURCE_BUILT_PROVIDER = 'PINNED_LINUX_BUILD_RECEIPT_KEY_BINARIES_AND_FULL_BEFORE_AFTER'


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def atom(text):
    require(text in MODULES, 'Unknown stock module')
    return text


def string(text):
    # Erlang string syntax; JSON ASCII escapes are not Erlang escapes.
    return '"' + ''.join('\\' + ch if ch in '\\"' else ch if 32 <= ord(ch) < 127 else '\\x{' + format(ord(ch), 'x') + '}' for ch in text) + '"'


def prepare(installed, directory, modules, source_built=False):
    require(tuple(modules) == MODULES, 'Exact stock atomics/counters pair required')
    rows = []
    for module in modules:
        matches = sorted((installed/'lib').glob('erts-*/ebin/' + module + '.beam'))
        require(len(matches) == 1 and matches[0].resolve().is_relative_to(installed.resolve()), 'One confined provider stock image required: ' + module)
        original = matches[0].resolve()
        copied = directory/('stock-' + module + '.beam')
        shutil.copyfile(original, copied)
        require(copied.read_bytes() == original.read_bytes(), 'Stock image copy differs')
        rows.append(dict(Module=module, ProviderPath=str(original), ProviderRelativePath=original.relative_to(installed).as_posix(),
                         CopiedFile=copied.name, Sha256=digest(copied), Bytes=copied.stat().st_size))
    profile = SOURCE_BUILT_PROFILE if source_built else PROFILE
    (directory/'stock-images588.json').write_text(json.dumps(dict(Schema=2, ExecutionProfile=profile, Images=rows), indent=2)+'\n', encoding='utf-8')


def owner_expression(directory, output):
    index = json.loads((directory/'stock-images588.json').read_bytes())
    require(index['ExecutionProfile'] in (PROFILE, SOURCE_BUILT_PROFILE), 'Unknown stock identity profile')
    rows = index['Images']
    require(output in ('stock-owners-before588.etf', 'stock-owners-after588.etf', 'stock-owners-initial-preloaded588.etf'), 'Invalid owner evidence name')
    pairs = '[' + ','.join('{' + atom(row['Module']) + ',' + string(row['ProviderPath']) + '}' for row in rows) + ']'
    # Each record is built in a fresh fun scope so before/after expressions do
    # not bind variables to values from the other snapshot accidentally.
    require_initial_same_file = (output != 'stock-owners-initial-preloaded588.etf'
                                 or index['ExecutionProfile'] == PROFILE)
    running_identity = ('ExpectedMd5588=M:module_info(md5),'
                        if require_initial_same_file else
                        'true=is_binary(M:module_info(md5)),16=byte_size(M:module_info(md5)),')
    return ('W(' + string(output) + ',[begin '
            'M:module_info(),ExpectedPath588=filename:absname(P),'
            'ActualOwner588=code:which(M),'
            'case ActualOwner588 of preloaded->true=lists:member(M,erlang:pre_loaded());'
            '_->ExpectedPath588=filename:absname(ActualOwner588) end,'
            '{ok,{M,ExpectedMd5588}}=beam_lib:md5(P),'
            + running_identity +
            '{M,P,ExpectedPath588,ActualOwner588,ExpectedMd5588,M:module_info(md5),'
            'M:module_info(compile),M:module_info(attributes),M:module_info(exports)} '
            'end||{M,P}<-' + pairs + ']),')


def expression(directory, output):
    setup = ''
    if output == 'stock-owners-before588.etf':
        index = json.loads((directory/'stock-images588.json').read_bytes())
        rows = index['Images']
        pairs = '[' + ','.join('{' + atom(row['Module']) + ',' + string(row['ProviderPath']) + '}' for row in rows) + ']'
        setup = (
            'true=lists:all(fun({M,_})->code:which(M)=:=preloaded end,' + pairs + '),'
            + owner_expression(directory, 'stock-owners-initial-preloaded588.etf')
            + 'W("stock-load-receipts588.etf",[begin '
              'InitialMd5588=M:module_info(md5),true=code:unstick_mod(M),'
              '{ok,LoadedBytes588}=file:read_file(P),'
              '{module,M}=code:load_binary(M,P,LoadedBytes588),'
              '{ok,{M,LoadedFileMd5598}}=beam_lib:md5(P),LoadedFileMd5598=M:module_info(md5),'
            + ('InitialMd5588=LoadedFileMd5598,' if index['ExecutionProfile'] == PROFILE else '')
            +
              '{M,true,{module,M},InitialMd5588,M:module_info(md5),code:which(M)} '
              'end||{M,P}<-' + pairs + ']),'
            + 'W("stock-loaded-native-witness588.etf",(fun()->'
              'R588=atomics:new(1,[]),ok=atomics:put(R588,1,37),37=atomics:get(R588,1),'
              'Cs588=[begin C588=counters:new(1,O588),{atomics,CR588}=C588,'
              'true=is_reference(CR588),ok=counters:put(C588,1,41),'
              'ok=counters:add(C588,1,1),42=counters:get(C588,1),'
              '{O588,counters:get(C588,1),counters:info(C588)} end||O588<-[[],[atomics]]],'
              '{atomics:get(R588,1),atomics:info(R588),Cs588} end)()),'
        )
    return setup + owner_expression(directory, output)


def verify(installed, directory):
    index = json.loads((directory/'stock-images588.json').read_bytes())
    require(index['ExecutionProfile'] in (PROFILE, SOURCE_BUILT_PROFILE) and index['Schema'] == 2, 'Loaded stock profile required')
    rows = index['Images']
    for name in ['stock-owners-initial-preloaded588.etf', 'stock-load-receipts588.etf', 'stock-loaded-native-witness588.etf']:
        require((directory/name).is_file(), 'Genuine load/native witness absent: ' + name)
    require([row['Module'] for row in rows] == list(MODULES), 'Stock evidence pair differs')
    for row in rows:
        path = (installed/row['ProviderRelativePath']).resolve()
        require(path.is_relative_to(installed.resolve()) and path == Path(row['ProviderPath']).resolve(), 'Provider image path changed')
        require(digest(path) == row['Sha256'] == digest(directory/row['CopiedFile']), 'Provider or copied image changed')
    before, after = directory/'stock-owners-before588.etf', directory/'stock-owners-after588.etf'
    require(before.is_file() and after.is_file() and before.read_bytes() == after.read_bytes(), 'Actual stock owner changed across subject')
