using SymbolFetch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zodiacon.DebugHelp;

namespace ConsoleApp1
{
    class Program
    {
        public static SymbolHandler SymbolHandler { get; private set; }
        public static ICollection<SymbolInfo> symbols;
        public static IList<SymbolInfo> types;
        public static ulong BaseAddress;
        private static ResourceDownloader downloader = new ResourceDownloader();

        private static async Task LoadSymbol()
        {
            string filename = "C:\\localsymbols\\ntkrnlmp.pdb\\BD8C7CDE0D907F0F5554F160B99CA6021\\ntkrnlmp.pdb";
            var isPdb = Path.GetExtension(filename).Equals(".pdb", StringComparison.InvariantCultureIgnoreCase);
            var handler = SymbolHandler.Create();
            BaseAddress = await handler.TryLoadSymbolsForModuleAsync(filename, isPdb ? 0x1000000UL : 0);
            SymbolHandler = handler;

            symbols = await Task.Run(() => SymbolHandler.EnumSymbols(BaseAddress));
            symbols = await Task.Run(() => SymbolHandler.EnumSymbols(BaseAddress));
            types = SymbolHandler.EnumTypes(BaseAddress);
            
        }

        private static void TestReadSymbol()
        {
            LoadSymbol();
            Console.ReadLine();
            foreach (var i in symbols)
            {
                Console.WriteLine("symbol name:" + i.Name + " | 偏移地址:" + (i.Address - i.ModuleBase));
            }
            var ssdt = symbols.First(sym => sym.Name == "KeServiceDescriptorTable");
            //var fun = symbols.First(sym => sym.Name == "NtOpenProcess");
            string hexOutput = String.Format("{0:X}", ssdt.Address - ssdt.ModuleBase);
            Console.WriteLine("SSDT地址:" + hexOutput);
            Console.ReadLine();
            foreach (var i in types)
            {
                Console.WriteLine("Type name:" + i.Name);
            }
            Console.ReadLine();
            var type = types.First(sym => sym.Name == "_EPROCESS");
            var TypeStruct = SymbolHandler.BuildStructDescriptor(BaseAddress, type.Index);
            foreach (var t in TypeStruct)
            {
                Console.WriteLine(t.Name + "|" + t.Offset + "|" + t.Size);
            }
        }

        static void Main(string[] args)
        {
            while (true)
            {
                TestReadSymbol();
                
            }
        }
    }
}
