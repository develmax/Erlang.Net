import hashlib,json,pathlib
root=pathlib.Path(__file__).parent;namespace={'__file__':str(root/'unused')}
exec((pathlib.Path('D:\\Distr\\Work\\Codex\\Erlang.Net\\Erlang.Net\\artifacts\\ordsets-native-selection-audit\\fun-map-key-contracts\\raw-row-diff.py')).read_text().split('\ngold = ')[0],namespace);tree=namespace['tree']
def value(data,node):
 tag=node['tag'];start=node['start']+1
 if tag==97:return data[start]
 if tag==98:return int.from_bytes(data[start:node['end']],'big',signed=True)
 if tag==107:return data[start+2:node['end']].decode('utf-8')
 if tag==112:return {'OpaqueFunBytesSha256':hashlib.sha256(data[node['start']:node['end']]).hexdigest().upper()}
 if node['atom'] is not None:return node['atom']
 return [value(data,c) for c in node['children']]
gold=(root/'original.etf').read_bytes();expected=tree(gold);reports={}
for backend in ['Managed','Beam']:
 actual=(root/(backend+'.actual.etf')).read_bytes();observed=tree(actual);diffs=[];passed=[]
 for i,(e,a) in enumerate(zip(expected['children'],observed['children'])):
  if gold[e['start']:e['end']]==actual[a['start']:a['end']]:passed.append(e['children'][0]['atom']);continue
  ei,ai=e['children'][1],a['children'][1]
  diffs.append({'Row':i,'Case':e['children'][0]['atom'],'InputBytesIdentical':gold[ei['start']:ei['end']]==actual[ai['start']:ai['end']], 'Original':value(gold,e['children'][2]),'Actual':value(actual,a['children'][2])})
 reports[backend]={'Rows':12,'IdenticalRows':len(passed),'IdenticalCases':passed,'Differences':diffs}
(root/'raw-row-differences.json').write_text(json.dumps(reports,indent=2)+'\n');print(json.dumps(reports,indent=2))
