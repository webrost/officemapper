using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OfficeMapper.Controllers
{
    public class TasksApiController : ApiController
    {
        [HttpGet]
        [Route("api/fileshares/createstructure")]
        public void CreateStructures()
        {
            string path = @"\\fs01.ukrtransnafta.com\UTN";
            Lib.FileShares fs = new Lib.FileShares(path);
            fs.BuildFileStructure();
        }

        [HttpGet]
        [Route("api/importphones")]
        public void importphones()
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                model.ImportPhones();
            }
        }

        /// <summary>
        /// Обновляет структуру файловых сервсисов из реальной файловой структуры в базу
        /// </summary>
        [HttpGet]
        [Route("api/fileshares/UpdateDatabaseStructure")]
        public void Test()
        {
            string FileSharesCode = System.Configuration.ConfigurationManager.AppSettings["FileSharesCode"].ToString().Trim();
                        
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                var FileServices = model.UserServices
                    .Where(x => x.Id.Trim().StartsWith(FileSharesCode + ".") || x.Id.Trim() == FileSharesCode)
                    .ToList();
                foreach (string path in FileServices.Where(x=>x.ServiceName != null).Select(x=>x.ServiceName))
                {
                    Lib.FileShares fs = new Lib.FileShares(path);
                    fs.Build(path);
                }
            }
        }

        [HttpGet]
        [Route("api/ioc")]
        public void IoC()
        {
            //Lib.WorkflowManager m = new Lib.WorkflowManager();
            //m.Run();
        }

        /// <summary>
        /// Импорт данных по дружбе
        /// </summary>
        [HttpGet]
        [Route("api/importDruzhba")]
        public void importDruzhba()
        {
            string FileName = "phonesDR_AU.xlsx";
            string Folder = "DRUZHBA";
            int codeOffset = 200;

            ///--- import file to Druzhba
            Models.LevelRange range = new Models.LevelRange() {
                startRow = 2,
                level = 0,
                endRow = GetLastRow(FileName, Folder)-1,
                code="0"
            };
            DeleteRecords(FileName);
            List<Models.LevelRange>  ranges = GetSubRanges(range, FileName, Folder, codeOffset);
            UpdateObjects(FileName);
        }

        /// <summary>
        /// return tree from file
        /// </summary>
        /// <param name="range"></param>
        /// <param name="FileName"></param>
        /// <param name="Folder"></param>
        /// <param name="codeOffset"></param>
        /// <returns></returns>
        private List<Models.LevelRange> GetSubRanges(Models.LevelRange range, string FileName, string Folder, int codeOffset)
        {
            #region ---Get sub ranges
            string FileShareResources = System.Configuration.ConfigurationManager.AppSettings["FileShareResources"].ToString();
            string PhonesDruzhba = System.IO.Path.Combine(FileShareResources, "Phones", Folder, FileName);
            List<Models.LevelRange> ranges = new List<Models.LevelRange>();

            if(range.endRow > range.startRow)
            {
                using (var sr = System.IO.File.OpenRead(PhonesDruzhba))
                {
                    NPOI.XSSF.UserModel.XSSFWorkbook wb = new NPOI.XSSF.UserModel.XSSFWorkbook(sr);
                    int searchLevel = range.level + 1;
                    int currPosition = range.startRow;
                    int endRangePosition = currPosition;
                    int rangeNumber = 0;

                    while (currPosition <= range.endRow)
                    {
                        if (wb.GetSheet("TDSheet").GetRow(currPosition).Cells[3] != null && wb.GetSheet("TDSheet").GetRow(currPosition).Cells[3].ToString().Trim() == searchLevel.ToString())
                        {
                            ranges.Add(new Models.LevelRange()
                            {
                                startRow = currPosition+1,
                                level = searchLevel,
                                code = range.level == 0?
                                range.code = wb.GetSheet("TDSheet").GetRow(currPosition).Cells[1].ToString()
                                : range.code + "." + (codeOffset + rangeNumber).ToString(),
                                entry = GetEntry(currPosition, FileName, Folder)
                            });
                            if (rangeNumber > 0)
                            {
                                ranges[rangeNumber - 1].endRow = currPosition - 1;
                            }
                            rangeNumber += 1;
                        }
                        currPosition += 1;
                    }
                    ranges[rangeNumber - 1].endRow = currPosition - 1;
                }
            }
            #endregion

            if (ranges.Count > 0)
            {
                range.childLevels = ranges;
                foreach(var cl in range.childLevels)
                {
                    cl.childLevels = GetSubRanges(cl, FileName, Folder, codeOffset);
                    SaveEntry(cl, FileName);                    
                }
                return range.childLevels;
            }
            else
            {
                return new List<Models.LevelRange>();
            }
        }

        private int GetLastRow(string FileName, string Folder)
        {
            string FileShareResources = System.Configuration.ConfigurationManager.AppSettings["FileShareResources"].ToString();
            string PhonesDruzhba = System.IO.Path.Combine(FileShareResources, "Phones", Folder, FileName);
            using (var sr = System.IO.File.OpenRead(PhonesDruzhba))
            {
                NPOI.XSSF.UserModel.XSSFWorkbook wb = new NPOI.XSSF.UserModel.XSSFWorkbook(sr);
                return wb.GetSheet("TDSheet").LastRowNum;
            }
        }

        /// <summary>
        /// delete all record imported from file
        /// </summary>
        /// <param name="FileName"></param>
        private void DeleteRecords(string FileName)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                model.PhonesDruzhbas.DeleteAllOnSubmit(model.PhonesDruzhbas.Where(x => x.FromFile == FileName));
                model.SubmitChanges();
                
            }
        }

        /// <summary>
        /// return CompanyObject for current user
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="filia"></param>
        /// <returns></returns>
        private string GetCompanyObject(string userCode, string filia)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                int total = userCode.Split('.').Count();
                string currCode = "";

                for(int i = 0; i < total; i++)
                {
                    currCode = (currCode + "." + userCode.Split('.')[i]).Trim('.');
                    string currDep = model.PhonesDruzhbas.Count(x => x.Code == currCode) > 0
                        ?model.PhonesDruzhbas.First(x => x.Code == currCode).Dep:"";
                    if(model.CompanyObjects.Count(x=>x.ObjName == currDep && x.Filial == filia) > 0)
                    {
                        return currDep;
                    }
                }
                return "";
            }
        }

        /// <summary>
        /// Update CompanyObject name in Database
        /// </summary>
        private void UpdateObjects(string FileName)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach (var o in model.PhonesDruzhbas)
                {
                    o.ObjectName = GetCompanyObject(o.Code, FileName);
                }
                model.SubmitChanges();
            }
        }

        /// <summary>
        /// Save row in database
        /// </summary>
        /// <param name="rowNum"></param>
        /// <param name="code"></param>
        private void SaveEntry(Models.LevelRange entry, string FileName)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                Models.PhonesDruzhba phone = new Models.PhonesDruzhba();
                phone.Code = entry.code;
                phone.FromFile = FileName;
                if (entry.entry.isDep)
                {
                    phone.Dep = entry.entry.DepName;                                       
                }else
                {
                    phone.PIB = entry.entry.FIO;
                    phone.Birthday = entry.entry.Birthday;
                    phone.Post = entry.entry.post;
                    phone.ObjectName = entry.ObjectName;
                    phone.Phone = entry.entry.phone;
                    phone.Email = entry.entry.email;
                    phone.Mobile = entry.entry.mobile;                    
                }
                model.PhonesDruzhbas.InsertOnSubmit(phone);
                model.SubmitChanges();                               
            }
        }

        /// <summary>
        /// Return entity from xlsx file by rowNum
        /// </summary>
        /// <param name="rowNum"></param>
        /// <returns></returns>
        private Models.AuthUser GetEntry(int rowNum, string FileName, string Folder)
        {
            Models.AuthUser user = new Models.AuthUser();
            string FileShareResources = System.Configuration.ConfigurationManager.AppSettings["FileShareResources"].ToString();
            string PhonesDruzhba = System.IO.Path.Combine(FileShareResources, "Phones", Folder, FileName);
            int f = 0;
            using (var sr = System.IO.File.OpenRead(PhonesDruzhba))
            {
                NPOI.XSSF.UserModel.XSSFWorkbook wb = new NPOI.XSSF.UserModel.XSSFWorkbook(sr);
                user.isDep = 
                    wb.GetSheet("TDSheet").GetRow(rowNum).Cells[1] == null 
                    || wb.GetSheet("TDSheet").GetRow(rowNum).Cells[1].ToString().Trim() == ""
                    || int.TryParse(wb.GetSheet("TDSheet").GetRow(rowNum).Cells[1].ToString(), out f);

                if(user.isDep)
                {
                    user.DepName = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[0].ToString();
                }
                else
                {
                    user.FIO = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[1].ToString();
                    user.post = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[0].ToString();
                    user.Birthday = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[4].ToString();
                    user.email = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[8].ToString();
                    user.phone = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[5].ToString();
                    user.mobile = wb.GetSheet("TDSheet").GetRow(rowNum).Cells[7].ToString();
                }
            }
            return user;
        }

    }
}
