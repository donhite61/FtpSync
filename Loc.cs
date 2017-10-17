using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace FTPSync
{
    public class Loc
    {
        public static string DatDir { get; set; }
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
                        Ftp.DatsList.Remove(dat.OrderNum);
                        continue;
                    }
                    Ftp.datsUpload.Add(dat); // if not found on ftp
                }
            }
            return true;
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

