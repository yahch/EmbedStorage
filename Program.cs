using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Swan;
using Zio.FileSystems;

namespace EmbedStorage
{
    class Program
    {

        public static List<StorageModel> FileStorages;

        static void Main(string[] args)
        {
            Load();

            var url = "http://+:5000/";

            using (var server = new WebServer(url))
            {

                server.WithLocalSession();
                server.RegisterModule(new WebApiModule());
                server.Module<WebApiModule>().RegisterController<APIController>();
                server.RunAsync();
                Console.ReadKey(true);
            }
        }

        public static bool Save()
        {
            try
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                using (var fs = GetWorkFileSystem().OpenFile("/data/storage.bin",
                    System.IO.FileMode.OpenOrCreate,
                    System.IO.FileAccess.ReadWrite,
                    System.IO.FileShare.None))
                {
                    serializer.Serialize(fs, FileStorages);
                }
                return true;
            }
            catch (Exception ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
                return false;
            }
        }

        public static void Load()
        {
            try
            {
                if (!GetWorkFileSystem().FileExists("/data/storage.bin")
                    || GetWorkFileSystem().GetFileLength("/data/storage.bin") < 10)
                {
                    FileStorages = new List<StorageModel>();
                    return;
                }

                using (var fs = GetWorkFileSystem().OpenFile("/data/storage.bin",
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.ReadWrite))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer =
              new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    FileStorages = deserializer.Deserialize(fs) as List<StorageModel>;
                }

            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                FileStorages = new List<StorageModel>();
            }
            catch (Exception ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
            }
        }

        public static SubFileSystem GetWorkFileSystem()
        {
            var fs = new PhysicalFileSystem();
            var path = fs.ConvertPathFromInternal(AppDomain.CurrentDomain.BaseDirectory);
            var subfs = new SubFileSystem(fs, path);
            return subfs;
        }
    }
}
