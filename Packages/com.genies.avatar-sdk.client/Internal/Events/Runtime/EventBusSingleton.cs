namespace Genies.Events
{
    public static class EventBusSingleton
    {
        private static EventBus _eventBus;

        public static EventBus EventBus
        {
            get
            {
                if (_eventBus == null)
                {
                    _eventBus = new EventBus();
                }

                return _eventBus;
            }
        }
    }
}
