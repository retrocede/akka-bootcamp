using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        #region Message Types

        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to
        /// updates for <see cref="Counter"/>
        /// </summary>
        public class Watch
        {
            public CounterType Counter { get; }

            public Watch(CounterType counter)
            {
                Counter = counter;
            }
        }

        /// <summary>
        /// Unsubscribe the <see cref="ChartingActor"/>
        /// from updates for <see cref="Counter"/>
        /// </summary>
        public class Unwatch
        {
            public CounterType Counter { get; }

            public Unwatch(CounterType counter)
            {
                Counter = counter;
            }
        }
        #endregion
        
        /// <summary>
        /// Methods for generating new instances of all <see cref="PerformanceCounter"/>
        /// we want to monitor
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators = new Dictionary<CounterType, Func<PerformanceCounter>>()
        {
            { CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true) },
            { CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true) },
            { CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true) }
        };
        
        /// <summary>
        /// Methods for creating new <see cref="Series"/> with set colors and names
        /// for each <see cref="PerformanceCounter"/>
        /// </summary>
        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries = new Dictionary<CounterType, Func<Series>>()
        {
            { 
                CounterType.Cpu,
                () => new Series(CounterType.Cpu.ToString())
                {
                    ChartType = SeriesChartType.SplineArea,
                    Color = Color.DarkGreen
                }
            },
            {
                CounterType.Memory,
                () => new Series(CounterType.Memory.ToString())
                {
                    ChartType = SeriesChartType.FastLine,
                    Color = Color.MediumBlue
                }
            },
            {
                CounterType.Disk,
                () => new Series(CounterType.Disk.ToString())
                {
                    ChartType = SeriesChartType.SplineArea,
                    Color = Color.DarkRed
                }
            }
        };

        private Dictionary<CounterType, IActorRef> _counterActors;
        private IActorRef _chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor) : this(chartingActor,
            new Dictionary<CounterType, IActorRef>())
        {
            
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor,
            Dictionary<CounterType, IActorRef> counterActors)
        {
            _chartingActor = chartingActor;
            _counterActors = counterActors;

            Receive<Watch>(watch =>
            {
                if (!_counterActors.ContainsKey(watch.Counter))
                {
                    // create child actor to monitor this if one doesn't already exist
                    var counterActor = Context.ActorOf(Props.Create(() => new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerators[watch.Counter])));
                    _counterActors[watch.Counter] = counterActor;
                }
                
                // register this series with the ChartingActor
                _chartingActor.Tell(
                    new ChartingActor.AddSeries(
                        CounterSeries[watch.Counter]()
                    )
                );
                
                // tell counterActor to publish stats to _chartingActor
                _counterActors[watch.Counter].Tell(
                    new SubscribeCounter(watch.Counter, _chartingActor)
                );
            });

            Receive<Unwatch>(unwatch =>
            {
                if (!_counterActors.ContainsKey(unwatch.Counter))
                {
                    // do nothing, this watch doesn't exist
                    return;
                }
                
                // unsubscribe the ChartingActor from updates
                _counterActors[unwatch.Counter].Tell(
                    new UnsubscribeCounter(unwatch.Counter, _chartingActor)
                );
                
                // remove this series from the ChartingActor
                _chartingActor.Tell(
                    new ChartingActor.RemoveSeries(unwatch.Counter.ToString())
                );
            });
        }
    }
}