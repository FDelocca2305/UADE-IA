namespace Services.MicroServices.UpdateService
{
    public interface ILateUpdateListener
    {
        public void MyLateUpdate();

        public void SubscribeService();
        public void UnsubscribeService();
    }
}