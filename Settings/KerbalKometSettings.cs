using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalKomets
{
    public class KerbalKometSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Auto-track komets when discovered", toolTip = "If enabled, komets will automatically be tracked when discovered.", autoPersistance = true)]
        public bool autoTrackKomets = true;

        [GameParameters.CustomParameterUI("Send press release when discovered", toolTip = "If enabled, you'll receive a press release when a komet is discovered.", autoPersistance = true)]
        public bool sendPressRelease = true;

        [GameParameters.CustomIntParameterUI("Komet discovery chance", maxValue = 10000, minValue = 1, stepSize = 1, toolTip = "N out of 10000 chances to discover a komet. The larger the number, the bigger the chance.", autoPersistance = true)]
        public int presenceChance = 1;

        [GameParameters.CustomIntParameterUI("Max Komets", maxValue = 100, minValue = 1, stepSize = 1, toolTip = "Maximum number of komets allowed at any given time.", autoPersistance = true)]
        public int maxKomets = 10;

        #region Properties
        public static int MaxKomets
        {
            get
            {
                KerbalKometSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalKometSettings>();
                return settings.maxKomets;
            }
        }

        public static int PresenceChance
        {
            get
            {
                KerbalKometSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalKometSettings>();
                return settings.presenceChance;
            }
        }

        public static bool SendPressRelease
        {
            get
            {
                KerbalKometSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalKometSettings>();
                return settings.sendPressRelease;
            }

            set
            {
                KerbalKometSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalKometSettings>();
                settings.sendPressRelease = value;
            }
        }

        public static bool AutoTrackKomets
        {
            get
            {
                KerbalKometSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalKometSettings>();
                return settings.autoTrackKomets;
            }

            set
            {
                KerbalKometSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbalKometSettings>();
                settings.autoTrackKomets = value;
            }
        }
        #endregion

        #region CustomParameterNode

        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }

        public override string Section
        {
            get
            {
                return "Kerbal Komets";
            }
        }

        public override string Title
        {
            get
            {
                return "Discovery";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
        #endregion
    }
}
