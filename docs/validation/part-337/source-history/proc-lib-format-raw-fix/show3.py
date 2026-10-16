import json
from pathlib import Path
p=Path('artifacts/root-formatter-original-full-comparison3.json')
def pretty(t):
 if t[0]=='list':
  xs=t[1]; tail=t[2]
  if tail==['nil'] and xs and all(x[0]=='integer' and 0<=x[1]<128 for x in xs): return ''.join(chr(x[1]) for x in xs)
  return {'list':[pretty(x) for x in xs], 'tail':pretty(tail)}
 if t[0]=='tuple':return {'tuple':[pretty(x) for x in t[1]]}
 if t[0]=='integer':return t[1]
 if t[0]=='nil':return []
 return t
for r in json.loads(p.read_bytes())['Rows']:
 if not r['Passed'] and r['Track']=='report':
  print(r['Name']);print('OTP',json.dumps(pretty(r['Original'])));print('SDK',json.dumps(pretty(r['Sdk'])))


