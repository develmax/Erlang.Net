"""Passive actual-gate audit only; deliberately accepts pending Root review and never mutates Main."""
import sys,importlib.util,json
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common312',Path(__file__).with_name('common312.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1],reviewed=False)
print(json.dumps({'Status':'ACTUAL_LOCAL_GATES_PASS_ROOT_REVIEW_STILL_REQUIRED','SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'Passed':c['ExpectedPassed'],'EvidenceRows':len(index),'RootExecutionApproved':False,'MainEdited':False,'GitMutation':False}))
