
#region Using Directives

using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents the result, when an error occurs in the DeepL API.
    /// </summary>
    internal class ErrorResult
    {
        #region Public Properties

        /// <summary>
        /// Gets the error message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        #endregion
    }
}
