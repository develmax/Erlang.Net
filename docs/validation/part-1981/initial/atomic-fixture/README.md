# Part 1981 split-carrier fixture correction

The original fixture passed the nested checked-entry gate and had one owned frame. Inside that frame it had no branch. The existing branch slot allocator was invoked without a forced allocation and declined even after renaming the terminal definition. The live-spare negative correctly declined.

The revised synthetic body has an integer test and an explicit framed cold path before the terminal suffix. Failure raises with the projected field, which remains available after atomic aliasing. No extra use of the retiring tuple carrier is introduced.

Reflection through the exact F0650837 compiler confirms the positive invokes the real splitter. It emits allocation width 2 with slot layout [0,0,1]. The negative refuses to reuse its live spare. The existing ordinary atomic plan still declines both. The same exact compiler splitter returns width 9 from the frozen Part 1979 Sudoku body whose old width is 10.

The fixture-only patch passed strict forward and reverse replay. The original failure log and both reflection inputs are retained. No suite, compiler build or corpus compilation was run.
