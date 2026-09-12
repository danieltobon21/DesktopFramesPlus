using System;

namespace Desktop_Frames
{
    /// <summary>
    /// Single source of truth for the visual defaults applied when a frame is created
    /// or when a legacy / hand-edited frames.json is missing one of these properties.
    ///
    /// Why this exists: the border default used to be hardcoded as "2" in five
    /// different files (frame creation, legacy migration, reset-customizations,
    /// auto-organize, split/new-frame helpers). Each site could disagree with what
    /// the user had picked in the Customize dialog, and a value of 0 (the "no border"
    /// the user had chosen) was treated as "missing" and silently replaced by 2 on the
    /// next startup. Change it here once and the whole app follows.
    /// </summary>
    public static class FrameAppearanceDefaults
    {
        /// <summary>
        /// Frame border thickness, in pixels. 0 means no border at all.
        /// Valid range in the UI is 0..5.
        /// </summary>
        public const int BorderThickness = 0;

        /// <summary>
        /// Frame border color. null keeps the default gray, which is only visible
        /// while <see cref="BorderThickness"/> is greater than 0.
        /// </summary>
        public const string BorderColor = null;

        /// <summary>True when a freshly created frame must be drawn without a border.</summary>
        public static bool IsBorderlessByDefault => BorderThickness <= 0;
    }
}
