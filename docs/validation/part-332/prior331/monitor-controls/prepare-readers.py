from pathlib import Path
import json,hashlib,ast
r=Path.cwd();old=r/'artifacts/part329-remote-new-review';new=r/'artifacts/part331-remote-new-review'
for name in ['passive-common.py','review-allocator-profile.py','review-memory-profile.py']:
 s=(old/name).read_text().replace('part329-remote-new-review','part331-remote-new-review').replace('HEAD329','HEAD331');(new/name).write_bytes(s.encode())
s=(old/'review-ci.py').read_text().replace('d83ed875d0c77939e20049afcb46fa297fee82dd','6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d').replace('38899129114','38904967552')
s=s.replace('part-329','CURRENT_PART').replace('part-328','part-329').replace('CURRENT_PART','part-331').replace('6747','CURRENT_COUNT').replace('6738','6747').replace('CURRENT_COUNT','6775').replace('root-part329','root-part331')
s=s.replace('15458BB90CB4D8BCE27F4EC07E1D91BBF3ECF6A05E293BCC0AACCC080236939A','F9A7C4CCBA9543F73BDE7750CDD3E949F12DD912AEE89E0D6F225C7EF51429BA').replace('(1170,12,7)','(979,19,15)').replace('FFBF65596ED330ACF20F82FC2BFBB3F319824A4B54184AB8ECCE2F66E4757F52','559184A858FEDF503164F7D303B9E17DEFAD80292533EE4AB7D9B7C221EBCA5E').replace('C6A8AFDB13B0E6241A765FFFFA48808489626CA8D6138093692CB2027D755D17','8914E21E6485FB04504DE9B654E818524FD3C44CD009EC730B1F98E6E2E6C05A').replace('911C51D20E8D98B19CE85B40947C4A6D4CBBF245BDF38452C0ABA7ED56C65C64','9E6119DDB243964E57FB9AC2A35B550B03B4B4495DE54AD6505BE6CB5A76FF34').replace("freeze['FinalSourceClosureSha256']","freeze['SourceClosureSha256']").replace('PUBLISHED329','PUBLISHED331').replace('1170_COMMITTED','979_COMMITTED').replace('Previous328','Previous329').replace('PublishedStagePathsRehashed=1170','PublishedStagePathsRehashed=979').replace('PublishedSourcePaths=12','PublishedSourcePaths=19').replace('PublishedCSharpRawExact=7','PublishedCSharpRawExact=15').replace('len(added)==9','len(added)==28').replace("sum(n.startswith('mfa/') for n in names)==509","sum(n.startswith('mfa/') for n in names)==515").replace('RegisteredMfas=509','RegisteredMfas=515').replace('PlusNine=True','PlusTwentyEight=True')
(new/'review-ci.py').write_bytes(s.encode())
s=(old/'review-ordinary.py').read_text().replace('d83ed875d0c77939e20049afcb46fa297fee82dd','6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d').replace('38899260501','38905274355').replace("oldroot=m.A/'part328-remote-beam310/ordinary/artifact-12031689572'","oldroot=m.A/'part329-remote-new-review/ordinary/artifact-12046826413'").replace("m.A/'part328-remote-beam310/ordinary-semantic-independent-guard.json'","m.A/'part329-remote-new-review/ordinary-semantic-independent-guard.json'").replace('bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612','d83ed875d0c77939e20049afcb46fa297fee82dd')
s=s.replace('5941','CURRENT_COUNT').replace('5933','5941').replace('CURRENT_COUNT','5962').replace('4990','4998').replace("n+(8 if key=='ModuleResults' else 0)","n+(21 if key=='ModuleResults' else 0)")
s=s.replace('pold,pnew=paths(328),paths(329);assert len(pold)==237 and len(pnew)==239','pold,pnew=paths(329),paths(331);assert len(pold)==239 and len(pnew)==242')
s=s.replace("{'tools/Erlang.Differential/NonTailRecursionCases.cs','tools/Erlang.Differential/NonTailRecursionCaseNames.cs'}","{'tools/Erlang.Differential/ProcLibCases.cs','tools/Erlang.Differential/ProcLibCaseNames.cs','tools/Erlang.Differential/ProcLibCaseSources.cs'}")
s=s.replace("newtext.replace('        .. UndefinedLambdaCases.Modules,\\n        .. NonTailRecursionCases.Modules','        .. UndefinedLambdaCases.Modules')","newtext.replace('        .. NonTailRecursionCases.Modules,\\n        .. ProcLibCases.Modules','        .. NonTailRecursionCases.Modules')")
start=s.index('managedpath=');end=s.index("for k in ['OriginalOtpDefectExclusions'",start)
block='''localtests=m.read(m.ROOT/'docs/validation/part-331/final-main/tests.json'); assert localtests['Passed']==6775 and localtests['Failed']==0
localnames={x['Name'] for x in localtests['Tests'] if x['Status']=='Passed'}
focuspath=m.A/'proc-lib331-actual3/focus-fixtures.json';assert m.sha(focuspath.read_bytes())=='8F4401C8D0BA2D0CE376D9057D180C15D6C106C4E354303495C79DA4E30402E4'
focus=m.read(focuspath);assert focus['Status']=='PASS' and focus['Cases']==[dict(Name='Erlang.Tests.ProcLibMfaContracts:RunAllFixtures',Status='PASS',ElapsedMilliseconds=1697)]
for native in v['ModuleResults'][-21:]:
 assert native['Name'].startswith('proc-lib-local-') and 'oracle/compiled-module/'+native['Name'] in localnames
 assert native['SourceSha256']==m.sha(native['Source'].encode()) and native['Expected']==native['FixtureExpected']==native['Actual'] and native['CallerContext']=='UntrappedFresh'
 assert native['CompilerPolicy']=='Default' and native['Passed'] and native['StandardStreamsMatch']
 assert all(native.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
'''
s=s[:start]+block+s[end:]
s=s.replace('PUBLISHED329','PUBLISHED331').replace('Previous328','Previous329').replace('Current329','Current331').replace('Prior328','Prior329').replace('AllOld237','AllOld239').replace("'CompiledModules':4998","'CompiledModules':5019").replace("'NewSourceFiles':2","'NewSourceFiles':3").replace("'NewOriginalCases':8","'NewOriginalCases':21").replace("'Managed8Sha256':m.sha(managedpath.read_bytes())","'Managed21GroupReceiptSha256':m.sha(focuspath.read_bytes())").replace('EIGHT_NEW_ACTUAL_MANAGED_CASES','TWENTY_ONE_NEW_PROC_LIB_ORIGINAL_AND_SDK_CASES')
(new/'review-ordinary.py').write_bytes(s.encode())
rows=[]
for name in ['passive-common.py','review-ci.py','review-ordinary.py','review-allocator-profile.py','review-memory-profile.py']:
 p=new/name;ast.parse(p.read_text());rows.append(dict(Path=name,Bytes=p.stat().st_size,Sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()))
p=new/'passive-readers-source-freeze.json';p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_331_PASSIVE_READERS_PREPARED_BEFORE_DOWNLOAD',Head='6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d',Files=rows),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
