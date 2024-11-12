namespace SeweralIdeas.Utils
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T s_instance;

        protected Singleton() { }

        public static T GetInstance()
        {
            if ( s_instance == null )
                s_instance = new T();
            return s_instance;
        }
    }
}