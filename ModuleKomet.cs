using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
License: GPLV3

If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalKomets
{
    public class ModuleKomet : ModuleAsteroid
    {
        [KSPField]
        public bool debugMode;

        [KSPField]
        public string tailTransformName = string.Empty;

        [KSPField]
        public string kometTextureURL = string.Empty;

        [KSPField]
        public string guaranteeResources = string.Empty;

        [KSPField]
        public string kometExperimentID = string.Empty;

        [KSPField]
        public float waterPercentage = 50f;

        [KSPField]
        public float minimumResourcePercent;

        [KSPField]
        public float eccentricityMin = 0.7f;

        [KSPField]
        public float eccentricityMax = 0.99f;

        [KSPField]
        public double kometMinAltitude;

        [KSPField]
        public double kometMaxAltitude;

        [KSPField(isPersistant = true)]
        public bool isAKomet;

        [KSPField(isPersistant = true)]
        public bool resourcesConverted;

        protected Transform tailTransform = null;

        [KSPEvent(guiActive = true, guiName = "Turn Into Komet")]
        public void ToggleKomet()
        {
            isAKomet = !isAKomet;

            SetupAsKomet(isAKomet);

            //Do some things that are needed when you manually flip an asteroid.
            if (isAKomet)
            {
                //Setup orbit
                setupOrbit();

                //Rename asteroid to komet
                setKometName();
            }
        }

        public override void OnStart(StartState state)
        {
            if (isAKomet)
                sampleExperimentId = kometExperimentID;

            base.OnStart(state);

            //Get the tail transform
            if (string.IsNullOrEmpty(tailTransformName) == false)
                tailTransform = this.part.FindModelTransform(tailTransformName);
            if (tailTransform == null)
                tailTransform = this.part.FindModelTransform(tailTransformName + "(Clone)");
            if (tailTransform == null)
            {
                Debug.Log("[ModuleKomet] - tailTransformName not found.");
                return;
            }

            //Check to see if the asteroid is registered as a komet
            if (KerbalKometScenario.Instance.IsKometRegistered(this.part.vessel.vesselName))
            {
                this.isAKomet = true;
                KerbalKometScenario.Instance.UnregisterKomet(this.part.vessel.vesselName);
                Debug.Log("[ModuleKomet] - " + this.part.vessel.vesselName + " flipped to komet.");
            }

            //Setup komet/asteroid
            SetupAsKomet(this.isAKomet);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (isAKomet == false)
                return;
            if (tailTransform == null)
            {
                Debug.Log("[ModuleKomet] - tailTransform is null");
                return;
            }

            tailTransform.LookAt(Planetarium.fetch.Sun.transform);
        }

        public void SetupAsKomet(bool isKomet)
        {
            isAKomet = isKomet;

            //Setup GUI
            setupGUI();

            //Setup emitters
            setupEmitters();

            //Setup texture
            setupTexture();

            if (isAKomet)
            {
                //Setup resources
                setupResources();
            }
        }

        public static string CreateKometName(double discoveryTime)
        {
            try
            {
                //Uses a variant of the comet naming system created by the International Astronomical Union
                //Prefixes: P = komet with an orbital period of less than 200 years. K = komet with an orbital period of > 200 years.
                //Y#D#: Year and Day of discovery
                //Name: name of the kerbal that discovered the komet
                //Ex P-Y1D34/Bob
                
                string kometType = "P";

                //If our orbital period is > 200 years then redesignate the komet type.

                //Discovery date
                string discoveryDate = KSPUtil.dateTimeFormatter.PrintDateCompact(discoveryTime, false, false);
                discoveryDate = discoveryDate.Replace(", ", "");

                //Astronomer
                ProtoCrewMember crew = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
                string astronomer = crew.name;
                HighLogic.CurrentGame.CrewRoster.Remove(crew);
                astronomer = astronomer.Replace(" Kerman", "");

                //Formulate the komet's name
                return kometType + "-" + discoveryDate + "/" + astronomer;
            }
            catch (Exception ex)
            {
                Debug.Log("[ModuleKomet] - Error during CreateKometName: " + ex);
                return "Komet";
            }
        }

        protected void setupOrbit()
        {
            Orbit orbit = Orbit.CreateRandomOrbitAround(Planetarium.fetch.Sun, kometMinAltitude, kometMaxAltitude);

            //Komets have eccentric orbits, let's randomize the eccentricity.
            orbit.eccentricity = UnityEngine.Random.Range(eccentricityMin, eccentricityMax);

            //Set the new orbit.
            FlightGlobals.fetch.SetShipOrbit(orbit.referenceBody.flightGlobalsIndex, orbit.eccentricity, orbit.semiMajorAxis, orbit.inclination, orbit.LAN, orbit.meanAnomaly, orbit.argumentOfPeriapsis, orbit.ObT);
        }

        protected void setKometName()
        {
            string prefix = "Kmt. ";

            AsteroidName = CreateKometName(Planetarium.GetUniversalTime());
            this.part.vessel.vesselName = prefix + AsteroidName;
            this.part.initialVesselName = prefix + AsteroidName;
            this.part.partInfo.title = prefix + AsteroidName;

            //Dirty the GUI
            if (this.part.vessel.loaded)
                MonoUtilities.RefreshContextWindows(this.part);

            Debug.Log("[ModuleKomet] - New komet designation: " + AsteroidName);
        }

        protected void setupResources()
        {
            //If we've already done the resource swap them we're done.
            if (resourcesConverted)
                return;

            //Find the Ore, Water, and guaranteed resources (typically Organics, RareMetals, and ExoticMinerals)
            //If Community Resource Pack isn't installed, that's ok, we'll just keep the Ore as is and exit.
            ModuleAsteroidResource[] asteroidResources = this.part.FindModulesImplementing<ModuleAsteroidResource>().ToArray();
            ModuleAsteroidResource oreResource = null;
            ModuleAsteroidResource waterResource = null;
            List<ModuleAsteroidResource> extraResources = new List<ModuleAsteroidResource>();
            for (int index = 0; index < asteroidResources.Length; index++)
            {
                if (asteroidResources[index].resourceName == "Ore")
                    oreResource = asteroidResources[index];
                else if (asteroidResources[index].resourceName == "Water")
                    waterResource = asteroidResources[index];
                
                //If the resource is guaranteed then add it to the guarantee list.
                else if (string.IsNullOrEmpty(guaranteeResources) == false)
                {
                    if (guaranteeResources.Contains(asteroidResources[index].resourceName))
                        extraResources.Add(asteroidResources[index]);
                }
            }
            if (oreResource == null)
            {
                Debug.Log("[ModuleKomet] - oreResource is null, cannot setup resources.");
                return;
            }
            if (waterResource == null)
            {
                Debug.Log("[ModuleKomet] - waterResource is null, cannot setup resources.");
                return;
            }

            //Adjust the abundance for the guaranteed resources to at least the minimum
            float guaranteedResourcePercent = minimumResourcePercent / 100.0f;
            ModuleAsteroidResource[] resExtras = extraResources.ToArray();
            for (int index = 0; index < resExtras.Length; index++)
            {
                resExtras[index].abundance += guaranteedResourcePercent;

                //Reduce ore percentage accordingly.
                oreResource.abundance -= guaranteedResourcePercent;
//                Debug.Log("[ModuleKomet] - " + resExtras[index].resourceName + " abundance: " + resExtras[index].abundance);
            }

            //Adjust the Water and Ore abundance
            float waterAbundancePercent = (waterPercentage / 100.0f) * oreResource.abundance;
            oreResource.abundance = oreResource.abundance = waterAbundancePercent;
            waterResource.abundance += waterAbundancePercent;
//            Debug.Log("[ModuleKomet] - oreResource abundance: " + oreResource.abundance);
//            Debug.Log("[ModuleKomet] - waterResource abundance: " + waterResource.abundance);

            ModuleAsteroidInfo asteroidInfo = this.part.FindModuleImplementing<ModuleAsteroidInfo>();
            Debug.Log(asteroidInfo.resources);

            //Set the converted flag.
            resourcesConverted = true;
        }

        protected void setupTexture()
        {
            //This code isn't working. I can call the methods needed to change the texture but the asteroid model itself doesn't show the new texture.
            Transform potatoRoid = this.part.gameObject.transform.Find("model");
            Transform childTransform;
            if (potatoRoid == null)
            {
                Debug.Log("[ModuleKomet] - potatoRoid is null.");
                return;
            }

            Texture kometTexture = GameDatabase.Instance.GetTexture(kometTextureURL, false);
            if (kometTexture == null)
            {
                Debug.Log("[ModuleKomet] - kometTexture is null.");
                return;
            }

            Renderer renderer = potatoRoid.GetComponent<Renderer>();
            for (int index = 0; index < potatoRoid.childCount; index++)
            {
                childTransform = potatoRoid.GetChild(index);
                if (childTransform.name.Contains("Cube"))
                {
                    renderer = childTransform.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Debug.Log("[ModuleKomet] - Found renderer, changing texture for " + childTransform.name);
                        renderer.material.SetTexture("_MainTex", kometTexture);
                    }
                    else
                    {
                        Debug.Log("[ModuleKomet] - Can't find Renderer");
                    }
                }
            }
        }

        protected void setupGUI()
        {
            if (debugMode)
                Events["ToggleKomet"].active = true;
            else
                Events["ToggleKomet"].active = false;

            if (isAKomet)
                Events["ToggleKomet"].guiName = "Turn Into Asteroid";
            else
                Events["ToggleKomet"].guiName = "Turn Into Komet";
        }

        protected void setupEmitters()
        {
            KSPParticleEmitter[] emitters = this.part.GetComponentsInChildren<KSPParticleEmitter>();

            if (emitters == null)
                return;

            foreach (KSPParticleEmitter emitter in emitters)
            {
                //If we're a komet then show the emitter
                if (isAKomet)
                {
                    emitter.emit = true;
                    emitter.enabled = true;
                }

                //No komet
                else
                {
                    emitter.emit = false;
                    emitter.enabled = false;
                }
            }
        }
    }
}
