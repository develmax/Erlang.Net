from common import *
import argparse
p=argparse.ArgumentParser()
for x in ['repository','expected-head','manifest-sha256']:p.add_argument('--'+x,required=True)
a=p.parse_args();root,tool,plan,*_=verify(a.repository,a.expected_head,a.manifest_sha256)
print(json.dumps({'Status':'PASS_EXACT_NATIVE_ENTRY_SOURCE_INPUTS','ManifestSha256':digest(tool/'manifest.json'),'Files':len(plan['Files']),'OriginalExecuted':False}))
