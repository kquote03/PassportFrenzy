using System.Text;

namespace PassportFrenzy
{
    /// <summary>
    /// Replaces tokens like {Jump} {Photo} {Use} {Mash} {Submit} {Debug} {Attach}
    /// with the right key/button name for the last-used device.
    /// </summary>
    public static class InputPrompts
    {
        static readonly (string token, string km, string pad)[] Tokens =
        {
            ("{Jump}", "[Space]", "[A]"),
            ("{Photo}", "[F]", "[X]"),
            ("{Use}", "[E]", "[Y]"),
            ("{Mash}", "[Space]", "[A]"),
            ("{Submit}", "[Space/Enter]", "[A]"),
            ("{Debug}", "[F1]", "[Select]"),
            ("{Attach}", "Drag photos onto the paper", "Flick [L-Stick] right"),
        };

        public static string Format(string template)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0) return template;
            bool pad = InputDeviceTracker.Current == ControlScheme.Gamepad;
            var sb = new StringBuilder(template);
            foreach (var t in Tokens) sb.Replace(t.token, pad ? t.pad : t.km);
            return sb.ToString();
        }
    }
}
