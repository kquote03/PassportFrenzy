using System.Text;

namespace PassportFrenzy
{
    /// <summary>
    /// Replaces tokens like {Jump} {Photo} {Use} {Mash} {Submit} {Confirm} {Debug} {Attach}
    /// with the right key/button name for the last-used device. {Submit}/{Mash}/{Attach}/{PhoneNav} are phone controls
    /// (used while running); {Confirm} is for full-screen overlay menus.
    /// </summary>
    public static class InputPrompts
    {
        static readonly (string token, string km, string pad)[] Tokens =
        {
            ("{Move}", "[A/D]", "[L-Stick]"),
            ("{Right}", "[D]", "[L-Stick right]"),
            ("{Jump}", "[Space]", "[A]"),
            ("{Photo}", "[F]", "[X]"),
            ("{Use}", "[E]", "[Y]"),
            ("{Mash}", "[Space]", "[A]"),
            ("{Submit}", "[Space]", "[A]"),
            ("{PhoneNav}", "[W/S]", "[L-Stick]"),
            ("{PhoneControls}", "[W/S]+[Space]", "[L-Stick]+[A]"),
            ("{Confirm}", "[Space/Enter]", "[A]"),
            ("{Debug}", "[F1]", "[Select]"),
            ("{Attach}", "Press [D] or drag photos onto the paper", "Push [L-Stick] right"),
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
