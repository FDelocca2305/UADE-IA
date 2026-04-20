namespace Services.MicroServices.UpdateService
{
    public interface IFixedUpdateListener
    {
        public void MyFixedUpdate();

        public void SubscribeService();
        public void UnsubscribeService();
    }
}