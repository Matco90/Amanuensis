using Amanuensis.Common.Enum;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Common.Entities
{
    public class TranscriptionResult
    {
        public string Transcription {  get; set; }
        public AmanuensisErrorCode_Type? ErrorCode { get; set; }
        public OperationStatus_Type Status { get; set; }
    }
}
