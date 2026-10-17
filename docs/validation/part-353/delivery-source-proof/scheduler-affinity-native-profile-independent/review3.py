import ast,hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'artifacts/scheduler-affinity-native-profile-proposal3'
tool=p/'source/tools/Erlang.SchedulerAffinityOriginalObservations'
def sha(q):return hashlib.sha256(q.read_bytes()).hexdigest().upper()
def load(q):return json.loads(q.read_text(encoding='utf-8-sig'))
assert sha(p/'source-freeze.json')=='0ED32D791D127B18183ACAA21B6B395EF8CCFB82A518CA8251509871D1A74EDF'
f=load(p/'source-freeze.json')
for r in f['Files']:
    q=p/r['Path'];assert q.stat().st_size==r['Bytes'] and sha(q)==r['Sha256'],q
m=load(tool/'manifest.json');assert sha(tool/'manifest.json')=='4C9A79CD6E8BC18F68316D42B2F30DB171E67A242CA81CA84BB8A022E8BA5BFB'
for r in m['Files']:
    q=tool/r['Path'];assert q.stat().st_size==r['Bytes'] and sha(q)==r['Sha256'],q
for r in m['Dependencies']:
    q=root/r['Path'];assert q.stat().st_size==r['Bytes'] and sha(q)==r['Sha256'],q
w=m['Workflow'];assert sha(p/'source'/w['Path'])==w['Sha256']
old=root/'artifacts/scheduler-affinity-native-profile-proposal2/source'
assert (p/'source'/w['Path']).read_bytes()==(old/w['Path']).read_bytes()
for q in tool.glob('*.py'):ast.parse(q.read_bytes(),filename=str(q))
for r in m['Cases']:
    assert sha(tool/r['Source'])==r['SourceSha256']
    assert (tool/r['Source']).read_bytes()==(root/'artifacts/scheduler-affinity352-proposal6/portable'/(r['Module']+'.erl')).read_bytes()
controls=load(p/'controls2.exit.json');assert controls['ExitCode']==0 and controls['Settled']
result={
 'Status':'PASS_SOURCE_ONLY_ORIGINAL_PROFILE3_WITH_CONFIRMED_PROFILE2_CORRECTIONS',
 'Passed':True,'SourceFreezeSha256':sha(p/'source-freeze.json'),'SourceManifestSha256':sha(tool/'manifest.json'),
 'FrozenFilesVerified':len(f['Files']),'ToolFilesVerified':len(m['Files']),'WorkflowRawUnchanged':True,
 'FiveFixturesRawExactFocused6':True,
 'Corrections':[
  {'Finding':'Tracked directory/dangling symlink inventory','ReviewedImplementation':'git ls-files --stage -z, mode/stage validation;120000 requires is_symlink and os.fsencode(os.readlink(path)), never Path.read_bytes;100644/100755 require nonsymlink regular file and raw bytes; before/after exact mode/path/size/hash rows.',
   'Controls':'Three fake-FS link payloads drive actual source_file_identity branch and throw if target read attempted; two raw regular modes; three invalid mode/stage refusals. These are synthetic branch controls, not actual Linux symlink execution.'},
  {'Finding':'ERL_ROOTDIR override could select unsealed runtime','ReviewedImplementation':'Shared sanitized_environment removes ERL_ROOTDIR plus five prior ERL settings for BOTH owned build and each VM. Expected runtime root injected only after cleanup. VM records raw runtime-root.etf with expected/actual binary strings from filename:absname(code:root_dir()), asserts equality before profile/compile; Python exact ETF admission repeats both equal sealed resolved installation root.',
   'PinnedEvidence':'erts/etc/unix/erl.src.src explicit ERL_ROOTDIR branch independently inspected; mode120000 pinned paths independently inspected.'}],
 'UnchangedReviewedGates':['Exact reviewed manifest/subject/workflow/helper hashes','Owned bounded configure/make/install normal receipts','Pinned clean tracked sourcebefore/after and installed binary/BEAM/version hashes','Explicit PREFIX/lib/erlang','Five fresh +S2:2 VMs with actual profile2/2','Loaded fixture module_info(md5)==beam_lib MD5','Both termination and remaining-group-kill flags false; reaped/streamsclosed/exit0/no cleanupfailure','Always upload partial failure evidence'],
 'PriorAdmission':'Report2 SHA9050 and correction remain immutable; prior admission was superseded by confirmed preparation/provenance blockers, not native guest RED.',
 'BlockersFound':[],
 'RemainingGates':['Publication/exact workflow head and authorized dispatch','Actual source build and five original captures','Independent transport/raw ETF/source/build/cleanup semantic audit'],
 'SdkExecuted':False,'NativeExecuted':False,'NetworkExecuted':False,'MainModified':False}
out=Path(__file__).with_name('report3.json');assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Status':result['Status'],'Report':str(out),'Sha256':sha(out)}))
