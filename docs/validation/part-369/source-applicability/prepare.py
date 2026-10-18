from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(name,value):(base/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8',newline='\n')
old=json.loads((root/'artifacts/binary-integration369/baseline/docs/mfa-compatibility.json').read_text(encoding='utf-8-sig'))
current=json.loads((root/'docs/mfa-compatibility.json').read_text(encoding='utf-8-sig'))
key=lambda e:(e['Module'],e['Function'],e['Arity'])
previous=[e for e in old['Entries'] if e['Status']=='Verified compatible']
now={key(e):e for e in current['Entries']}
save('retained-ledger.json',{'BaselineVerified':len(previous),'ExactPreserved':sum(now.get(key(e))==e for e in previous),'Changed':[{'Mfa':f'{key(e)[0]}:{key(e)[1]}/{key(e)[2]}','Current':now.get(key(e))} for e in previous if now.get(key(e))!=e],'Note':'Exact ledger preservation, not independent proof of every old implementation path.'})
assert len(previous)==76
records=[]
def add(name,arity,family,domain,errors,dependency):
    records.append({'Mfa':f'binary:{name}/{arity}','Family':family,'DomainAndBranches':domain,'ErrorsAndFrames':errors,'Dependencies':dependency,'Classification':'SOURCE_APPLICABILITY_REVIEWED; ROOT_FINAL_EVIDENCE_AND_CLASSIFICATION_PENDING'})
bif='Whole original public effective args; native BIF error_info uses actual registry/native boundary; current-code body+image ownership checked.'
for name,arity,domain in [
 ('at',2,'Byte-aligned binary and integer index0<=i<byteLength, including unaligned retained binary views. Reject negative/noninteger/out-of-range and partial bitstrings.'),
 ('first',1,'Nonempty byte-aligned binary; first logical byte. Empty/nonbinary/partial fail.'),
 ('last',1,'Nonempty byte-aligned binary; last logical byte. Empty/nonbinary/partial fail.'),
 ('part',2,'Exact tuple2 position/length; negative length reverses range origin; zero endpoints inclusive; all arithmetic checked against logical bytes before slicing.'),
 ('part',3,'Same range partition with separate original arguments; unsigned position and signed length; invalid ranges do not construct a view.')]:
 add(name,arity,'primitive18',domain,bif,'ErlangIoData.Part; BitString logical offset; <=512bits copies, larger retains actual descriptor.')
for arity in [1,2,3]:
 add('bin_to_list',arity,'primitive18','Binary; full range or exact tuple2/separate position,length. Source zero/negative/positive length and range checks preserve wrapper public arguments. Output proper byte integer list.', 'BinaryListConversion.ErrorLine selects source error_info vs badarg helper; whole caller chain retained via RuntimeExceptionCapture.Call; bin_to_list wrapper owner stripped once.', 'BinaryWrapperSource/Trace; backwards list construction with cooperative checkpoints.')
add('list_to_bin',1,'primitive18','Top Nil or Cons only; arbitrary nested proper iodata with binary tails and byte heads. Top binary rejected. Singleton aligned binary list returns same object. Invalid reached integer/tuple/partial bits/improper tail fails.',bif,'BinaryWrapperOutput visits each structural node, including empty lists, and every bounded byte chunk; no segment-only cancellation hole.')
for arity in [1,2]:
 add('copy',arity,'primitive18','Binary and nonnegative machine-Uint count (default1). Validate count/type/alignment before empty result. Result byte length multiply checked; new owned physical backing; byte contents repeated modulo source logical length.',bif,'Own int BitLength and actual CLR allocation limits; system_limit not invented large allocation. Cooperative copy chunks, cancellation before writes.')
 add('decode_unsigned',arity,'primitive18','Binary; big default or exact big/little atom. Empty/allzero=>0; leading/trailing zero stripping equivalent to unsigned BigInteger decode; no dependence on parent offset.',bif,'Logical ByteSpan; BigInteger unsigned direction; native source itself has atomic integer loops, no promise of identical reduction count.')
 add('encode_unsigned',arity,'primitive18','Nonnegative arbitrary integer; big default or exact big/little atom; zero=>one zero byte; minimal unsigned representation.',bif,'Genuine Integer value and actual owned binary output; current BitLength/CLR physical limits apply, not native heap size identity.')
for name,direction in [('longest_common_prefix','front'),('longest_common_suffix','back')]:
 add(name,1,'primitive18',f'Proper nonempty binary list; validate every head before trivialempty optimization. MAX_ARITYVAL count check precedes improper-tail final check. Compare logical bytes from {direction}, returning minimal mismatch/shortest length.',bif,'Finite count bound0x00ffffff matches pinned arity layout; per-node/per-byte/per-candidate charging. Native trap partition not seeded.')
add('referenced_byte_size',1,'primitive18','Every BitString including partial bits. Heap returns floor(logicalbits/8); refc returns actual current backing array length. Retained views/volatile backing movement, shrink and aliases use same descriptor.',bif,'Own real allocation, explicit heap/refc kind, observed payload arrays. Generic append/pin/NIF freeze/sharing events source-reviewed; no equal native physical capacity or optimizer layout assertion.')
search=json.loads((root/'artifacts/binary-search369-proposal1/branch-proof.json').read_text(encoding='utf-8'))
for e in search['PerMfa']:
    m=e['Mfa'];f=m.split(':')[1].split('/')[0];a=int(m.split('/')[1])
    add(f,a,'search7',' '.join(e['Branches']),bif,'Immutable trie exact leftmost-longest/nonoverlap algorithm; actual typed runtime resource ref and acquired lease; public resource lifecycle/ETF admission, cooperative compile/search/results. No fabricated opaque IDs.')
wrap=json.loads((root/'artifacts/binary-wrappers369-proposal1/branch-proof.json').read_text(encoding='utf-8'))
for e in wrap['PerMfa']:
    m=e['Mfa'];f=m.split(':')[1].split('/')[0];a=int(m.split('/')[1])
    frames={
      ('replace',3):'Public error helper line1147 preserves args3; live callback do_replace907 plus one recursive908, replace4 line895 and replace3 line797. Inner replace4 catches callback errors before replace3 rethrows.',
      ('replace',4):'Public badarg helper1144, cause badopt helper1141 only for caught throw:badopt. Full args4 and genuine callback frame/caller chain; all other guest classes collapse by source catch.',
      ('encode_hex',1):'Public guard error helper1147 args1; valid source-phase generated helpers/comprehensions retain actual location/continuation metadata.',
      ('encode_hex',2):'Public guard error helper1147 args2; same real phase helpers with exact casing selection.',
      ('decode_hex',1):'Public badarg helper1144 args1. Even-rounded partial bits propagate actual badmatch at decode_hex1/1 line1087 then decode_hex/1 line1078; full failed input remains in reason.',
      ('join',2):'Public badarg helper1144 args2; valid join3 continuation source1136..1138 present during construction; helper failures caught/rethrown with whole public args.'
    }[(f,a)]
    add(f,a,'wrapper6',e['Clauses'],frames,'Current source overrides obsolete preliminary frame/backing cautions: physical join writable allocation, hex phase allocation and matching pin. Genuine own callback/source identities; no frame projection.')
assert len(records)==31 and len({e['Mfa'] for e in records})==31
save('per-mfa.json',{'OtpCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','Counts':{'primitive18':18,'search7':7,'wrapper6':6},'Entries':records})
names=set()
for folder,glob in [('src/Erlang.Runtime','Binary*.cs'),('src/Erlang.Terms','Binary*.cs')]:
    names.update(p.relative_to(root).as_posix() for p in (root/folder).glob(glob))
names.update('src/'+s for s in [
 'Erlang.Terms/BitString.cs','Erlang.Terms/BitStringStorage.cs','Erlang.Terms/FriendAssemblies.cs','Erlang.Terms/TermsAssemblyNames.cs','Erlang.Terms/ObservedPayloadStorage.cs',
 'Erlang.Runtime/RuntimeReferenceResources.cs','Erlang.Runtime/RuntimeReferenceResources.BinarySearch.cs','Erlang.Runtime/ReferenceResourceKind.cs','Erlang.Runtime/ModuleRegistry.NativeEntries.cs','Erlang.Runtime/ModuleRegistry.cs','Erlang.Runtime/CoreModules.cs','Erlang.Runtime/ErlangIoData.cs','Erlang.Runtime/Mailbox.cs','Erlang.Runtime/ProcessContext.cs','Erlang.Runtime/NifNativeBinaryStorage.cs','Erlang.Runtime/NifNativeBinaryNode.cs','Erlang.Runtime/NifNativeBinaryCallbacks.cs','Erlang.Runtime/RuntimeExceptionCapture.cs',
 'Erlang.Compiler/BitExpressionBindings.cs','Erlang.Compiler/BitConstruction.cs','Erlang.Compiler/BitPattern.cs','Erlang.Compiler/ComprehensionExecution.cs','Erlang.Compiler/ZipGeneratorCursor.cs','Erlang.Compiler/Execution.cs','Erlang.Serialization/ExternalTermFormat.cs'])
save('source-bindings.json',{'Scope':'Read-only current checkpoint; Root must refresh after any style/semantic delta','Files':[{'Path':n,'Sha256':sha(root/n),'Bytes':(root/n).stat().st_size} for n in sorted(names)]})
paths=['artifacts/binary369-primary-actual2-peer1/acceptance.json','artifacts/binary-integration369-actual2/comparison1/comparison.json','artifacts/binary-integration369-supplement/comparison1/comparison.json','artifacts/binary-integration369/final-protocol1/integration.json','artifacts/binary-sharing369-proposal1/source-freeze.json','artifacts/binary-backing369-followup1/source-freeze.json','artifacts/binary-hex369-protocol4/source-proof.json']
save('evidence-bindings.json',{'Files':[{'Path':n,'Sha256':sha(root/n)} for n in paths],'Caution':'Primary finite cohort admitted; prejoin supplement remains historical differences. Closure14 and final regression are Root-owned pending gates, not fabricated passing facts.'})
print(len(records),'MFAs',len(names),'current source bindings')
