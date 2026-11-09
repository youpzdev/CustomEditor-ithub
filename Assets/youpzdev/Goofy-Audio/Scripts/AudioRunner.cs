using UnityEngine;

class AudioRunner : MonoBehaviour
{
    private void Update()
    {
        Audio.Tick();
    }

    private void OnDestroy()
    {
        Audio.OnRunnerDestroyed(this);
    }
}
