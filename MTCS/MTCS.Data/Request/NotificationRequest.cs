using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class NotificationRequest
    {
        public string UserId { get; set; }  
        public string Title { get; set; }   
        public string Body { get; set; }    
    }
}
