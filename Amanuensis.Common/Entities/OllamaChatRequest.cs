using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Common.Entities
{
    public class OllamaChatRequest
    {
        public string model { get; set; }
        public bool stream { get; set; }
        public List<OllamaChatMessage> messages { get; set; }
    }
}
