from pathlib import Path
import ast,json,hashlib
r=Path.cwd();old=r/'artifacts/part331-remote-new-review';d=r/'artifacts/part332-remote-new-review'
for n in ['passive-common.py','review-allocator-profile.py','review-memory-profile.py']:
 s=(old/n).read_text().replace('part331-remote-new-review','part332-remote-new-review').replace('HEAD331','HEAD332');(d/n).write_bytes(s.encode())
s=(old/'review-ci.py').read_text().replace('6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d','aaf48552bee429b0447d3e28e8274d34ad590d73').replace('38904967552','38911204008')
s=s.replace('part-331/final-main','part-332/final-candidate').replace('part-329/final-main','part-331/final-main').replace('6775','CURRENT').replace('6747','6775').replace('CURRENT','6822')
s=s.replace("stagepath=m.A/'root-part331-stage-index-guard.json'","stagepath=m.A/'proc-lib332-actual-stage-independent/report-rich.json'").replace('F9A7C4CCBA9543F73BDE7750CDD3E949F12DD912AEE89E0D6F225C7EF51429BA','5FA698CBBD7C47DC404DDBC2C29D12897C37C570614D46A8AF84D2E36E039580')
s=s.replace("(stage['StagedPaths'],stage['SourcePaths'],stage['CSharpPaths'])==(979,19,15)","(stage['Paths'],stage['SourcePaths'])==(946,25)")
s=s.replace('559184A858FEDF503164F7D303B9E17DEFAD80292533EE4AB7D9B7C221EBCA5E','8CA193FB17ED3418F6FD79127FE3CBB64AAFCC423AE32EB564A1B8E82D417E71').replace('8914E21E6485FB04504DE9B654E818524FD3C44CD009EC730B1F98E6E2E6C05A','FBB9A95750DD49DA34C8FF76B4CE10D2D1EC640C98A32555D9D4667BFDC899E7').replace('9E6119DDB243964E57FB9AC2A35B550B03B4B4495DE54AD6505BE6CB5A76FF34','56908A656DEA730DE49C960EB82B4C5656A46E5B94B0E77E75F98BAA07A12F95')
s=s.replace(" if x['Path'].endswith('.cs'):assert records[x['Path']]['ComparisonMode']=='BinaryExact'"," if x['Path'].endswith('.cs'):assert m.sha(blobs[x['Path']])==x['Sha256']")
s=s.replace('len(added)==28','len(added)==47').replace("sum(n.startswith('mfa/') for n in names)==515","sum(n.startswith('mfa/') for n in names)==521").replace('RegisteredMfas=515','RegisteredMfas=521').replace('PUBLISHED331','PUBLISHED332').replace('979_COMMITTED','946_COMMITTED').replace('Previous329','Previous331').replace('PlusTwentyEight=True','PlusFortySeven=True').replace('PublishedStagePathsRehashed=979','PublishedStagePathsRehashed=946').replace('PublishedSourcePaths=19','PublishedSourcePaths=25').replace('PublishedCSharpRawExact=15','PublishedCSharpRawExact=21')
ast.parse(s);(d/'review-ci.py').write_bytes(s.encode());p=d/'ci-reader-source-freeze.json';files=[]
for n in ['passive-common.py','review-ci.py']:
 q=d/n;files.append(dict(Path=n,Bytes=q.stat().st_size,Sha256=hashlib.sha256(q.read_bytes()).hexdigest().upper()))
p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_ACTUAL332_CURRENT_HEAD_CI_READER_NO_REPLAY_YET',PublishedHead='aaf48552bee429b0447d3e28e8274d34ad590d73',LocalCompositeStatusPreserved=True,Files=files),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
