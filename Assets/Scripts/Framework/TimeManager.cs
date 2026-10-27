using UnityEngine;

// Optional slow-motion controller. Writes time only when its state changes.
public class TimeManager : Singleton<TimeManager>
{
    public float TimeFactor { get; private set; } = 1f;
    public bool IsPaused { get; private set; }
    public float DeltaTime => Time.deltaTime;
    public float FixedDeltaTime => IsPaused ? 0f : Time.fixedDeltaTime;

    public void SetTimeFactor(float factor)
    {
        TimeFactor = Mathf.Max(0f, factor);
        Apply();
    }
    public void PauseTime() { IsPaused = true; Apply(); }
    public void ResumeTime() { IsPaused = false; Apply(); }
    private void Apply() => Time.timeScale = IsPaused ? 0f : TimeFactor;
}
