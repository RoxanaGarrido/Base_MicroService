using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace Base_MicroService
{
    public enum MSStatus
    {
        Healthy = 1,
        Unhealthy = 2,
    }

    public class HealthStatusMessage
    {
        public string ID { get; set; }
        public DateTime date { get; set; }
        public string serviceName { get; set; }
        public int status { get; set; }
        public string message { get; set; }
        public double memoryUsed { get; set; }
        public double CPU { get; set; }
    }
}
