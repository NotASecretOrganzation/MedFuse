using System.Text.RegularExpressions;

namespace DrugDosageApp.Services
{
    public class NDC11Validator
    {
        private static readonly Regex Ndc11Pattern = new(@"^\d{11}$", RegexOptions.Compiled);
        private readonly Dictionary<string, string> _labelerCodes = new();

        public bool IsValidNDC11(string ndc11)
        {
            if (!Ndc11Pattern.IsMatch(ndc11))
                return false;

            // Verify labeler code (first 4-5 digits)
            string labelerCode = ndc11.Substring(0, 5);
            return _labelerCodes.ContainsKey(labelerCode);
        }

        public (string Labeler, string Product, string Package) ParseNDC11(string ndc11)
        {
            if (!IsValidNDC11(ndc11))
                throw new ArgumentException("Invalid NDC-11 code");

            return (
                ndc11.Substring(0, 5),  // Labeler
                ndc11.Substring(5, 4),  // Product
                ndc11.Substring(9, 2)   // Package
            );
        }
    }
}