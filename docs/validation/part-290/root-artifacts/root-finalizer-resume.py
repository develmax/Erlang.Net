from pathlib import Path
import argparse,hashlib,json,shutil,subprocess

# PROPOSAL ONLY: author never executes this finalizer. Root must review every input.
p=argparse.ArgumentParser();p.add_argument('--ordinary-guard',required=True);p.add_argument('--ordinary-staging',required=True);p.add_argument('--ordinary-summary',required=True);args=p.parse_args()
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-290';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());f=read(a/'root-part290-final-source-freeze-revision2.json');parent=f['ParentHead'];assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip()==parent
assert len(f['Files'])==30
for row in f['Files']:assert sha((r/row['Path']).read_bytes())==row['Sha256'] and (r/row['Path']).stat().st_size==row['Bytes']
# Production/test/solution and registry remain exactly published289.
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','Erlang.Net.slnx','docs/supported-mfas.json'],cwd=r).strip()
prior=read(r/'docs/validation/part-289/evidence-receipt.json');assert prior['Passed']==6588 and prior['RegisteredMfas']==502
closed=r/'docs/changelog/0043.md';closedsha=sha(closed.read_bytes());assert closedsha=='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1'
entry=(a/'spawn-protocol-part290-entry-draft.md').read_text(encoding='utf-8-sig');summary=Path(args.ordinary_summary).read_text(encoding='utf-8-sig').strip();assert summary and '5836' in summary and '8/8' in summary and '@@' not in summary;assert '@@' not in entry
ordinaryguard=Path(args.ordinary_guard).resolve();guard=read(ordinaryguard)
# Schema-independent refusal: root supplies independently reviewed PASS guard and exact summary.
assert guard['Status']=='PASS_ACTUAL_ORIGINAL5836_AND_FD8_EXACT_HEAD289' and guard['Passed']==5836 and guard['Failed']==0 and guard['FdPassed']==8 and guard['CompiledModules']==4895 and guard['Diagnostics']==163 and guard['Expressions']==778 and guard['AllHistoricalRowsExact'] and guard['HistoricalExpressionsDiagnosticsSixExclusionsAndContextExact']
ci=read(a/'nif-boundary-ci289-38685817020-independent-guard.json');assert ci['Passed']==6588 and ci['Failed']==0 and ci['OrderedNamesExact']
assert read(a/'root-part290-tool-native-build-guard.json')['DllSha256']=='3087CC0FD9008B59FAA2A0DF0C4F4010D548194070BB3B9D86372609B3A9E022'
assert read(a/'root-part290-tool-tokens-before.json')==read(a/'root-part290-tool-tokens-after.json')
assert 'Предупреждений: 0' in (a/'root-part290-tool-formatted-target.log').read_text() and 'Ошибок: 0' in (a/'root-part290-tool-formatted-target.log').read_text()
assert (a/'root-part290-tool-managed.log').read_text().strip()=='NIF internal checks passed; native fixture NOT executed.'
assert (a/'root-part290-tool-native.log').read_text().strip()=='NIF internal and native fixture checks passed.'
assert 'Предупреждений: 0' in (a/'root-part290-semantic-constants-target.log').read_text() and 'Ошибок: 0' in (a/'root-part290-semantic-constants-target.log').read_text()
assert (a/'root-part290-semantic-constants-managed.log').read_text().strip()=='NIF internal checks passed; native fixture NOT executed.'
assert (a/'root-part290-semantic-constants-native.log').read_text().strip()=='NIF internal and native fixture checks passed.'
assert read(a/'root-part290-semantic-constants-revision.json')['CoreChecksExpandedIntegerValuesExact']
assert o.exists() and not (o/'evidence-receipt.json').exists(), 'Resume only the inspected incomplete archive'
def copy(source,destination,expected=None):
 raw=source.read_bytes()
 if expected:assert sha(raw)==expected.upper(),source
 destination.parent.mkdir(parents=True,exist_ok=True)
 if destination.exists():assert destination.read_bytes()==raw
 else:destination.write_bytes(raw)
for row in read(a/'spawn-protocol-part290-archive-plan.json')['Files']:
 assert (r/row['SourcePath']).stat().st_size==row['Bytes'];copy(r/row['SourcePath'],r/row['Destination'],row['Sha256'])
for stagingpath in [a/'nif-boundary-part290-ci289-38685817020-evidence-staging/manifest.json',Path(args.ordinary_staging).resolve()]:
 staging=read(stagingpath)
 for row in staging['Files']:
  destination=r/row.get('Destination',row.get('TargetPath',''));assert destination.is_relative_to(o)
  copy(stagingpath.parent/row['Destination'],destination,row['Sha256'])
 copy(stagingpath,o/'staging-manifests'/stagingpath.parent.name/'manifest.json')
copy(ordinaryguard,o/'ordinary289-independent-guard.json')
copy(Path(__file__),o/'root-artifacts/root-finalizer-resume.py')
copy(a/'root-stage290-guard.py',o/'root-artifacts/root-stage290-guard.py')
for source in sorted(a.glob('root-part290-*')):
 if source.is_file():copy(source,o/'root-artifacts'/source.name)
copy(a/'spawn-protocol-part290-semantic-constant-review.json',o/'source-reviews/semantic-constant-review.json')
for row in f['Files']:copy(r/row['Path'],o/'tool-source'/row['Path'],row['Sha256'])
copy(a/'root-part290-final-source-freeze-revision2.json',o/'source-freeze.json')
copy(a/'root-part290-final-source-freeze.json',o/'source-freeze-prior-revision1.json')
closure=[];output=r/'tools/Erlang.NifFixtureHost/bin/Debug/net10.0'
for source in sorted(output.rglob('*')):
 if source.is_file():
  raw=source.read_bytes();digest=sha(raw);blob=o/'tool-output/blobs'/digest;copy(source,blob,digest);closure.append({'Path':source.relative_to(output).as_posix(),'Bytes':len(raw),'Sha256':digest,'RepositoryBlob':blob.relative_to(r).as_posix()})
