# Preserve matched tuple heads before recursive list construction

The pinned `sys_core_alias.erl` builds structural keys for construction and matching, then inserts an alias in the matching clause when a construction uses that key. The C# change covers a terminal case over an already bound variable whose arm returns a cons with an identical tuple head and a static local call as its tail. The checked pattern, any guard, the tail call and its source line keep their original bindings.

The 1,366 released abstract-code payloads contain one matching function, `yeccscan:lex/1`. Four case arms reconstruct an unchanged tuple. The two-field catch-all returns a different three-field tuple and remains unchanged. The pinned code keeps each existing tuple in the frame across the recursive call. Closure helper lowering and the outer `scan/3` frame have independent differences.

The permanent fixture checks constructors in two unchanged and one changed arm, including a guarded match. The native recipe exercises the real exported scanner, including retained comment/whitespace tokens, both forms of punctuation, a reserved word and EOF. No compiler build or test ran for this proposal.

