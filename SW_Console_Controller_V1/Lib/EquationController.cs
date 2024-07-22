using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolidWorks.Interop.sldworks;

namespace SW_Console_Controller_V1.Lib
{
    internal static class EquationController
    {
        private static EquationMgr Manager;
        public static Dictionary<string, int> OldEquations;

        static public void Initialize(EquationMgr manager)
        {
            Manager = manager;
            // the equationmanager stores both equations and global variables in one weird list, so index all variables so they can be looked up
            int equationLength = Manager.GetCount();
            OldEquations = new Dictionary<string, int>();
            for (int i = 0; i < equationLength; i++)
            {
                OldEquations.Add(Manager.Equation[i].Split('=')[0].Replace("\"", "").Trim(), i);
                //OldEquations[i] = Manager.Equation[i].Split('=')[0].Replace("\"", "").Trim();
            }
        }

        static public void SetEquation(string name, string equation)
        {
            // find index of parameter name in the OldEquations list
            int index = OldEquations[name.Trim()];
            Manager.Equation[index] = $"\"{name}\"= {equation}";
        }
    }
}
