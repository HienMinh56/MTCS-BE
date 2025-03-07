using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Response
{
    public class GetContractFileResponse
    {
        public string ContractId { get; set; }
        public Contract Contract { get; set; }
    }
}
