"""Independent source/transport peer with supplemental raw log and fd marker checks."""
from pathlib import Path
import ast,base64,hashlib,json,re,zipfile,subprocess
R=Path.cwd();A=R/'artifacts';O=A/'ordinary343-peer-independent';O.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
reader=A/'part343-remote-new-review/review-ordinary.py';guard=read(reader.with_name('ordinary-semantic-guard.json'));refusal=read(reader.with_name('ordinary-strict-refusal.json'))
assert sha(reader.read_bytes())==guard['SourceSha256']=='279693F106FD76083EB2EC0058D4A834EE00D34A1B00871E091C6443E6E836B0'
# Re-run reviewed Root-authored passive assertions only, without its exclusive output writes.
source=reader.read_text(encoding='utf-8');prefix=source[:source.index("out=f/'ordinary-semantic-guard.json'")]
scope={'__file__':str(reader)};exec(compile(prefix,'reviewed-root-passive343-source','exec'),scope)
assert scope['result']==guard and refusal['Changes']==scope['changes'] and len(scope['changes'])==1 and not guard['StrictRawEqualityPassed']
d=scope['d'];v=scope['v'];fd=scope['fd'];head=guard['Head'];aid=12077509369;rid=38970910651;jid=117208785362
assert guard['Artifact']==aid and guard['Job']==jid and guard['Run']==rid
# Independently repeat API digest and complete extraction binding without importing helper code.
run=read(d/'run.json');job=read(d/'jobs.json')['jobs'];item=read(d/'artifacts.json')['artifacts'];manifest=read(d/'download-manifest.json')
assert len(job)==len(item)==1 and job[0]['id']==jid and item[0]['id']==aid and item[0]['workflow_run']['id']==rid
assert run['id']==rid and run['head_sha']==head and run['run_attempt']==job[0]['run_attempt']==1 and run['status']==job[0]['status']=='completed' and run['conclusion']==job[0]['conclusion']=='success'
assert item[0]['workflow_run']['head_sha']==head and item[0]['name']=='differential' and not item[0]['expired']
assert len(manifest)==len({r['Path'].replace('\\','/') for r in manifest})
assert {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name not in ['download-manifest.json','run-map.json']}=={r['Path'].replace('\\','/') for r in manifest}
for row in manifest:
 b=(d/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
zpath=d/f'artifact-{aid}.zip';ex=d/f'artifact-{aid}';assert zpath.stat().st_size==item[0]['size_in_bytes'] and item[0]['digest']=='sha256:'+sha(zpath.read_bytes()).lower()
with zipfile.ZipFile(zpath) as z:
 names=z.namelist();assert len(names)==len(set(names));files={n for n in names if not n.endswith('/')};assert files=={p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}
 for name in files:assert z.read(name)==(ex/name).read_bytes()
# Supplemental evidence deliberately omitted by original343 reader: exact job PASS order and OTP source/version.
log=(d/f'job-{jid}.log').read_text(encoding='utf-8-sig');assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M)
expected=[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
assert passes==expected and len(passes)==6270
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 markers=re.findall(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']);assert len(markers)==1
 assert base64.b64decode(markers[0],validate=True)==bytes.fromhex('83680277026f6b7704646f6e65')
for key in ['Complete','VersionVerified']:
 if key in fd:assert fd[key] is True
if 'Baseline' in fd:assert fd['Baseline']=='OTP-29.1.1'
if 'Planned' in fd:assert fd['Planned']==8
if 'Executed' in fd:assert fd['Executed']==8
# Native rows are tied to actual local passed names; expected values remain published constants, not SDK-derived replacements.
local=json.loads(subprocess.check_output(['git','show',head+':docs/validation/part-343/full/tests.json'],cwd=R));localnames={r['Name'] for r in local['Tests'] if r['Status']=='Passed'}
for row in v['ModuleResults'][5318:]:assert 'oracle/compiled-module/'+row['Name'] in localnames
report={'Status':'PASS_INDEPENDENT_PUBLISHED343_ORIGINAL6270_FD8_SOURCE_TRANSPORT_AND_LOG_SUPPLEMENT','Head':head,'Run':rid,'Job':jid,'Artifact':aid,'Passed':6270,'Failed':0,'FdPassed':8,'NewLengthCases':9,'AllOld6261ContractsOrderedAndExactExceptOnePredeclaredMapStdoutVariance':True,'PublishedNewSourceAndGoldenExpectedValuesBound':True,'NineNewLocalPassedCaseNamesBound':True,'RawReportSha256':sha((ex/'differential.json').read_bytes()),'RootSemanticGuardSha256':sha(reader.with_name('ordinary-semantic-guard.json').read_bytes()),'RootStrictRefusalSha256':sha(reader.with_name('ordinary-strict-refusal.json').read_bytes()),'RootReaderSha256':sha(reader.read_bytes()),'TransportApiZipAllMembersIndependentlyRehashed':True,'JobLogExactly6270PassLinesInReportOrder':True,'PinnedOriginalCommitAndVersionObservedInBoundLog':True,'FdEightRawEtfSuccessMarkersDecodedExactly':True,'FdGlobalKeysPresent':sorted(fd.keys()),'StrictRawEqualityPassed':False,'PredeclaredMapStdoutVarianceRows':1,'RawNormalization':False,'ReferenceCoverageComplete':False,'SixExistingOriginalExclusionsPreserved':True,'WholeMfaVerificationClaimed':False,'SdkExecuted':False,'NetworkUsed':False,'MainModified':False,'DownloadedCodeExecuted':False,'PeerMethodDisclosure':'Root-authored passive reader assertions independently source-reviewed and reexecuted without output writes; API ZIP/log/fd/golden local bindings separately checked here.'}
p=O/'report.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Status':report['Status'],'Sha256':sha(p.read_bytes())}))
