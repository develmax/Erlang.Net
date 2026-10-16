from pathlib import Path
import ast,json,hashlib
r=Path.cwd();old=r/'artifacts/part332-remote-new-review';d=r/'artifacts/part333-remote-new-review'
for n in ['passive-common.py','review-allocator-profile.py','review-memory-profile.py']:
 s=(old/n).read_text().replace('part332-remote-new-review','part333-remote-new-review').replace('HEAD332','HEAD333');(d/n).write_bytes(s.encode())
s=(old/'review-ci.py').read_text().replace('aaf48552bee429b0447d3e28e8274d34ad590d73','e9725c4b2eef68014aeb70afd0abcf9f646065fd').replace('38911204008','38920570561')
s=s.replace('part-332/final-candidate','CURRENT_PART').replace('part-331/final-main','part-332/final-candidate').replace('CURRENT_PART','part-333/final-candidate').replace('6822','CURRENT_COUNT').replace('6775','6822').replace('CURRENT_COUNT','6871')
s=s.replace("m.A/'proc-lib332-actual-stage-independent/report-rich.json'","m.A/'proc-lib333-actual-stage-independent/report.json'").replace('5FA698CBBD7C47DC404DDBC2C29D12897C37C570614D46A8AF84D2E36E039580','3FF02F66120FDFC4966B54E52DC669C14871950DA8627EF55430B62F87D83B9F').replace("(stage['Paths'],stage['SourcePaths'])==(946,25)","(stage['Paths'],stage['SourcePaths'])==(947,18)")
s=s.replace('8CA193FB17ED3418F6FD79127FE3CBB64AAFCC423AE32EB564A1B8E82D417E71','AA5249A3F81F74F825626EAAF1DACB1E44564105B6BD0589DB5A0DD9CA4046C4').replace('FBB9A95750DD49DA34C8FF76B4CE10D2D1EC640C98A32555D9D4667BFDC899E7','368E328BD251F219270018D37E2FAE4681460A83808B9F741C78B34DBD6C5E72').replace('56908A656DEA730DE49C960EB82B4C5656A46E5B94B0E77E75F98BAA07A12F95','BBECB7B32C2807BB7DC74F06AD500C5517283468864F0203FE35C54F084DC2CE')
s=s.replace('len(added)==47','len(added)==49').replace("sum(n.startswith('mfa/') for n in names)==521","sum(n.startswith('mfa/') for n in names)==528").replace('RegisteredMfas=521','RegisteredMfas=528').replace('PUBLISHED332','PUBLISHED333').replace('946_COMMITTED','947_COMMITTED').replace('Previous331','Previous332').replace('PlusFortySeven=True','PlusFortyNine=True').replace('PublishedStagePathsRehashed=946','PublishedStagePathsRehashed=947').replace('PublishedSourcePaths=25','PublishedSourcePaths=18').replace('PublishedCSharpRawExact=21','PublishedCSharpRawExact=14')
ast.parse(s);(d/'review-ci.py').write_bytes(s.encode());rows=[]
for n in ['passive-common.py','review-ci.py']:
 p=d/n;rows.append(dict(Path=n,Bytes=p.stat().st_size,Sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()))
p=d/'ci-reader-source-freeze.json';p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_EXACT_PUBLISHED333_CI_READER',Head='e9725c4b2eef68014aeb70afd0abcf9f646065fd',ExpectedTests=6871,ExpectedIntegrationOutcomes=11,ActualObservedSuccessNotYetClaimed=True,Files=rows),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
