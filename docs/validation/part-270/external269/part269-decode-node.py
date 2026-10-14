from pathlib import Path
import json,struct
def decode(data):
 p=1
 assert data[0]==131
 def take(n):
  nonlocal p
  x=data[p:p+n];p+=n;assert len(x)==n;return x
 def num(n):return int.from_bytes(take(n),'big')
 def term():
  tag=num(1)
  if tag==97:return num(1)
  if tag==98:return int.from_bytes(take(4),'big',signed=True)
  if tag==70:return struct.unpack('>d',take(8))[0]
  if tag in (100,118):return take(num(2)).decode('utf-8' if tag==118 else 'latin1')
  if tag in (115,119):return take(num(1)).decode('utf-8' if tag==119 else 'latin1')
  if tag in (104,105):return [term() for _ in range(num(1 if tag==104 else 4))]
  if tag==106:return []
  if tag==107:return take(num(2)).decode('latin1')
  if tag==108:
   xs=[term() for _ in range(num(4))];tail=term();return xs if tail==[] else {'List':xs,'Tail':tail}
  if tag==109:return {'BinaryHex':take(num(4)).hex()}
  if tag==116:
   xs=[(term(),term()) for _ in range(num(4))];return {k:v for k,v in xs}
  if tag in (110,111):
   n=num(1 if tag==110 else 4);sign=num(1);return int.from_bytes(take(n),'little')*(-1 if sign else 1)
  if tag in (88,103):return {'Pid':[term(),num(4),num(4),num(4 if tag==88 else 1)]}
  if tag in (114,90):
   n=num(2);node=term();creation=num(4 if tag==90 else 1);return {'Ref':[node,creation,[num(4) for _ in range(n)]]}
  raise ValueError((tag,p))
 result=term();assert p==len(data),(p,len(data));return result
if __name__=='__main__':
 root=Path('artifacts/part-269-pinned-node-identity/node-identity');rows={}
 for directory in ['start','start-stop','start-stop-restart','pending-handle']:
  rows[directory]={p.name:decode(p.read_bytes()) for p in (root/directory).glob('*.etf') if not p.name.endswith('-local.etf')}
 rows['Runtime']=decode((root/'runtime.etf').read_bytes())
 Path('artifacts/part269-node-decoded-observations.json').write_text(json.dumps(rows,indent=2)+'\n')
 for name,files in rows.items():
  print(name,json.dumps(files)[:200] if name=='pending-handle' else '')
