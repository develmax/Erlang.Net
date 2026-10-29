"""Fresh execution of verified cached original419/420 workers and exact managed cases; no build."""
import argparse, hashlib, importlib.util, json, os, re, shutil, sys, time
from pathlib import Path
sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
GROUPS = [
    dict(Id='419', Fixture='EtsCompiledOriginal419', Modules=['beam_ets_compiled_subject419', 'beam_ets_compiled_observer419'], Observer='beam_ets_compiled_observer419', Case='oracle/compiled-module/ets-compiled419-252-full-typed-raw', Base='artifacts/library419-ets-matchspec-permanent', Count=252),
    dict(Id='420', Fixture='EtsCursorsOriginal420', Modules=['beam_ets_cursors420'], Observer='beam_ets_cursors420', Case='oracle/compiled-module/ets-source-cursors420-all246-full-typed-raw', Base='artifacts/library420-ets-cursors-permanent', Count=246),
]
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def need(value, message):
    if not value: raise RuntimeError(message)
def save(path, value):
    with path.open('x', encoding='utf-8') as stream: json.dump(value, stream, indent=2)
def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path); module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module
def estr(value): return '"' + str(value).replace('\\', '/').replace('"', '\\"') + '"'
def guard(repo, tool):
    plan = json.loads((tool/'inputs.json').read_bytes()); need(plan['Pin'] == PIN, 'Pin differs')
    for row in plan['Inputs']: need(sha(repo/row['Path']) == row['Sha256'], 'Source changed: ' + row['Path'])
    need(sha(tool/'run.py') == plan['RunnerSha256'], 'Runner changed')
    return plan

