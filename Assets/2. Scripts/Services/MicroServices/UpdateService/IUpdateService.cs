namespace Services.MicroServices.UpdateService
{
    public interface IUpdateService : IGameService
    {
        public void AddUpdateListener(IUpdateListener p_listener);
        public void AddFixedUpdateListener(IFixedUpdateListener p_listener);
        public void AddLateUpdateListener(ILateUpdateListener p_listener);
        
        public void RemoveUpdateListener(IUpdateListener p_listener);
        public void RemoveFixedUpdateListener(IFixedUpdateListener p_listener);
        public void RemoveLateUpdateListener(ILateUpdateListener p_listener);
        
        public void MyUpdate();
        public void MyFixedUpdate();
        public void MyLateUpdate();
    }
}