assert closure; (o/'tool-output-closure.json').write_text(json.dumps(closure,indent=2)+'\n')
volume=r/'docs/changelog/0044.md';combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode();assert len(combined)<=32768 and len(combined.splitlines())<=400;assert b'## Part 290' not in volume.read_bytes();volume.write_bytes(combined)
index=r/'CHANGELOG.md';raw=index.read_bytes();old='part289 (active)'.encode();assert raw.count(old)==1;index.write_bytes(raw.replace(old,'parts289–290 (active)'.encode()))
note='Part290: the standalone trusted NIF fixture host builds with zero warnings/errors and passes managed ownership plus real Windows x64 native fixture checks. Thirty tool/workflow source paths, with no runtime, compiler, test, MFA or solution changes. Part 289 Main 6588/full, exact-head CI 6588 and original OTP 5836/5836 plus fd 8/8 are independently archived; no repeated Main full run is claimed. Registry 502 and erlang 344/351 remain unchanged. Seven original NIF observations and Linux process controls remain pending. General NIF/load_nif, all-writer pressure/EXITING, physical resources and whole erlang remain unfinished.'
for name in ['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']:
 q=r/name;data=q.read_bytes();boundary=__import__('re').search(br'\r?\n\r?\n',data);assert boundary;newline=b'\r\n' if b'\r\n' in data else b'\n';cut=boundary.end();q.write_bytes(data[:cut]+note.encode()+newline+newline+data[cut:])
scopepath=r/'docs/module-readiness-scope.json';scope=read(scopepath);scope['ComparisonTestReport']='validation/part-289/tests.json';assert not any(c['Id']=='NifFixtureHost' for c in scope['Components']);scope['Components'].append({'Id':'NifFixtureHost','Project':'tools/Erlang.NifFixtureHost','Status':'Partially compatible','Implemented':['Standalone target with zero warnings/errors; strict 17-file token/quoted-literal formatting guard; managed ownership checks and actual reviewed Windows x64 seven-callback fixture execution. These are tool gates, not additional Main tests.'],'Remaining':['Original seven NIF observations and Linux collector controls pending; arbitrary NIF ABI, native threads, dirty jobs, on_load, source/BEAM replacement and registry/purge integration unfinished.'],'TestPrefixes':[]});scopepath.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
# Historical6588 input is deliberately reused; this readiness command runs no tests.
subprocess.run(['dotnet','run','--project','tools/Erlang.Tool','--no-build','--','readiness','docs','artifacts/tests.json','artifacts/readiness290'],cwd=r,check=True,timeout=120)
ready=read(a/'readiness290/module-readiness.json');assert ready['LocalPassedTests']==6588 and ready['RegisteredMfas']==502 and ready['PreviousRegisteredMfas']==502 and ready['RegisteredMfaDelta']==0
for name in ['module-readiness.json','MODULE_READINESS.md']:
 shutil.copyfile(a/'readiness290'/name,r/'docs'/name)
 copy(a/'readiness290'/name,o/name)
q=r/'docs/dependency-decisions.md';q.write_bytes(q.read_bytes()+b'\nPart290: standalone trusted fixture host uses existing runtime and BCL NativeMemory/NativeLibrary; no new dependency, generic NIF registration, license banner, public package release or LICENSE.local.txt publication. Pinned OTP headers and attribution remain consolidated.\n')
q=r/'docs/OTP-PORTS.md'
q.write_bytes(q.read_bytes()+b'\n## Part 290: trusted NIF ABI prerequisite\n\nPinned erl_nif.h, erl_nif_api_funcs.h and erl_drv_nif.h define the native entry/function structs and Windows callback table. tools/Erlang.NifFixtureHost preserves all 180 callback positions and two trailing pointers; seven callbacks used by the unchanged reviewed C fixture are implemented. CLR adaptations use opaque environment addresses, rooted managed term handles, explicit native-allocation ownership and weak retired-image markers with once-only unload leases. This is a standalone source-guided ABI prerequisite, not a port of the complete native loader, BEAM heap, registry transaction or purge lifecycle. The original seven-scenario observation collector is prepared; native original execution and full NIF API compatibility remain pending.\n')
q=r/'.gitattributes';line=b'docs/validation/part-290/** binary\n';assert line.strip() not in q.read_bytes();q.write_bytes(q.read_bytes()+line)
assert sha(closed.read_bytes())==closedsha
receipt={'Status':'STANDALONE_TOOL_TARGET_MANAGED_AND_REVIEWED_NATIVE_PASS_NO_NEW_MAINFULL','ParentHead':parent,'SourcePaths':30,'ProductionChanges':0,'DefaultMfas':502,'ErlangMfas':344,'ErlangReference':351,'Historical289MainPassed':6588,'Historical289Full':True,'CI289Passed':6588,'Ordinary289Guard':guard,'Ordinary289Passed':5836,'Ordinary289FdPassed':8,'NifOriginal7Executed':False,'LinuxCollectorControlsExecuted':False,'MainFullRepeated':False,'WholeCompatibility':False,'Closed0043Sha256':closedsha}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':sha(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file() and q.name!='evidence-receipt.json'];(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print('Finalized standalone Part290',len(receipt['Files']),'proof files; no Mainfull claim')
