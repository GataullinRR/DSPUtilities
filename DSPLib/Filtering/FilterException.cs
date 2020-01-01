using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extensions;

namespace DSPLib.Filtering
{
    public class FilterException : Exception
    {
        public enum Errors : int
        {
            NONE = 0,
            ID_OVERFLOW,
            PARAMETER_NOT_SUPPORTED_BY_FILTER,
            ATTEMPT_TO_SETUP_NEGATIVE_NUMBER_AS_PARAMETER_VALUE,
            PARAMETER_SET_NOT_IMPLEMENTED,
            PARAMETER_GET_NOT_IMPLEMENTED,
            PARAMETER_TYPE_INVALID,
            PARAMETER_CANNOT_BE_NULL
        }

        readonly public Errors Error;

        public override string Message
        {
            get
            {
                return "Message: {0} ErrorCode: {1}".Format(base.Message as object, Error);
            }
        }

        public FilterException(Errors errorCode, string additionalMessage, Exception innerException)
            : base(additionalMessage, innerException)
        {
            Error = errorCode;
        }

        public FilterException(Errors errorCode, Exception innerException)
            : base("", innerException)
        {
            Error = errorCode;
        }

        public FilterException(Errors errorCode, string additionalMessage)
            : base(additionalMessage)
        {
            Error = errorCode;
        }

        public FilterException(Errors errorCode)
            : base()
        {
            Error = errorCode;
        }
    }
}
