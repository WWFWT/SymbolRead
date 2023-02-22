using SymbolFetch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Zodiacon.DebugHelp;

namespace ConsoleApp1
{
    class Program
    {
        public static SymbolHandler symbolHandler { get; private set; }
        public static ulong baseAddress = 0;

        static string BuildUrl(string fileFullPath)
        {
            PeHeaderReader reader = new PeHeaderReader(fileFullPath);
            string pdbName;

            if(string.IsNullOrEmpty(reader.pdbName)) {
                return "";
            }

            if (reader.pdbName.Contains("\\")) {
                string[] tmp = reader.pdbName.Split('\\');
                pdbName = (tmp[tmp.Length - 1]);
            } else {
                pdbName = reader.pdbName;
            }

            return "http://msdl.microsoft.com/download/symbols/" +
                pdbName + "/" + reader.debugGUID.ToString("N").ToUpper() + reader.pdbage + "/" + pdbName;
        }

        static bool HttpDownload(string url, string path, ref float progress)
        {
            string tempFilePath = System.IO.Path.GetDirectoryName(path) + ".temp";
            if (System.IO.File.Exists(tempFilePath)) {
                System.IO.File.Delete(tempFilePath);
            }

            try
            {
                FileStream fs = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream responseStream = response.GetResponseStream();

                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                long newSize = 0;
                long allSize = response.ContentLength;

                while (size > 0) {
                    fs.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                    newSize += size;
                    progress = ((float)newSize) / ((float)allSize);
                }
                fs.Close();
                responseStream.Close();
                
                if (System.IO.File.Exists(path)) {
                    System.IO.File.Delete(path);
                }
                System.IO.File.Move(tempFilePath, path);
                System.IO.File.Delete(tempFilePath);
                return true;
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        private static Dictionary<string, SymbolInfo> LoadSymbol()
        {
            string filename = "../../ntkrnlmp.pdb";
            var isPdb = Path.GetExtension(filename).Equals(".pdb", StringComparison.InvariantCultureIgnoreCase);

            symbolHandler = SymbolHandler.Create();
            baseAddress = symbolHandler.TryLoadSymbolsForModuleAsync(filename, isPdb ? 0x1000000UL : 0).Result;
            

            ICollection<SymbolInfo> symbols = symbolHandler.EnumSymbols(baseAddress);
            IList<SymbolInfo> types = symbolHandler.EnumTypes(baseAddress);

            Dictionary<string, SymbolInfo> res = new Dictionary<string, SymbolInfo>();
            foreach(var symbol in symbols) {
                if (!res.ContainsKey(symbol.Name))
                {
                    res.Add(symbol.Name, symbol);
                }
            }
            foreach(var type in types) {
                if (!res.ContainsKey(type.Name))
                {
                    res.Add(type.Name, type);
                }
            }

            return res;
        }

        static StructDescriptor GetStruct(SymbolInfo type)
        {
            return symbolHandler.BuildStructDescriptor(baseAddress, type.Index);
        }
        
        static void Main(string[] args)
        {
            // Get Url
            string url = BuildUrl("C:\\Windows\\System32\\ntoskrnl.exe");
            Console.WriteLine(url);

            // Test Pdb Download
            float progress = 0f;
            Task task = Task.Factory.StartNew(() => HttpDownload(url, "../../ntkrnlmp.pdb", ref progress));

            // Wait
            while(!task.IsCompleted)
            {
                Console.WriteLine("下载中...    " + progress * 100 + "%");
                Task.Delay(1000).Wait();
            }
            Console.WriteLine("下载完成！");

            // Test LoadSymbol
            var ret = LoadSymbol();
            foreach (var i in ret)
            {
                Console.WriteLine("name:" + i.Value.Name);
            }

            // Test Read Struct
            var typeStruct = GetStruct(ret["_EPROCESS"]);
            foreach (var t in typeStruct)
            {
                Console.WriteLine(t.Name + " | offset:" + t.Offset + " | size:" + t.Size);
            }

            Console.ReadLine();
        }
    }
}
