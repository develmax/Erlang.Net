import copy
import hashlib
import json
import re
import subprocess
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(name, value):
    (OUT/name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

def tokens(source):
    return re.findall(r'"(?:\\.|[^"\\])*"|\S', source)

ledger_path = ROOT/'docs/mfa-compatibility.json'
ledger = read(ledger_path)
assert len(ledger['Entries']) == 617
assert sum(row['Status']=='Verified compatible' for row in ledger['Entries']) == 46
assert len(ledger['VerifiedSourceFiles']) == 357
changed = [row for row in ledger['VerifiedSourceFiles'] if sha(ROOT/row['Path']) != row['Sha256']]
assert {row['Path'] for row in changed} == {'src/Erlang.Runtime/'+name for name in ['QueueCoreModule.cs','QueueModuleNames.cs','QueueCoreTrace.cs','QueueCoreLinear.cs','QueueCoreReverse.cs']}
profile_path = ROOT/'artifacts/queue-original368-original1/tool/profile.json'
profile = read(profile_path)
new_mfas = {tuple([part.split(':')[0],part.split(':')[1].split('/')[0],int(part.split('/')[-1])]) for part in profile['Mfas']}
assert len(new_mfas) == 30
raw = subprocess.check_output(['git','-C',str(ROOT.parent/'otp'),'show',PIN+':lib/stdlib/src/queue.erl'])
exports = set()
for group in re.findall(r'-export\(\[(.*?)\]\)',raw.decode(),re.S):
    exports.update(('queue',name,int(arity)) for name,arity in re.findall(r'([a-z_]+)/(\d+)',group))
assert len(exports) == 38
old_mfas = {(row['Module'],row['Function'],row['Arity']) for row in ledger['Entries'] if row['Module']=='queue'}
assert len(old_mfas) == 8 and exports-old_mfas == new_mfas
names_path = ROOT/'src/Erlang.Runtime/QueueModuleNames.cs'
names = dict(re.findall(r'const string (\w+) = "([^"]+)"',names_path.read_text()))
registered = set()
for module in ['QueueCoreModule.cs','QueueEndsModule.cs']:
    source = (ROOT/'src/Erlang.Runtime'/module).read_text()
    registered.update(('queue',names[name],int(arity)) for name,arity in re.findall(r'registry.Register\(\s*QueueModuleNames.Module,\s*QueueModuleNames\.(\w+),\s*(\d+)',source))
higher = (ROOT/'src/Erlang.Runtime/QueueHigherModule.cs').read_text()
registered.update(('queue',names[name],int(arity)) for name,arity in re.findall(r'\(QueueModuleNames\.(\w+),\s*(\d+)\)',higher))
assert registered == exports
old_guards = {row['Path']:row for row in ledger['VerifiedSourceFiles']}
queue_paths = sorted(path.relative_to(ROOT).as_posix() for path in (ROOT/'src/Erlang.Runtime').glob('Queue*.cs'))
added = [{'Path':path,'Sha256':sha(ROOT/path)} for path in queue_paths if path not in old_guards]
proposal = copy.deepcopy(ledger)
for row in proposal['VerifiedSourceFiles']:
    row['Sha256'] = sha(ROOT/row['Path'])
proposal['VerifiedSourceFiles'].extend(added)
proposal['VerifiedSourceFiles'].sort(key=lambda row:row['Path'])
for module,function,arity in sorted(new_mfas):
    proposal['Entries'].append({
        'Module':module,'Function':function,'Arity':arity,'Status':'Needs verification',
        'Scope':f'Local Part368 implementation of pinned OTP-29.1.1 {module}:{function}/{arity}; full observable source/term/error/stack/cooperative contract remains under verification. Source protocol uses genuine own VM state where applicable; implementation presence is not complete-contract evidence.',
        'Evidence':[],
        'Limits':['Actual current-source whole values and raw default/0/1/2/8 stacks, callback effects/order, own identity/trap bindings, resource/cooperative behavior and final regression gates remain required. No complete classification is proposed by this source-only inventory addition.']})
assert proposal['Entries'][:617] == ledger['Entries']
assert len(proposal['Entries']) == 647
assert sum(row['Status']=='Verified compatible' for row in proposal['Entries']) == 46
for row in proposal['VerifiedSourceFiles']:
    assert sha(ROOT/row['Path']) == row['Sha256']
assert 'src/Erlang.Runtime/ListFoldRight.cs' in old_guards
write('proposed-mfa-compatibility.json',proposal)
write('guard-delta.json',{
 'BaseLedgerSha256':sha(ledger_path),'Before':357,'After':len(proposal['VerifiedSourceFiles']),
 'Refresh':[{'Path':row['Path'],'PreviousSha256':row['Sha256'],'Sha256':sha(ROOT/row['Path'])} for row in changed],
 'Add':added,'ListFoldRightAlreadyGuardedExact':old_guards['src/Erlang.Runtime/ListFoldRight.cs'],
 'PriorEntriesPreserved':617,'PriorVerifiedPreserved':46,'AddNeedsVerification':30,'NewVerified':0,
 'ApplyOnlyIfBaseLedgerAndEveryProposedSourceHashExact':True})
defaults=[]
for name,baseline in [('QueueCoreLinear.cs','queue-core-reverse-successor3'),('QueueCoreReverse.cs','queue-core-reverse-successor3'),('QueueCoreTrace.cs','queue-core-successor2')]:
    path=ROOT/'src/Erlang.Runtime'/name
    old=ROOT/'artifacts'/baseline/'source/src/Erlang.Runtime'/name
    source=path.read_text()
    source=re.sub(r',\s*int ownerArity = 1','',source)
    source=re.sub(r',\s*bool includeOwner = true','',source)
    source=re.sub(r',\s*Term\? (initialTail|tail) = null','',source)
    source=source.replace('initialTail ?? Nil.Value','Nil.Value')
    source=re.sub(r',\s*includeOwner(?=\s*\))','',source)
    source=re.sub(r',\s*ownerArity,\s*tail(?=\s*\))','',source)
    source=re.sub(r',\s*ownerArity(?=\s*\))','',source)
    if name=='QueueCoreTrace.cs':
        source=re.sub(r'includeOwner\s*\?\s*Frame\(owner, Term.I\(ownerArity\), line\)\s*:\s*null','Frame(owner, Term.I(1), line)',source)
        source=source.replace('Frame(name, Cons.From(arguments), line), name','Frame(name, Cons.From(arguments), line)')
        source=re.sub(r'Capture\(\s*context,\s*first,\s*owner,\s*Frame','Capture(context, first, Frame',source)
        source=re.sub(r'\s*string sourceOwner,','',source)
        source=re.sub(r'\s*&& \(caller.Function == sourceOwner \|\| QueueTailAliases.Matches\(caller.Function, sourceOwner\)\)','',source)
    assert tokens(source)==tokens(old.read_text()), 'Unexpected shared default body delta '+name
    defaults.append({'Path':path.relative_to(ROOT).as_posix(),'Sha256':sha(path),'FrozenPriorPath':old.relative_to(ROOT).as_posix(),'FrozenPriorSha256':sha(old),'TokenExactAfterDefaultSpecializationAndDeclaredOwnerGuardInverse':True})
write('defaults-proof.json',defaults)
source_rows=[{'Path':path,'Sha256':sha(ROOT/path)} for path in queue_paths]
write('queue-source-bindings.json',source_rows)
write('source-review.json',{
 'Status':'CURRENT_SOURCE_APPLICABILITY_PRESERVES46_ADD30_NEEDS_ONLY',
 'BaseLedgerSha256':sha(ledger_path),'EntriesBefore':617,'EntriesAfter':647,'VerifiedBefore':46,'VerifiedAfter':46,
 'GuardsBefore':357,'GuardsAfter':len(proposal['VerifiedSourceFiles']),'OtherExistingGuardsExact':352,
 'PinnedQueueSourceSha256':hashlib.sha256(raw).hexdigest().upper(),
 'Profile':{'Path':profile_path.relative_to(ROOT).as_posix(),'Sha256':sha(profile_path),'Mfas':30},
 'PinnedExportsAndActualRegistrationSetExact38':True,
 'ActiveSourceInputsCorrected':{'Path':'artifacts/queue-original368-original1/tool/source-inputs.corrected.json','Sha256':sha(ROOT/'artifacts/queue-original368-original1/tool/source-inputs.corrected.json')},
 'FrozenOriginalProposal2':{'Path':'artifacts/queue-all368-original-proposal2/source-freeze.json','Sha256':sha(ROOT/'artifacts/queue-all368-original-proposal2/source-freeze.json')},
 'PreservationArgument':[
  'Only five named queue files changed among357 prior guards. All generic runtime/compiler/terms/serialization/native dependencies remain byte exact, including shared ListFoldRight.',
  'New QueueModuleNames constants only extend observed own VM atom state; approved own physical-map contracts use actual own atom indexes, not literal captured native words.',
  'CoreModule retains original eight registrations and adds only Ends/Higher module calls. New modules register disjoint30 names proven against pinned38minusold8.',
  'CoreLinear/Reverse defaults arity1/tailNil/includeOwnertrue mechanically specialize to frozen prior bodies. No list algorithm/scheduling change for old eight.',
  'CoreTrace now skips a queue source wrapper only if it is the explicit source owner or a proven pinned tail alias. Existing eight public/helper calls pass their actual own owner; their old direct path skips the same wrapper. New internal higher helpers retain unrelated enclosing queue frames instead of dropping them.',
  'QueueTailAliases lists only explicit unconditional source tail calls; head is deliberately absent because it has distinct public errors and calls get/2, not get/1.',
  'New ends classes are isolated and preserve shallow Cons/Nil guards, inline balancing owner/line, empty errors and last1/2 tail versus peek continuation; actual native profiles still pending.',
  'Member source uses genuine16-cell reduction entries, ceilcharge, strict Term.Equals and rear retained/front tail continuation; no captured native partition seeds. Higher callback/helper semantics are separately reviewed by owner and must pass actual whole-field original comparisons.',
 ],
 'Pending':'New30 remain Needs verification. Actual focus/native full profiles/corruption controls/own identity and trap witnesses/final canonical regression remain required; this is a guarded source proposal, not promotion.',
 'NoMainWritesBuildSdkNativeCiNetwork':True})
(OUT/'README.md').write_text('''# Part368 current source and guard proposal\n\nThis successor binds the active profile2 and source-inputs.corrected.json plus frozen original proposal2. The first source review remains unchanged. This artifact preserves all617 previous MFA entries and46 Verified classifications. It appends exactly30 Needs verification entries from the pinned queue38 exports minus existing8, with no synthetic execution evidence. Actual current registration sets match those38 exports exactly.\n\nOnly CoreModule, ModuleNames, CoreTrace, CoreLinear and CoreReverse change among357 old guards; the other352 hashes are exact. Every new Queue*.cs type is guarded, including Member and TailAliases. ListFoldRight is already guarded and unchanged. The shared default/body proof and owner-aware trace argument preserve the old eight queue paths and all other previously verified dependencies.\n\nApply only when base ledger and all proposed current source hashes match. No Main, runtime, canonical documentation or CI writes, builds or executions were performed. All30 new classifications remain pending actual gates.\n''',encoding='utf-8')
write('seal.json',{'Files':[{'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='seal.json']})
print(json.dumps({'SealSha256':sha(OUT/'seal.json'),'LedgerSha256':sha(OUT/'proposed-mfa-compatibility.json'),'ReviewSha256':sha(OUT/'source-review.json'),'GuardsAfter':len(proposal['VerifiedSourceFiles']),'AddedSources':len(added)}))
