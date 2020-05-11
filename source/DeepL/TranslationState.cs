
namespace DeepL
{
    /// <summary>
    /// Represents an enumeration of the possible states of a document translation process.
    /// </summary>
    public enum TranslationState
    {
        /// <summary>
        /// The translation job is waiting in line to be processed.
        /// </summary>
        Queued,

        /// <summary>
        /// The translation is currently ongoing.
        /// </summary>
        Translating,

        /// <summary>
        /// The translation is done and the translated document is ready for download.
        /// </summary>
        Done,

        /// <summary>
        /// An irrecoverable error occurred while translating the document.
        /// </summary>
        Error
    }
}
