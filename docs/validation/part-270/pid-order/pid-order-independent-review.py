from pathlib import Path
import json,hashlib,subprocess,re
a=Path('artifacts');p=a/'pid-order-source-freeze.json';j=json.loads(p.read_text());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
head='783088ff0b6095c1e984e913b385a1cfceae6d7c'
assert j['Receipt']['SeedHead']==head and len(j['Files'])==3
files={f['Path']:f for f in j['Files']}
for path,f in files.items():
 assert sha(f['SourceContent'].encode())==f['Sha256'].upper()
 if f['BeforeSource'] is not None:
  blob=subprocess.check_output(['git','show',head+':'+path]);assert blob.decode().replace('\r\n','\n')==f['BeforeSource'].replace('\r\n','\n')
utils=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:erts/emulator/beam/utils.c'])
assert utils==(a/'pid-order-evidence/utils.c').read_bytes()
text=utils.decode();start=text.index('pid_common:');block=text[start:text.index('goto pop_next;',start)]
assert block.index('if (a_pid_ser != b_pid_ser)')<block.index('if (a_pid_num != b_pid_num)')<block.index('CMP_NODES(anode, bnode)')
macro=text[text.index('#define CMP_NODES'):text.index('bodyrecur:',text.index('#define CMP_NODES'))]
assert macro.index('erts_cmp_atoms')<macro.index('creation <')
new=files['src/Erlang.Terms/TermOrder.cs']['SourceContent'];old=files['src/Erlang.Terms/TermOrder.cs']['BeforeSource']
prefix='    private static int ';boundary='    private static int Codepoints('
expected=old.replace('return Identity(', 'return PidIdentity(',1)
at=expected.index('case (PortTerm x, PortTerm y):');expected=expected[:at]+expected[at:].replace('return Identity(','return PortIdentity(',1)
oldstart=expected.index(prefix+'Identity(');oldend=expected.index(boundary)
newstart=new.index(prefix+'PidIdentity(');newend=new.index(boundary)
assert expected[:oldstart]+new[newstart:newend]+expected[oldend:]==new
pid=new[newstart:new.index(prefix+'PortIdentity(')];port=new[new.index(prefix+'PortIdentity('):newend]
assert pid.index('ai.CompareTo(bi)')<pid.index('Codepoints(an, bn)')<pid.index('ac.CompareTo(bc)')
assert port.index('Codepoints(an, bn)')<port.index('ac.CompareTo(bc)')<port.index('ai.CompareTo(bi)')
reader=subprocess.check_output(['git','show',head+':src/Erlang.Serialization/ExternalTermFormat.cs']).decode()
layout=subprocess.check_output(['git','show',head+':src/Erlang.Serialization/EtfPidLayout.cs']).decode()
assert 'id | ((ulong)serial << EtfPidLayout.SerialBitShift)' in reader and 'SerialBitShift = 32;' in layout
cases=files['tools/Erlang.Differential/IdentifierOrderCases.cs']['SourceContent']
matches=re.findall(r'"(identifier-order-[^"]+)"\s*,\s*"(-module[^"\n]+)"',cases)
assert len(matches)==11 and len({name for name,_ in matches})==11
def packet(data):
 assert data[:3] in (bytes([131,88,119]),bytes([131,120,119]));tag=data[1];n=data[3];node=data[4:4+n].decode();body=data[4+n:]
 assert len(body)==12
 if tag==88:return ('pid',node,int.from_bytes(body[:4],'big'),int.from_bytes(body[4:8],'big'),int.from_bytes(body[8:],'big'))
 return ('port',node,int.from_bytes(body[:8],'big'),int.from_bytes(body[8:],'big'))
rows=[]
for name,source in matches:
 literals=re.findall(r'binary_to_term\(<<([0-9,]+)>>\)',source);assert len(literals)==2
 left,right=[packet(bytes(map(int,x.split(',')))) for x in literals]
 def nativekey(t):return (t[3],t[2],t[1],t[4]) if t[0]=='pid' else (t[1],t[3],t[2])
 order=(nativekey(left)>nativekey(right))-(nativekey(left)<nativekey(right))
 assert order==(0 if name.endswith('equal-payload-node-creation') else -1)
 rows.append({'Name':name,'Left':left,'Right':right,'SourceBasedOrder':order,'SourceSha256':sha(source.encode())})
compiled=files['tools/Erlang.Differential/CompiledModuleCases.cs']
assert compiled['SourceContent'].replace(', .. IdentifierOrderCases.Modules','')==compiled['BeforeSource']
receipt=j['Receipt'];assert receipt['OldGenerated']['Passed']==5 and receipt['OldGenerated']['Failed']==6 and receipt['NewGenerated']['Passed']==11 and receipt['NewGenerated']['Failed']==0 and not receipt['OriginalNativeExecuted']
result={'Status':'PASS SourceReviewNativeExecutionPending','FreezeSha256':sha(p.read_bytes()),'SeedHead':head,'OwnedPaths':list(files),'OnlyPidAndPortComparisonChanged':True,'ExistingCasesUnchanged':True,'NewDeclaredCases':11,'AllHandmadeEtfPacketsDecodedIndependently':True,'PackedUnsignedPidSerialThenNumber':True,'NodeOrderUnicodeScalarsThenUnsignedCreation':True,'PortNodeCreationThenUnsignedIdPreserved':True,'OriginalNativeExecuted':False,'PriorOldGeneratedFailures':6,'NewGeneratedPasses':11,'Cases':rows,'NodeMigrationConstraint':'Keep stable intrinsic identity and dictionary hash while changing node/creation wire views; actual native269 local encoding stable and intrinsic phash/map identity stable. No naive mutable Node participation in dictionary hashes.'}
(a/'pid-order-independent-source-review.json').write_text(json.dumps(result,indent=2)+'\n');print(sha((a/'pid-order-independent-source-review.json').read_bytes()))
