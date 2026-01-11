using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace BetterQuicksave
{
    public class BetterQuicksaveMCMSettings : AttributeGlobalSettings<BetterQuicksaveMCMSettings>
    {
        public override string Id => "BetterQuicksave";
        public override string DisplayName => "Better Quicksave";
        public override string FolderName => "BetterQuicksave";
        public override string FormatType => "json";

        [SettingPropertyInteger(
            displayName: "Maximum Quicksaves",
            minValue: 1,
            maxValue: 50,
            Order = 0,
            RequireRestart = false,
            HintText = "Number of quicksave slots to maintain. Older saves will be deleted when limit is reached.")]
        [SettingPropertyGroup("General Settings")]
        public int MaxQuicksaves { get; set; } = 3;

        [SettingPropertyText(
            displayName: "Quicksave Name Prefix",
            Order = 1,
            RequireRestart = false,
            HintText = "Prefix for quicksave filenames. Example: 'quicksave_' results in 'quicksave_001', 'quicksave_002', etc.")]
        [SettingPropertyGroup("General Settings")]
        public string QuicksavePrefix { get; set; } = "quicksave_";

        [SettingPropertyDropdown(
            displayName: "Quickload Key",
            Order = 2,
            RequireRestart = false,
            HintText = "The key to press for quickloading a save. Does not require rebinding in game options.")]
        [SettingPropertyGroup("Keybindings")]
        public Dropdown<int> QuickloadKey { get; set; } = new Dropdown<int>(new[]
        {
            68, // F10 (Default)
            67, // F9
            69, // F11
            70, // F12
            36, // Home
            35, // End
            45, // Insert
            46, // Delete
            33, // Page Up
            34, // Page Down
        }, 0);

        [SettingPropertyBool(
            displayName: "Per-Character Saves",
            Order = 3,
            RequireRestart = false,
            HintText = "If enabled, each character gets their own quicksave slot(s). The character's name will be added to the save filename.")]
        [SettingPropertyGroup("Saving")]
        public bool PerCharacterSaves { get; set; } = true;

        public int QuickloadKeyCode => QuickloadKey.SelectedValue;
    }
}
