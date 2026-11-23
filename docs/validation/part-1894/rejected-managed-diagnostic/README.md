# Stopped diagnostic on the rejected compiler

The separately frozen Part 1894 compiler already failed strong validation of the typed record source. Its full managed diagnostic encountered four observed timeout or memory failures before it was stopped locally after 4,098.53 seconds. The harness did not finish or produce a test report. The preserved log bytes and post-stop hashes are evidence for this diagnostic only. The corrected linked source has its own complete test gate.
