using CacheManager.Core;
using CacheManager.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace Base_MicroService
{
    public class BaseMicroService
    {
        /// <summary> The Timer </summary>
        private static Timer _timer = null;

        /// <summary> The Log </summary>
        readonly static ILogger _log = 
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

        /// <summary> The connection factory </summary>
        /// <summary> The bus </summary>
        private IBus _bus;   
        private ICacheManager<object> _cache = null;




    }
}
