namespace WA.Susie
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception of Susie internal error.
    /// </summary>
    [Serializable]
    public class SusieException : Exception
    {
        internal SusieException()
        {
        }

        internal SusieException(string message)
            : base(message)
        {
        }

        internal SusieException(string message, Exception inner)
            : base(message, inner)
        {
        }

        internal SusieException(API.ReturnCode returnCode)
            : base(GetErrorMessage(returnCode))
        {
        }

        protected SusieException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal static string GetErrorMessage(API.ReturnCode returnCode)
        {
            switch (returnCode)
            {
                case API.ReturnCode.Success:
                    return null;
                case API.ReturnCode.NotImplemented:
                    return "Susie Plugin: Not implemented.";
                case API.ReturnCode.FailedToProcess:
                    return "Susie Plugin: Failed to process.";
                case API.ReturnCode.UnknownFormat:
                    return "Susie Plugin: Unknown format.";
                case API.ReturnCode.CorruptedData:
                    return "Susie Plugin: Data Corruption.";
                case API.ReturnCode.FailedToAllocateMemory:
                    return "Susie Plugin: Failed to allocate memory.";
                case API.ReturnCode.MemoryError:
                    return "Susie Plugin: Memory error.";
                case API.ReturnCode.FailedToReadFile:
                    return "Susie Plugin: Failed to read file.";
                case API.ReturnCode.Reserved:
                    return "Susie Plugin: Reserved.";
                case API.ReturnCode.InternalError:
                    return "Susie Plugin: Internal error.";
                default:
                    break;
            }

            return null;
        }
    }
}
