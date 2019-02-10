using System.Windows.Forms;
using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
    public class ButtonToggleActor : UntypedActor
    {
        #region Message Types
        /// <summary>
        /// Toggles this button on/off and sends appropriate messages
        /// to <see cref="PerformanceCounterCoordinatorActor"/>
        /// </summary>
        public class Toggle {}
        #endregion

        private readonly CounterType _myCounterType;
        private readonly Button _myButton;
        private readonly IActorRef _coordinatorActor;
        private bool _isToggledOn;

        public ButtonToggleActor(IActorRef coordinatorActor, Button myButton, CounterType myCounterType,
            bool isToggledOn = false)
        {
            _coordinatorActor = coordinatorActor;
            _myButton = myButton;
            _myCounterType = myCounterType;
            _isToggledOn = isToggledOn;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle && _isToggledOn)
            {
                // currently on
                
                // stop watching
                _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(_myCounterType));

                FlipToggle();
            }
            else if (message is Toggle && !_isToggledOn)
            {
                // currently off
                
                // start watching
                _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(_myCounterType));

                FlipToggle();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            // flip
            _isToggledOn = !_isToggledOn;
            
            // update text
            string state = _isToggledOn ? "ON" : "OFF";
            _myButton.Text = $"{ _myCounterType.ToString().ToUpperInvariant() } ({ state })";
        }
    }
}