from pathlib import Path
p=Path('artifacts/library451-source-authority.py');s=p.read_text();s=s.replace("['lib/kernel/src/dist_util.erl'","['lib/kernel/src/inet_tcp_dist.erl','lib/kernel/src/dist_util.erl'");p.write_text(s)
