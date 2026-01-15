using DeathCounterNETShared;
class StateManager
{
    public static readonly TimeSpan COUNTER_PERIOD = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan PAUSE_BETWEEN_REGS = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan MIN_FART_DELAY = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan MAX_FART_DELAY = TimeSpan.FromSeconds(30);
    public static readonly int TRIGGER_COUNT = 5;

    public static readonly HashSet<string> REG_COMMANDS = ["!go", "!пипяу"];
    public static readonly HashSet<string> BUFF_COMMANDS = ["!buff"];
    
    public EventHandler<StateActionEventArgs>? OnAction { get; set; }
    public StateManager()
    {
        state = State.WaitingForRegStart;
    }
    public void ProcessMessage(string message)
    {
        message = message.TrimExtended();

        if(state == State.WaitingForRegStart)
        {         
            if(!REG_COMMANDS.Contains(message))
            {
                return;
            }

            goCounter.Add();

            if((DateTime.Now - lastRegTime) < PAUSE_BETWEEN_REGS)
            {
                return;
            }

            if(goCounter.Count >= TRIGGER_COUNT)
            {
                OnAction?.Invoke(this, new StateActionEventArgs("!go"));
                lastRegTime = DateTime.Now;
                isFartCharged = 0;
                state = State.WaitingForBuff;
                goCounter.Clear();
            }
        }
        else if(state == State.WaitingForBuff)
        {
            if((DateTime.Now - lastRegTime) >= PAUSE_BETWEEN_REGS)
            {
                state = State.WaitingForRegStart;
                ProcessMessage(message);
                return;
            }

            if(REG_COMMANDS.Contains(message))
            {
                goCounter.Add();
            }
            else if(BUFF_COMMANDS.Contains(message))
            {
                buffCounter.Add();

                if(buffCounter.Count >= TRIGGER_COUNT)
                {
                    OnAction?.Invoke(this, new StateActionEventArgs("!buff"));

                    InitiateFart();

                    state = State.WaitingForRegStart;
                    buffCounter.Clear();
                }
            } 
        }
    }
    private void InitiateFart()
    {
        if(Interlocked.CompareExchange(ref isFartCharged, 1, 0) == 0)
        {
            Task.Run(async () =>
            {
                int randomDelay = random.Next(0, MAX_FART_DELAY.Seconds - MIN_FART_DELAY.Seconds + 1);
                await Task.Delay(MIN_FART_DELAY + TimeSpan.FromSeconds(randomDelay));
                OnAction?.Invoke(this, new StateActionEventArgs("!fart"));
                isFartCharged = 0;
            });
        }
    }

    private readonly Random random = new();
    private DateTime lastRegTime = DateTime.MinValue;
    private readonly TimeSpanCounter goCounter = new (COUNTER_PERIOD);
    private readonly TimeSpanCounter buffCounter = new (COUNTER_PERIOD);
    private int isFartCharged = 0;
    private State state;
}

enum State
{
    WaitingForRegStart,
    WaitingForBuff,
}

class StateActionEventArgs(string message) : EventArgs
{
    public string Message { get; init; } = message;
}