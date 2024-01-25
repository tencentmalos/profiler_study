namespace FramePro;

public enum ThreadState
{
	Initialized,
	Ready,
	Running,
	Standby,
	Terminated,
	Waiting,
	Transition,
	DeferredReady,
	PREEMPT,
	OWEPREEMPT,
	TURNSTILE,
	SLEEPQ,
	SLEEPQTIMO,
	RELINQUISH,
	NEEDRESCHED,
	IDLE,
	IWAIT,
	SUSPEND,
	REMOTEPREEMPT,
	REMOTEWAKEIDLE
}
