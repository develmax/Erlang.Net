"""Root-only, bound two-phase finalizer proposal. This file has not been executed."""
import argparse,hashlib,json,pathlib,re
ROOT=pathlib.Path.cwd();OUT=ROOT/'docs/validation/part-313'
META=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/OTP-PORTS.md']
def require(v,m):
    if not v:raise ValueError(m)
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def safe(name):
    p=(ROOT/name).resolve();require(p.is_relative_to(ROOT.resolve()) and '.git' not in p.relative_to(ROOT.resolve()).parts,'Escaping path');return p
def pin(item):
    p=safe(item['Path']);b=p.read_bytes();require(type(item['Bytes']) is int and len(b)==item['Bytes'] and sha(b)==item['Sha256'],'Bound file changed: '+str(p));return p

def gates(c):
    require(c['BoundToActualFinalGates'] is True and c['Part']==313,'Unbound proposal cannot write Main')
    f=read(pin(c['SourceFreeze']));cl=pin(c['SourceClosure']);require(sha(cl.read_bytes())==f['FinalSourceClosureSha256'],'Source closure seal changed')
    require((f['SourcePaths'],f['CSharpPaths'],f['SourceClosurePaths'])==(45,19,2205),'Wrong source scope')
    for e in read(cl)['Files']:
        b=safe(e['Path']).read_bytes();require(len(b)==e['Bytes'] and sha(b)==e['Sha256'],'Raw local source changed: '+e['Path'])
    full=read(pin(c['FullGuard']));native=read(pin(c['NativeGuard']));tests=read(pin(c['Tests']));integration=read(pin(c['Integration']))
    require((full['Passed'],full['Failed'],full['IntegrationResults'],full['Registry'],full['ErlangMfas'],full['PublicMfaDelta'])==(6640,0,11,505,347,0),'Full gate mismatch')
    require(full['SourceHashesExact'] is True and full['All6640PreviousOrderedNamesExact'] is True and full['SourceFreezeSha256']==c['SourceFreeze']['Sha256'] and full['SourceClosureSha256']==c['SourceClosure']['Sha256'],'Full source/name binding changed')
    require(native['Status']=='ACTUAL_MAIN_POST_FULL_WINDOWS_LEGACY_SEVEN_AND_NEW_REALLOCATION_THREE_PASS' and native['FullPassed']==6640 and native['SourceFilesRehashed']==2205 and native['SourceFreezeSha256']==c['SourceFreeze']['Sha256'],'Native source gate mismatch')
    require(native['OriginalAndSdkCompared'] is False and native['OriginalExecutedForNewCases'] is False,'Scope cannot relabel new original')
    require([(e['Case'],e['ReturnedMask']) for e in native['NewCases']]==[('null_positive',1539),('null_zero',1601),('resize_sequence',2047)],'Native masks changed')
    require(tests['Passed']==6640 and tests['Failed']==0 and len(tests['Tests'])==6640 and all(e['Status']=='Passed' for e in tests['Tests']) and len(integration)==11 and integration==read(safe('docs/validation/part-312/integration-results.json')),'Actual tests/integrations missing')
    pin(c['Closed0046']);return full,native

