"""Pure source parser for the two published 332 fixture catalogs; no Erlang or C# execution."""
import json,re
def constants(text):
 out={}
 for name,expression in re.findall(r'(?m)^\s*(?:public|private) const string (\w+) = (.*);\s*$',text):
  matches=list(re.finditer(r'"(?:\\.|[^"\\])*"|\b[A-Za-z_]\w*\b',expression));end=0;parts=[]
  for index,match in enumerate(matches):
   assert expression[end:match.start()].strip()==('+' if index else ''),name
   parts.append(match.group());end=match.end()
  assert not expression[end:].strip(),name
  out[name]=''.join(json.loads(x) if x.startswith('"') else out[x] for x in parts)
 return out
def contracts(blobs):
 prefix='tools/Erlang.Differential/ProcLibMetadata';names=constants(blobs[prefix+'CaseNames.cs'].decode());sources=constants(blobs[prefix+'CaseSources.cs'].decode());catalog=blobs[prefix+'Cases.cs'].decode()
 pattern=r'new\(\s*ProcLibMetadataCaseNames\.(\w+),\s*ProcLibMetadataCaseSources\.(\w+),\s*Term\.Tuple\(Term\.A\(OracleOutcomeTags\.Success\), Term\.A\(ProcLibMetadataCaseNames\.Ok\)\)'
 pairs=re.findall(pattern,catalog);assert len(pairs)==43 and names['Ok']=='ok'
 return [dict(Name=names[n],Source=sources[s],Expected='{ok,ok}') for n,s in pairs]
