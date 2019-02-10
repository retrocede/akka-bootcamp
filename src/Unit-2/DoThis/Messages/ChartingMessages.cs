using Akka.Actor;

namespace ChartApp.Messages
{
    #region Reporting
    /// <summary>
    /// Indicates it is time to sample all counters
    /// </summary>
    public class GatherMetrics {}

    /// <summary>
    /// Metric data at time of sample
    /// </summary>
    public class Metric
    {
        public string Series { get; }
        public float CounterValue { get; }

        public Metric(string series, float counterValue)
        {
            Series = series;
            CounterValue = counterValue;
        }
    }
    #endregion
    
    #region Performance Counter Management

    /// <summary>
    /// Types of Counters supported by this example
    /// </summary>
    public enum CounterType
    {
        Cpu,
        Memory,
        Disk
    }

    /// <summary>
    ///  Enables a counter and begins publishing values to <see cref="Subscriber"/>
    /// </summary>
    public class SubscribeCounter
    {
        public CounterType Counter { get; }
        public IActorRef Subscriber { get; }

        public SubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Counter = counter;
            Subscriber = subscriber;
        }
    }

    /// <summary>
    /// Unsubscribes <see cref="Subscriber"/> from receiving updates for a counter.
    /// </summary>
    public class UnsubscribeCounter
    {
        public CounterType Counter { get; }
        public IActorRef Subscriber { get; }

        public UnsubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Counter = counter;
            Subscriber = subscriber;
        }
    }
    #endregion
}