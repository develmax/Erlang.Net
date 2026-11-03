import pathlib,json,hashlib,subprocess
r=pathlib.Path.cwd();lab=r/'artifacts/ordsets-native-selection-audit/from-list-source-boundaries';sha=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest().upper()
a=json.loads((r/'artifacts/library376-official-compiler/official-admission.json').read_text(encoding='utf-8-sig'));assert a['SourceCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
m=pathlib.Path(a['ExecutionManifestPath']);assert sha(m)==a['ExecutionManifestSha256'];files=json.loads(m.read_text(encoding='utf-8-sig'));provider=pathlib.Path(a['InstalledOtpRoot'])
def verify():
 for x in files:assert sha(provider/x['Path'])==x['Sha256'],x['Path']
verify();q=lambda p:json.dumps(pathlib.Path(p).as_posix())
expression='{ok,ordsets_core_worker}=compile:file('+q(r/'tests/Erlang.Tests/Resources/OrdsetsCompatibility/ordsets_core_worker.erl')+',[debug_info,{outdir,'+q(lab)+'}]),{ok,from_list_source_boundaries}=compile:file('+q(lab/'from_list_source_boundaries.erl')+',[debug_info,{outdir,'+q(lab)+'}]),Bytes=ordsets_core_worker:run(from_list_source_boundaries,run),ok=file:write_file("original.etf",Bytes),io:format("12 source-boundary rows~n"),halt().'
p=subprocess.run([str(provider/'bin/erl.exe'),'+S','2:2','-pa',str(lab),'-noshell','-eval',expression],cwd=lab,capture_output=True,timeout=30)
(lab/'original.stdout').write_bytes(p.stdout);(lab/'original.stderr').write_bytes(p.stderr);assert p.returncode==0,p.stderr;verify()
(lab/'provenance.json').write_text(json.dumps({'PinnedCommit':a['SourceCommit'],'ProviderFilesVerifiedBeforeAndAfter':len(files),'Expression':expression,'ExitCode':p.returncode,'SourceSha256':sha(lab/'from_list_source_boundaries.erl'),'BeamSha256':sha(lab/'from_list_source_boundaries.beam'),'OriginalSha256':sha(lab/'original.etf'),'WorkerSourceSha256':sha(r/'tests/Erlang.Tests/Resources/OrdsetsCompatibility/ordsets_core_worker.erl'),'WorkerBeamSha256':sha(lab/'ordsets_core_worker.beam')},indent=2)+'\n');print(p.stdout.decode().strip())
