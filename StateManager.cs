using DeathCounterNETShared;
class StateManager
{
    public static readonly TimeSpan COUNTER_PERIOD = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan PAUSE_BETWEEN_REGS = TimeSpan.FromSeconds(90);
    public static readonly int TRIGGER_COUNT = 5;

    public static readonly HashSet<string> REG_COMMANDS = ["!go", "!пипяу"];
    public static readonly HashSet<string> BUFF_COMMANDS = ["!buff"];
    
    public EventHandler<StateActionEventArgs>? OnAction { get; set; }
    public StateManager()
    {
        state = State.WaitingForGameStart;
    }
    public void ProcessMessage(string message)
    {
        message = message.TrimExtended();

        if(state == State.WaitingForGameStart)
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
                state = State.WaitingForBuff;
                goCounter.Clear();
            }
        }
        else if(state == State.WaitingForBuff)
        {
            if((DateTime.Now - lastRegTime) >= PAUSE_BETWEEN_REGS)
            {
                state = State.WaitingForGameStart;
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
                    state = State.WaitingForGameStart;
                    buffCounter.Clear();
                }
            } 
        }
    }
    private DateTime lastRegTime = DateTime.MinValue;
    private readonly TimeSpanCounter goCounter = new (COUNTER_PERIOD);
    private readonly TimeSpanCounter buffCounter = new (COUNTER_PERIOD);
    private State state;
}

enum State
{
    WaitingForGameStart,
    WaitingForBuff,
}

class StateActionEventArgs(string message) : EventArgs
{
    public string Message { get; init; } = message;
}