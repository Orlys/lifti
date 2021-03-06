﻿namespace Lifti.Tokenization.Preprocessing
{
    public class CaseInsensitiveNormalizer : IInputPreprocessor
    {
        public PreprocessedInput Preprocess(char input)
        {
            return new PreprocessedInput(char.ToUpperInvariant(input));
        }
    }
}
