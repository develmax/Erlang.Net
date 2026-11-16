# Part 1012 release profiles and wx colour arguments

Apply the compile options recorded in the pinned Windows OTP 29.1.1 release to five applications. Preserve the Dialyzer version define for the five modules that use it. Move the converted colour into its argument register before restoring the adjacent argument in three-argument wx tail commands.

| Application | Images | CInf differences before and after | Dbgi differences before and after | Docs differences before and after |
| --- | ---: | ---: | ---: | ---: |
| xmerl | 35 | 33 → 0 | 33 → 16 | 33 → 3 |
| observer | 44 | 27 → 0 | 27 → 24 | 27 → 0 |
| dialyzer | 29 | 25 → 0 | 28 → 23 | 27 → 10 |
| mnesia | 31 | 25 → 0 | 25 → 6 | 25 → 1 |
| debugger | 24 | 24 → 0 | 24 → 13 | 24 → 0 |

The full corpus has 313 Equal, 1050 Different and three SourceRejected. The new exact images are wxAuiDockArt, wxColourData and wxTreeCtrl. All previous matches remain Equal, including ELDAPv3.

- [Full corpus](otp-corpus.json)
- [Three focused checks](focused.json)
