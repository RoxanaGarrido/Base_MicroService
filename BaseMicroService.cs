//using Autofac;
//using CacheManager.Core;
//using EasyNetQ;
//using EasyNetQ.Topology;
//using EasyNetQ.MessageVersioning;
//using JetBrains.Annotations;
//using log4net;
//using NodaTime;
//using System;
//using System.ComponentModel;
//using System.Threading.Tasks;
//using System.Timers;
//using System.Xml.Linq;
//using Topshelf;
//using Validation;
//using IContainer = Autofac.IContainer;
//using CircuitBreaker.Net;
//using CircuitBreaker.Net.Exceptions;
//using Requires = CodeContracts.Requires;
//using Assumes = Validation.Assumes;
//using System.Diagnostics;
//using System.Threading;
//using Timer = System.Timers.Timer;

namespace Base_MicroService
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Timers;
    using Autofac;
    using CircuitBreaker.Net;
    using CircuitBreaker.Net.Exceptions;
    using CodeContracts;
    using EasyNetQ;
    using JetBrains.Annotations;
    using log4net;
    using Topshelf;
    using Timer = System.Timers.Timer;
    using NodaTime;
    using CacheManager.Core;
    using System.Threading;
    using IContainer = Autofac.IContainer;

    public class BaseMicroService<T> where T : class, new()
    { 
        /// <summary> The Timer </summary>
        private static Timer _timer = null;

        /// <summary> The Log </summary>
        readonly static ILog _log =
            LogManager.GetLogger(typeof(BaseMicroService<T>));

        /// <summary> Identifier for the worker </summary>
        private string _workerId;

        /// <summary> The lifetimescope </summary>
        readonly ILifetimeScope _lifetimescope;

        /// <summary> The name </summary>
        private static string _name;

        /// <summary> The host </summary>
        private static HostControl _host;

        /// <summary> The type </summary>
        private static T _type;

        private static ILogger _logger;
        private IContainer _diContainer;

        /// <summary> The connection factory </summary>
        /// <summary> The bus </summary>
        private IBus _bus;
        private ICacheManager<object> _cache = null;

        public IBus Bus
        {
            get { return _bus; }
            set { _bus = value; }
        }

        public ICacheManager<object> Cache
        {
            get { return _cache; }
            set { _cache = value; }
        }

        public string ID
        {
            get { return _workerId.ToString(); }
            set { _workerId = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public HostControl Host
        {
            get
            {
                return _host;
            }
            set { _host = value; }
        }

        public IContainer IOCContainer
        {
            get { return _diContainer; }
            set { _diContainer = value; }
        }

        public BaseMicroService()
        {
            double interval = 60000;
            _timer = new Timer(interval); //trigger event every minute
            Assumes.True(_timer != null, "_timer is null");
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _workerId = Guid.NewGuid().ToString();
            _name = (nameof(T));
        }

        public virtual bool Resume()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Resuming");
                return true;
            }
        }

        /// <summary>
        /// Log service stopped, stop timer and close connections
        /// </summary>
        /// <returns></returns>
        public virtual bool Stop()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Stopping");
            }

            Assumes.True(_log != null, string.Intern("_log is null"));
            _log?.Info(_name + string.Intern(" Service is Stopped"));
            Assumes.True(_timer != null, string.Intern("_timer is null"));
            _timer.AutoReset = false;
            _timer.Enabled = false;
            return true;
        }


        public virtual bool Pause()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Pausing");
                return true;
            }
        }

        public virtual bool Continue()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Continuing");
                return true;
            }
        }

        public virtual bool Shutdown()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Shutting Down");
                return true;
            }
        }


        /// <summary>
        /// Configures and start the timer
        /// </summary>
        /// <param name="hc"></param>
        /// <returns></returns>
        public virtual bool Start(HostControl hc)
        {
            _host = hc;
            Console.WriteLine(_name + string.Intern("Service Started."));
            Assumes.True(_timer != null, string.Intern("_timer is null"));
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();
            return true;
        }

        protected virtual void OnTick([NotNull] object sender, [NotNull] ElapsedEventArgs e)
        {
            Console.WriteLine(string.Intern("Heartbeat")); //intern misma referencia
            Requires.NotNull<ILog>(_log, string.Intern("log is null"));
            _log?.Debug(_name + " (" + _workerId.ToString() + string.Intern("): ") +
                        SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().ToLongTimeString() +
                        string.Intern(": Heartbeat"));

            HealthStatusMessage h = new HealthStatusMessage
            {
                ID = _workerId,
                memoryUsed = Environment.WorkingSet,
                CPU = Convert.ToDouble(getCPUCounter()),
                date = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                serviceName = Name,
                message = "OK",
                status = (int)MSStatus.Healthy
            };

            Bus.Publish(h, "HealthStatus");
        }

        public void TryRequest(Action action, int maxFailures, int startTimeoutMS = 100,
            int resetTimeout = 10000, Action<Exception> OnError = null)
        {
            try
            {
                Requires.True(maxFailures >= 1, "maxFailures must be >= 1");
                Requires.True(startTimeoutMS >= 1, "startTimeoutMS must be >= 1");
                Requires.True(resetTimeout >= 1, "resetTimeout must be >= 1");

                //Initializes the circuit breaker
                var circuitBreaker = new CircuitBreaker(
                   TaskScheduler.Default,
                   maxFailures: maxFailures,
                   invocationTimeout: TimeSpan.FromMilliseconds(startTimeoutMS),
                   circuitResetTimeout: TimeSpan.FromMilliseconds(resetTimeout));

                circuitBreaker.Execute(() => action);
            }
            catch (CircuitBreakerOpenException e1)
            {
                OnError?.Invoke(e1);
                Console.Write(e1.Message);
            }
            catch (CircuitBreakerTimeoutException e2)
            {
                OnError?.Invoke(e2);
                Console.Write(e2.Message);
            }
            catch (Exception e3)
            {
                OnError?.Invoke(e3);
                Console.Write(e3.Message);
            }
        }

        public void PublishMemoryUpdateMessage(int gen1, int gen2, float timeSpent,
            string MemoryBefore, string MemoryAfter)
        {
            // publish a message
            MemoryUpdateMessage msg = new MemoryUpdateMessage
            {
                Text = "Memory MicroService Ran",
                Date = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc(),
                Gen1CollectionCount = gen1,
                Gen2CollectionCount = gen2,
                TimeSpentPercent = timeSpent,
                MemoryBeforeCollection = MemoryBefore,
                MemoryAfterCollection = MemoryAfter
            };

            Bus.Publish(msg, "MemoryStatus");
        }

        public float getCPUCounter()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter
            {
                CategoryName = string.Intern("Processor"),
                CounterName = string.Intern("% Processor Time"),
                InstanceName = string.Intern("_Total")
            };

            //will always start at 0
            dynamic firstValue = cpuCounter.NextValue();
            Thread.Sleep(1000);
            // now matches task manager reading
            float secondValue = cpuCounter.NextValue();
            return secondValue;
        }
    }
}
