using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyConsole.Helpers
{
    public class CreateLogRequest
    {
      
            public string Name { get; set; }
            public string Request { get; set; }
            public string Response { get; set; }
            public int Status { get; set; }
            public EntityReference CreateNewPaymentRequest { get; set; }
            public EntityReference RescheduleInstallmentRequest { get; set; }
       
    }
}
