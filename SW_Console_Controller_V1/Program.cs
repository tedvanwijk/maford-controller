﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SolidWorks.Interop.sldworks;

namespace SW_Console_Controller_V1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // means we are directly debugging in vs, let exceptions happen so we can debug
                string input;
                using (StreamReader reader = new StreamReader("../../SampleData.json"))
                {
                    input = reader.ReadToEnd();
                }

                JsonSerializerSettings settings = new JsonSerializerSettings{
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                Properties properties = JsonConvert.DeserializeObject<Properties>(input, settings);
                SldWorks swApp = new SldWorks();
                SWController controller = new SWController(properties, swApp, input);
                Console.WriteLine("Done!");
                Console.ReadLine();
            }
            else
            {
                // we are running from the API, catch errors and return them as a string for proper error handling by the API
                try
                {
                    string input = Encoding.UTF8.GetString(Convert.FromBase64String(args[0]));

                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    Properties properties = JsonConvert.DeserializeObject<Properties>(input, settings);
                    SldWorks swApp = new SldWorks();
                    SWController controller = new SWController(properties, swApp, input);
                    Console.WriteLine("Done!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR");
                    Console.WriteLine(ex.ToString());
                }

            }
        }
    }
}
