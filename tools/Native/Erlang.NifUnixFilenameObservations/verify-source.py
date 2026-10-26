from pathlib import Path
import argparse
from inputs import verify_source
p=argparse.ArgumentParser();p.add_argument('--expected-head',required=True);p.add_argument('--source-manifest-sha256',required=True);a=p.parse_args();tool=Path(__file__).resolve().parent;verify_source(a,tool.parents[2],tool);print('Raw Unix observer source inputs verified; nothing executed.')
