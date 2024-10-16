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
        private static CustomPropertyManager Manager;
        public static Dictionary<string, int> OldEquations;

        static public void Initialize(CustomPropertyManager manager)
        {
            Manager = manager;
        }

        static public void SetEquation(string name, string value)
        {
            Manager.Set2(name, value);
        }

        static public void SetEquation(string name, double value)
        {
            Manager.Set2(name, value.ToString());
        }

        static public void SetEquation(string name, decimal value)
        {
            Manager.Set2(name, value.ToString());
        }

        static public void SetEquation(string name, int value)
        {
            Manager.Set2(name, value.ToString());
        }
    }
}
