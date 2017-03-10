using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalKomets
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class KometManager : MonoBehaviour
    {
        public static KometManager Instance;

        //Lets you do things like manually turn an asteroid into a komet.
        public bool debugMode;

        //Eccentricity modifiers for when the asteroid is a komet.
        //Komets are highly eccentric.
        public float eccentricityMin = 0.7f;
        public float eccentricityMax = 0.99f;

        //Minimum orbital altitude (m)
        public double kometMinAltitude = 100000.0f;

        //Max orbital altitude (m)
        public double kometMaxAltitude = 100000000000.0f;

        //Percent chance that, when starting a new save or using the mod for the first time in an existing save,
        //that an existing asteroid will become a komet. You're guaranteed at least one.
        public int startingKometsChance = 30;

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            GameEvents.onAsteroidSpawned.Add(OnAsteroidSpawned);
            GameEvents.onVesselCreate.Add(OnAsteroidSpawned);
            GameEvents.onVesselDestroy.Add(OnVesselDestroyed);

            ConfigNode node = GameDatabase.Instance.GetConfigNode("KERBALKOMETS");
            if (node == null)
                return;

            //Get the settings for Kerbal Komets (if any)
            if (node.HasValue("eccentricityMin"))
                float.TryParse(node.GetValue("eccentricityMin"), out eccentricityMin);

            if (node.HasValue("eccentricityMax"))
                float.TryParse(node.GetValue("eccentricityMax"), out eccentricityMax);

            if (node.HasValue("kometMinAltitude"))
                double.TryParse(node.GetValue("kometMinAltitude"), out kometMinAltitude);

            if (node.HasValue("kometMaxAltitude"))
                double.TryParse(node.GetValue("kometMaxAltitude"), out kometMaxAltitude);

            if (node.HasValue("startingKometsChance"))
                int.TryParse(node.GetValue("startingKometsChance"), out startingKometsChance);

            createdStartingKomets();
        }

        public void Destroy()
        {
            GameEvents.onAsteroidSpawned.Remove(OnAsteroidSpawned);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroyed);
            GameEvents.onVesselCreate.Remove(OnAsteroidSpawned);
        }

        public void OnVesselDestroyed(Vessel doomed)
        {
            KerbalKometScenario.Instance.UnregisterKomet(doomed.vesselName);
        }

        public void OnAsteroidSpawned(Vessel asteroid)
        {
            if (asteroid.vesselType != VesselType.SpaceObject)
                return;
            //Just in case there were no asteroids spawned when the save was first created, let's make sure we create our starting komets.
            if (createdStartingKomets())
                return;

            int presenceChance = KerbalKometSettings.PresenceChance;
            //Roll 3d6 to approximate a bell curve, then convert it to a value between 1 and 100.
            float roll = 0.0f;
            roll = UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll *= 5.5556f;
            Debug.Log("[KometManager] - Rolled a " + roll + " to see if the asteroid is a komet. presenceChance: " + presenceChance);

            //If we roll high enough, then flip the asteroid into a komet.
            if (roll >= (float)presenceChance)
            {
                ConvertToKomet(asteroid);
            }

            else
            {
                Debug.Log("[KometManager] - No komet.");
            }
        }

        public void ConvertToKomet(Vessel asteroid)
        {
            //Set name
            asteroid.vesselName = "Kmt. " + ModuleKomet.CreateKometName(Planetarium.GetUniversalTime());
            if (asteroid.rootPart != null)
                asteroid.rootPart.initialVesselName = asteroid.vesselName;
            Debug.Log("[KometManager] - New komet " + asteroid.vesselName + " discovered!");

            //Generate a random orbit for the komet.
            Orbit orbit = Orbit.CreateRandomOrbitAround(Planetarium.fetch.Sun, kometMinAltitude, kometMaxAltitude);

            //Komets have eccentric orbits, let's randomize the eccentricity.
            orbit.eccentricity = UnityEngine.Random.Range(eccentricityMin, eccentricityMax);

            //Set the orbit
            asteroid.orbit.SetOrbit(orbit.inclination, orbit.eccentricity, orbit.semiMajorAxis, orbit.LAN, orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, 0.0f, Planetarium.fetch.Sun);

            //Hack: Astroids appear to spawn unloaded, and they don't appear to have any part modules when they first show up. To get around this, register the komet in the komet registry.
            //When the asteroid comes into physics range, we'll flip it to a komet.
            if (KerbalKometScenario.Instance.IsKometRegistered(asteroid.vesselName) == false)
                KerbalKometScenario.Instance.RegisterKomet(asteroid.vesselName);

            //Send out a press release
            if (KerbalKometSettings.SendPressRelease)
                sendPressRelease(asteroid);

            //Automatically track the new komet.
            if (KerbalKometSettings.AutoTrackKomets)
                SpaceTracking.StartTrackingObject(asteroid);
        }

        protected void sendPressRelease(Vessel komet)
        {
            try
            {
                Debug.Log("[KometManager] - Generating press release");
                StringBuilder resultsMessage = new StringBuilder();
                MessageSystem.Message msg;
                string[] kometNameItems = komet.vesselName.Split(new char[] { '/' });

                resultsMessage.AppendLine(kometNameItems[1] + "'s Komet has been discovered! It has been designated " + komet.vesselName + " by astronomers.");
                resultsMessage.AppendLine(" ");
                resultsMessage.AppendLine("We currently know very little about the komet, other than the following:");
                resultsMessage.AppendLine(" ");
                resultsMessage.AppendLine("Discovered: " + KSPUtil.dateTimeFormatter.PrintDateCompact(komet.DiscoveryInfo.lastObservedTime, false, false));
                resultsMessage.AppendLine("Orbiting: " + komet.orbit.referenceBody.name);
                resultsMessage.AppendLine("Size Category: " + DiscoveryInfo.GetSizeClassSizes(komet.DiscoveryInfo.objectSize));
                resultsMessage.AppendLine("Orbital Period: " + KSPUtil.PrintTimeLong(komet.orbit.period));
                resultsMessage.AppendLine(komet.DiscoveryInfo.signalStrengthPercent.OneLiner);

                msg = new MessageSystem.Message(kometNameItems[1] + "'s Komet Discovered!", resultsMessage.ToString(),
                    MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE);
                MessageSystem.Instance.AddMessage(msg);
            }
            catch (Exception ex) 
            {
                Debug.Log("[KometManager] - Oops: " + ex);
            }
        }

        protected bool createdStartingKomets()
        {
            //If we've already created the starting komets then we're done.
            if (KerbalKometScenario.Instance.GetStartingKometsFlag())
                return false; //We didn't create any starting komets

            //Go through the list of vessels, and find the tracked and untracked asteroids.
            //The first one found becomes a komet. For the rest, there is a chance that they'll become komets.
            bool firstKometCreated = false;
            Vessel[] unloadedVessels = FlightGlobals.VesselsUnloaded.ToArray();
            int roll;
            for (int index = 0; index < unloadedVessels.Length; index++)
            {
                if (unloadedVessels[index].vesselType == VesselType.SpaceObject)
                {
                    if (firstKometCreated)
                    {
                        roll = UnityEngine.Random.Range(1, 100);
                        Debug.Log("[KometManager] - Creating starting komets: Rolled a " + roll + " out of 100. Target number is " + startingKometsChance);

                        if (roll >= startingKometsChance)
                            ConvertToKomet(unloadedVessels[index]);
                    }

                    else
                    {
                        Debug.Log("[KometManager] - Creating guaranteed komet");
                        firstKometCreated = true;

                        ConvertToKomet(unloadedVessels[index]);
                    }
                }
            }

            //Set the created flag
            KerbalKometScenario.Instance.SetStartingKometsFlag(true);
            return true; //We created starting komets.
        }
    }
}
