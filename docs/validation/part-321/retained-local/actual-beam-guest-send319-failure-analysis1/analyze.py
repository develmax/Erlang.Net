from pathlib import Path
import re,struct,json,hashlib
r=Path.cwd();out=Path(__file__).resolve().parent;c=r/'artifacts/actual-beam-guest-send319-composed2/candidate';b=(c/'tests/Erlang.Tests/Resources/Beam/guest_connected_send319.beam').read_bytes();sha=lambda z:hashlib.sha256(z).hexdigest().upper()
assert sha(b)=='2306C0C21F7F0E95BE4934C0230FA042B8EF864EF67438EBB637274AE8D5136D'
chunks={};pos=12
while pos<len(b):
 name=b[pos:pos+4].decode();n=struct.unpack('>I',b[pos+4:pos+8])[0];chunks[name]=b[pos+8:pos+8+n];pos+=8+(n+3)//4*4
class Cursor:
 def __init__(self,b):self.b=b;self.p=0
 def byte(self):z=self.b[self.p];self.p+=1;return z
 def compact(self):
  p=self.byte();tag=p&7
  if not p&8:return tag,p>>4
  if not p&16:return tag,((p>>5)<<8)|self.byte()
  n=p>>5
  if n<7:n+=2
  else:t,n=self.compact();assert t==0;n+=9
  z=self.b[self.p:self.p+n];assert len(z)==n;self.p+=n
  return tag,int.from_bytes(z,'big',signed=tag==1)
at=chunks['AtU8'];count=struct.unpack('>i',at[:4])[0];a=Cursor(at[4:]);atoms=[None]
for _ in range(abs(count)):
 n=a.compact()[1] if count<0 else a.byte();atoms.append(a.b[a.p:a.p+n].decode());a.p+=n
catalog_text=(c/'src/Erlang.Runtime/BeamOpcodeCatalog.cs').read_text();catalog={int(n):(name,int(arity)) for n,name,arity in re.findall(r'new\(\s*(\d+),\s*"([^"\n]+)",\s*(\d+),',catalog_text)};assert len(catalog)>180
code=chunks['Code'];headerlen=struct.unpack('>I',code[:4])[0];cur=Cursor(code[4+headerlen:]);instructions=[];function=None
while cur.p<len(cur.b):
 offset=cur.p;opcode=cur.byte();name,arity=catalog[opcode];operands=[];index=0
 while index<arity:
  tag,val=cur.compact()
  if tag==7:
   if val==1:
    t,extra=cur.compact();assert t==0 and index+1==arity;arity+=extra;operands.append(('count',extra));index+=1;continue
   if val in (2,4):t,n=cur.compact();assert t==0;operands.append(('extended'+str(val),n))
   elif val==5:
    reg=cur.compact();t,n=cur.compact();assert t==0;operands.append(('typed',reg,n))
   elif val==3:
    t,n=cur.compact();assert t==0
    pairs=[(cur.compact(),cur.compact()) for _ in range(n)];operands.append(('allocation',pairs))
   else:raise AssertionError(('unsupported decoder extension',offset,val))
  else:operands.append((tag,val))
  index+=1
 if name=='func_info':function=atoms[operands[1][1]]+'/'+str(operands[2][1])
 instructions.append({'Index':len(instructions),'Offset':offset,'Opcode':opcode,'Name':name,'Function':function})
 assert name!='int_code_end' or cur.p==len(cur.b)
constant={}
for source in (c/'src/Erlang.Runtime').glob('Beam*.cs'):
 text=source.read_text()
 cls=re.search(r'(?:class|struct)\s+(\w+)',text)
 if cls:
  for n,v in re.findall(r'const string (\w+)\s*=\s*"([^"\n]*)"',text):constant[cls[1]+'.'+n]=v
link=(c/'src/Erlang.Runtime/BeamLinkedCodeReader.cs').read_text();cases=re.findall(r'case (\w+\.\w+):',link.split('private static void ValidateRoles',1)[1].split('private static void CheckImport',1)[0]);supported={constant[x] for x in cases};unsupported=[x for x in instructions if x['Name'] not in supported]
assert unsupported
bang=[x for x in instructions if x['Function']=='bang/2'];bang=[x for x in bang if x['Name'] not in ('label','line','func_info')]
report={'Status':'PASS_PASSIVE_EXACT_FAILED_BEAM_OPCODE_ROLE_ANALYSIS','BeamSha256':sha(b),'DecodedInstructions':len(instructions),'ExactCodeBytesConsumed':cur.p,'FirstUnsupportedRole':unsupported[0],'UnsupportedRoles':sorted(set(x['Name'] for x in unsupported)),'UnsupportedOccurrences':unsupported,'BangBodyDecodedInstructions':bang,'WholeModuleRoleValidationBeforeExportSelection':True,'ActualFailureBeforeBangExecution':True,'NoAllowlistOrBeamMutation':True,'SdkVmCompilerExecutedByAnalysis':False}
(out/'analysis.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'FirstUnsupported':unsupported[0],'Unsupported':report['UnsupportedRoles'],'Bang':bang}))
