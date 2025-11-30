using DeathCounterNETShared;

class TimeSpanCounter
{
    public TimeSpan Period { get; init; }
    public TimeSpanCounter(TimeSpan period)
    {
        Period = period;
    }
    public int Count => _queue.Count;
    public void Clear() => _queue.Clear();
    public void Add()
    {
        DateTime now = DateTime.Now;

        while(_queue.TryPeek(out DateTime first) && (now - first) >= Period)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(now);
    }

    private readonly Queue<DateTime> _queue = new();
}