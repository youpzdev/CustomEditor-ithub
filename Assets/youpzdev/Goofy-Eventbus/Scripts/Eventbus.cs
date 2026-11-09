using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EventBus<T>
{
    private class Subscription
    {
        public Action<T> Callback;
        public WeakReference<Object> Target;
        public bool Once;
        public bool Active = true;

        public bool IsAlive => Active && (Target == null || (Target.TryGetTarget(out var target) && target != null));
    }

    private static readonly List<Subscription> subscribers = new();

    private static int raiseDepth;
    private static bool needsCleanup;

    static EventBus() {EventBusRuntime.Register(Clear);}

    public static int Count
    {
        get
        {
            Prune();
            int count = 0;
            for (int i = 0; i < subscribers.Count; i++) if (subscribers[i].Active) count++;

            return count;
        }
    }

    public static bool HasSubscribers => Count > 0;

    public static void Subscribe(Action<T> callback, Object target = null) {Add(callback, target, false);}
    public static void SubscribeOnce(Action<T> callback, Object target = null) {Add(callback, target, true);}

    public static Action Listen(Action<T> callback, Object target = null)
    {
        var subscription = Add(callback, target, false);

        return () => Remove(subscription);
    }

    public static Action ListenOnce(Action<T> callback, Object target = null)
    {
        var subscription = Add(callback, target, true);

        return () => Remove(subscription);
    }

    public static void Unsubscribe(Action<T> callback)
    {
        for (int i = subscribers.Count - 1; i >= 0; i--)
            if (subscribers[i].Active && subscribers[i].Callback == callback) {Remove(subscribers[i]); return;}
    }

    public static void Raise(T evt)
    {
        int count = subscribers.Count;
        raiseDepth++;
        try
        {
            for (int i = count - 1; i >= 0; i--)
            {
                var subscription = subscribers[i];
                if (!subscription.IsAlive) {Deactivate(subscription); continue;}

                var callback = subscription.Callback;
                if (subscription.Once) Deactivate(subscription);

                try {callback(evt);}
                catch (Exception e) {Debug.LogException(e);}
            }
        }
        finally
        {
            raiseDepth--;
            if (raiseDepth == 0) Compact();
        }
    }

    public static void Clear()
    {
        for (int i = 0; i < subscribers.Count; i++) Deactivate(subscribers[i]);
        if (raiseDepth == 0) Compact();
    }

    private static Subscription Add(Action<T> callback, Object target, bool once)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        if (!ReferenceEquals(target, null) && target == null) throw new ArgumentException("EventBus target has been destroyed", nameof(target));
        Prune();
        for (int i = 0; i < subscribers.Count; i++)
            if (subscribers[i].Active && subscribers[i].Callback == callback) return subscribers[i]; // ayo no duplicates >:(

        var subscription = new Subscription
        {
            Callback = callback,
            Target = target != null ? new WeakReference<Object>(target) : null,
            Once = once
        };
        subscribers.Add(subscription);

        return subscription;
    }

    private static void Remove(Subscription subscription)
    {
        Deactivate(subscription);
        if (raiseDepth == 0) Compact();
    }

    private static void Deactivate(Subscription subscription)
    {
        if (!subscription.Active) return;
        subscription.Active = false;
        subscription.Callback = null;
        needsCleanup = true;
    }

    private static void Prune()
    {
        for (int i = 0; i < subscribers.Count; i++)
            if (!subscribers[i].IsAlive) Deactivate(subscribers[i]);
        if (raiseDepth == 0) Compact();
    }

    private static void Compact()
    {
        if (!needsCleanup) return;
        int count = 0;
        for (int i = 0; i < subscribers.Count; i++)
            if (subscribers[i].Active) subscribers[count++] = subscribers[i];
        subscribers.RemoveRange(count, subscribers.Count - count);
        needsCleanup = false;
    }
}

internal static class EventBusRuntime
{
    private static readonly List<Action> clearActions = new();

    internal static void Register(Action clear) {clearActions.Add(clear);}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        for (int i = 0; i < clearActions.Count; i++) clearActions[i]();
    }
}
