using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUpgrades
{
    internal class SaveManager
    {
        [System.Serializable]
        public class ModSaveData
        {
            public List<int> Levels = new List<int> { 0, 0, 0, 0, 0 };
        }
    }
}
