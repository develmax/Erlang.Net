from pathlib import Path
p=Path('artifacts/pid-order-prototype/tools/Erlang.Differential/IdentifierOrderCases.cs');s=p.read_text();s=s.replace('true=maps:keys(M)=:=[A,B]','true=lists:sort(maps:keys(M))=:=[A,B]');p.write_text(s)
