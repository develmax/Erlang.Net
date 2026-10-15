from pathlib import Path
import base64,hashlib,json,re,subprocess,zipfile
r=Path.cwd();a=r/'artifacts';head='9fbe77098e2e7939d0b9fbfd41e6cfc481567865';rid=38685834317;pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
d=a/f'root-ordinary289-{rid}-terminal';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));h=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p]);text=lambda p:blob(p).decode()
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert len(jobs)==len(items)==1;job=jobs[0];item=items[0]
assert run['id']==rid and run['head_sha']==head and run['name']=='Pinned OTP differential oracle' and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success'
assert job['id']==116260327745 and job['run_id']==rid and job['head_sha']==head and job['status']=='completed' and job['conclusion']=='success' and len(job['steps'])==10 and all(x['conclusion']=='success' for x in job['steps'])
assert item['id']==11949943361 and item['workflow_run']['head_sha']==head and not item['expired']
z=d/f"artifact-{item['id']}.zip";ex=d/f"artifact-{item['id']}";assert z.stat().st_size==item['size_in_bytes'] and h(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as zip:
 assert len(zip.namelist())==3 and set(zip.namelist())=={'differential.json','differential.progress.json','fd-differential.json'}
 for name in zip.namelist():assert zip.read(name)==(ex/name).read_bytes()
manifest=j(d/'download-manifest.json');actual={x.relative_to(d).as_posix() for x in d.rglob('*') if x.is_file() and x.name!='download-manifest.json'}
assert actual=={x['Path'].replace('\\','/') for x in manifest} and len(actual)==len(manifest)
for x in manifest:
 p=d/x['Path'];assert p.stat().st_size==x['Bytes'] and h(p.read_bytes())==x['Sha256']
v=j(ex/'differential.json');oldpath='docs/validation/part-282/original-280/part-280-differential/differential.json';old=json.loads(blob(oldpath));fd=j(ex/'fd-differential.json');ofd=json.loads(blob('docs/validation/part-282/original-280/part-280-differential/fd-differential.json'))
assert v['Planned']==v['Executed']==v['Passed']==5836 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
for key,count in [('Results',778),('ModuleResults',4895),('DiagnosticResults',163)]:assert len(v[key])==count and all(x['Passed'] for x in v[key])
for key in ['Results','DiagnosticResults','OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation']:assert v[key]==old[key]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
for key,count in [('ExpressionTrack',778),('CompiledModuleTrack',4895),('CompilerDiagnosticTrack',163)]:
 t=v[key];assert t['Planned']==t['Executed']==t['Passed']==count and t['Failed']==0
assert v['ExpressionTrack']['Mode']=='erl_eval:exprs'
assert v['CompiledModuleTrack']['ReferenceMode']==old['CompiledModuleTrack']['ReferenceMode'] and v['CompiledModuleTrack']['ImplementationMode']==old['CompiledModuleTrack']['ImplementationMode'] and v['CompilerDiagnosticTrack']['ReferenceMode']==old['CompilerDiagnosticTrack']['ReferenceMode']
for row in v['ModuleResults']+v['DiagnosticResults']:assert h(row['Source'].encode())==row['SourceSha256']
assert all(x['Actual']==x['Expected'] for x in v['Results']+v['ModuleResults'])
assert all(x['Reference']==x['Expected'] for x in v['DiagnosticResults'])
om={x['Name']:x for x in old['ModuleResults']};nm={x['Name']:x for x in v['ModuleResults']};names=[x['Name'] for x in v['ModuleResults']];assert len(nm)==4895 and len(om)==4879
strict_differences=[{'Name':name,'Historical':value,'Actual':nm.get(name)} for name,value in om.items() if nm.get(name)!=value]
assert not strict_differences, 'Preserve and investigate strict historical differences; do not normalize'
assert [x for x in names if x in om]==list(om)

def constants(path):return {n:json.loads(s) for n,s in re.findall(r'const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")',text(path))}
def declarations(path):return [(json.loads(n),json.loads(b)) for n,b in re.findall(r'Add\(\s*("(?:[^"\\]|\\.)*")\s*,\s*("(?:[^"\\]|\\.)*")',text(path))]
# Source-derived construction, not expected values copied from native output.
c=constants('tools/Erlang.Differential/DistributionInputCaseNames.cs');decl=declarations('tools/Erlang.Differential/DistributionInputCases.cs');assert len(decl)==10
expected_inputs=[('atom-nil-direct','[]'),('atom-empty-binary-apply','<<>>'),('atom-nested-iolist-direct','[1,[2,<<3,4>>],[]]'),('atom-binary-tail-iolist-apply','[1|<<2,3>>]'),('atom-out-of-range-iolist-direct','[256]'),('atom-improper-iolist-apply','[1|2]'),('atom-integer-direct','17'),('atom-nonbinary-bitstring-apply','<<1:1>>'),('ordinary-reference-nil-direct','[]'),('ordinary-reference-iolist-apply','[1,<<2>>]')]
assert decl==expected_inputs
new=[];expected_rows=[]
for name,data in decl:
 reference=name.startswith('ordinary-reference');dynamic=name.endswith('apply');handle_source='make_ref()' if reference else c['InvalidHandle'];handle=c['OrdinaryReference'] if reference else c['InvalidHandle']
 source=c['ModulePrefix']+'oracle_dist_input:invoke(erlang,dist_ctrl_put_data,'+handle_source+','+data+'). '+('invoke(M,F,H,D)->try ' if dynamic else 'invoke(_M,_F,H,D)->try ')+(c['DynamicCall'] if dynamic else c['DirectCall'])+' of V->{unexpected_success,V} catch C:R:Stack->[{M1,F1,Args,_Annotations}|_]=Stack,'+(c['ReferenceProjection'] if reference else c['ExactProjection'])+' end.'
 expected='{ok,{error,badarg,{erlang,dist_ctrl_put_data,['+handle+','+data+']}}}'
 expected_rows.append((c['Prefix']+name,source,expected))
c=constants('tools/Erlang.Differential/ApplyFrameCaseNames.cs');decl=declarations('tools/Erlang.Differential/ApplyFrameCases.cs');assert len(decl)==6
bad='{error,badarg,{erlang,hd,[not_a_list]}}';undef='{error,undef,{missing_apply_frame_module,absent,[17]}}';trace='[{synthetic_module,synthetic_function,[17],[]}]'
expected_apply=[('badarg-direct-and-dynamic','{ok,{'+bad+','+bad+'}}'),('undef-direct-and-dynamic','{ok,{'+undef+','+undef+'}}'),('invalid-module-keeps-apply-frame','{ok,{error,badarg,{erlang,apply,[0,hd,[]]}}}'),('invalid-function-keeps-apply-frame','{ok,{error,badarg,{erlang,apply,[erlang,0,[]]}}}'),('improper-arguments-keeps-apply-frame','{ok,{error,badarg,{erlang,apply,[erlang,hd,[1|2]]}}}'),('explicit-raise-preserves-all-classes-and-empty-stack','{ok,{'+','.join('{'+cl+',reason,'+st+'}' for st in [trace,'[]'] for cl in ['error','throw','exit'])+'}}')]
assert [n for n,b in decl]==[n for n,e in expected_apply]
for (name,body),(_,expected) in zip(decl,expected_apply):expected_rows.append((c['Prefix']+name,c['ModulePrefix']+body+'.'+c['Helpers'],expected))
for name,source,expected in expected_rows:
 row=nm[name];assert row['Source']==source and row['SourceSha256']==h(source.encode()) and row['Expected']==row['FixtureExpected']==row['Actual']==expected
 assert row['Passed'] and row['CallerContext']=='UntrappedFresh' and row['CompilerPolicy']=='Default' and row['StandardStreamsMatch'] and row['NativeAndSdkStandardOutputExactlyMatch']
 assert all(row[k] is None for k in ['ExpectedStandardOutputHex','ExpectedStandardOutputAlternatives','ExpectedStandardErrorHex','NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
 assert re.fullmatch('[0-9A-F]{64}',row['CSharpSha256'])
 new.append({'Name':name,'SourceSha256':row['SourceSha256'],'Expected':expected,'CallerContext':row['CallerContext'],'CompilerPolicy':row['CompilerPolicy']})
assert [x for x in names if x not in om]==[x['Name'] for x in new]
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==ofd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
log=d/f"job-{job['id']}.log";raw=log.read_bytes();txt=raw.decode('utf-8-sig');assert head in txt and pin in txt and 'checking OTP version... 29.1.1' in txt
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',txt,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x for x in names]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
assert 'Compare the supported language corpus' in txt and 'Compare inherited descriptor IO against the original fd driver' in txt
progress=j(ex/'differential.progress.json');assert progress['CompletedComparisons']==5672 and progress['Source']==v['ModuleResults'][-1]['Source'] and progress['NativeEvidence']['ExitCode']==0 and progress['NativeEvidence']['FixtureName']==names[-1] and progress['NativeEvidence']['Stage']=='managed module execution' and progress['NativeEvidence']['StandardError']==''
paths=['tools/Erlang.Differential/DistributionInputCases.cs','tools/Erlang.Differential/DistributionInputCaseNames.cs','tools/Erlang.Differential/ApplyFrameCases.cs','tools/Erlang.Differential/ApplyFrameCaseNames.cs','tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/OriginalOtpDefects.cs','.github/workflows/differential.yml']
bindings=[{'Path':p,'Sha256':h(blob(p)),'Bytes':len(blob(p))} for p in paths]
result={'Status':'PASS_ACTUAL_ORIGINAL5836_AND_FD8_EXACT_HEAD289','Head':head,'PinnedCommit':pin,'Run':rid,'Job':job['id'],'Artifact':item['id'],'Passed':5836,'Failed':0,'Expressions':778,'CompiledModules':4895,'Diagnostics':163,'HistoricalModuleRows':4879,'AllHistoricalRowsExact':True,'HistoricalExpressionsDiagnosticsSixExclusionsAndContextExact':True,'RawHistoryDifferences':strict_differences,'FdPassed':8,'FdHistoricalRowsExact':True,'NewSixteen':new,'NewSixteenSourcesAndIndependentExpectedTermsMatch':True,'RawZipSha256':h(z.read_bytes()),'RawJobSha256':h(raw),'ReportSha256':h((ex/'differential.json').read_bytes()),'CommittedSourceBindings':bindings,'ProgressScope':'5672 is final compiled precomparison snapshot; terminal5836 report and ordered raw5836 PASS lines establish whole comparison count.','NativeNifSevenPassed':False,'ReferenceCoverageComplete':False,'MainEdited':False,'GitEdited':False,'NativeRerun':False}
out=a/'nif-boundary-original289-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Guard':str(out),'Sha256':h(out.read_bytes()),'Passed':5836,'FdPassed':8,'New16':16,'StrictHistoricalDifferences':0}))
