using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace CurrencyVisualization
{
    static class Program
    {
        public static Dictionary<string, List<DailyRates>> allSymbols;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            allSymbols = new Dictionary<string, List<DailyRates>>();
            

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);

            var timer = new System.Threading.Timer((e) =>
            {
                LoadDLL();
                

                Console.WriteLine("\nNext Iteration\n\n");
            }, null, startTimeSpan, periodTimeSpan);

            
            Application.Run(new ChartForm());
            Console.ReadKey();
        }





        public static void LoadDLL()
        {
            DirectoryInfo libsDir = new DirectoryInfo("./libs/");

            if (libsDir.Exists)
            {
                foreach (FileInfo file in libsDir.GetFiles("*.dll", SearchOption.AllDirectories))
                {
                    new Thread(y =>
                    {
                        Console.WriteLine("Foreach thread started. Processing: " + file.FullName);

                        Assembly tempAssembly = null;

                        //Before loading the assembly, check all current loaded assemblies in case talready loaded
                        //has already been loaded as a reference to another assembly
                        //Loading the assembly twice can cause major issues
                        foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            //Check the assembly is not dynamically generated as we are not interested in these
                            if (loadedAssembly.ManifestModule.GetType().Namespace != "System.Reflection.Emit")
                            {
                                //Get the loaded assembly filename
                                string sLoadedFilename = loadedAssembly.CodeBase.Substring(loadedAssembly.CodeBase.LastIndexOf('/') + 1);

                                //If the filenames match, set the assembly to the one that is already loaded
                                if (sLoadedFilename.ToUpper() == file.Name.ToUpper())
                                {
                                    tempAssembly = loadedAssembly;
                                    break;
                                }
                            }
                        }

                        //If the assembly is not aleady loaded, load it manually
                        if (tempAssembly == null)
                        {
                            tempAssembly = Assembly.LoadFile(file.FullName);

                            List<DailyRates> dailyRatesList = new List<DailyRates>();
                            Dictionary<DateTime, Double> openPrices = null;
                            Dictionary<DateTime, Double> highPrices = null;
                            Dictionary<DateTime, Double> lowPrices = null;
                            Dictionary<DateTime, Double> closePrices = null;
                            string symbolName = null;

                            Type classToInvoke = FindClassWithInterface(tempAssembly, typeof(ICurrencyInfo));
                            try
                            {
                                var classConstructor = Activator.CreateInstance(classToInvoke);

                                openPrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetOpenPrices", BindingFlags.InvokeMethod, null, classConstructor, null);
                                highPrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetHighPrices", BindingFlags.InvokeMethod, null, classConstructor, null);
                                lowPrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetLowPrices", BindingFlags.InvokeMethod, null, classConstructor, null);
                                closePrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetClosePrices", BindingFlags.InvokeMethod, null, classConstructor, null);

                                symbolName = (string)classToInvoke.InvokeMember("GetSymbolName", BindingFlags.InvokeMethod, null, classConstructor, null);
                            }
                            catch
                            {
                                Console.WriteLine("Some err");
                            }

                            foreach (var item in openPrices)
                            {
                                dailyRatesList.Add(new DailyRates()
                                {
                                    Date = item.Key,
                                    Open = item.Value,
                                });
                            }


                            List<Thread> threads = new List<Thread>()
                        {
                                new Thread(x => {
                                foreach (var item in highPrices)
                                {
                                    foreach (var listItem in dailyRatesList)
                                    {
                                        if (item.Key == listItem.Date)
                                        {
                                            listItem.High = item.Value;
                                        }
                                    }
                                }
                            }),
                                new Thread(x => {
                                foreach (var item in lowPrices)
                                {
                                    foreach (var listItem in dailyRatesList)
                                    {
                                        if (item.Key == listItem.Date)
                                        {
                                            listItem.Low = item.Value;
                                        }
                                    }
                                }
                            }),
                                new Thread(x => {
                                foreach (var item in closePrices)
                                {
                                    foreach (var listItem in dailyRatesList)
                                    {
                                        if (item.Key == listItem.Date)
                                        {
                                            listItem.Close = item.Value;
                                        }
                                    }
                                }
                            })
                        };

                            foreach (Thread t in threads)
                            {
                                t.Start();
                            }

                            foreach (Thread t in threads)
                            {
                                t.Join();
                            }


                            if (allSymbols.ContainsKey(symbolName))
                            {
                                allSymbols[symbolName] = dailyRatesList;
                            }
                            else
                            {
                                allSymbols.Add(symbolName, dailyRatesList);
                            }
                        }
                    }).Start();

                    
                }
            }
        }

        

        public static Type FindClassWithInterface(Assembly assembly, Type RequiredInterfaceType)
        {
            Type searchedType = null;

            var libraryTypes = assembly.GetTypes();

            foreach (var type in libraryTypes)
            {
                var typeInterfaces = type.GetInterfaces()
                   .Where(p => p.Name == RequiredInterfaceType.Name).FirstOrDefault();

                if (typeInterfaces != null)
                {
                    Console.WriteLine("Classes from lib implementing ICurrencyInfo interface");
                    Console.WriteLine(type + " " + typeInterfaces);

                    searchedType = type;
                }

                //Without .FirstOrDefault returns list of all classes which implements my custom interface. Anyway here I can expect there is only one class implementing it so .FirstOrDefault
                //foreach (var inter in typeInterfaces)
                //{
                //    Console.WriteLine("Classes from lib implementing ICurrencyInfo interface");
                //    Console.WriteLine(type + " " + inter);
                //}
            }

            return searchedType;
        }
    }
}





/*
This is my unsuccessful try of getting generic list from .dll. It fails on parsing generic type of list. FullName: CurrencyVisualization.DailyRates vs DLLProject.DailyRates
are different classes. Wasn't able to go through items of list.

    Solution: divide each price types into 4 different dictionaries with basic generic types DateTime and Double.

    https://stackoverflow.com/questions/2608960/unable-to-cast-lists-with-reflection-in-c-sharp

    Just some of my old code:

    Type genericTypeOfList = tempAssembly.DefinedTypes.Where(type => type.Name == typeof(DailyRates).Name).FirstOrDefault();
                        
    Type listType = typeof(List<>).MakeGenericType(genericTypeOfList);
    IList list = (IList)Activator.CreateInstance(listType);


    Type myType = tempAssembly.GetType("TestLibrary.DailyRates");
    object myObject = Activator.CreateInstance(myType);


    //double val = (double)myType.GetProperty("Open").GetValue(myType, null);


    //Console.WriteLine(val);
         

    Type classToInvoke = FindClassWithInterface(tempAssembly, typeof(ICurrencyInfo));
    var classConstructor = Activator.CreateInstance(classToInvoke);

    list = (IList)classToInvoke.InvokeMember("GetRatesList", BindingFlags.InvokeMethod, null, classConstructor, null);
    
    
    List<DailyRates> dailyRatesList = new List<DailyRates>();

                        
    for(int i = 0; i < list.Count; i++)
    {

    }
    foreach (var item in list)
    {
    Console.WriteLine(item.ToString());
    }
    */
