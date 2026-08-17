using Amanuensis.Common.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Common.Exceptions
{
    public class AmanuensisException : Exception
    {

        public AmanuensisErrorCode_Type ErrorCode { get; }

        public AmanuensisException(AmanuensisErrorCode_Type errorCode, string errorMessage, Exception? originalException = null):base(errorMessage, originalException)
        {
            ErrorCode = errorCode;
        }

    }
}
