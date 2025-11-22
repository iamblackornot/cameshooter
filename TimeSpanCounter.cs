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

        //ConsoleHelper.PrintDebug($"{now.Ticks} - check");

        while(_queue.TryPeek(out DateTime first) && (now - first) >= Period)
        {
            //ConsoleHelper.PrintDebug($"{first.Ticks} - dequeue");
            _queue.Dequeue();
        }

        //ConsoleHelper.PrintDebug($"{now.Ticks} - enqueue");
        _queue.Enqueue(now);
    }

    private readonly Queue<DateTime> _queue = new();
}