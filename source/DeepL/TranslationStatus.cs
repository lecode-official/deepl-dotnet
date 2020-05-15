
#region Using Directives

using System;
using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents the status of an ongoing translation of a document that was uploaded to the DeepL API.
    /// </summary>
    public class TranslationStatus
    {
        #region Public Properties

        /// <summary>
        /// Gets the unique ID assigned to the uploaded document and the requested translation process
        /// </summary>
        [JsonProperty("document_id")]
        public string DocumentId { get; private set; }

        /// <summary>
        /// Gets the state that the document translation process is currently in.
        /// </summary>
        [JsonProperty("status")]
        public TranslationState State { get; private set; }

        /// <summary>
        /// Gets the estimated number of seconds until the translation is done. This parameter is only included while the translation is
        /// ongoing.
        /// </summary>
        [JsonProperty("seconds_remaining")]
        public Nullable<int> SecondsRemaining { get; private set; }

        /// <summary>
        /// Gets the number of characters billed to your account.
        /// </summary>
        [JsonProperty("billed_characters")]
        public Nullable<long> BilledCharacters { get; private set; }

        #endregion
    }
}
