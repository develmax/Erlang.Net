from common import *
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    for key in ['repository','expected-head','manifest-sha256']:
        parser.add_argument('--'+key,required=True)
    args = parser.parse_args()
    verify(args.repository,args.expected_head,args.manifest_sha256)
    print('PASS source closure; no build or observation executed')
