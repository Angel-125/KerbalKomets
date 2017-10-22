using System;
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
        protected bool startingKometsCreated;

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

            if (node.HasValue("startingKometsCreated"))
                startingKometsCreated = bool.Parse(node.GetValue("startingKometsCreated"));
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (string komet in registeredKomets)
                node.AddValue("KOMET", komet);

            node.AddValue("startingKometsCreated", startingKometsCreated);
        }

        public bool GetStartingKometsFlag()
        {
            return startingKometsCreated;
        }

        public void SetStartingKometsFlag(bool created)
        {
            startingKometsCreated = created;
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

        public int GetKometCount()
        {
            //Get registered komet count
            //Remember, newly spawned asteroids won't have any part modules...
            int registeredKometCount = registeredKomets.Count;

            //Search loaded vessels
            int totalVessels = FlightGlobals.VesselsLoaded.Count;
            Vessel vessel;
            List<ModuleKomet> komets = null;
            int totalKometModules = 0;
            ModuleKomet komet;
            for (int index = 0; index < totalVessels; index++)
            {
                vessel = FlightGlobals.VesselsLoaded[index];
                komets = vessel.FindPartModulesImplementing<ModuleKomet>();
                if (komets == null)
                    continue;
                if (komets.Count > 0)
                {
                    totalKometModules = komets.Count;
                    for (int kometIndex = 0; kometIndex < totalKometModules; kometIndex++)
                    {
                        komet = komets[kometIndex];
                        if (komet.isAKomet)
                        {
                            registeredKometCount += 1;
                        }
                    }
                }
            }

            //Search unloaded vessels
            totalVessels = FlightGlobals.VesselsUnloaded.Count;
            ProtoVessel protoVessel;
            ProtoPartSnapshot partSnapshot = null;
            int protoPartCount = 0;
            int protoModuleCount = 0;
            ProtoPartModuleSnapshot moduleSnapshot = null;
            for (int index = 0; index < totalVessels; index++)
            {
                protoVessel = FlightGlobals.VesselsUnloaded[index].protoVessel;

                //Look through proto parts and find komet modules.
                protoPartCount = protoVessel.protoPartSnapshots.Count;
                for (int partIndex = 0; partIndex < protoPartCount; partIndex++)
                {
                    partSnapshot = protoVessel.protoPartSnapshots[partIndex];

                    protoModuleCount = partSnapshot.modules.Count;
                    for (int moduleIndex = 0; moduleIndex < protoModuleCount; moduleIndex++)
                    {
                        moduleSnapshot = partSnapshot.modules[moduleIndex];
                        if (moduleSnapshot.moduleName == "ModuleKomet")
                        {
                            if (moduleSnapshot.moduleValues.HasValue("isAKomet"))
                            {
                                if (moduleSnapshot.moduleValues.GetValue("isAKomet") == "true")
                                    registeredKometCount += 1;
                                break;
                            }
                        }
                    }
                }
            }

            return registeredKometCount;
        }
    }
}
