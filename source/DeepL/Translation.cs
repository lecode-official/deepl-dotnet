
#region Using Directives

using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents a single translation, which is part of a <see cref="TranslationResult"/>.
    /// </summary>
    public class Translation
    {
        #region Using Directives

        /// <summary>
        /// Gets the language detected in the source text. It reflects the value of the source language parameter, when specified.
        /// </summary>
        [JsonProperty("detected_source_language")]
        public string DetectedSourceLanguage { get; private set; }

        /// <summary>
        /// Gets the translated text.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; private set; }

        #endregion
    }
}