def original(a, repo, out, owned, group, installed, erl):
    directory = out/('original' + group['Id']); directory.mkdir(); errors = []; passed = False
    fixture = repo/'tests/Erlang.Tests/Fixtures'/group['Fixture']; copied = {}
    try:
        for module in group['Modules']:
            source = fixture/(module + '.beam'); destination = directory/source.name
            shutil.copyfile(source, destination); copied[source.name] = sha(source)
            need(sha(destination) == copied[source.name], 'Copied worker differs')
        need(not (directory/'ets.beam').exists() and not (directory/'lists.beam').exists(), 'Original library shadowed')
        env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', ERL_CRASH_DUMP=str(directory/'erl_crash.dump'))
        for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_USE_SERVER','ERLC_SERVER_ID','ERLC_CONFIGURATION','ERL_ROOTDIR']: env.pop(key, None)
        modules = ','.join(group['Modules']); observer = group['Observer']
        expression = ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
            'Root=filename:absname(' + estr(installed) + '),Root=filename:absname(code:root_dir()),'
            '8=erlang:system_info(wordsize),1=erlang:system_info(schedulers_online),'
            'W("profile.etf",{erlang:system_info(wordsize),erlang:system_info(schedulers),erlang:system_info(schedulers_online),erlang:system_info(debug_compiled),erlang:system_info(otp_release)}),'
            '[begin M:module_info(),P=filename:absname(atom_to_list(M)++".beam"),P=filename:absname(code:which(M)),{ok,{M,D}}=beam_lib:md5(P),D=M:module_info(md5),W(atom_to_list(M)++"-owner.etf",{M,P,D,M:module_info(compile)}) end||M<-[' + modules + ']],'
            '[begin M:module_info(),P=filename:absname(code:which(M)),true=lists:prefix(Root++"/",P),W(atom_to_list(M)++"-owner.etf",{M,P,M:module_info(md5),M:module_info(compile)}) end||M<-[ets,lists]],'
            'V=' + observer + ':run(),N=' + str(group['Count']//6) + ',N=length(V),S=' + observer + ':run_stacks(),5=length(S),true=lists:all(fun({_,_,R})->length(R)=:=N end,S),W("values.etf",V),W("stacks.etf",S),halt(0).')
        (directory/'invocation.eval').write_text(expression, encoding='utf-8')
        owned.owned([str(erl),'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-pa',str(directory),'-eval',expression],directory,'original',180,env,str(directory))
        for name in ['values.etf','stacks.etf','profile.etf']:
            need((directory/name).is_file(), 'Original observation absent: ' + name)
        for name, digest in copied.items(): need(sha(directory/name) == digest, 'Worker changed: ' + name)
        passed = True
    except Exception as error: errors.append(repr(error))
    result = dict(Passed=passed and not errors, Errors=errors, FreshOriginalCompilation=False, FreshOriginalExecution=passed, VerifiedCachedWorkers=copied, SourcePin=PIN, FrameProjection=False)
    save(directory/'completion.json', result); return result

def managed(a, repo, out, owned, group):
    directory = out/('managed' + group['Id']); directory.mkdir(); errors = []; started = time.time_ns()
    host = repo/'tests/Erlang.Tests/bin'/a.configuration/'net10.0/Erlang.Tests.dll'; binary = host.parent
    base = repo/group['Base']; before = set(base.iterdir()) if base.exists() else set(); protected = {}; fresh = None
    def inventory():
        return {q.name:sha(q) for q in sorted(binary.iterdir()) if q.is_file() and (q.suffix.lower() == '.dll' or q.name.endswith('.deps.json') or q.name.endswith('.runtimeconfig.json'))}
    try:
        need(host.is_file(), 'Managed host absent'); protected = inventory(); need('Erlang.Runtime.dll' in protected, 'Runtime absent')
        owned.owned(['dotnet','exec','--fx-version',a.framework_version,str(host),'--case',group['Case'],'--report',str(directory/'report.json')],directory,'managed',180,dict(os.environ,DOTNET_PROCESSOR_COUNT='2',PYTHONDONTWRITEBYTECODE='1'),str(repo))
    except Exception as error: errors.append(repr(error))
    # Receipt collection is independent of child success, so failed cases retain captures.
    completed = time.time_ns()
    try:
        added = (set(base.iterdir()) if base.exists() else set()) - before
        need(len(added) == 1, 'Exactly one fresh managed GUID output required')
        fresh = added.pop(); need(fresh.is_dir() and re.fullmatch('[0-9a-fA-F]{32}', fresh.name), 'Invalid GUID output')
        for name in ['actual-values.etf','actual-stacks.etf','actual-values.etf.json','actual-stacks.etf.json','comparison.json']:
            path = fresh/name
            need(path.is_file() and started <= path.stat().st_mtime_ns <= completed, 'Capture absent/outside owned window: ' + name)
            shutil.copyfile(path,directory/name)
    except Exception as error: errors.append('capture: ' + repr(error))
    try:
        need(protected and inventory() == protected, 'Managed DLL/config inventory changed')
        report = json.loads((directory/'report.json').read_bytes()); tests = report['Tests']
        need(report['Passed'] == 1 and report['Failed'] == 0 and report['SelectedCount'] == 1 and len(tests) == 1 and tests[0]['Name'] == group['Case'] and tests[0]['Status'] == 'Passed', 'Exact managed case failed')
        receipt = json.loads((directory/'managed.exit.json').read_bytes())
        need(receipt['Settled'] and receipt['ExitCode'] == 0 and not receipt['TimedOut'] and not receipt['Failures'], 'Managed child not settled successfully')
        proof = json.loads((directory/'comparison.json').read_bytes())
        need(proof['Expected'] == proof['Examined'] == proof['Equal'] == group['Count'], 'Managed observation count differs')
        need(not proof['Errors'] and not proof['Differences'] and proof['ProfileArity'] == 3 and not proof['Projection'], 'Managed comparison/cleanup errors')
        for field in ['WholeTerms', 'WholeRawEtf']: need(proof[field] == {'values.etf':True,'stacks.etf':True}, 'Whole comparison differs: ' + field)
        need(not proof['WholeOriginalEts51Claim'] and not proof['ApiOrFullContractPromotion'], 'Unexpected compatibility promotion')
    except Exception as error: errors.append('postflight: ' + repr(error))
    result = dict(Passed=not errors, Errors=errors, Case=group['Case'], StartedUnixNs=started, CompletedUnixNs=completed, FreshOutput=str(fresh) if fresh else None, ProtectedDllAndConfigBefore=protected, CleanupEvidence='Source-bound comparison.json emitted after independent actor/runtime cleanup; Errors must be empty. No added cleanup flags claimed.')
    save(directory/'completion.json',result); return result

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--repository',type=Path,required=True); parser.add_argument('--official-admission',type=Path,required=True); parser.add_argument('--admission-sha256',required=True); parser.add_argument('--output',type=Path,required=True); parser.add_argument('--configuration',choices=['Debug','Release'],default='Debug'); parser.add_argument('--framework-version',default='10.0.11'); a = parser.parse_args()
    repo = a.repository.resolve(); tool = Path(__file__).resolve().parent; out = a.output.resolve()
    need(os.name == 'nt' and out.is_relative_to(repo/'artifacts') and not out.exists(), 'Fresh confined Windows output required')
    guard(repo,tool); out.mkdir(parents=True); owned = load('ets540_owned',repo/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
    errors = []; results = {}; after = None; before = None; installed = None; erl = None
    try:
        collect = load('ets540_provider',repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py'); a.build_receipt = None; a.otp_source = None
        installed,erl,before,after = collect.provider(a,repo); save(out/'provider-before.json',before)
    except Exception as error: errors.append('provider: ' + repr(error))
    for group in GROUPS:
        ident = group['Id']
        # Original groups are independent, managed routes still run if provider/original fails.
        original_result = original(a,repo,out,owned,group,installed,erl) if installed else dict(Passed=False,Errors=['Original provider preparation failed'])
        managed_result = managed(a,repo,out,owned,group)
        equal = {}; different = []
        if original_result['Passed'] and managed_result['Passed']:
            for name in ['values.etf','stacks.etf']:
                left = out/('original'+ident)/name; right = out/('managed'+ident)/('actual-'+name)
                equal[name] = left.read_bytes() == right.read_bytes()
                if not equal[name]: different.append(name)
        else: different.append('Whole comparison blocked by failed prerequisite route')
        results[ident] = dict(Original=original_result,Managed=managed_result,WholeFreshRawEqual=equal,Differences=different,Passed=bool(equal) and all(equal.values()) and not different,DeclaredObservations=group['Count'])
    if after is not None:
        try:
            current = after(); save(out/'provider-after.json',dict(Inventory=current,ProviderEqual=current == before['Inventory'])); need(current == before['Inventory'],'Provider changed')
        except Exception as error: errors.append('provider-postflight: ' + repr(error))
    try: guard(repo,tool)
    except Exception as error: errors.append('source-postflight: ' + repr(error))
    result = dict(Passed=all(row['Passed'] for row in results.values()) and not errors,Groups=results,Errors=errors,Observations=498,FreshOriginalCompilation=False,FrameProjection=False,RawEtfRewritten=False,ApiDelta=0,FullContractDelta=0,NativeDiagnosticPhysicalEvidenceIncluded=False,NativeProducerRedirect=False,Files={q.relative_to(out).as_posix():sha(q) for q in out.rglob('*') if q.is_file()})
    save(out/'completion.json',result); return 0 if result['Passed'] else 1
if __name__ == '__main__': raise SystemExit(main())
