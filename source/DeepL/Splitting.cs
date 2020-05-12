
namespace DeepL
{
    /// <summary>
    /// The splitting options, which determine how the translation engine will split the input text.
    /// </summary>
    public enum Splitting
    {
        /// <summary>
        /// No splitting at all, the whole input is treated as one sentence. For applications that send one sentence per text parameter, it
        /// is advisable to use this option, in order to prevent the engine from splitting the sentence unintentionally.
        /// </summary>
        None,

        /// <summary>
        /// Splits on interpunction only, ignoring newlines.
        /// </summary>
        Interpunction,

        /// <summary>
        /// Splits on interpunction and on new lines.
        /// </summary>
        InterpunctionAndNewLines
    }
}
