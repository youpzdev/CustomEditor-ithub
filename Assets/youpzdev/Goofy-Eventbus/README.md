# Goofy EventBus

A type-safe event bus for Unity. Each event type has its own subscriber list.
Copy Scripts/Eventbus.cs into Assets, or import the library package.
Copy Scripts/SubscriptionGroup.cs too when using grouped cancellation.
There are no package dependencies.

## Basic usage

```csharp
public struct EnemyDiedEvent
{
    public int score;
}

EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied, this);
EventBus<EnemyDiedEvent>.Raise(new EnemyDiedEvent { score = 100 });
EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
```

The optional target is a UnityEngine.Object. A listener with a destroyed target
is removed before it can receive an event. Without a target, subscriptions
remain until cancelled or cleared. Disabling a target does not unsubscribe it.
Use OnEnable and OnDisable when the listener should follow component activity.

```csharp
private void OnEnable()
{
    EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied, this);
}

private void OnDisable()
{
    EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
}
```

A null callback or an already destroyed target is rejected with an argument
exception. Keep subscription operations and Raise on Unity's main thread.

## One event and cancellation handles

```csharp
EventBus<LevelLoadedEvent>.SubscribeOnce(OnLevelLoaded, this);

Action cancel = EventBus<EnemyDiedEvent>.Listen(OnEnemyDied, this);
cancel();

Action cancelOnce = EventBus<LevelLoadedEvent>.ListenOnce(OnLevelLoaded, this);
cancelOnce();
```

Listen and ListenOnce return an Action that unsubscribes that exact binding.
Calling it repeatedly is safe. An old cancellation action cannot remove a
replacement registered after the original binding was removed.

A callback can only have one active subscription per event type. Repeating
Subscribe, SubscribeOnce, Listen or ListenOnce with the same callback keeps
the original target and once setting. A cancellation action returned for a
duplicate refers to that existing binding.

## Grouped subscriptions

SubscriptionGroup collects cancellation actions from different event types.
Use one reusable group to match a component's enabled lifetime.

```csharp
private readonly SubscriptionGroup subscriptions = new();

private void OnEnable()
{
    subscriptions.Add(EventBus<EnemyDiedEvent>.Listen(OnEnemyDied, this));
    subscriptions.Add(EventBus<LevelLoadedEvent>.ListenOnce(OnLevelLoaded, this));
}

private void OnDisable()
{
    subscriptions.Clear();
}
```

Add rejects null. Clear runs the actions present at the start in insertion
order. Actions added by a cancellation remain for the next Clear. A nested
Clear during cancellation leaves the current pass in charge and does not
consume those additions. Successful actions are removed. A throwing action
is logged and retained for a later retry; other actions still run.

Repeating Clear after success has no effect. Dispose calls Clear and the group
can be reused afterwards. A failed cancellation can still be pending after
Dispose, so retry Clear if cancellation itself can fail. Keep group operations
on Unity's main thread, like EventBus operations.

## Dispatch rules

- Listeners run in reverse subscription order, as in earlier versions.
- New listeners wait until the next Raise. A nested Raise is a separate
  dispatch and includes listeners added before that nested call.
- Unsubscribe, cancellation and Clear immediately skip pending listeners.
- A once listener is removed before its callback, including nested dispatch.
- Exceptions are logged per callback; remaining listeners still run.
- Raise does not allocate a temporary copy of the subscriber list. A listener
  may still allocate memory through its own work.

```csharp
int listeners = EventBus<EnemyDiedEvent>.Count;
bool hasListeners = EventBus<EnemyDiedEvent>.HasSubscribers;
EventBus<EnemyDiedEvent>.Clear();
```

Count and HasSubscribers prune destroyed targets. Clear only affects its event
type. All instantiated event buses reset on entering a new Play session,
including when domain reload is disabled.

## Tests

Tests/Editor/EventBusTests.cs contains NUnit EditMode coverage for mutation
inside callbacks, nested dispatch, owner destruction, exceptions, cancellation
and the runtime reset entry point. Import with Unity Test Framework available.
The reset test invokes the attributed entry point directly; actual Play Mode
transitions are a separate integration check. SubscriptionGroupTests covers
group lifecycle with two real event buses, cancellation retry and reentrancy.

## License

MIT. See LICENSE.

## Related libraries

- [Goofy Pooling](https://github.com/youpzdev/unity-goofy-pooling)
- [Goofy Timers](https://github.com/youpzdev/unity-goofy-timers)
- [Goofy Saves](https://github.com/youpzdev/unity-goofy-saves)
