using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class UpdateContractRequest
    {
        public string ContractId { get; set; } = null!;
        public string Summary { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Status { get; set; }
        public List<string> Descriptions { get; set; } = new();
        public List<string> Notes { get; set; } = new();
        public List<string> FileIdsToRemove { get; set; } = new();
        public IFormFileCollection? AddedFiles { get; set; } = null;

    }
}
