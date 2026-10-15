import pathlib,json,hashlib,subprocess
p=pathlib.Path('artifacts');sha=lambda b:hashlib.sha256(b).hexdigest().upper();f=p/'dist_next-distribution-trace-permanent-promotion-source-freeze-revision2.json';b=json.loads(f.read_bytes());old=json.loads((p/'dist_next-distribution-trace-source-freeze-final-revision8.json').read_bytes());root=pathlib.Path(b['Candidate']);program=None
for r in b['Files']:
 raw=(root/r['Path']).read_bytes();assert sha(raw)==r['Sha256'] and raw.decode()==r['SourceContent']
 if r['Path']=='tests/Erlang.Tests/Program.cs':program=r
 else:assert r['SourceContent']==next(o['SourceContent'] for o in old['Files'] if o['Path']==r['Path'])
assert program is not None
text=program['SourceContent'];line='Test(Erlang.Tests.DistributionTraceHostNames.Test, Erlang.Tests.DistributionTraceHostChecks.Run);';assert text.count(line)==1;start=text.index(line);assert start<text.index('int failed =')<text.index('foreach (');removed=text[:start]+text[start+len(line):];removed=removed.replace('\r\n\r\nint failed =','\r\nint failed =',1) if False else removed
before=program['BeforeSourceContent'];delta=text[len(text[:start]):];nl='\r\n' if text[start+len(line):].startswith('\r\n') else '\n';assert text[:start]+text[start+len(line)+len(nl):]==before
committed=subprocess.check_output(['git','cat-file','blob',b['BaseMainHead']+':tests/Erlang.Tests/Program.cs']).decode();assert before.replace('\r\n','\n')==committed.replace('\r\n','\n')
assert len(b['Files'])==22
receipt={'Status':'PASS_PERMANENT22_SOURCE_REACHABLE','Freeze':str(f),'FreezeSha256':sha(f.read_bytes()),'Files':22,'Other21ExactlyExecutedFinal8':True,'ProgramSingleInsertionBeforeTestLoop':True,'ProgramInsertionInverseWholeMainSourceExact':True,'HistoricalRejectedRevision1Preserved':'native269-tt-permanent22-revision1-unreachable-source-finding.json','ExpectedNewFullCountUnobserved':6562,'ReviewerBuildExecuted':False,'MainEdited':False,'NativeExecuted':False};o=p/'native269-tt-permanent22-revision2-independent-source-review.json';o.write_bytes((json.dumps(receipt,indent=2)+'\n').encode());print(o,sha(o.read_bytes()))
