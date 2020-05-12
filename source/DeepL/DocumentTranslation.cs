
#region Using Directives

using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents an ongoing translation of a document that was uploaded to the DeepL API.
    /// </summary>
    public class DocumentTranslation
    {
        #region Public Properties

        /// <summary>
        /// Gets a unique ID assigned to the uploaded document and the translation process. Must be used when referring to this particular
        /// document in subsequent API requests.
        /// </summary>
        [JsonProperty("document_id")]
        public string DocumentId { get; private set; }

        /// <summary>
        /// Gets a unique key that is used to encrypt the uploaded document as well as the resulting translation on the server side. Must be
        /// provided with every subsequent API request regarding this particular document.
        /// </summary>
        [JsonProperty("document_key")]
        public string DocumentKey { get; private set; }

        #endregion
    }
}
