using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ai.services.api.Models
{
    public class ChatGroupEntity
    {
        public string Id { get; set; }
        public string? Icon { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ServiceType { get; set; }
    }
}
