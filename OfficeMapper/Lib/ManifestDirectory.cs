using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Xml.Serialization;

namespace OfficeMapper.Lib
{
    [Serializable]
    public class ManifestDirectory
    {
        private static string ManifestName = @"manifest.xml";
        /// <summary>
        /// sAMAccountName владельца информации в файловой шаре
        /// </summary>
        public List<string> Owners { get; set; }

        /// <summary>
        /// Класс инфомации в каталоге КИ та КТ, ДСК и т.д.
        /// </summary>
        public string InformationClass { get; set; }
        public string Description { get; set; }
        public string DirectoryPath { get; set; }

        public bool NewDomain { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ManifestDirectory(string directoryPath) {
            DirectoryPath = directoryPath;

            if(Exists(directoryPath))
            {
                ManifestDirectory md = Load(directoryPath);
                if(md != null)
                {
                    Owners = md.Owners;
                    InformationClass = md.InformationClass;
                    Description = md.Description;
                    DirectoryPath = md.DirectoryPath;
                }
            }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ManifestDirectory()
        {
        }

        /// <summary>
        /// Загружает текущий манифест каталога
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static ManifestDirectory Load(string directoryPath)
        {
            ManifestDirectory md = null;
            if (Exists(directoryPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ManifestDirectory));
                using (var fStream = new FileStream(Path.Combine(directoryPath, ManifestName), FileMode.Open, FileAccess.Read, System.IO.FileShare.None))
                {
                    md = (ManifestDirectory)serializer.Deserialize(fStream);
                }
            }
            return md;
        }

        /// <summary>
        /// Определяет существование манифеста в заданном каталоге
        /// </summary>
        /// <returns></returns>
        public static bool Exists(string directoryPath)
        {
            return File.Exists(Path.Combine(directoryPath,ManifestName));
        }

        /// <summary>
        /// Сериализует и сохраняет данные в скрытый и системный файл
        /// </summary>
        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ManifestDirectory));
            using (var fStream = new FileStream(Path.Combine(DirectoryPath, ManifestName), FileMode.Create, FileAccess.Write, System.IO.FileShare.None))
            {
                serializer.Serialize(fStream, this);
            }
            string path = Path.Combine(DirectoryPath, ManifestName);
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden 
                | FileAttributes.ReadOnly 
                | FileAttributes.System);
        }
    }
}