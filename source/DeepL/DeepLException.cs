
#region Using Directives

using System;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents an exception, which is thrown by the <see cref="DeepLClient"/> to signal any errors during the parsing process. Having a
    /// single exception type makes error handling much easier.
    /// </summary>
    public class DeepLException : Exception
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="DeepLException"/> instance.
        /// </summary>
        public DeepLException() { }

        /// <summary>
        /// Initializes a new <see cref="DeepLException"/> instance.
        /// </summary>
        /// <param name="message">The error message, which describes what went wrong during the parsing.</param>
        public DeepLException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="DeepLException"/> instance.
        /// </summary>
        /// <param name="message">The error message, which describes what went wrong during the parsing.</param>
        /// <param name="innerException">The original exception, which caused this exception to be thrown.</param>
        public DeepLException(string message, Exception innerException):
            base (message, innerException) { }

        #endregion
    }
}
