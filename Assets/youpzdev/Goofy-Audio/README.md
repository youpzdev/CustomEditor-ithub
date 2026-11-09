# Goofy Audio

Unity audio playback with reusable AudioSource objects. Copy the Scripts folder
into Assets. No Timer or other Goofy utility is required.

```csharp
Audio.Play(clickClip);
Audio.Play(explosionClip, transform.position, volume: 0.8f);

var sound = Audio.PlayTracked(engineClip, loop: true, owner: this);
sound.Volume = 0.4f;
sound.Pitch = 1.2f;
sound.Pause();
sound.Resume();
sound.Stop();
```

Both Play overloads keep their existing void signatures. PlayTracked returns
an AudioHandle for one playback. A stopped or finished handle never controls
a later playback that reuses the same source. A destroyed owner stops its sound.
Looping playback continues until stopped or its owner is destroyed.

```csharp
sound.FadeTo(0.2f, 0.5f);
sound.Stop(fadeOut: 1f);
sound.Follow(engineTransform);
sound.Follow(null);
```

FadeTo changes volume linearly from its current value. A new fade or Stop(fadeOut)
replaces the previous transition; assigning Volume directly cancels it. Duration
uses unscaled time, while pausing the handle freezes its fade. Zero duration
applies immediately. Volumes clamp to 0..1; nonfinite values and negative or
nonfinite durations are logged and rejected without changing an existing fade.
FadeTo zero volume keeps playback active. Stop(fadeOut) returns the source only
after reaching zero; Stop() still stops immediately. A finished clip or destroyed
owner can release the source before its fade finishes.

Follow moves the source to a Transform immediately and on each runner update,
including while paused. It switches playback to fully spatial audio. Follow(null)
detaches; destroying the followed target also leaves the last position. This does
not replace the playback owner or change its lifetime. A released handle cannot
move, fade or stop a later playback that reuses its source.

The internal runner checks AudioSource.isPlaying. Completion does not depend
on Time.timeScale, Timer.Pause or Timer.CancelAll. Paused sources stay reserved.
IsPlaying reports native playback, IsPaused reports a handle pause, and IsValid
reports an active lease. PauseAll and ResumeAll affect current playback only.

Volume is clamped to 0..1. Pitch must be finite and within -3..3, excluding
values with an absolute value below 0.01. Invalid numeric settings are logged
and rejected. A null clip is a no-op and returns null from PlayTracked.
The mixerGroup parameter remains available on all playback overloads.

```csharp
Audio.MaxInactive = 16;
Audio.Prewarm(8);
int playing = Audio.CountActive;
int available = Audio.CountInactive;
Audio.StopAll();
Audio.Clear();
```

MaxInactive defaults to 32 and limits free sources, not concurrent voices.
Prewarm fills up to the requested free count within this limit; repeating it
does not add another batch. Clear stops playback and destroys free sources.
The runner survives scene changes. Destroying it invalidates its handles;
the next playback creates a fresh runner. Call the API on the Unity main thread.

Tests/Editor contains NUnit tests for reuse, ownership, pause, pool capacity
and native playback completion. The repository license is MIT.
