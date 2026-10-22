"""Root-executed same-Windows formatter oracle. No compiler or golden mutation."""
import argparse, ctypes, hashlib, importlib.util, json, os, shutil
from pathlib import Path

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
REMOVED = ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_USE_SERVER','ERLC_SERVER_ID','ERLC_CONFIGURATION']

def load(name, path):
    spec=importlib.util.spec_from_file_location(name,path); module=importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

def save(path, value):
    path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

def main():
    args=argparse.ArgumentParser(); args.add_argument('--repository',type=Path,required=True); args.add_argument('--admission',type=Path,required=True); args.add_argument('--output',type=Path,required=True); a=args.parse_args()
    root=a.repository.resolve(); packet=Path(__file__).resolve().parent; out=a.output.resolve()
    assert os.name=='nt' and not out.exists() and out.is_relative_to(root/'artifacts')
    inventory=load('inventory437',packet/'scripts/bounded_inventory.py'); sha=inventory.digest
    for name,expected in json.loads((packet/'manifest.json').read_bytes()).items():
        assert sha(packet/name)==expected, name
    fixture=root/'tests/Erlang.Tests/Fixtures/DdllErrorOriginal416'
    bindings=json.loads((packet/'source-bindings.json').read_bytes())
    for name,expected in bindings.items(): assert sha(fixture/name)==expected,name
    admission=json.loads(a.admission.read_bytes()); assert admission['SourceCommit']==PIN and admission['ReleaseTag']=='OTP-29.1.1'
    installed=Path(admission['InstalledOtpRoot']).resolve(); execution=Path(admission['ExecutionManifestPath'])
    assert sha(execution)==admission['ExecutionManifestSha256']
    expected={row['Path'].replace('\\','/'):row['Sha256'] for row in json.loads(execution.read_bytes())}
    assert len(expected)==6427 and inventory.inventory(installed)==expected
    stock=[key for key in expected if key.endswith('/ebin/erl_ddll.beam')]; assert len(stock)==1
    owned=load('owned437',packet/'scripts/owned.py'); decoder=load('etf437',packet/'scripts/passive_etf.py')
    out.mkdir(); ebin=out/'ebin'; ebin.mkdir()
    shutil.copyfile(fixture/'beam_ddll_error416.beam',ebin/'beam_ddll_error416.beam')
    # Worker line/source metadata and child MFA remain exactly the committed original.
    env=dict(os.environ)
    for key in REMOVED: env.pop(key,None)
    env['ERL_FLAGS']='+S 1:1 +A 1'
    expr=('[V,S,A]=init:get_plain_arguments(),'
          '{module,erl_ddll}=code:ensure_loaded(erl_ddll),'
          'P=code:which(erl_ddll),{ok,B}=prim_file:read_file(P),'
          'ok=prim_file:write_file(A,term_to_binary({erl_ddll,P,erlang:md5(B),erl_ddll:module_info(exports)},[{minor_version,2}])),'
          'ok=prim_file:write_file(V,term_to_binary(beam_ddll_error416:run(),[{minor_version,2}])),'
          'ok=prim_file:write_file(S,term_to_binary(beam_ddll_error416:run_stacks(),[{minor_version,2}])),'
          'erlang:halt(0).')
    failures=[]; settled=None; whole=0; before=inventory.inventory(installed)==expected
    try:
        settled=owned.owned([str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-pa',str(ebin),'-eval',expr,'-extra',str(out/'values.etf'),str(out/'stacks.etf'),str(out/'authority.etf')],out,'original',120,env,str(root))
        def proper(node):
            assert node[0]=='list' and len(node)==2,node
            return node[1]
        old=json.loads((fixture/'original-completion.json').read_bytes()); cases=old['Cases']
        values=proper(decoder.decode_exact((out/'values.etf').read_bytes())); stacks=proper(decoder.decode_exact((out/'stacks.etf').read_bytes()))
        assert len(cases)==len(values)==len(stacks)==44
        for name,row,group in zip(cases,values,stacks):
            assert row[0]=='tuple' and row[1][0]==('atom',name) and len(row[1])==2
            assert group[0]=='tuple' and group[1][0]==('atom',name) and len(group[1])==2
            profiles=proper(group[1][1]); assert len(profiles)==5
            for depth,profile in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],profiles):
                assert profile[0]=='tuple' and profile[1][0]==depth
                outcome=profile[1][1]; assert outcome[0]=='tuple' and outcome[1][0]==('atom',name)
            whole+=6
        authority=decoder.decode_exact((out/'authority.etf').read_bytes()); assert authority[0]=='tuple' and authority[1][0]==('atom','erl_ddll')
        actual=''.join(chr(x[1]) for x in proper(authority[1][1]))
        assert Path(actual).resolve()==(installed/stock[0]).resolve()
        assert authority[1][2]==('binary',hashlib.md5((installed/stock[0]).read_bytes()).digest())
    except Exception as error: failures.append(repr(error))
    finally:
        after=inventory.inventory(installed)==expected
        if not after: failures.append('Provider closure changed')
        files={file.relative_to(out).as_posix():sha(file) for file in out.rglob('*') if file.is_file()}
        receipt={'Passed':not failures and whole==264 and before and after,'Pin':PIN,'WholeRows':whole,'WorkerSha256':bindings['beam_ddll_error416.beam'],'OriginalRuntimeSha256':sha(installed/'bin/erl.exe'),'StockDdllSha256':expected[stock[0]],'ProviderFiles':6427,'ProviderBeforeEqual':before,'ProviderAfterEqual':after,'AdmissionSha256':sha(a.admission),'ExecutionManifestSha256':sha(execution),'Settled':bool(settled and settled['Settled'] and settled['ExitCode']==0 and not settled['TimedOut']),'SystemDefaultUiLanguage':ctypes.windll.kernel32.GetSystemDefaultUILanguage(),'AnsiCodePage':ctypes.windll.kernel32.GetACP(),'Host':os.environ.get('COMPUTERNAME'),'GitHubRunId':os.environ.get('GITHUB_RUN_ID'),'Files':files,'Errors':failures,'Projection':False,'CompilerUsed':False,'HistoricalGoldenRewritten':False}
        save(out/'completion.json',receipt)
    if not receipt['Passed']: raise SystemExit(1)
    print(json.dumps({'Profile':str(out/'completion.json'),'Sha256':sha(out/'completion.json')}))

if __name__=='__main__': main()
