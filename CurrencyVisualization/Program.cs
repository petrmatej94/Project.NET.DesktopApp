using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;


namespace CurrencyVisualization
{
    static class Program
    {
        public static Dictionary<string, List<DailyRates>> allSymbols;
        private static ChartForm chart;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            allSymbols = new Dictionary<string, List<DailyRates>>();
            chart = new ChartForm();

            //Loads files that are already in folder with libs
            LoadExistingDLLs();
            
            //Start watcher to check changes in directory
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = @".\libs\";
            watcher.IncludeSubdirectories = false;
            watcher.Filter = "*.dll";

            watcher.Created += Watcher_Created;
            watcher.Renamed += Watcher_Renamed;
            watcher.Deleted += Watcher_Deleted;
            watcher.EnableRaisingEvents = true;
            
            Application.Run(chart);
            Console.ReadKey();
        }


        private static void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string name = e.Name.Substring(0, e.Name.LastIndexOf("."));
            if(allSymbols.ContainsKey(name))
            {
                allSymbols.Remove(name);
                chart.MyRefresh();
            }
        }


        private static void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string name = e.Name.Substring(0, e.Name.LastIndexOf("."));
            string oldName = e.OldName.Substring(0, e.OldName.LastIndexOf("."));

            if (allSymbols.ContainsKey(oldName))
            {
                if(allSymbols.ContainsKey(name))
                {
                    //Symbol is already present in project
                    allSymbols.Remove(name);
                }
                allSymbols.Remove(oldName);
            }
            new Thread(a => LoadDLL(e.FullPath)).Start();
        }


        private static void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string name = e.Name.Substring(0, e.Name.LastIndexOf("."));
            if (!allSymbols.ContainsKey(name))
            {
                new Thread(a => LoadDLL(e.FullPath)).Start();
            }
        }


        public static void LoadExistingDLLs()
        {
            DirectoryInfo libsDir = new DirectoryInfo("./libs/");

            if (libsDir.Exists)
            {
                foreach (FileInfo file in libsDir.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
                {
                    new Thread(y =>
                    {
                        LoadDLL(file.FullName);
                    }).Start();
                }
            }
        }


        public static void LoadDLL(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);

            Console.WriteLine("Currently loading ... : " + fi.Name);
            Assembly newAssembly = Assembly.LoadFile(fi.FullName);

            List<DailyRates> dailyRatesList = new List<DailyRates>();
            Dictionary<DateTime, Double> openPrices = null;
            Dictionary<DateTime, Double> highPrices = null;
            Dictionary<DateTime, Double> lowPrices = null;
            Dictionary<DateTime, Double> closePrices = null;
            string symbolName = null;

            Type classToInvoke = FindClassWithInterface(newAssembly, typeof(ICurrencyInfo));

            var classConstructor = Activator.CreateInstance(classToInvoke);

            openPrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetOpenPrices", BindingFlags.InvokeMethod, null, classConstructor, null);
            highPrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetHighPrices", BindingFlags.InvokeMethod, null, classConstructor, null);
            lowPrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetLowPrices", BindingFlags.InvokeMethod, null, classConstructor, null);
            closePrices = (Dictionary<DateTime, Double>)classToInvoke.InvokeMember("GetClosePrices", BindingFlags.InvokeMethod, null, classConstructor, null);

            symbolName = (string)classToInvoke.InvokeMember("GetSymbolName", BindingFlags.InvokeMethod, null, classConstructor, null);
            


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
            chart.MyRefresh();
            Console.WriteLine("Loading finished ... : " + fi.Name);
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
