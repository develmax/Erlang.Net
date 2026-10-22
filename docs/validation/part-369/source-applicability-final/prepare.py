from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[2];base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(n,x):(base/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8',newline='\n')
old=root/'artifacts/binary369-final-applicability-peer1';guards=root/'artifacts/binary369-reviewed-guards-proposal1/guarded-files-proposal.json'
g=json.loads(guards.read_bytes())['VerifiedSourceFiles'];assert len(g)==450 and all(sha(root/r['Path'])==r['Sha256'] for r in g)
oldrows=json.loads((old/'source-bindings.json').read_bytes())['Files']
deltas=[{'Path':r['Path'],'Before':r['Sha256'],'After':sha(root/r['Path'])} for r in oldrows if sha(root/r['Path'])!=r['Sha256']]
assert [r['Path'] for r in deltas]==['src/Erlang.Runtime/BinaryReplacePlan.cs']
per=json.loads((old/'per-mfa.json').read_bytes())
for e in per['Entries']:
 e['Eligibility']='ELIGIBLE_AFTER_ROOT_FINAL_GATES; NOT_PROMOTED_BY_THIS_REPORT'
 if e['Mfa'] in ['binary:replace/3','binary:replace/4']:
  e['FinalSourceDelta']='Integer insert first pins replacement at native match boundary. Binary replacement plus insert_replaced=true creates deferred badfun only if a match invokes it; no-match returns subject. Fun replacement ignores insert as before. All resulting public catch/rethrow fields remain source-authentic; new7 original witnesses required.'
assert len(per['Entries'])==31
save('per-mfa-final.json',per)
proof=root/'artifacts/binary369-retained-comparison-peer1/attempt3/comparison.json';p=json.loads(proof.read_bytes());assert p['Observations']==8117 and not p['Unexpected'] and len(p['NegativeControls'])==68 and all(x['Rejected'] for x in p['NegativeControls'])
save('eligibility-addendum.json',{
 'Status':'SOURCE_ELIGIBILITY_AFTER_FINAL_GATES; ROOT_DISCHARGES_ACTUAL_EVIDENCE_NO_PROMOTION_HERE',
 'OtpCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','Mfas':31,'Families':{'primitive':18,'search':7,'wrapper':6},
 'PreviousAuditSha256':sha(old/'source-freeze.json'),'ReviewedGuards':450,'GuardProposalSha256':sha(guards),'CurrentAll450Exact':True,
 'Old64SourceChanges':deltas,'UnsignedFinalSource':{'Path':'src/Erlang.Runtime/BinaryUnsigned.cs','Sha256':sha(root/'src/Erlang.Runtime/BinaryUnsigned.cs'),'Proof':'Compute unsigned byteCount once and reject output above genuine own BitLength binary limit before allocation/overflow. Valid byte-order/minimal-zero branches unchanged.'},
 'RetainedReplay':{'Report':proof.relative_to(root).as_posix(),'Sha256':sha(proof),'Observations':8117,'Strict':7683,'WholeOrSourceBound':8087,'Unexpected':0,'Controls':68,'NativeOnlyDiagnostics':18,'OwnTopologyRows':12,'OwnTopologyMeaning':'Two raw atom/tuple cursor inputs across value+5depths. Native literal topology is not the approved own-state equivalence criterion; source hashing/cursor algorithms remain unchanged, actual atom indices may change after module registration. Require canonical own-state HOST controls; do not rewrite the strict failures or silently call them literal matches.'},
 'CurrentMainClosure':'Use Root final2 primary/closure7 and final canonical evidence. Retained replay uses its explicitly older immutable five-DLL closure plus reviewed fix6 source-subset argument, not a claim it executed finalDLLs.',
 'SourceProtocolPerMfa':'per-mfa-final.json preserves all31 type/clause/domain/order/error/caller/resource/storage partitions. Actual state is own genuine registry/backing/layout; no captured native IDs/capacity/partitions injected.',
 'PendingRootDischarge':['wrapper certificate including new7 original/fullfield source-counterexample witnesses','final primary3510 plus closure14/backing and sharing wholefield/source-bound admission','final canonical7639 exact names/outcomes/receipts/source stability, including mandatory own hash/cursor HOST checks','retained76 source/delta proof plus this wholefamily replay;16 unchanged CoreModules bodies separately accounted','final450guards/source plan to actual final build/DLL evidence'],
 'Boundaries':['No native heap address/absolute allocator-capacity/timing identity claim','No general compiler private_append/SSA sizing optimizer completeness claim','No whole port/NIF/compiler infrastructure promotion','Genuine own-state protocol still mandatory; missing event proof cannot be waived as VM-dependent','Finite observations alone never establish full source contract'],
 'MainMutatedByReviewer':False,'CompatibilityPromoted':False,'ImplementationPublished':False,
})
print('final source450exact; all31 eligibility conditional; one reviewed source delta')
