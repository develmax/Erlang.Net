import argparse
from source_inputs import verify

parser = argparse.ArgumentParser()
for name in ['repository', 'manifest-sha256', 'expected-head']:
    parser.add_argument('--' + name, required=True)
args = parser.parse_args()
repository, tool, manifest = verify(args.repository, args.manifest_sha256, args.expected_head)
assert len(manifest['Cases']) == 5
print('PASS exact scheduler collector, five fixtures, workflow and ownership helper source')
