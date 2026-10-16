"""Pure source parser for the two published 332 fixture catalogs; no Erlang or C# execution."""
import json,re
def constants(text):
 out={}
 for name,expression in re.findall(r'(?m)^\s*(?:public|private) const string (\w+) = (.*);\s*$',text):
  parts=re.findall(r'"(?:\\.|[^"\\])*"|\b[A-Za-z_]\w*\b',expression)
  assert ''.join(parts)==re.sub(r'\s*\+\s*','',expression),name
  out[name]=''.join(json.loads(x) if x.startswith('"') else out[x] for x in parts)
 return out
def contracts(blobs):
 prefix='tools/Erlang.Differential/';result=[]
 for group,count in [('ProcLibStartup',31),('ReceiveWait',8)]:
  names=constants(blobs[prefix+group+'CaseNames.cs'].decode());sources=constants(blobs[prefix+group+'CaseSources.cs'].decode());catalog=blobs[prefix+group+'Cases.cs'].decode()
  pattern=(r'Case\(\s*' if group=='ProcLibStartup' else r'\(')+group+r'CaseNames\.(\w+),\s*'+group+r'CaseSources\.(\w+)'
  pairs=re.findall(pattern,catalog);assert len(pairs)==count
  if group=='ProcLibStartup':
   expected={k:'{ok,ok}' for k,_ in pairs}
   for k in ['StartThree','StartFour','LinkThree','Registered','MaximumTimeout']:expected[k]='{ok,ready}'
   for k in ['LinkFour','FailTwo','FailThree','FailStack','FailBadClass','FailMalformed']:expected[k]='{ok,true}'
   for k in ['AtomTimeout','NegativeTimeout','LargeTimeout','FloatTimeout']:expected[k]='{ok,{caught,false}}'
   expected['ErrorReturn']='{ok,{true,{error,payload}}}'
  else:
   expected={k:'{ok,matched}' for k,_ in pairs}
   for k in ['UnmatchedInvalid','ExpressionError']:expected[k]='{ok,{caught,preserved}}'
   expected['ExpressionBeforeMatch']='{ok,{{matched,evaluated},evaluated}}'
  result.extend(dict(Name=names[n],Source=sources[s],Expected=expected[n]) for n,s in pairs)
 assert len(result)==39
 return result
