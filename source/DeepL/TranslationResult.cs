
#region Using Directives

using System.Collections.Generic;
using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents the result of the translation of a text using the DeepL API, which contains one or more translations.
    /// </summary>
    internal class TranslationResult
    {
        #region Using Directives

        /// <summary>
        /// Gets the translations (one for each text that was fed to the translation engine).
        /// </summary>
        [JsonProperty("translations")]
        public IEnumerable<Translation> Translations { get; private set; }

        #endregion
    }
}
