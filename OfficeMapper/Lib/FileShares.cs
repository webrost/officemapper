using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace OfficeMapper.Lib
{
    public class FileShares
    {
        public List<ManifestDirectory> Manifests { get; set; }

        public string RootDirectory { get; set; }

        /// <summary>
        /// Создает элемент и проверяет налилие шары и доступ к ней
        /// </summary>
        /// <param name="path"></param>
        public FileShares(string path)
        {
            if(System.IO.Directory.Exists(path) && HasAccess(path))
            {
                RootDirectory = path;
                Manifests = new List<ManifestDirectory>();
            }
            else
            {
                throw new Exception(string.Format("Directory {0} does not exists",path));
            }            
        }

        /// <summary>
        /// Возвращает перечень шар компании
        /// </summary>
        /// <returns></returns>
        public static List<Models.FileShareEntry> GetAllShares(string username)
        {
            List<Models.FileShareEntry> shares = new List<Models.FileShareEntry>();
            //bool LearningM
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach(var fs in model.FileShares.ToList())
                {
                    Models.FileShareEntry entry = new Models.FileShareEntry()
                    {
                        Path = fs.Name,
                        Owners = fs.FileShareOwners.Select(x => x.samAccountName).ToList(),
                        Rights = fs.FileShareAccesses.Select(x => new Lib.FileShareAccessEntry() {
                            AccountName = x.samAccountName,
                            AllowRead = x.AllowRead != null ? x.AllowRead.Value : false,
                            AllowWrite = x.AllowWrite != null ? x.AllowWrite.Value : false
                        }).ToList(),
                        CurrentUserAccountName = username,
                        AllowRead = fs.FileShareAccesses.Count(x=>x.samAccountName == username && x.AllowRead == true) >0,
                        AllowWrite = fs.FileShareAccesses.Count(x => x.samAccountName == username && x.AllowWrite == true) > 0
                    };
                    shares.Add(entry);
                }
            }
            return shares;
        }

        /// <summary>
        /// Инициализирует объект и загружает структуру из файловой системы 
        /// в базу данных (обновляет данные из манифестов)
        /// </summary>
        /// <param name="currDir"></param>
        public void Build(string currDir)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (HasAccess(currDir) && System.IO.Directory.GetDirectories(currDir).Length > 0)
                {
                    foreach (var di in (new System.IO.DirectoryInfo(currDir)).GetDirectories())
                    {
                        if (ManifestDirectory.Exists(currDir))
                        {
                            ///--Получаю манифест
                            ManifestDirectory md = new ManifestDirectory(currDir);

                            ///---Проверяю наличие записи в базе, если нет - создаю
                            if(model.FileShares.Count(x=>x.Name == currDir) == 0)
                            {
                                Models.FileShare share = new Models.FileShare()
                                {
                                    Name = currDir                                    
                                };

                                share.FileShareOwners = new System.Data.Linq.EntitySet<Models.FileShareOwner>();
                                ///---Добавление в базу владельцев файловой шары
                                foreach (var owner in md.Owners)
                                {
                                    share.FileShareOwners.Add(new Models.FileShareOwner() {
                                        samAccountName = owner
                                    });
                                }

                                model.FileShares.InsertOnSubmit(share);
                                model.SubmitChanges();
                            }
                        }
                        Build(di.FullName);
                    }
                }
            }
        }

        private bool HasAccess(string path)
        {
            try {
                if(System.IO.Directory.GetDirectories(path).Length > 0)
                {

                }
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        /// <summary>
        /// НАЧАЛЬНАЯ. Строит файловую структуру согласно функциям и Департаментам
        /// </summary>
        public void BuildFileStructure()
        {
            BuildFS(RootDirectory);
            BuildFS(@"\\fs01.ukrtransnafta.com\UTN\100 Загальна");
        }

        public void BuildFS(string rootPath)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach (var dep in model.Phones.ToList().Where(x => x.Code != null
                 && x.Code.Split('.').Length == 2
                 && x.Dep != null))
                {
                    string upperLetter = "";
                    foreach(string letter in dep.Dep.Split(' '))
                    {
                        upperLetter += letter[0].ToString().ToUpper();
                    }
                    string dirName = dep.Code + " " + upperLetter;
                    if (!Directory.Exists(Path.Combine(rootPath, dirName).Replace(":", "")))
                    {
                        Directory.CreateDirectory(Path.Combine(rootPath, dirName).Replace(":", ""));

                        ManifestDirectory md = new ManifestDirectory();
                        md.DirectoryPath = Path.Combine(rootPath, dirName);
                        md.Description = "Initial creating directory";
                        md.Owners = new List<string>();
                        md.Owners.Add("anisimov@ukrtransnafta.com");
                        md.Owners.Add("ivasenko@ukrtransnafta.com");
                        md.Save();

                        ///---Создаем дочерние каталоги
                        //foreach (var subDep in model.Phones.ToList().Where(x => x.Code != null
                        // && x.Code.Split('.').Length == 2
                        // && x.Code.StartsWith(dep.Code + ".")
                        // && x.Dep != null))
                        //{
                        //    string subDirName = subDep.Code + " " + subDep.Dep;
                        //    if (!Directory.Exists(Path.Combine(rootPath, dirName, subDirName).Replace(":", "")))
                        //    {
                        //        Directory.CreateDirectory(Path.Combine(rootPath, dirName, subDirName).Replace(":", ""));

                        //        ManifestDirectory md1 = new ManifestDirectory();
                        //        md1.DirectoryPath = Path.Combine(rootPath, dirName, subDirName).Replace(":", "");
                        //        md1.Description = "Initial creating directory";
                        //        md1.Owners = new List<string>();
                        //        md1.Owners.Add("anisimov@ukrtransnafta.com");
                        //        md1.Owners.Add("ivasenko@ukrtransnafta.com");
                        //        md1.Save();
                        //    }
                        //}
                    }
                }
            }
        }
    }
}