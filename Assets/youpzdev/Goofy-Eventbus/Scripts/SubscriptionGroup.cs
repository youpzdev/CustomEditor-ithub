using System;
using System.Collections.Generic;
using UnityEngine;

public class SubscriptionGroup : IDisposable
{
    private readonly List<Action> cancellations = new();

    private bool clearing;

    public void Add(Action cancel)
    {
        if (cancel == null) throw new ArgumentNullException(nameof(cancel));
        cancellations.Add(cancel);
    }

    public void Clear()
    {
        if (clearing) return;
        clearing = true;
        int count = cancellations.Count;
        int failed = 0;
        try
        {
            for (int i = 0; i < count; i++)
            {
                var cancel = cancellations[i];
                try {cancel();}
                catch (Exception e)
                {
                    cancellations[failed++] = cancel;
                    Debug.LogException(e);
                }
            }
        }
        finally
        {
            cancellations.RemoveRange(failed, count - failed);
            clearing = false;
        }
    }

    public void Dispose() {Clear();}
}
