using System.Globalization;

namespace Flicker.Generation
{
    // Turns typed seed text into a seed: numbers as-is, words hashed (FNV-1a, case-insensitive).
    public static class SeedText
    {
        public static bool TryParse(string text, out int seed)
        {
            seed = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string trimmed = text.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed)) return true;

            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in trimmed.ToLowerInvariant())
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                seed = (int)hash;
            }
            return true;
        }
    }
}
