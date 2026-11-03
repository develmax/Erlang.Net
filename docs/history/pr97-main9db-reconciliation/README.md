# PR97 inherited source-snapshot reconciliation

Accepted main `9db75424abe215bff7bd0dcbf576d094e9bfc63e` and PR97 `7f098d4dc2590b35e8c4b872e9e24663968ba22a` contain different versions of the existing Part544 current-source snapshot. This directory preserves the exact accepted-main bytes before integrating the inherited PR97 version at its existing path. Neither version is new current-source execution evidence. Both commit histories remain available through the integration parents. New current source bindings use the separate Part555/NativeGC825 snapshot and admission receipts.

No historical outcome, test expectation or compatibility classification is promoted by this archive.
