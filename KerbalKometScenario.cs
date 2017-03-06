﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalKomets
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class KerbalKometScenario : ScenarioModule
    {
        public static KerbalKometScenario Instance;

        protected List<string> registeredKomets = new List<string>();

        public override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            string[] komets = node.GetValues("KOMET");

            for (int index = 0; index < komets.Length; index++)
                registeredKomets.Add(komets[index]);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (string komet in registeredKomets)
                node.AddValue("KOMET", komet);
        }

        public bool IsKometRegistered(string vesselName)
        {
            return registeredKomets.Contains(vesselName);
        }

        public void RegisterKomet(string kometName)
        {
            if (registeredKomets.Contains(kometName) == false)
                registeredKomets.Add(kometName);
        }

        public void UnregisterKomet(string kometName)
        {
            if (registeredKomets.Contains(kometName))
                registeredKomets.Remove(kometName);
        }
    }
}