"""Bounded source and retained evidence review, no execution or Main edits."""
from pathlib import Path
import hashlib,json
r=Path.cwd();b=r/'artifacts/nif-actual-beam-caller310-validated1';j=lambda p:json.loads(p.read_bytes());h=lambda data:hashlib.sha256(data).hexdigest().upper()
freeze=b/'validation-freeze.json';mp=b/'promotion-map.json'
assert h(freeze.read_bytes())=='6C9F0AD268237E161147AD457E9D02F1DF30056634F3A874F37F38321B4EEBAA'
assert h(mp.read_bytes())=='79A1571B2C56892FCB3F756E22A676333FE907D3AD4843A221D91D26BDA5CDDC'
for row in j(freeze)['Files']:
 data=(b/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256']
mapping=j(mp);assert (mapping['SourcePaths'],mapping['CSharpPaths'],mapping['SourceRuntimePaths'])==(9,8,5)
for row in mapping['Paths']:
 data=(b/'overlay'/row['Path']).read_bytes();assert len(data)==row['ResultBytes'] and h(data)==row['ResultSha256']
before=b/'tokens8-final-before.json';after=b/'tokens8-final-after.json'
assert before.read_bytes()==after.read_bytes() and h(before.read_bytes())=='64BD5D8C6B9030DA7B4403323794C470069A0CC5D6A9105AEF7F87DBB840E237'
pin_records=[]
for relative,prefix,scheme in [('pinned-evidence/nif-start-pinned-proof.json','pinned-evidence',True),('original-and-pin-evidence/pinned-source-proof.json','original-and-pin-evidence',False)]:
 for row in j(b/relative):
  name=row['GitPath'].replace('/','_') if scheme else ('pinned-ops.tab' if row['GitPath'].endswith('/ops.tab') else 'pinned-bif_instrs.tab')
  data=(b/prefix/name).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256']
  assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==row['GitBlob']
  if not scheme:assert row['PinnedCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
  pin_records.append({'Path':row['GitPath'],'GitBlob':row['GitBlob'],'Sha256':h(data)})
text=lambda name:(b/name).read_text(encoding='utf-8-sig')
assert 'vi(nif_start, Vst) ->\n    Vst;' in text('pinned-evidence/lib_compiler_src_beam_validator.erl')
assert 'cg_instr(nif_start, [], _Dst) ->\n    [nif_start];' in text('pinned-evidence/lib_compiler_src_beam_ssa_codegen.erl')
loader=text('pinned-evidence/erts_emulator_beam_emu_emu_load.c');assert 'case op_nif_start:' in loader and 'are_nifs[stp->function_number-1] = 1;' in loader and 'ci -= 1;' in loader
ops=text('original-and-pin-evidence/pinned-ops.tab');assert 'call_ext_last u==2 u$func:erlang:load_nif/2' in ops and 'deallocate_return' in ops
assert 'nif_start() {\n}' in text('original-and-pin-evidence/pinned-bif_instrs.tab')
interpreter=text('overlay/src/Erlang.Runtime/BeamRegisterInterpreter.cs')
assert interpreter.index('if (!sourceLoadNif)')<interpreter.index('Term imported = await BeamExternalImports.Invoke')<interpreter.index('if (sourceLoadNif && externalLastFrame is not null)')
host=text('overlay/tests/Erlang.Tests/SourceLoadNifHostChecks.ActualBeam.cs')
for clause in ['foreach (bool registered in new[] { false, true })','Require(undefined)','Refused(capability, context)','await Task.Yield()','ReferenceEquals(caller.Owner, expectedOwner)','ReferenceEquals(caller.Image, expectedImage)','Require(ownedFailure','Require(managedOwner.Calls == 0)']:
 assert clause in host
v=j(b/'validation-result.json');assert v['TargetWarnings']==v['TargetErrors']==v['TargetExitCode']==0 and v['FocusedGroups']==16 and v['NestedActualOwnerAndOnLoadRows']==12 and not v['NativeExecuted'] and not v['OriginalExecuted'] and not v['PublicLoadNifActivated']
log=text('focus16-final.log');assert len([line for line in log.splitlines() if line.startswith('PASS ')])==28
result={'Status':'BOUNDED_INDEPENDENT_SOURCE_AND_RETAINED_EVIDENCE_PASS','CandidateFreezeSha256':h(freeze.read_bytes()),'PromotionMapSha256':h(mp.read_bytes()),'FrozenFilesVerified':len(j(freeze)['Files']),'SourcePaths':9,'CSharpPaths':8,'RuntimePaths':5,'Tokens8Exact':True,'PinnedCompilerValidatorLoaderMarkerRolesVerified':True,'PinnedGitBlobIdentities':pin_records,'ActualArchivedCallExtOnlyManagedHostProven':True,'CallExtAndCallExtLastActualExecutionProven':False,'OriginalNifEligibilityMetadataParity':False,'PendingPurgeAndFunFenceCompleted':False,'PublicLoadNifActivated':False,'GeneralNativeAbiComplete':False,'NewNativeSourceInputsExpected':1013,'FullCasesExpectedUnchanged':6627,'BuildExecutedByReviewer':False,'NativeExecutedByReviewer':False,'OriginalExecutedByReviewer':False,'MainEdited':False,'GitMutated':False,'Bounds':['nif_start is admitted as bounded marker; native loader eligibility bitmap metadata is not implemented.','call_ext_last ordering is source-guided only; preserved real fixture executes call_ext_only.','Managed image binding installation through actual load/2 is not native shared-library activation.']}
out=r/'artifacts/actual-beam310-independent-peer-review.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'Status':result['Status'],'FrozenFiles':result['FrozenFilesVerified']}))
