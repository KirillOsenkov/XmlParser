using System.Text;

namespace Microsoft.Language.Xml.Utilities
{
    public static class Normalization
    {
        /// <summary>
        /// Get normalized value
        /// </summary>
        /// <param name="value"><see cref="string"/> to normalize.</param>
        /// <remarks>
        /// Normalization specs:
        /// <seealso href="https://www.w3.org/TR/2006/REC-xml11-20060816/#sec-line-ends">2.2.12 [XML] Section 3.3.3</seealso/>
        /// <seealso href="https://learn.microsoft.com/en-us/openspecs/ie_standards/ms-xml/389b8ef1-e19e-40ac-80de-eec2cd0c58ae">2.11 [XML} End-of-Line Handling</seealso/>
        /// </remarks>
        public static string GetNormalizedAttributeValue(this string value) =>
            GetNormalizedAttributeValue(new StringBuilder(value));

        internal static string GetNormalizedAttributeValue(StringBuilder inputBuffer)
        {
            var outputBuffer = PooledStringBuilder.GetInstance();
            NormalizeAttributeValueTo(inputBuffer, outputBuffer);
            return outputBuffer.ToStringAndFree();
        }

        internal static string GetNormalizedAttributeValue(this SyntaxNode node)
        {
            var inputBuffer = PooledStringBuilder.GetInstance();
            var writer = new System.IO.StringWriter(inputBuffer.Builder, System.Globalization.CultureInfo.InvariantCulture);
            node.WriteTo(writer);
            var outputBuffer = PooledStringBuilder.GetInstance();
            inputBuffer.Builder.NormalizeAttributeValueTo(outputBuffer);
            inputBuffer.Free();
            return outputBuffer.ToStringAndFree();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void NormalizeAttributeValueTo(this StringBuilder inputBuffer, PooledStringBuilder outputBuffer)
        {
            var inputBufferLength = inputBuffer.Length;
            char lastChar = default;
            for (int charIndex = 0; charIndex < inputBufferLength; charIndex++)
            {
                var c = inputBuffer[charIndex];
                switch (c)
                {
                    // If there is a sequence of CR and LF or CR 0x85 or CR 0x2028 replace them with a space (0x32)
                    case '\r' when (charIndex + 1 < inputBufferLength && (inputBuffer[charIndex + 1] is '\n' or '\x85' or '\x2028')):
                        outputBuffer.Builder.Append(' ');
                        charIndex++; // Skip next onece
                        break;
                    // If current char is single CR or 0x85 or 0x2028 replace with a space(0x32)
                    case '\r':
                    case '\x85':
                    case '\x2000':
                        outputBuffer.Builder.Append(' ');
                        break;
                    // if current char is LF and previus not is LF or CR 
                    case '\n' when lastChar != '\n' && lastChar != '\r':
                        outputBuffer.Builder.Append(' ');
                        break;
                    case '\t':
                        outputBuffer.Builder.Append(' ');
                        break;
                    default:
                        outputBuffer.Builder.Append(c);
                        break;
                }
                lastChar = c;
            }
        }
    }
}