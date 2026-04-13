namespace PBG.Threads
{
    public static class TaskBlocker
    {
        private static readonly ManualResetEventSlim _waitHandle = new(false);

        public static void Block()
        {
            _waitHandle.Wait();
        }

        public static void Unblock()
        {
            _waitHandle.Set();
        }

        public static void Reset()
        {
            _waitHandle.Reset();
        }
    }
}
