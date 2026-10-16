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
 prefix='tools/Erlang.Differential/ProcLibOption';names=constants(blobs[prefix+'CaseNames.cs'].decode());sources=constants(blobs[prefix+'CaseSources.cs'].decode());catalog=blobs[prefix+'Cases.cs'].decode()
 pattern=r'new\(\s*ProcLibOptionCaseNames\.(\w+),\s*ProcLibOptionCaseSources\.(\w+),\s*Term\.Tuple\(Term\.A\(OracleOutcomeTags\.Success\), Term\.A\(ProcLibOptionCaseNames\.Ok\)\)'
 pairs=re.findall(pattern,catalog);assert len(pairs)==41 and names['Ok']=='ok'
 return [dict(Name=names[n],Source=sources[s],Expected='{ok,ok}') for n,s in pairs]
