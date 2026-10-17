using UnityEngine;

namespace PassportFrenzy
{
    public enum PaletteRole { None, Sky, Ground, Primary, Accent }

    /// <summary>
    /// Placeholder-art helper: tints this SpriteRenderer from the current country's palette.
    /// The ONLY component allowed on a "Visual" child. Artists: delete it (or set role None) once real sprites exist.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PaletteTint : MonoBehaviour
    {
        public PaletteRole role = PaletteRole.Primary;
        [Tooltip("Multiplied with the palette color (use grey to darken).")]
        public Color multiply = Color.white;

        public void Apply(CountryDefinition c)
        {
            if (c == null || role == PaletteRole.None) return;
            var sr = GetComponent<SpriteRenderer>();
            Color baseColor = role switch
            {
                PaletteRole.Sky => c.skyColor,
                PaletteRole.Ground => c.groundColor,
                PaletteRole.Primary => c.primaryColor,
                PaletteRole.Accent => c.accentColor,
                _ => sr.color,
            };
            sr.color = baseColor * multiply;
        }
    }
}
