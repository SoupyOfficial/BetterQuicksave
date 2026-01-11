using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BetterQuicksave.Patches;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace BetterQuicksave
{
    public static class QuicksaveManager
    {
        public static bool CanQuickload => Game.Current?.CurrentState == Game.State.Running && Game.Current.GameType.SupportsSaving;

        private static readonly EventListeners eventListeners = new EventListeners();
        private static readonly FileDriver SaveDriver = new FileDriver();
        private static int NextQuicksaveNumber { get; set; } = 1;
        private static string CurrentPlayerName
        {
            get
            {
                Hero player = Campaign.Current?.MainParty?.LeaderHero;
                return player != null ? $"{player.Name} {player.Clan.Name}" : "No Name";
            }
        }

        private static LoadResult CurrentLoadGameResult { get; set; } = null;
        private static string QuicksaveNamePattern
        {
            get
            {
                string playerCharacterName = string.Empty;
                if (Config.PerCharacterSaves && CurrentPlayerName.Length > 0)
                {
                    playerCharacterName = $"{Regex.Escape(CurrentPlayerName)} ";
                }
                string prefix = Regex.Escape(Config.QuicksavePrefix);
                string saveNumber = Config.MaxQuicksaves > 1 ? @"(\d{3})" : string.Empty;

                return $"^{playerCharacterName}{prefix}{saveNumber}$";
            }
        }

        static QuicksaveManager()
        {
            SubModule.OnGameInitFinishedEvent += eventListeners.OnGameInitFinished;
            SubModule.OnGameEndEvent += eventListeners.OnGameEnd;
            SubModule.OnApplicationTickEvent += eventListeners.OnApplicationTick;
            QuickSaveCurrentGamePatch.OnQuicksave += eventListeners.OnQuicksave;
        }

        /// <summary>
        /// Empty method used to invoke constructor, needed in order to setup event listeners at the right time
        /// and to prevent duplicate listeners from accidentally being created.
        /// </summary>
        public static void Init() { }

        public static string GetNextQuicksaveName()
        {
            if (NextQuicksaveNumber > Config.MaxQuicksaves)
            {
                NextQuicksaveNumber = 1;
            }

            string characterName = Config.PerCharacterSaves ? $"{CurrentPlayerName} " : string.Empty;
            string saveNum = Config.MultipleQuicksaves ? $"{NextQuicksaveNumber:000}" : string.Empty;

            return $"{characterName}{Config.QuicksavePrefix}{saveNum}";
        }

        public static bool IsValidQuicksaveName(string name)
        {
            return Regex.IsMatch(name, QuicksaveNamePattern);
        }

        public static void Quickload()
        {
            CurrentLoadGameResult = GetLatestQuicksave();
            if (CurrentLoadGameResult == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("No quicksaves available."));
            }
            else
            {
                if (CurrentLoadGameResult.Successful)
                {
                    if (Mission.Current != null)
                    {
                        Mission.Current.RetreatMission();
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Unable to load quicksave:",
                        Colors.Yellow));
                    foreach (LoadError loadError in CurrentLoadGameResult.Errors)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(loadError.Message, Colors.Red));
                    }

                    CurrentLoadGameResult = null;
                }
            }
        }

        private static LoadResult GetLatestQuicksave()
        {
            try
            {
                var saveFiles = SaveDriver.GetSaveGameFileInfos();
                var latest = saveFiles
                    .Where(f => IsValidQuicksaveName(f.Name))
                    .Select(f => new
                    {
                        Info = f,
                        Timestamp = File.GetLastWriteTime(FileDriver.GetSaveFilePath(f.Name).FileFullPath)
                    })
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefault();

                if (latest == null)
                {
                    return null;
                }

                return SaveManager.Load(latest.Info.Name, SaveDriver);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Failed to quickload: {ex.Message}", Colors.Red));
                return null;
            }
        }

        private static void HandleCurrentLoadGameResult()
        {
            if (GameStateManager.Current.ActiveState is MapState)
            {
                if (Mission.Current != null)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage("Mission is not null, failed to quickload!", Colors.Red));
                }
                else
                {
                    LoadSave(CurrentLoadGameResult);
                }

                CurrentLoadGameResult = null;
            }
        }

        private static void LoadSave(LoadResult lgr)
        {
            try
            {
                // Use SandBoxSaveHelper for safer load handling
                SandBoxSaveHelper.TryLoadSave(
                    new SaveGameFileInfo { Name = lgr.MetaData["save_name"], MetaData = lgr.MetaData },
                    (loadResult) =>
                    {
                        if (!loadResult.Successful)
                        {
                            InformationManager.DisplayMessage(
                                new InformationMessage("Quickload failed!", Colors.Red));
                        }
                    },
                    () => { }
                );
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"Quickload error: {ex.Message}", Colors.Red));
            }
        }

        private static void SetNextQuicksaveNumber()
        {
            try
            {
                var saveFiles = SaveDriver.GetSaveGameFileInfos();
                foreach (SaveGameFileInfo saveFile in saveFiles)
                {
                    Match match = Regex.Match(saveFile.Name, QuicksaveNamePattern);
                    if (match.Success)
                    {
                        Int32.TryParse(match.Groups[1].Value, out int num);
                        NextQuicksaveNumber = num + 1;
                        return;
                    }
                }
            }
            catch
            {
                // If we can't read save files, just reset to 1
            }

            NextQuicksaveNumber = 1;
        }

        private class EventListeners
        {
            public void OnApplicationTick()
            {
                if (CurrentLoadGameResult != null)
                {
                    HandleCurrentLoadGameResult();
                }
            }

            private void OnPlayerCharacterChanged(Hero previousHero, Hero newHero, MobileParty party, bool isMainParty)
            {
                SetNextQuicksaveNumber();
            }

            public void OnQuicksave()
            {
                InformationManager.DisplayMessage(new InformationMessage("Quicksaved."));
                NextQuicksaveNumber++;
            }

            public void OnGameInitFinished()
            {
                if (Campaign.Current != null)
                {
                    CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this,
                        OnPlayerCharacterChanged);
                }
            }

            public void OnGameEnd()
            {
                if (Campaign.Current != null)
                {
                    CampaignEvents.OnPlayerCharacterChangedEvent.ClearListeners(this);
                }
            }
        }
    }
}
