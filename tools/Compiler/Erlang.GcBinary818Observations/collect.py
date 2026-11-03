"""Owned original + same compiled-MFA runtime comparison; no result normalization."""
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

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')

def require(ok, message):
    if not ok:
        raise RuntimeError(message)

def load(path):
    spec = importlib.util.spec_from_file_location('owned818', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def row(path):
    return dict(Path=str(path.resolve()), Sha256=sha(path))

def main():
    p=argparse.ArgumentParser()
    p.add_argument('--repository',type=Path,required=True)
    p.add_argument('--runtime-source-root',type=Path,required=True)
    p.add_argument('--host',type=Path,required=True)
    p.add_argument('--dotnet',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    s=p.add_mutually_exclusive_group(required=True)
    s.add_argument('--official-admission',type=Path)
    s.add_argument('--build-receipt',type=Path)
    p.add_argument('--admission-sha256',required=True)
    p.add_argument('--otp-source',type=Path)
    p.add_argument('--expected-head')
    a=p.parse_args()
    repo=a.repository.resolve(); source=a.runtime_source_root.resolve()
    tool=Path(__file__).resolve().parent; out=a.output.resolve()
    require(out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh confined artifact directory required')
    require(a.host.is_file() and a.dotnet.is_file(),'Actual built runtime host and dotnet required')
    require((source/'src/Runtime/Erlang.Runtime.Core').is_dir(),'Actual build source directory required')
    out.mkdir(parents=True)
    owned=load(repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py')
    installed,erl,provider,after=owned.provider(a,repo)
    inputs=[row(path) for path in sorted((source/'src').rglob('*.cs'))]
    inputs += [row(path) for path in sorted((repo/'tools/Native/Erlang.NifNativeBackend').glob('*.cs'))]
    inputs += [row(path) for path in sorted(tool.rglob('*')) if path.is_file()]
    inputs += [row(path) for path in sorted(a.host.resolve().parent.glob('*')) if path.is_file() and path.suffix in ['.dll','.json','.so','.dylib']]
    inputs += [row(a.dotnet),row(Path(provider['AdmissionPath']))]
    errors=[]; results=[]
    def recheck():
        for item in inputs:
            require(sha(item['Path'])==item['Sha256'],'Input changed: '+item['Path'])
        require(after()==provider['Inventory'],'Original provider changed')
    try:
        original=out/'original'; original.mkdir()
        for file in (tool/'fixtures').glob('*.erl'):
            shutil.copyfile(file,original/file.name)
        env=os.environ.copy(); env['ERL_CRASH_DUMP']=str(original/'erl_crash.dump')
        command=[str(erl),'+S','2:2','+A','1','-noshell','-eval',
            'case compile:file("gc_binary818_original.erl",[debug_info,return_errors,return_warnings]) of {ok,gc_binary818_original,_}->gc_binary818_original:run(); Error->io:format("~tp~n",[Error]),halt(2) end.']
        owned.execute(repo,command,original,env)
        coverage=json.loads((original/'coverage.json').read_bytes())
        require(len(coverage)==6 and all(value is True for value in coverage.values()),'Actual compiler opcode coverage incomplete: '+repr(coverage))
        for name in ['original.etf','original-deterministic.etf','gc_binary818.beam','disassembly.etf','disassembly.txt','coverage.json',('original.exit.json' if os.name == 'nt' else 'original.process.json')]:
            inputs.append(row(original/name))
        admission=dict(Status='SOURCE_BOUND_ORIGINAL_GC_BINARY818',Pin=PIN,Inputs=inputs,
            BeamPath=str(original/'gc_binary818.beam'),BeamSha256=sha(original/'gc_binary818.beam'),
            OpcodeCoveragePath=str(original/'coverage.json'),OpcodeCoverageSha256=sha(original/'coverage.json'),
            OriginalRawPath=str(original/'original.etf'),OriginalDeterministicPath=str(original/'original-deterministic.etf'),
            Provider=provider,RuntimeSourceSnapshot=str(source),SourceToBinaryBuildAssociationIndependentlyProven=False,ExpectedRows=38,ApiRegistryDelta=0,FullCompatibilityClaimed=False,
            ProcessEntry='same original capture/0 -> spawn_monitor(Module,entry,[Parent]) -> rows -> row -> exported dynamic apply')
        admission_path=out/'admission.json'; save(admission_path,admission)
        # Independent profile failures must not prevent collecting the other profile.
        for profile in ['ordinary','private-owned']:
            route=out/profile; route.mkdir()
            try:
                owned.execute(repo,[str(a.dotnet.resolve()),str(a.host.resolve()),'--gc-binary818-child',str(admission_path),sha(admission_path),profile,str(route/'managed')],route,env)
                completion=json.loads((route/'managed/completion.json').read_bytes())
                require(completion['Passed'] and completion['WholeRawAndDeterministicEqual'] and completion['RuntimeClosed'],'Managed child contract failed')
                results.append(dict(Profile=profile,Passed=True,Completion=completion))
            except Exception as error:
                errors.append(profile+': '+repr(error)); results.append(dict(Profile=profile,Passed=False,Error=repr(error)))
    except Exception as error:
        errors.append('preparation: '+repr(error))
    finally:
        try: recheck()
        except Exception as error: errors.append('source/provider cleanup: '+repr(error))
        save(out/'summary.json',dict(Passed=not errors and len(results)==2,Profiles=results,Errors=errors,
            ExpectedRows=38,ApiRegistryDelta=0,FullCompatibilityClaimed=False,FramesDropped=False,NormalizationPerformed=False))
    require(not errors and len(results)==2,'; '.join(errors))

if __name__=='__main__':
    main()
