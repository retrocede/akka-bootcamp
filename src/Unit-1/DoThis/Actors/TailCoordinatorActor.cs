using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using Akka.Actor;

namespace WinTail.Actors
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region Message Types

        /// <summary>
        ///  Start Tailing the file at path
        /// </summary>
        public class StartTail
        {
            public string FilePath { get; }
            public IActorRef ReporterActor { get; }
            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }
        }

        /// <summary>
        /// Stop tailing the file at path.
        /// </summary>
        public class StopTail
        {
            public string FilePath { get; }

            public StopTail(string filePath)
            {
                FilePath = filePath;
            }
        }
        #endregion

        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maxNumberOfRetries
                TimeSpan.FromSeconds(30), // withinTimeRange
                x =>
                {
                    if (x is ArithmeticException)
                    {
                        return Directive.Resume;
                    }
                    else if (x is NotSupportedException)
                    {
                        return Directive.Stop;
                    }
                    else
                    {
                        return Directive.Restart;
                    }
                }
            );
        }
    }
}