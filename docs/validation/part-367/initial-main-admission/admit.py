from pathlib import Path
import hashlib, importlib.util, json, collections
R=Path(__file__).resolve().parents[2]; O=Path(__file__).resolve().parent
T=R/'artifacts/queue-original367-original1/tool'; C=R/'artifacts/queue-integration367/capture1'; S=C/'sdk'; G=S/'queue_core'
def H(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p): return json.loads(p.read_bytes())
def require(ok, reason):
    if not ok: raise ValueError(reason)
def load(p,name):
    s=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
decoder=load(T/'passive_etf_family.py','queue_admitted_decoder')
def decode(p): return decoder.decode_exact(p.read_bytes())
def named(node,names):
    require(node[0]=='list' and len(node[1])==len(names),'row count/type')
    rows={}
    for name,row in zip(names,node[1]):
        require(row[0]=='tuple' and len(row[1])==2 and row[1][0]==name and name not in rows,'row name/order/shape')
        rows[name]=row[1][1]
    return rows
profile=J(T/'profile.json'); f=profile['Fixtures'][0]; names=[('atom',x) for x in f['Names']]
require(len(profile['Fixtures'])==1 and len(names)==160 and len(set(names))==160,'profile160')
require(profile['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and profile['SubjectModule']=='queue','subject/pin')
seal=J(S/'capture-seal.json'); sealed={}
for row in seal['Files']:
    p=(S/row['Path']).resolve();require(p.is_relative_to(S.resolve()),'seal path traversal')
    require(p.stat().st_size==row['Bytes'] and H(p)==row['Sha256'],'sealed file hash')
    require(row['Path'] not in sealed,'duplicate sealed file');sealed[row['Path']]=row['Sha256']
require(len(sealed)==6,'six sealed files')
completion=J(S/'completion.json');require(completion['FailedGroups']==0 and len(completion['Groups'])==1 and not completion['OriginalExecuted'] and not completion['Published'],'completion')
group=completion['Groups'][0];require(group['Id']==f['Id'] and not group['ExecutionSkipped'],'actual capture group')
source_inputs=J(T/'source-inputs.corrected.json'); source_rows={}
for field,base in [('RepositoryInputs',R),('ToolInputs',T)]:
    for row in source_inputs[field]:
        p=base/row['Path'];require(p.exists() and H(p)==row['Sha256'] and p.stat().st_size==row['Bytes'],'source input binding '+row['Path']);source_rows[(field,row['Path'])]=row['Sha256']
fixtures=profile['FixtureSources'];require(len(fixtures)==profile['ExpectedUniqueSourceCount']==11,'eleven unique fixture sources')
for row in fixtures:
    p=(T if row['Location']=='tool' else R)/row['Path'];require(H(p)==row['Sha256'],'profile source hash')
expected_paths=[(T/f['Path']).resolve()]+[(T/x).resolve() for x in f['Companions']]
require(len(group['Compile'])==len(expected_paths)==11,'compile11')
for row,p in zip(group['Compile'],expected_paths):
    require(row['Passed'] and Path(row['Path']).resolve()==p and row['SourceSha256']==H(p) and row['DeclaredSourceFile']==p.name,'compile exact source/basename')
require(group['ReturnedSha256']==H(G/'returned.etf') and group['StacksSha256']==H(G/'stack-observations.etf'),'group raw hashes')
receipts=[]
for lane in ['build','capture']:
    p=C/(lane+'.exit.json');q=J(p);require(q['Settled'] and not q['TimedOut'] and q['ExitCode']==0 and not q['Failures'],'normal settled '+lane)
    for stream in ['stdout','stderr']:require(H(C/(lane+'.'+stream+'.log'))==q[stream.title()+'Sha256'],'receipt log hash')
    receipts.append(dict(Lane=lane,ReceiptSha256=H(p),Pid=q['Pid'],ElapsedSeconds=q['ElapsedSeconds'],Normal=True))
closure=J(C/'main-dll-closure.json');require(closure['MainSource'] and not closure['PrivateCandidateDllsUsed'] and closure['SubjectModule']=='queue' and len(closure['Files'])==5,'actual Main closure')
for row in closure['Files']:require(H(R/row['Path'])==H(R/row['CopiedTo'])==row['Sha256'],'Main versus harness DLL hash')
project=(R/'artifacts/queue-integration367/main-capture/MainCapture.csproj').read_text();require('ProjectReference' in project and '../../../src/Erlang.Compiler/' in project and '../../../tools/Erlang.Differential/' in project,'Main project references')
metadata_names=(R/'artifacts/queue-integration367/main-capture/FamilyRuntimeMetadataNames.cs').read_text();require('Module = "queue"' in metadata_names and '"proplists"' not in metadata_names,'queue subject metadata')
values=named(decode(G/'returned.etf'),names);depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
stacks=named(decode(G/'stack-observations.etf'),depths);stackrows={d:named(node,names) for d,node in stacks.items()}
counts=collections.Counter()
for kind,rows in [('values',values)]+[(repr(d),x) for d,x in stackrows.items()]:
    for outcome in rows.values():
        require(outcome[0]=='tuple','outcome tuple');parts=outcome[1]
        if parts[0]==('atom','ok'):require(len(parts)==2,'success shape');counts['success']+=1
        else:
            require(parts[0]==('atom','error') and parts[1][0]=='atom' and len(parts)==(3 if kind=='values' else 4),'error shape')
            if kind!='values':
                require(parts[3][0]=='list','complete raw stack list')
                for frame in parts[3][1]:require(frame[0]=='tuple' and len(frame[1])==4,'complete raw frame')
            counts['error']+=1
runtime=decode(G/'runtime-metadata.etf');require(runtime[0]=='tuple' and len(runtime[1])==3 and runtime[1][0]==('atom','sdk_runtime_provenance'),'runtime metadata shape')
md5=runtime[1][1];initpid=runtime[1][2];require(md5[0]=='binary' and len(md5[1])==16 and initpid[0]=='pid','runtime own metadata')
source_metadata=decode(G/'source-module-metadata.etf'); modules={row[1][0][1]:row[1][1][1] for row in source_metadata[1]};require(len(modules)==11 and all(len(v)==16 for v in modules.values()),'source metadata11')
binding=J(G/'source-function-bindings.json');require(binding['SourceResolverInvoked'] and not binding['GuestCodeExecutedByDiagnostic'] and len(binding['Bindings'])==6,'actual source bindings6')
for b in binding['Bindings']:
    require(b['Module']=='queue_core_contract' and b['Name']==b['ResolvedName']==b['ResolvedNamedFunction']=='local_identity' and b['NamedDefinitionExists'] and b['ResolverBound'],'named actual source resolver')
    for a,c in [('Index','SourceIndex'),('OldIndex','SourceOldIndex'),('OldUnique','SourceOldUnique'),('UniqueHex','SourceUniqueHex')]:require(b[a]==b[c],'source identity field')
    require(b['Arity']==1 and b['EnvironmentCount']==0 and b['ResolvedAnonymousBodySha256'] is None and b['UniqueHex']==modules[b['Module']].hex().upper(),'local fun own source module MD5')
opaque=[]
def opaque_check(label,rows,raw):
    result=rows[('atom','opaque_raw')];require(result[0]=='tuple' and result[1][0]==('atom','ok'),'opaque success')
    body=result[1][1];require(body[0]=='tuple' and len(body[1])==4,'opaque body shape')
    original,queue,returned,drained=body[1];require(original[0]=='list' and len(original[1])==14,'all14 arbitrary values')
    def same(a,b):
        if a[0]=='new_fun' and b[0]=='new_fun':
            sa,sb=a[10],b[10];return raw[sa[1]:sa[1]+sa[2]]==raw[sb[1]:sb[1]+sb[2]]
        if a[0]!=b[0]:return False
        if a[0] in ('tuple','list'):return len(a[1])==len(b[1]) and all(same(x,y) for x,y in zip(a[1],b[1]))
        return a==b
    require(same(returned,original),'full returned arbitrary value identity')
    require(drained[0]=='tuple' and len(drained[1])==2 and same(drained[1][0],original) and drained[1][1]==('tuple',(('list',()),('list',()))),'full drain identity/empty')
    require(queue[0]=='tuple' and len(queue[1])==2 and same(queue[1][0],('list',tuple(reversed(original[1][7:])))) and same(queue[1][1],('list',original[1][:7])),'full raw queue representation')
    pid,reference,fun,external=original[1][:4]
    require(pid[0]=='pid' and pid[1]==initpid[1] and pid[4]==initpid[4],'genuine own actor PID schema')
    require(reference[0]=='reference' and reference[1]==pid[1] and reference[2]==pid[4] and 1<=len(reference[3])<=5,'genuine own reference schema')
    require(fun[0]=='new_fun' and fun[2]==1 and fun[3]==modules['queue_core_contract'] and fun[5]==('atom','queue_core_contract') and fun[8]==initpid and fun[9]==(),'all fixed local FUN fields against own metadata')
    bs=[b for b in binding['Bindings'] if b['Index']==fun[4] and b['OldIndex']==fun[6][1] and b['OldUnique']==fun[7][1] and b['UniqueHex']==fun[3].hex().upper()];require(bs,'local FUN raw fields actual resolver binding')
    require(fun[7]==('integer',int.from_bytes(fun[3][:4],'big')>>5),'local FUN old unique own module')
    require(external==('export_fun',('atom','erlang'),('atom','abs'),('integer',1)),'exact external FUN fields')
    expected_relationships=('tuple',(('atom','ok'),('tuple',(('atom','true'),('atom','false'),('integer',14),('atom','true'),('atom','true'),('atom','true'),('atom','true'),('atom','true'),('atom','true'),('atom','true'),('atom','identity_value'),('integer',7),('atom','true')))))
    require(rows[('atom','opaque_relationships')]==expected_relationships,'all13 actual guest relational observations')
    opaque.append(dict(Profile=label,Pid=repr(pid),Reference=repr(reference),LocalFunSourceBound=True,All14ValuesPreserved=True,All13Relationships=True,RawFunWireRepeatedIdentically=True))
opaque_check('values',values,(G/'returned.etf').read_bytes())
for d,rows in stackrows.items():opaque_check(repr(d),rows,(G/'stack-observations.etf').read_bytes())
report=dict(Status='PASS_PASSIVE_MAIN_CAPTURE_ADMISSION_ORIGINAL_PENDING',Values=160,FullStackObservations=800,Profiles=5,CompiledSources=11,ExactNamesOrder=True,ExactRawSeals=True,MainDlls=closure['Files'],Receipts=receipts,SourceBindings=6,OpaqueProfiles=opaque,SourceBoundLocalFun=True,Subject='queue',QueueOwnModuleMd5=md5[1].hex().upper(),SdkSealSha256=H(S/'capture-seal.json'),ProfileSha256=H(T/'profile.json'),SourceInputsSha256=H(T/'source-inputs.corrected.json'),DecoderSha256=H(T/'passive_etf_family.py'),TrustedSourceBindingHelper=dict(Path='artifacts/proplists-stack-relational-peer3/source_binding.py',Sha256=H(R/'artifacts/proplists-stack-relational-peer3/source_binding.py'),Use='Unmodified helper reserved for actual original FunT/module metadata comparison; no fake original table supplied.'),All960OutcomeShapes=dict(counts),Normalization=False,Projection=False,RawDecoderSpanIsDiagnosticNotWireField=True,OriginalCompared=False,CompatibilityPromoted=False,MainModified=False,ExecutedCompiledOrDownloadedContent=False,Limitations=['Normal OS process exit is receipt-bound; capture harness awaits process completion and runtime disposal but does not emit a separate internal scheduler-unwind receipt.','Current five Main DLLs equal captured DLLs; source-to-binary identity relies on recorded build/project-reference provenance, not an independent rebuild.','Raw native numeric PID/reference/code identities remain exact differences until source-bound all-field relational comparison; no frames or values will be stripped.','Source-input metadata retains inherited correction prose about older cohorts; actual profile/source roster is checked as11 sources/160 names, not inferred from that prose.'])
(O/'admission.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k] for k in ['Status','Values','FullStackObservations','SourceBindings','All960OutcomeShapes','SdkSealSha256']},indent=2))
