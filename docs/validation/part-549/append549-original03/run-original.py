import pathlib,runpy,os,hashlib,json
root=pathlib.Path(__file__).resolve().parents[3];out=pathlib.Path(__file__).resolve().parent
helper=runpy.run_path(str(root/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py'))
erl=pathlib.Path(r'D:\Distr\Work\Codex\Erlang.Net\Erlang.Net\artifacts\library376-official-compiler\installed\bin\erl.exe')
q=lambda p:str(p).replace('\\','/')
env=dict(os.environ);env['ERL_CRASH_DUMP']=str(out/'erl_crash.dump')
expr=''.join('((fun() -> case compile:file("'+q(out/(name+'.erl'))+'",[debug_info,{outdir,"'+q(out)+'"}]) of {ok,'+name+'}->ok; E->erlang:error({compile_failed,E}) end end)()),' for name in ['append_root_observer','append_root_capture'])+'halt(0).'
helper['owned']([str(erl),'+S','2:2','+SDcpu','1:1','+SDio','1','+A','1','-noshell','-eval',expr],out,'compile',60,env,str(out))
expr='V=append_root_capture:run(),ok=file:write_file("'+q(out/'original.etf')+'",term_to_binary(V,[{minor_version,2}])),io:format("~p~n",[V]),halt(0).'
helper['owned']([str(erl),'+S','2:2','+SDcpu','1:1','+SDio','1','+A','1','-noshell','-pa',str(out),'-eval',expr],out,'original',60,env,str(out))
(out/'source-image-hashes.json').write_text(json.dumps({p.name:hashlib.sha256(p.read_bytes()).hexdigest().upper() for p in out.iterdir() if p.suffix in ['.erl','.beam','.etf']},indent=2))
