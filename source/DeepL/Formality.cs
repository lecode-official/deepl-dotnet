
namespace DeepL
{
    /// <summary>
    /// The formality options, which determine whether the translated text should lean towards formal or informal language. This feature currently
    /// only works for target languages German, French, Italian, Spanish, Dutch, Polish, Portuguese, and Russian.
    /// </summary>
    public enum Formality
    {
        /// <summary>
        /// The default.
        /// </summary>
        Default,

        /// <summary>
        /// For a more formal language.
        /// </summary>
        More,

        /// <summary>
        /// For a less formal language.
        /// </summary>
        Less
    }
}
