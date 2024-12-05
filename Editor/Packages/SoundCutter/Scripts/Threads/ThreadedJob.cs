using System.Collections;
using System.Threading;

namespace Assets.SoundCutter.Scripts.Threads
{
    public class ThreadedJob
    {
        private readonly object _handle = new object();
        private bool _isDone;
        private Thread _thread;

        public bool IsDone
        {
            get
            {
                bool tmp;
                lock (_handle)
                {
                    tmp = _isDone;
                }
                return tmp;
            }
            set
            {
                lock (_handle)
                {
                    _isDone = value;
                }
            }
        }

        public virtual void Start()
        {
            _thread = new Thread(Run);
            _thread.Start();
        }

        public virtual void Abort()
        {
            _thread.Abort();
        }

        protected virtual void ThreadFunction()
        {
        }

        protected virtual void OnFinished()
        {
        }

        public virtual bool Update()
        {
            if (IsDone)
            {
                OnFinished();
                return true;
            }
            return false;
        }

        private IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }

        private void Run()
        {
            ThreadFunction();
            IsDone = true;
        }
    }
}