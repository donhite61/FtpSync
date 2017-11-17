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
        public static string RptDir { get; set; }
        public static List<Dat> DatList { get; set; }

        internal static void LoadLocalDats()
        {
            Tools.Log("Start LoadLocalDats");
            Loc.DatList = new List<Dat>();
            string[] array2 = Directory.GetFiles(DatDir,"*.dat");
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
            Tools.Log("Start SortLocalDats");
            if (Ftp.DatsList.Count < 1 && Loc.DatList.Count < 1)
                return false;

            Ftp.DatsUpload = new List<Dat>();
            Ftp.DatsDownload = new List<Dat>();

            if (Loc.DatList.Count > 0)
            {
                foreach (var dat in Loc.DatList)
                {
                    if (Ftp.DatsList.TryGetValue(dat.OrderNum, out Dat matchingFtpDat))
                    {
                        dat.FtpNameTime = matchingFtpDat.FtpNameTime; // get matching ftp file name
                        int timediff = string.Compare(matchingFtpDat.FtpNameTime, dat.FileModDate);
                        if (timediff < 0) // local dat newer
                        {
                            Ftp.DatsUpload.Add(dat); // add dat to upload list
                            Tools.Log(dat.OrderNum + " was added to uploadList");
                        }

                        else if (timediff > 0) //ftp dat newer
                        {
                            Ftp.DatsDownload.Add(dat); // add dat to download list
                            Tools.Log(dat.FtpNameTime + "_" + dat.OrderNum + ".dat" + " was added to downloadList");
                        }
                        else // if file has not changed
                        {
                            if (OrderOlderThan30Days(dat)) // check for move to archive
                            {
                                MoveLocDatToArchive(dat);
                                MoveFtpDatToArchive(dat);
                                Tools.Log(dat.OrderNum + " was old and moved to archive");
                            }
                        }
                        Ftp.DatsList.Remove(dat.OrderNum); // if ftp dat found local, remove from ftp dat list
                        Tools.Log(dat.OrderNum + " was removed from ftpDatList list");
                    }
                    else // if not on ftp
                    {
                        if (OrderOlderThan30Days(dat))
                            MoveLocDatToArchive(dat);
                        else
                        {
                            Ftp.DatsUpload.Add(dat); // if not found on ftp add to upload
                            Tools.Log(dat.OrderNum + " was added to uploadList");
                        }
                    }
                }
            }
            return true;
        }

        private static void MoveFtpDatToArchive(Dat dat)
        {
            Tools.Log("Start MoveFtpDatToArchive " + dat.FtpNameTime + "_" + dat.OrderNum + ".dat");

            var curDatPath = Ftp.ServerPath + "/" + dat.FtpNameTime + "_" + dat.OrderNum + ".dat";
            var achDatPath = "/" + Ftp.AchDir + "/" + dat.FtpNameTime + "_" + dat.OrderNum + ".dat";
            Ftp.RenameFile(curDatPath, achDatPath);
        }

        private static void MoveLocDatToArchive(Dat dat)
        {
            Tools.Log("Start MoveLocDatToArchive " + dat.OrderNum + ".dat");
            var curDatPath = dat.locFileName;
            var achDatPath = Loc.AchDir + "\\" + dat.OrderNum + ".dat";
            if (System.IO.File.Exists(achDatPath))
                System.IO.File.Delete(achDatPath);

            File.Move(curDatPath, achDatPath);
        }

        private static bool OrderOlderThan30Days(Dat dat)
        {
            string fDate = dat.FileModDate.Substring(0, 4) + "-" + dat.FileModDate.Substring(4, 2) + "-" + dat.FileModDate.Substring(6, 2);
            DateTime fileDate = DateTime.ParseExact(fDate, "yyyy-MM-dd", null);
            DateTime curDate = DateTime.Now;
            var diff = (curDate - fileDate).TotalDays;
            if (diff > 30)
            {
                Tools.Log(dat.OrderNum + " found over 30 days");
                return true;
            }
            return false;
        }

        public static Dat AddDatSize(Dat dat) // if file is to be uploaded
        {
            long actsize = new System.IO.FileInfo(dat.locFileName).Length;
            if(dat.RecSize != actsize)
            {
                Tools.Log("File size incorrect, fixing " + dat.OrderNum);
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

