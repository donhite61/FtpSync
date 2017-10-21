using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace FTPSync
{
    public class Loc
    {
        public static string DatDir { get; set; }
        public static string AchDir { get; set; }
        public static List<Dat> DatList { get; set; }

        internal static void LoadLocalDats()
        {
            Loc.DatList = new List<Dat>();
            string[] array2 = Directory.GetFiles(DatDir);
            foreach (string file in array2)
            {
                var lineList = Tools.ReadFileToList(file);
                var dat = new Dat();
                dat = ReadInfoFromFile(lineList, file);
                if (dat == null)
                    continue;

                Loc.DatList.Add(dat);
            }
        }

        internal static bool SortLocalDats()
        {
            if (Ftp.DatsList.Count < 1 && Loc.DatList.Count < 1)
                return false;

            Ftp.datsUpload = new List<Dat>();
            Ftp.datsDownload = new List<Dat>();

            if (Loc.DatList.Count > 0)
            {
                foreach (var dat in Loc.DatList)
                {
                    Dat matchingFtpDat = null;
                    if (Ftp.DatsList.TryGetValue(dat.OrderNum, out matchingFtpDat))
                    {
                        int timediff = string.Compare(matchingFtpDat.FtpNameTime, dat.FileModDate);
                        if (timediff < 0)
                        {
                            dat.FtpNameTime = matchingFtpDat.FtpNameTime;
                            Ftp.datsUpload.Add(dat);
                        }
                        else if (timediff > 0)
                        {
                            dat.FtpNameTime = matchingFtpDat.FtpNameTime;
                            Ftp.datsDownload.Add(dat);
                        }
                        else
                        {
                            if (OrderOlderThan30Days(dat))
                            {
                                dat.FtpNameTime = matchingFtpDat.FtpNameTime;
                                MoveLocDatToArchive(dat);
                                MoveFtpDatToArchive(dat);
                            }

                            Ftp.DatsList.Remove(dat.OrderNum);
                            continue;
                        }
                    }
                    else // if not on ftp
                    {
                        if (OrderOlderThan30Days(dat))
                            MoveLocDatToArchive(dat);
                        else
                            Ftp.datsUpload.Add(dat); // if not found on ftp
                    }
                }
            }
            return true;
        }

        private static void MoveFtpDatToArchive(Dat dat)
        {
            var curDatPath = Ftp.ServerPath + "/" + dat.FtpNameTime + "_" + dat.OrderNum + ".dat";
            var achDatPath = "/" + Ftp.AchDir + "/" + dat.FtpNameTime + "_" + dat.OrderNum + ".dat";
            Ftp.RenameFile(curDatPath, achDatPath);
        }

        private static void MoveLocDatToArchive(Dat dat)
        {
            var curDatPath = dat.locFileName;
            var achDatPath = Loc.AchDir + "\\" + dat.OrderNum + ".dat";
            File.Move(curDatPath, achDatPath);
        }

        private static bool OrderOlderThan30Days(Dat dat)
        {
            string fDate = dat.FileModDate.Substring(0, 4) + "-" + dat.FileModDate.Substring(4, 2) + "-" + dat.FileModDate.Substring(6, 2);
            DateTime fileDate = DateTime.ParseExact(fDate, "yyyy-MM-dd", null);
            DateTime curDate = DateTime.Now;
            var diff = (curDate - fileDate).TotalDays;
            if (diff > 30)
                return true;

            return false;
        }

        public static Dat AddDatSize(Dat dat) // if file is to be uploaded
        {
            long actsize = new System.IO.FileInfo(dat.locFileName).Length;
            if(dat.RecSize != actsize)
            {
                if (dat.RecSize != -1)
                {
                    var lineList = Tools.ReadFileToList(dat.locFileName);
                    lineList.RemoveAt(lineList.Count - 1);
                    lineList.RemoveAt(lineList.Count - 1);
                    actsize -= 38;

                    File.WriteAllLines(dat.locFileName, lineList);
                }

                using (StreamWriter w = File.AppendText(dat.locFileName))
                {
                    actsize += 38;
                    w.WriteLine("[**** File Size ****]");
                    w.WriteLine("FileSize=" + actsize);
                }
            }
            
            return dat;
        }

        private static Dat ReadInfoFromFile(List<string> lineList, string file)
        {
            var dat = new Dat();
            bool fileSizeFound = false;
            foreach (var line in lineList)
            {
                var splitKey = line.Split('=');
                if (splitKey.Length < 2)
                    continue;
                switch (splitKey[0])
                {
                    case "Hite ID":
                        dat.OrderNum = splitKey[1];
                        break;
                    case "ModifiedTime":
                        dat.FileModDate = splitKey[1];
                        break;
                    case "FileSize":
                        fileSizeFound = true;
                        dat.RecSize = Convert.ToInt64(splitKey[1]);
                        break;
                }
            }

            if (dat.OrderNum == null || dat.FileModDate == null)
                return null;

            dat.locFileName = file;
            if (fileSizeFound == false)
                dat.RecSize = -1;

            return dat;
        }
    }
}

