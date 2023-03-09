namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Assets.Scripts.Vizzy.UI;
    using HarmonyLib;
    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using UnityEngine;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            base.OnModInitialized();

            // Backup programs on the very first time we run
            Task.Run(BackupProgramFiles);

            Harmony harmony = new Harmony("Vizzy Studio");
            harmony.PatchAll();

            VizzyStudioUI.Initialize();
        }

        private void BackupProgramFiles()
        {
            string backupDirectory = Path.GetDirectoryName(VizzyUIScript.FlightProgramsFolderPath) + "_backup";
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);

                DirectoryInfo originalDirectory = new DirectoryInfo(VizzyUIScript.FlightProgramsFolderPath);
                foreach (FileInfo file in originalDirectory.GetFiles())
                {
                    file.CopyTo(Path.Combine(backupDirectory, file.Name));
                }
            }
        }
    }
}