# COM3D2.DCMMemoryOptimization

Just Copy from [VAM]MMDX (@FridaDexDump) and [KK]MMDD (@Countd360).

Use BepInEx.ConfigurationManager(F1) to Edit Config.

**Config Description**:

* Enable GC Optimize:       [bool]   Enable GC Optimize (Disable GC when DCM is playing)
* GC Avoid Virtual Memory:  [bool]   Force to GC when out of Physical Memory (Avail less than 512 MiB)
* GC Suspend Limit:         [string] Force to GC when GC size reaches limit. (such as 40%, 4G, 2048M)
