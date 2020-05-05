
#region Public Properties

using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents the usage statistics of the DeepL API plan.
    /// </summary>
    public class UsageStatistics
    {
        #region Public Properties

        /// <summary>
        /// Gets the number of characters that have been translated so far.
        /// </summary>
        [JsonProperty("character_count")]
        public long CharacterCount { get; private set; }

        /// <summary>
        /// Gets the quota of characters that can be translated.
        /// </summary>
        [JsonProperty("character_limit")]
        public long CharacterLimit { get; private set; }

        #endregion
    }
}
