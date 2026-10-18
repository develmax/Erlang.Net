"""Prepare reviewable documentation only; writes only a fresh directory under this proposal."""
from pathlib import Path
import argparse,json,hashlib,collections,re
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def key(x):return x['Module'],x['Function'],x['Arity']
def main():
 a=argparse.ArgumentParser();a.add_argument('--completion',type=Path,required=True);a.add_argument('--output',type=Path,required=True);q=a.parse_args();out=q.output.resolve();assert out.is_relative_to(O.resolve()) and not out.exists(),'fresh owned output directory required'
 c=J(q.completion);assert c['ImplementationPublished'] is False
 assert c['SourceAuditAdmitted'] and c['AllNew30CompleteContractArgumentsAdmitted'] and c['OriginalAndComparisonsAccepted'],'final source/original gates required'
 assert c['ExpectedPassedTests'] is not None and c['IntegrationOutcomes']==11 and c['Packages']==2
 report=R/c['FullReport'];receipt=R/c['FullReceipt'];assert H(report)==c['FullReportSha256'] and H(receipt)==c['FullReceiptSha256'];r=J(report);exit=J(receipt)
 assert exit['Settled'] and not exit['TimedOut'] and exit['ExitCode']==0 and not exit['Failures'];assert r['Passed']==c['ExpectedPassedTests'] and r['Failed']==0
 tests=r['Tests'];assert len(tests)==r['Passed'] and all(t['Status']=='Passed' for t in tests);names=[t['Name'] for t in tests];assert len(names)==len(set(names))
 baseline=R/'artifacts/queue-integration368/baseline';oldnames=[t['Name'] for t in J(baseline/'tests.json')['Tests']];assert len(oldnames)==7561;oldset=set(oldnames);assert [n for n in names if n in oldset]==oldnames,'old ordered names must be preserved'
 registry=J(R/c['FinalSupportedRegistry']);old=J(baseline/'supported-mfas.json');by={key(x):x for x in registry};assert len(by)==len(registry)==647 and len(old)==617
 corrections={key(x):x['Limits'] for x in c.get('OldRegistryLimitsCorrections',[])};oldqueue={key(x) for x in old if x['Module']=='queue'}
 assert set(corrections)<=oldqueue and len(corrections)==len(c.get('OldRegistryLimitsCorrections',[])),'only explicitly approved old queue8 Limits descriptions may change'
 for x in old:
  expected=dict(x)
  if key(x) in corrections:
   assert corrections[key(x)]!=x['Limits'] and corrections[key(x)],'explicit changed nonempty Limits required';expected['Limits']=corrections[key(x)]
  assert by[key(x)]==expected,'old row custom metadata changed beyond approved queue Limits correction'
 oldkeys={key(x) for x in old};new=[x for x in registry if key(x) not in oldkeys];assert len(new)==30 and all(x['Module']=='queue' for x in new)
 ledger=J(R/c['FinalCompatibilityLedger']);entries=ledger['Entries'];assert len(entries)==647 and {key(x) for x in entries}==set(by)
 counts=collections.Counter(x['Status'] for x in entries);assert counts=={'Verified compatible':76,'Known differences':30,'Needs verification':541}
 queue=[x for x in entries if x['Module']=='queue'];assert len(queue)==38 and all(x['Status']=='Verified compatible' for x in queue)
 guards=ledger['VerifiedSourceFiles'];assert len(guards)==c['SourceGuardCount']==382
 for g in guards:assert H(R/g['Path'])==g['Sha256'],'stale source guard '+g['Path']
 readiness=J(R/c['FinalReadinessJson']);assert readiness['RegisteredMfas']==647 and readiness['VerifiedCompatibleMfas']==76 and readiness['KnownDifferenceMfas']==30 and readiness['NeedsVerificationMfas']==541 and readiness['LocalPassedTests']==r['Passed']
 assert len(readiness['MfaCompatibility'])==647 and readiness['VerifiedSourceFiles']==guards
 passed=r['Passed'];delta=passed-7561;link=c['EvidenceLink'];assert delta>=30
 summary=f'Part368 completes all30 remaining queue exports against pinned OTP-29.1.1. Delta **+30 APIs / +30 complete contracts / +{delta} tests**; totals **647 APIs /76complete/30Known/541pending** across16 modules. Queue: **38/38 present and38/38 complete**; proplists22/22 and erlang16 complete remain unchanged. Current-source validation: **{passed}/{passed} tests,11 integration outcomes,2 local packages**. Original Linux CI39078144460 and Windows Main are separate evidence. Implementation remains unpublished. [Evidence]({link}).'
 out.mkdir(parents=True);written=[]
 def write(path,text):
  f=out/path;f.parent.mkdir(parents=True,exist_ok=True);f.write_text(text,encoding='utf-8');written.append({'Target':path,'PreparedSha256':H(f),'BaseSha256':H(R/path) if (R/path).exists() else None})
 for doc in ['docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/NEXT_STEPS.md']:
  text=(R/doc).read_text();i=text.index('\n')+1
  if doc=='docs/NEXT_STEPS.md':
   begin=text.index('\n\n')+2;end=text.index('\n\n',begin);write(doc,text[:begin]+summary+' Next60%: prepared binary5; next40%: existing contract batches in dependency order.'+text[end:])
  else:write(doc,text[:i]+'\n'+summary+'\n'+text[i:])
 text=(R/'docs/WORK_PLAN.md').read_text();start=text.index('## Active local stage368');end=text.index('\n## ',start+3);text=text[:start]+'## Completed local stage368 — queue38\n\n'+summary+'\n\nNext60%: prepared binary5/other absent APIs with complete contracts. Next40%: existing pending contract batches, preserving382 reviewed source guards.\n'+text[end:]
 table=(O/'module-table.md').read_text();start=text.index('| Module | Implemented / pinned declarations');end=text.index('\n## ',start);text=text[:start]+table+'\n'+text[end:];write('docs/WORK_PLAN.md',text)
 canonical=(R/c['FinalReadinessMarkdown']).read_bytes();assert b'100% compatible: **76**' in canonical and b'| queue | 38 |' in canonical,'final canonical emitter required'
 target='docs/MODULE_READINESS.md';f=out/target;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(canonical);assert H(f)==H(R/c['FinalReadinessMarkdown']);written.append({'Target':target,'PreparedSha256':H(f),'BaseSha256':H(R/target),'CanonicalByteExact':True})
 completion=(O/'part368-completion.md.in').read_text().replace('{{PASSED}}',str(passed)).replace('{{TEST_DELTA}}',str(delta)).replace('{{EVIDENCE_LINK}}',link)
 volume=(R/'docs/changelog/0059.md').read_text();assert volume.count('## Part368 ')==1;start=volume.index('## Part368 ');part=volume[start:].replace('**Status:** in progress; implementation unpublished.','**Status:** complete locally; implementation unpublished.',1)+'\n'+completion
 combined=volume[:start]+part;index=(R/'CHANGELOG.md').read_text()
 if len(combined.encode('utf-8'))<=32768 and len(combined.splitlines())<=400:
  write('docs/changelog/0059.md',combined);index=index.replace('active; queue core completed locally, remaining exports in progress','active; all38 queue contracts completed locally, unpublished')
 else:
  assert not (R/'docs/changelog/0060.md').exists();write('docs/changelog/0059.md',volume[:start]);write('docs/changelog/0060.md','# Detailed work changelog — volume0060\n\n'+part);index=re.sub(r'^- \[0059.md\].*$', '- [0059.md](docs/changelog/0059.md) — part367 (closed)\n- [0060.md](docs/changelog/0060.md) — part368 (active; queue38 complete locally, unpublished)',index,flags=re.M)
 write('CHANGELOG.md',index)
 (out/'copy-plan.json').write_text(json.dumps({'Files':written,'CompletionInputSha256':H(q.completion),'FullReportSha256':H(report),'ActualMainModified':False},indent=2)+'\n');print(json.dumps({'PreparedFiles':len(written),'PassedTests':passed,'TestDelta':delta,'Output':str(out),'MainModified':False}))
if __name__=='__main__':main()
