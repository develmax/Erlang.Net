from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/erlang371-registration-source-review1'
OUT.mkdir(exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def digest(b): return hashlib.sha256(b).hexdigest().upper()
def dump(p,o): p.write_text(json.dumps(o,indent=2)+'\n',encoding='utf-8')

baseproof=ROOT/'artifacts/erlang371-contract-audit1'
ledgerpath=ROOT/'docs/mfa-compatibility.json'
ledger=json.loads(ledgerpath.read_text())
assert len(ledger['VerifiedSourceFiles'])==462
guards={r['Path']:r['Sha256'] for r in ledger['VerifiedSourceFiles']}
corepath=ROOT/'src/Erlang.Runtime/CoreModules.cs'
core=corepath.read_bytes()
insertion=b'        OrdsetCoreModule.Register(r);\n'
assert core.count(insertion)==1
oldcore=core.replace(insertion,b'')
assert digest(oldcore)=='A538B78EE3F0B0E8A206C69BD0B6A3883F6A8B2F2B310A951C64E4382DBE788A'
assert b'        QueueCoreModule.Register(r);\n'+insertion in core
(OUT/'CoreModules.pre-registration.cs.txt').write_bytes(oldcore)
(OUT/'CoreModules.final.cs.txt').write_bytes(core)

oldbindings=json.loads((baseproof/'source-bindings.json').read_text())
newbindings=[]
bindingchanges=[]
for r in oldbindings['Files']:
    current={'Path':r['Path'],'Sha256':sha(ROOT/r['Path']),'Bytes':(ROOT/r['Path']).stat().st_size}
    if current['Sha256']!=r['Sha256']:
        bindingchanges.append({'Path':r['Path'],'Before':r['Sha256'],'After':current['Sha256']})
    newbindings.append(current)
assert len(bindingchanges)==1 and bindingchanges[0]['Path']=='src/Erlang.Runtime/CoreModules.cs'

def tokens(p):
    text=p.read_text(encoding='utf-8-sig')
    pattern=r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[^\s]'
    return [t for t in re.findall(pattern,text) if not t.startswith('//') and not t.startswith('/*')]

ordfiles=[]
for p in sorted((ROOT/'src/Erlang.Runtime').glob('OrdsetCore*.cs')):
    rel=str(p.relative_to(ROOT)).replace('\\','/')
    proposal=ROOT/'artifacts/next-api371-proposal1/source'/rel
    assert tokens(proposal)==tokens(p),rel
    ordfiles.append({'Path':rel,'Sha256':sha(p),'Bytes':p.stat().st_size,'ProposalSha256':sha(proposal),'TokenEquivalent':True})
assert len(ordfiles)==4

guardchanges=[]
for rel,before in guards.items():
    actual=sha(ROOT/rel)
    if actual!=before:
        guardchanges.append({'Path':rel,'Before':before,'After':actual})
assert len(guardchanges)==1 and guardchanges[0]['Path']=='src/Erlang.Runtime/CoreModules.cs'
proposed=[{'Path':r['Path'],'Sha256':sha(ROOT/r['Path'])} for r in ledger['VerifiedSourceFiles']]
known={r['Path'] for r in proposed}
additional=[]
for r in newbindings+ordfiles:
    if r['Path'] not in known:
        additional.append({'Path':r['Path'],'Sha256':r['Sha256']})
        known.add(r['Path'])
proposed.extend(additional)
assert len(proposed)==469
dump(OUT/'verified-source-files.proposed.json',proposed)
dump(OUT/'final-source-bindings.json',{'ExistingErLangContractBindings':newbindings,'OrdsetsIntegratedBindings':ordfiles})

report={
 'Status':'INDEPENDENT_FINAL_SOURCE_REVIEW_ACCEPTED_CONDITIONAL_ON_ACTUAL_ORIGINAL_AND_FINAL_MAIN_GATES',
 'PinnedOtpCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','NoMainWrites':True,'NoBuildOrNetwork':True,
 'BaseLedgerSha256':sha(ledgerpath),'BaseGuardCount':462,'FinalProposedGuardCount':469,
 'ChangedExistingGuards':guardchanges,'AdditionalGuards':additional,
 'NarrowRegistrationProof':{'CoreModulesBeforeSha256':digest(oldcore),'CoreModulesAfterSha256':sha(corepath),'ExactChange':'One OrdsetCoreModule.Register(r) call immediately after QueueCoreModule.Register(r), no other byte change.','ReversibleByteEquality':True},
 'PreservedFourErLangContractProof':{'PriorProofPath':'artifacts/erlang371-contract-audit1/contract-proof.json','PriorProofSha256':sha(baseproof/'contract-proof.json'),'SourceBindingChanges':bindingchanges,'Review':'All hd/tl/tuple_size/is_integer3 handlers, error wrappers, guards, argument-binding and source metadata code remain byte-identical. New ordsets registration introduces eight distinct module ordsets signatures; no existing erlang signature is replaced and no erlang dispatch binding changes. New atoms/modules may alter VM own state, but these four handlers use type/member/count/BigInteger operations only. Their prior complete branch proofs remain valid on final sources.'},
 'OrdsetsFinalSourceReview':{'PriorReviewPath':'artifacts/erlang371-ordsets-independent-review1/review.json','PriorReviewSha256':sha(ROOT/'artifacts/erlang371-ordsets-independent-review1/review.json'),'FourFilesTokenEquivalentToProposal':True,'Review':'Style changes only; no altered token, literal or branch. Three comparison-free contracts new/0,is_empty/1,to_list/1 remain eligible after actual native/current-Main comparisons. size,is_equal,is_set,is_element,add_element remain Needs verification for source dependencies described in prior independent review.'},
 'ArgumentBindingGuardAddition':'ExpressionBindings,CompiledExpressionBindings,CompiledBindingScope are unchanged existing call-evaluation helpers. New guard rows bind left-to-right once-only argument capture relevant to these APIs; no general Core/SSA parity claim.',
 'ProposedPresenceDelta':8,'ConditionalCompleteContractDelta':7,'ConditionalErlangComplete':20,'ConditionalOrdsetsPresent':8,'ConditionalOrdsetsComplete':3,'ConditionalGlobalPresent':686,'ConditionalGlobalComplete':114,
 'NoStatusChangeApplied':True,'NoActualSupplementExecutionCreditedByReviewer':True,
 'RequiredRemainingGates':['Accept pinned original same-job source/module/build provenance and full raw packets.','Capture actual final Main DLL closure and compare every field of values/stacks without generic normalization.','Require all bounded four-MFA and comparison-free ordsets rows strict with zero differences.','Run affected final-source regression and preserve existing classifications/evidence.','Only Root then applies precise7 promotions, leaving5 remaining ordsets entries pending.'],
}
dump(OUT/'review.json',report)
files=[p for p in OUT.rglob('*') if p.is_file() and p.name!='source-freeze.json']
dump(OUT/'source-freeze.json',{'Files':[{'Path':str(p.relative_to(OUT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(files)]})
print(json.dumps({'ReviewSha256':sha(OUT/'review.json'),'FreezeSha256':sha(OUT/'source-freeze.json'),'Guards':len(proposed),'Changed':len(guardchanges),'Additional':len(additional)}))
