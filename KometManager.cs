using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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

        //Minimum roll, on a 1-100 roll, that an asteroid becomes a komet.
        public int presenceChance = 95;

        //Eccentricity modifiers for when the asteroid is a komet.
        //Komets are highly eccentric.
        public float eccentricityMin = 0.7f;
        public float eccentricityMax = 0.99f;

        //Minimum orbital altitude (m)
        public double kometMinAltitude = 100000.0f;

        //Max orbital altitude (m)
        public double kometMaxAltitude = 100000000000.0f;


        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            GameEvents.onAsteroidSpawned.Add(OnAsteroidSpawned);
            GameEvents.onVesselDestroy.Add(OnVesselDestroyed);

            ConfigNode node = GameDatabase.Instance.GetConfigNode("KERBALKOMETS");
            if (node == null)
                return;

            //Get the settings for Kerbal Komets (if any)
            if (node.HasValue("presenceChance"))
                int.TryParse(node.GetValue("presenceChance"), out presenceChance);

            if (node.HasValue("eccentricityMin"))
                float.TryParse(node.GetValue("eccentricityMin"), out eccentricityMin);

            if (node.HasValue("eccentricityMax"))
                float.TryParse(node.GetValue("eccentricityMax"), out eccentricityMax);

            if (node.HasValue("kometMinAltitude"))
                double.TryParse(node.GetValue("kometMinAltitude"), out kometMinAltitude);

            if (node.HasValue("kometMaxAltitude"))
                double.TryParse(node.GetValue("kometMaxAltitude"), out kometMaxAltitude);

        }

        public void Destroy()
        {
            GameEvents.onAsteroidSpawned.Remove(OnAsteroidSpawned);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroyed);
        }

        public void OnVesselDestroyed(Vessel doomed)
        {
            KerbalKometScenario.Instance.UnregisterKomet(doomed.vesselName);
        }

        public void OnAsteroidSpawned(Vessel asteroid)
        {
            int roll = UnityEngine.Random.Range(1, 100);

            if (roll >= presenceChance)
            {
                //Set name
                asteroid.vesselName = "Kmt.: " + ModuleKomet.CreateKometName(Planetarium.GetUniversalTime());

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
            }
        }
    }
}
