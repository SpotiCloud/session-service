namespace SessionService.Services.Event
{
    public interface IEventService
    {
        public void Publish<T>(string exchange, string topic, T data);
        public void subscribe<T>(string exchange, string queue, string topic, Action<T> handler);
    }
}
