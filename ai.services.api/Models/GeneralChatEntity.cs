using System.Collections.Generic;

namespace ai.services.api.Models
{
    public class GeneralChatEntity
    {
        public class Message
        {
            public string role { get; set; }
            public List<ContentItem> content { get; set; }
        }
        public class ContentItem
        {
            public string type { get; set; }
            public string text { get; set; }
            public ImageUrl image_url { get; set; }
        }
        public class ImageUrl
        {
            public string url { get; set; }
        }
    }
}
