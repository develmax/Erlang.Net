# Exact owned permit observation

Source-only two-file revision atop immutable `nif-native-fence-controls300-overlay`; parent hashes are recorded. No original source or Main is changed, and no build/native check is executed by the author.

Before starting a cancellable entry probe, the control now reads the real private static semaphore through reflection and requires CurrentCount==0. The controller holds A's owner monitor; B has not been released from its explicit start event; no prior probe owns a permit. Thus the owned retirement thread is the only possible lifecycle permit holder. Only then is the cancellable wait attempted, and the held permit observation is checked again afterward. An expired timer before scheduling Enter cannot supply the ownership proof by itself.

This is a read-only scheduling observation on the actual production semaphore, not a runtime diagnostic hook. No new output or native fixture change is introduced.
