namespace Services.MicroServices.UpdateService
{
    public interface IUpdateListener
    {
        public void MyUpdate();

        public void SubscribeUpdateService();
        public void UnsubscribeUpdateService();
    }
}