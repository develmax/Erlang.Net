from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(n,v):(base/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
ledger=root/'docs/mfa-compatibility.json';doc=json.loads(ledger.read_bytes());baseline=root/'artifacts/binary-integration369/baseline/docs/mfa-compatibility.json';old=json.loads(baseline.read_bytes())['VerifiedSourceFiles'];assert len(old)==382
oldmap={r['Path']:r['Sha256'] for r in old};assert len(oldmap)==382
scope=json.loads((root/'artifacts/binary369-final-applicability-peer1/source-bindings.json').read_bytes())
paths=set(oldmap)|{r['Path'] for r in scope['Files']}
extra=[
 'src/Erlang.Runtime/ResourceReferenceTerm.cs','src/Erlang.Runtime/ReferenceResourceEntry.cs','src/Erlang.Runtime/ReferenceResourceLease.cs','src/Erlang.Runtime/ReferenceResourceNames.cs','src/Erlang.Runtime/ReferenceResourceParameters.cs',
 'src/Erlang.Runtime/RuntimeReferenceResources.Distribution.cs','src/Erlang.Runtime/RuntimeReferenceResources.ConnectedDistribution.cs',
 'src/Erlang.Terms/IReferenceResourceSerialization.cs','src/Erlang.Terms/BitStringLayout.cs','src/Erlang.Runtime/NifNativeBinaryNames.cs',
 'src/Erlang.Serialization/EtfIoVecStream.cs','src/Erlang.Serialization/EtfIoVecLayout.cs',
 'src/Erlang.Compiler/BitSegment.cs','src/Erlang.Compiler/BitSegmentTypes.cs','src/Erlang.Compiler/BitSyntaxDefaults.cs','src/Erlang.Compiler/BitStorageLayout.cs','src/Erlang.Compiler/BitSizeAtoms.cs','src/Erlang.Compiler/BitFloat.cs','src/Erlang.Compiler/BitUnicode.cs',
]
for n in extra:assert (root/n).exists(),n
paths.update(extra)
reasons={
 'src/Erlang.Compiler/ComprehensionExecution.cs':'Binary generator pin before logical byte snapshot. Ordinary list/map comprehension source routes retain previous algorithms; fresh whole-stack family replay required for shared execution dependencies.',
 'src/Erlang.Compiler/Execution.cs':'NativeEntries catalog-native current-body/current-image/no-source-owner predicate preserves genuine native BIF caller continuation; source wrappers keep RuntimeTailCall. Broader BIF callback frame change requires fresh retained families, not assumed old76 proof.',
 'src/Erlang.Compiler/ZipGeneratorCursor.cs':'Binary generator input pin before reused byte snapshot. Other cursor types unchanged.',
 'src/Erlang.Runtime/CoreModules.cs':'Frozen part369 baseline diff is exactly BinaryModule.Register insertion; old16 predicate/self/node dispatch bodies unchanged.',
 'src/Erlang.Runtime/NifNativeBinaryCallbacks.cs':'Static subbinary uses logical SliceBits; inspect pins before partial-bit rejection as pinned enif_inspect_binary. Borrow/lifetime/error machinery retained.',
 'src/Erlang.Runtime/NifNativeBinaryNode.cs':'Build delegates native parent snapshot to locked storage FreezeView; bytes preserved, actual full backing retained for large child.',
 'src/Erlang.Runtime/NifNativeBinaryStorage.cs':'FreezeView checks live pointer/range and copies under lifetime gate; <=64 child copies, large child retains actual managed full parent snapshot. No fake capacity, physical Array.MaxLength boundary explicit.',
 'src/Erlang.Runtime/ProcessContext.cs':'Initialize mailbox Owner with genuine Self pid only; existing lifecycle/context metadata unchanged.',
 'src/Erlang.Serialization/ExternalTermFormat.cs':'Single BitString-case pin maps both native encoding and external_size traversal; no ETF wire field normalization or identity substitution.',
 'src/Erlang.Terms/BitString.cs':'Real backing descriptor/refc kind, immutable logical view, actual capacity/append ownership/pin. Type identity/IsBinary unchanged. Actual state relationships required; physical allocator/compiler optimization identity not claimed.',
}
changed=[n for n in sorted(oldmap) if sha(root/n)!=oldmap[n]]
assert set(changed)==set(reasons),(changed,set(reasons))
rows=[{'Path':n,'Sha256':sha(root/n)} for n in sorted(paths)]
save('guarded-files-proposal.json',{'VerifiedSourceFiles':rows})
save('review.json',{
 'Status':'SOURCE_GUARD_SUCCESSOR_READY_ROOT_APPLY_FINAL_EXECUTION_PENDING',
 'OtpCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5',
 'BaseLedgerPath':baseline.relative_to(root).as_posix(),'BaseLedgerSha256':sha(baseline),'CurrentLedgerAtReviewSha256':sha(ledger),'PreviousGuards':382,'FinalGuards':len(rows),'RemovedPaths':[],
 'UnchangedOld':382-len(changed),'ChangedOld':[{'Path':n,'Before':oldmap[n],'After':sha(root/n),'SourceArgument':reasons[n]} for n in changed],
 'New':[{'Path':n,'Sha256':sha(root/n),'Reason':'Selected binary31 direct implementation or explicit storage/compiler/resource/serialization dependency; guard freezes this reviewed byte identity, not a standalone correctness proof.'} for n in sorted(paths-set(oldmap))],
 'UnsignedFinalDelta':{'Path':'src/Erlang.Runtime/BinaryUnsigned.cs','Sha256':sha(root/'src/Erlang.Runtime/BinaryUnsigned.cs'),'Argument':'GetByteCount computed once; reject above supported BitLength binary output model before ToByteArray/bitlength overflow. Valid accepted encodings, endian and zero unchanged; no native unlimited-memory claim.'},
 'FinalReplaceDelta':{'Path':'src/Erlang.Runtime/BinaryReplacePlan.cs','Sha256':sha(root/'src/Erlang.Runtime/BinaryReplacePlan.cs'),'Argument':'Current Root fix6 includes integer insert match/pin and source binary-replacement insert=true badfun only when a match invokes it; no-match returns subject. Independent source/original closure7 gate remains required; guard is final byte identity, not fabricated native acceptance.'},
 'PreviousCompleteEntries':'76/76 exact baseline objects preserved by sealed applicability audit; guards do not promote status or weaken scope.',
 'CanonicalApply':'Root replaces only VerifiedSourceFiles with this proposal after checking every current hash; leaves Entries/status/evidence/limits untouched until final acceptance.',
 'RequiredFinalGates':['closure14 original/fullfield admission','fresh final Main/DLL replay of binary cohorts','fresh retained queue/proplist fullfield replay incl native BIF callback tails','canonical full current-source regression','independent actual current source closure admission'],
 'NoFiniteToFullPromotion':True,'NoPhysicalHeapOrCompilerOptimizerIdentityClaim':True,'MainChangedByReviewer':False,
})
print('guards',len(rows),'oldchanged',len(changed),'new',len(paths-set(oldmap)))
