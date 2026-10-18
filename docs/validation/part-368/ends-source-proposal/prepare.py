from pathlib import Path

ROOT = Path(__file__).resolve().parent
rename = {'InRear':'InReverse','OutRear':'OutReverse','GetRear':'GetReverse','PeekRear':'PeekReverse','DropRear':'DropReverse','ReverseHead':'Daeh','ReverseTail':'Liat','MisspelledTail':'Lait'}
for path in (ROOT/'source/src/Erlang.Runtime').glob('*.cs'):
    source = path.read_text()
    for old, new in rename.items():
        source = source.replace('QueueEndsNames.'+old, 'QueueModuleNames.'+new)
    source = source.replace('QueueEndsNames.', 'QueueModuleNames.')
    path.write_text(source, encoding='utf-8')
