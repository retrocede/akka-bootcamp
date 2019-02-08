using System;
using Akka.Actor;

namespace WinTail.Actors
{
    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public ValidationActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;

            if (String.IsNullOrEmpty(msg))
            {
                _consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsValid(msg);

                if (valid)
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! The message was valid."));
                }
                else
                {
                    _consoleWriterActor.Tell(new Messages.ValidationError("Invalid: input had odd number of characters."));
                }
            }
            
            Sender.Tell(new Messages.ContinueProcessing());
        }
        
        private bool IsValid(string message)
        {
            return message.Length % 2 == 0;
        }
    }
}