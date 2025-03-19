using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ai.services.api.Models
{
    public class ChatItemEntity
    {
        public class ContentItem
        {
            public string MediaType { get; set; }
            public string ContentType { get; set; }
            public string Content { get; set; }
            public string FileName { get; set; }
            public string FeedbackType { get; set; }
            public string FeedbackComment { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class Reference
        {
            public Int64 Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public string Url { get; set; }
            public string FilePath { get; set; }
        }

        public class ChatItem
        {
            public Int64 Id { get; set; }
            public string Role { get; set; }
            public string ChatGroupId { get; set; }
            public int ResponseRevision { get; set; }
            public List<ContentItem> Content { get; set; }
            public bool HasCode { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string ServiceType { get; set; }
            public List<Reference> References { get; set; }
            public string Url { get; set; }
        }
    }
}
