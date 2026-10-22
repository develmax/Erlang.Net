"""Adapt the existing collector for source-bound preloaded erlang, no execution."""
from pathlib import Path
import ast,shutil

ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent;TOOL=OUT/'tool'
BASE=ROOT/'artifacts/binary369-closure-original-proposal1/tool'
for name in ['ci-gates.py','passive_etf_family.py']:
    shutil.copyfile(BASE/name,TOOL/name)
shutil.copytree(BASE/'parse-gate',TOOL/'parse-gate',ignore=shutil.ignore_patterns('bin','obj','__pycache__'),dirs_exist_ok=True)
text=(BASE/'collect-original-corrected.py').read_text()
text=text.replace("assert profile.get('SubjectModule')=='binary'","assert profile.get('SubjectModule')=='erlang'\n    assert profile['MetadataPolicy']=='erlang-preloaded-v1'\n    preloaded=load(t/'preloaded_provenance.py','erlang370_preloaded')")
text=text.replace("failure=stage('observe',[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-eval',expression],directory,120)","assert isinstance(f['ObserveTimeoutSeconds'],int) and 1<=f['ObserveTimeoutSeconds']<=600\n                failure=stage('observe',[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-eval',expression],directory,f['ObserveTimeoutSeconds'])")
anchor="    if a.verify_only:print('PASS_SOURCE_BOUND_FAMILY_ADAPTER_AND_FIXTURE_PARSE_GATE');return"
assert anchor in text
text=text.replace(anchor,"    assert profile['Mfas'] and len(set(profile['TargetMfas']))==profile['TargetMfaCount']==351\n    assert set(profile['Mfas'])<=set(profile['TargetMfas'])\n"+anchor)
start=text.index("        installed=list(runtime.glob('lib/stdlib-*/ebin/binary.beam'))")
end=text.index("        owned=load(",start)
text=text[:start]+'''        versions=list((runtime/'releases').glob('*/OTP_VERSION'));assert any(x.read_text().strip()=='29.1.1' for x in versions)
        runtime_provenance=preloaded.runtime_inputs(runtime,build,before,out)
        write(out/'inputs.json',dict(SubjectHead=a.expected_head,PinnedOtpCommit=shared.PIN,SourceInputsSha256=digest(manifest),ProfileSha256=digest(t/'profile.json'),ParseGateSha256=digest(gate),BuildReceiptSha256=digest(receipt),ErlPath=str(runtime/'bin/erl'),ErlSha256=digest(runtime/'bin/erl'),ErlcPath=str(runtime/'bin/erlc'),ErlcSha256=digest(runtime/'bin/erlc'),**runtime_provenance))
'''+text[end:]
lines=text.splitlines()
matches=[i for i,line in enumerate(lines) if line.startswith("                expression='O=os:getenv")]
assert len(matches)==1
lines[matches[0]]='''                expression=preloaded.expression_prefix(loads,module,names)+'Rows='+value_expression+',ok=file:write_file(filename:join(O,"returned.etf"),term_to_binary(Rows)),Stacks='+worker+':'+worker_entry+'('+stack+','+entry+'),ok=file:write_file(filename:join(O,"stack-observations.etf"),term_to_binary(Stacks)),halt().' '''.rstrip()
text='\n'.join(lines)+'\n'
start=text.index("                metadata=decoder.decode_exact((directory/'metadata.etf').read_bytes())")
end=text.index("                rows=decoder.decode_exact((directory/'returned.etf').read_bytes())",start)
text=text[:start]+'''                metadata=decoder.decode_exact((directory/'metadata.etf').read_bytes())
                process_receipt=json.loads((directory/'observe.process.json').read_bytes())
                group['MetadataAdmission']=preloaded.validate(metadata,runtime_provenance,runtime,f,ebin,process_receipt['OwnedPid'],profile['Mfas'])
'''+text[end:]
text=text.replace("if f['StackShape']=='depth_cases':","if f['StackShape']=='depth_cases':")
text=text.replace("OriginalBuildsByCollector=0))","OriginalBuildsByCollector=0,SubjectModule='erlang',MetadataPolicy=profile['MetadataPolicy'],TargetMfaCount=351,SelectedMfaCount=len(profile['Mfas']),RemainingTargetMfas=sorted(set(profile['TargetMfas'])-set(profile['Mfas']))))")
text=text.replace("for expected,row in zip(f['Names'],rows[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',expected)","for expected,row in zip(f['Names'],rows[1]):\n                    assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',expected)\n                    preloaded.validate_outcome(row[1][1],False)")
text=text.replace("for name,row in zip(f['Names'],nested[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name)","for name,row in zip(f['Names'],nested[1]):\n                            assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name)\n                            preloaded.validate_outcome(row[1][1],True)")
ast.parse(text)
(TOOL/'collect-original-corrected.py').write_text(text,encoding='utf-8')
print('PASS_SOURCE_ONLY_PRELOADED_COLLECTOR_PREPARATION')
