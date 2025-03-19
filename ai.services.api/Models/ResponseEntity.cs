using System.Collections.Generic;

namespace ai.services.api.Models
{
    public class ResponseEntity
    {
        public bool isSuccess { get; set; } 
        public string Message { get; set; }
        public string Result { get; set; }
        public object[] Citations { get; set; }
        public object Data { get; set; }
        public dynamic ChatGroup { get; set; }
        public dynamic ChatItem { get; set; }
        public string url { get; set; }
        public string id { get; set; }
    }

    public class KeyVal
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
