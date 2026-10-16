"""Pure parser of the deliberately limited published C# string-constant fixture catalog."""
import json,re
def contracts(blobs):
 def constants(text):
  out={}
  for name,expression in re.findall(r'(?m)^\s*(?:public|private) const string (\w+) = (.*);\s*$',text):
   parts=re.findall(r'"(?:\\.|[^"\\])*"|\b[A-Za-z_]\w*\b',expression)
   assert ''.join(parts)==re.sub(r'\s*\+\s*','',expression),name
   out[name]=''.join(json.loads(x) if x.startswith('"') else out[x] for x in parts)
  return out
 prefix='tools/Erlang.Differential/';names=constants(blobs[prefix+'ProcLibCaseNames.cs'].decode());sources=constants(blobs[prefix+'ProcLibCaseSources.cs'].decode());catalog=blobs[prefix+'ProcLibCases.cs'].decode()
 pairs=re.findall(r'Case\(\s*ProcLibCaseNames\.(\w+),\s*ProcLibCaseSources\.(\w+)',catalog);assert len(pairs)==21
 expected={k:'{ok,ok}' for k,_ in pairs}
 expected.update(MfaSpawn='{ok,{true,{oracle_proc_lib,child,1}}}',MfaLink='{ok,true}',FunSpawn='{ok,{true,true,0}}',FunLink='{ok,true}',AckOne='{ok,{ok,true}}',AckTwo='{ok,{ok,true}}',Registered='{ok,proc_lib_parent}',ImproperAncestors='{ok,{ok,true}}')
 return [dict(Name=names[n],Source=sources[s],Expected=expected[n]) for n,s in pairs]
