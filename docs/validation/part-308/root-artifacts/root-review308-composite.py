from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';p=a/'root-part308-combined-validated1';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
f=p/'validation-freeze.json';assert sha(f.read_bytes())=='E0B165B9DEB90664D3C60D053E3851E5A2EFA47D1B67441C9559309591772455'
rows=read(f)['Files'];assert len(rows)==2189
for row in rows:
 b=(p/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
mp=p/'promotion-map.json';assert sha(mp.read_bytes())=='FE1A95C59252994F213EE7358362D2D757677475147A43E4A889FD69B44E7983'
for row in read(mp):
 b=(r/row['Path']).read_bytes();assert len(b)==row['ResultBytes'] and sha(b)==row['ResultSha256'],row['Path']
v=read(p/'validation-receipt.json');assert (v['FocusExpected'],v['FocusPassed'],v['FocusFailed'],v['BuildWarnings'],v['BuildErrors'])==(25,25,0,0,0)
focus=read(p/'evidence/focus25-report.json');assert focus['Passed']==focus['Expected']==25 and focus['Failed']==0
formatting=read(p/'evidence/format28-guard.json');assert formatting['BeforeSha256']==formatting['AfterSha256']=='4F75E0B31B0D4CFE5E7403579671AE14E0DAAA2F17FB0AFB88A41008769BE8E5'
ledger=read(a/'root-ledger309-independent-source-review.json');assert ledger['Production3ExactAcrossConstantRelocation']
peer312=read(a/'indirect-load-nif312-peer-allocator309-review1.json');assert not peer312['BlockingFindings'] and peer312['ActualRevision4FocusPassed']==24
program=(r/'tests/Erlang.Tests/Program.cs').read_text();assert program.count('Test(Erlang.Tests.NifAllocationLedgerHostNames.Test, Erlang.Tests.NifAllocationLedgerHostChecks.Run);')==1
assert program.count('Test(Erlang.Tests.BeamClosurePurgeHostNames.Test, Erlang.Tests.BeamClosurePurgeHostChecks.Run);')==1
result={'Status':'ROOT_COMPOSITE_SOURCE_FREEZE_2189_AND_29_PROMOTED_PATHS_25_FOCUS_REVIEW_PASS','CompositeFreezeSha256':sha(f.read_bytes()),'PromotionMapSha256':sha(mp.read_bytes()),'AllFrozenMembersVerified':2189,'PromotedPathsVerified':29,'CSharpPaths':28,'FocusedGroupsPassed':25,'FinalLedger3ActualRuntimeHostExecuted':True,'OriginalExecutedByReview':False,'FullExecutedByReview':False,'SourceReview':['BEAM new-fun entry waits for Pending even when invoker is already active; existing continuations remain admitted. Retired is checked before Pending. No lease is reserved for a blocked entry.','Allocation ledger checks counters before native allocation, registers actual pointers before commit, rolls back insertion failure and distinguishes requested bytes from backing request bytes. Reclaim preserves cumulative/peak counters.','Pinned indirect load_nif BIF uses one immutable cached error term, does not invoke the literal instruction handler and ignores argument values after evaluation. Remote exported helper prevents direct-call folding of external fun.','Composite Program retains both new permanent HOST registrations exactly once; source catalog preserves all4917 historical generated source strings and appends seven.'],'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'PendingPurgeSessionComplete':False,'PublicMemoryApisAdded':False}
out=a/'root-part308-independent-source-review.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
