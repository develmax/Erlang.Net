from pathlib import Path
import json,importlib.util,hashlib
r=Path.cwd();out=r/'artifacts/library404-io-printable-command-contract-review1';p=r/'tools/Erlang.NifTermCallbackOriginalObservations/passive_etf.py';s=importlib.util.spec_from_file_location('printable_contract_passive',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
rows=[];files=[]
for mode,wanted in [('default','latin1'),('latin1','latin1'),('unicode','unicode')]:
 reports=list((r/'artifacts/library404-permanent-comparisons'/('printable404-'+mode)).glob('*/comparison.json'));assert len(reports)==1
 report=reports[0];rec=json.loads(report.read_bytes());assert rec['ExaminedCompleteOutcomeRows']==180 and rec['WholeTypedEqualRows']==156
 assert len(rec['Differences'])==24
 names=[x['Original']['Items'][0]['Name'] for x in rec['Differences']]
 assert set(names)=={'error_bad_format','error_missing_argument','error_bad_width','error_bad_character'}
 assert all(names.count(n)==6 for n in set(names))
 orig=r/'artifacts/library404-printable-original1'
 for route,on,an in [('values',mode+'-values.etf','actual-values.etf'),('profiles',mode+'-stacks.etf','actual-stacks.etf')]:
  op=orig/on;ap=report.parent/an;ov=m.decode_exact(op.read_bytes());av=m.decode_exact(ap.read_bytes())
  files += [dict(Path=str(q.relative_to(r)),Sha256=hashlib.sha256(q.read_bytes()).hexdigest().upper()) for q in [op,ap]]
  def first_range(v):
   assert v[0]=='list'
   return next(z for z in v[1] if z[0]=='tuple' and z[1][0]==('atom','range'))
  if route=='values': pairs=[('value',first_range(ov),first_range(av))]
  else:
   assert ov[0]==av[0]=='list' and len(ov[1])==len(av[1])==5
   pairs=[]
   for left,right in zip(ov[1],av[1]):
    assert left[0]==right[0]=='tuple' and left[1][:2]==right[1][:2]
    pairs.append((repr(left[1][0]),first_range(left[1][2]),first_range(right[1][2])))
  for depth,left,right in pairs:
   expected=('tuple',(('atom','range'),('tuple',(('atom','returned'),('atom',wanted)))))
   assert left==right and left==expected,(mode,depth,left,right)
   rows.append(dict(Mode=mode,Depth=depth,ReturnedAtom=wanted,WholeCaseEqual=True,CommandErrorPath=False))
 files.append(dict(Path=str(report.relative_to(r)),Sha256=hashlib.sha256(report.read_bytes()).hexdigest().upper()))
(out/'range-rows.json').write_text(json.dumps(dict(Examined=18,Passed=len(rows),Rows=rows,Whole540SuitePassed=False,Existing72FormatDifferencesRetained=True),indent=2)+'\n',encoding='utf-8')
(out/'receipt-bindings.json').write_text(json.dumps(files,indent=2)+'\n',encoding='utf-8')
print('PASSIVE_COMMAND_ROWS',len(rows))


