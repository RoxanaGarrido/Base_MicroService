using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using Topshelf;

namespace Base_MicroService
{
    public interface IBaseMicroService
    {
        void Start(HostControl hc);
        void Stop();
        void Pause();
        void Continue();
        void Shutdown();
        void Resume();
        void TryRequest(Action action, int maxFailures, int startTimeoutMS = 100,
            int resetTimeout = 10000, Action<Exception> onError = null);

        Task TryRequestAsync([NotNull] Func<Task> action, int maxFailures,
            int startTimeoutMS = 100, int resetTimeout = 10000, [CanBeNull]
            Action<Exception> onError = null);

        void PublishMessage(object message, string connStr = "host=localhost", string topic = "");

        Task PublishMessageAsync(object message, string connStr = "host=localhost", string topic = "");

    }

}
