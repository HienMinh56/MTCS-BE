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
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public List<string> FileIdsToRemove { get; set; } = new();
        public IFormFileCollection? AddedFiles { get; set; } = null;

    }
}
