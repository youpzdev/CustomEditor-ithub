# Goofy Save

Small Unity save utility with optional AES-256 encryption. Requires the
Newtonsoft.Json package already used by this project. Copy Scripts/Save.cs and Scripts/SaveStore.cs
into another Unity project with that package installed.

```csharp
Save.Set("coins", 100);
int coins = Save.Get("coins", 0);
if (Save.TryGet<int>("coins", out var storedCoins)) Spend(storedCoins);
Save.Delete("coins");
Save.DeleteAll();
```

The default file remains Application.persistentDataPath/save.dat. Existing
encrypted files use the same device-derived key, AES mode and IV layout.
SetKey accepts exactly 32 UTF8 bytes. Configure encryption and the key before
reading a slot. Invalid keys are rejected without replacing the current key.
Encryption provides confidentiality; this format does not authenticate data.

## Independent stores

```csharp
var profile = new SaveStore(Path.Combine(Application.persistentDataPath, "profile.dat"));
var settings = new SaveStore(Path.Combine(Application.persistentDataPath, "settings.dat"));
settings.UseEncryption = false;
profile.AutoSave = false;
profile.Set("coins", 100);
settings.Set("volume", 0.8f);
profile.Flush();
```

Each SaveStore owns its path, cache, encryption key, encryption setting,
AutoSave flag, dirty state and load error. It exposes the same operations as
Save, including Configure for switching its selected path. The constructor
normalizes the path and rejects invalid paths with a standard path exception;
constructing a store does not read or create the file. Flush pending writes
before discarding a store. A new instance can reopen the file afterward.

Static Save delegates to one default SaveStore. Its existing public fields
UseEncryption and AutoSave remain fields, including support for ref access.
Their values are applied to the default store before each operation. Stores
created by the caller are independent of these global fields. Existing static
method groups and calls continue to work.

Use one active instance per file path. Multiple instances targeting the same
file can keep stale caches and overwrite each other's changes; coordination
between them is unsupported. Calls belong on Unity's main thread, and separate
paths do not make the API thread-safe. Reopening a file after finishing work
with its previous store is supported.
## Slots and batch writes

```csharp
Save.Configure(Path.Combine(Application.persistentDataPath, "slot-2.dat"));
Save.AutoSave = false;
Save.Set("coins", 100);
Save.Set("level", 4);
if (!Save.Flush()) ShowSaveError();
Save.AutoSave = true;
```

AutoSave defaults to true. Turning it back on affects later changes; call
Flush to save an existing batch. IsDirty remains true after a failed write,
so Flush can be retried. Configure returns false if pending changes cannot
be flushed, and keeps the current slot selected. A successful switch resets
the cache; values load lazily from the selected file. Encryption settings
and AutoSave apply to all slots selected by that store. FilePath exposes the selected absolute path.

Reload reads the selected file again and returns success. It refuses to
discard pending changes: flush them first. TryGet distinguishes a missing or
unreadable value from a valid default value. Get returns its supplied default.

## File failures and recovery

A failed load sets HasLoadError and blocks Set, Delete, DeleteAll and Flush.
The original file stays untouched. Correct the key or repair the file, then
call Reload. Selecting a different slot is also allowed if no pending writes
exist. A JSON null root is treated as a failed load.

Each flush writes and syncs a temporary file beside the target. Existing
files are replaced atomically with the previous version stored as .bak.
The first write moves the complete temporary file into place. Failed writes
keep buffered values and clean up the temporary file when possible. There
is no fallback that truncates the destination. A retry writes the same
buffered snapshot; no external operation or reward is replayed.

RestoreBackup validates .bak with the current encryption settings before
replacing the primary file. It preserves the replaced primary as .corrupt,
keeps the .bak file, and refreshes the cache. A failed recovery leaves the
cache and primary file unchanged. Recovery refuses pending changes.

Failures log the operation, file path and exception type. Stored values and
keys are not logged. Calls belong on Unity's main thread. Concurrent writers
from multiple processes or threads are unsupported. Back up important data
separately; the latest .bak and .corrupt files are replaced on later writes
or recovery operations.

## Tests

Tests/Editor/SaveTests.cs exercises corruption, encryption keys, batch writes,
slot switching, a locked destination, backup recovery and typed reads through
real temporary files. It also checks independent store caches, keys and
settings, disk reopening, and static API compatibility. It never accesses the player's save.dat.

MIT license. Part of the Goofy Tools collection by youpzdev.