def main():
    p=argparse.ArgumentParser();p.add_argument('phase',choices=['scope','finalize']);p.add_argument('--config',type=pathlib.Path,required=True);p.add_argument('--expected-config-sha256',required=True);args=p.parse_args()
    require(sha(args.config.read_bytes())==args.expected_config_sha256.upper(),'Root-bound config digest required');c=read(args.config);full,native=gates(c)
    note=pin(c['RuntimeNote']).read_text(encoding='utf-8-sig').strip();require(note.startswith('Part313:') and 'pending' in note.lower(),'Finalized note must disclose remote pending')
    scope_path=safe('docs/module-readiness-scope.json');marker=safe(c['ScopeMarker']);scope=read(scope_path);runtime=next(e for e in scope['Components'] if e['Id']=='Runtime')
    if args.phase=='scope':
        require(not marker.exists(),'Scope phase already applied');pin(c['ScopeBefore']);require(not any('Part313:' in e for e in runtime['Implemented']),'Runtime note already present')
        runtime['Implemented'].append(note);scope['ComparisonTestReport']='validation/part-312/tests.json';payload=(json.dumps(scope,indent=2,ensure_ascii=False)+'\n').encode()
        scope_path.write_bytes(payload);marker.parent.mkdir(parents=True,exist_ok=True);marker.write_text(json.dumps({'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'ScopeSha256':sha(payload),'RuntimeNoteSha256':c['RuntimeNote']['Sha256']},indent=2)+'\n');return
    require(marker.is_file() and read(marker)=={'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'ScopeSha256':sha(scope_path.read_bytes()),'RuntimeNoteSha256':c['RuntimeNote']['Sha256']},'Scope phase or readiness input changed')
    ready=read(pin(c['Readiness']));require((ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'],ready['ComparisonTestReport'])==(6640,505,505,0,'validation/part-312/tests.json'),'Readiness gate mismatch')
    require(scope['ComparisonTestReport']=='validation/part-312/tests.json' and runtime['Implemented'].count(note)==1,'Readiness scope note missing');require(not OUT.exists(),'Fresh part313 validation directory required')
    rows=read(pin(c['BaseManifest']))+c['FinalCopyRows'];copies=[];destinations=set()
    required_sources={c[k]['Path'] for k in ['SourceFreeze','SourceClosure','FullGuard','NativeGuard','Tests','Integration','Readiness']}
    require(required_sources<={e['SourcePath'] for e in c['FinalCopyRows']},'Final proof rows incomplete')
    for e in rows:
        source=safe(e['SourcePath']);b=source.read_bytes();dest=safe(e['Destination']);require(dest.is_relative_to(OUT) and dest not in destinations,'Duplicate/escaping copy target');destinations.add(dest)
        require(e.get('HashMode','BinaryExact')=='BinaryExact' and len(b)==e['Bytes'] and sha(b)==e['Sha256'],'Copy row changed');copies.append((dest,b))
    require(not ({OUT/'evidence-index.json',OUT/'evidence-receipt.json',OUT/'final-bound-config.json'}&destinations),'Reserved evidence target collision')
    entry=pin(c['EnglishEntry']).read_text(encoding='utf-8-sig').strip();require(entry.startswith('## Part 313') and not any(s in entry for s in ['FINALIZATION REQUIRED','{VERIFIED','SOURCE-ONLY PROPOSAL','UNBOUND']),'Changelog placeholders forbidden')
    volume=pin(c['VolumeBefore']);old=volume.read_bytes();nl=b'\r\n' if b'\r\n' in old else b'\n';combined=old.rstrip(b'\r\n')+nl+nl+entry.replace('\r\n','\n').replace('\n',nl.decode()).encode()+nl
    require(b'## Part 313' not in old and len(combined)<=32768 and len(combined.splitlines())<=400,'Active0047 capacity/duplicate failure');changes={volume:combined}
    require([e['Path'] for e in c['MetadataBefore']]==META,'Exact seven metadata targets required')
    for item in c['MetadataBefore']:
        path=pin(item);b=path.read_bytes();require(b'Part313:' not in b,'Metadata note already present');m=re.search(br'\r?\n\r?\n',b);require(m,'Metadata heading separator missing');n=b'\r\n' if b'\r\n' in b else b'\n';changes[path]=b[:m.end()]+note.encode()+n+n+b[m.end():]
    index_path=pin(c['ChangelogIndexBefore']);b=index_path.read_bytes();before=b'part312 (active)';require(b.count(before)==1,'Expected active label absent');changes[index_path]=b.replace(before,'parts312–313 (active)'.encode())
    require(safe('docs/changelog/0046.md') not in changes and not(set(changes)&destinations),'Closed volume or mutation/copy overlap')
    index_bytes=(json.dumps(rows,indent=2)+'\n').encode();receipt={'Status':'ACTUAL_MAIN313_FULL_NATIVE_READINESS_SEPARATE_PREVIOUS312_REMOTE_NEW313_PENDING','Passed':6640,'Failed':0,'IntegrationResults':11,'Registry':505,'RegistryDelta':0,'OptionalIoRegistry':507,'ErlangPresence':347,'SourcePaths':45,'CSharpPaths':19,'SourceResourceFiles':2205,'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'SourceClosureSha256':c['SourceClosure']['Sha256'],'EvidenceIndexSha256':sha(index_bytes),'ConfigSha256':sha(args.config.read_bytes()),'ComparisonTestReport':'validation/part-312/tests.json','Current313RemoteCiVerified':False,'NewOriginalThreeCompared':False,'Current313OrdinaryVerified':False,'ActualOomTested':False,'DefaultNativeInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'Closed0046Sha256':c['Closed0046']['Sha256'],'Files':rows}
    for dest,b in copies:dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(b)
    for dest,b in changes.items():dest.write_bytes(b)
    (OUT/'evidence-index.json').write_bytes(index_bytes);(OUT/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');(OUT/'final-bound-config.json').write_bytes(args.config.read_bytes())
    pin(c['Closed0046']);print(json.dumps({'Rows':len(rows),'Passed':6640,'RegistryDelta':0,'Volume0047Bytes':len(combined),'Remote313Pending':True}))
if __name__=='__main__':main()
