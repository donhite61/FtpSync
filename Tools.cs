﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace FTPSync
{
    public class Tools
    {
        public static List<string> ReadFileToList(string filePath)
        {
            var aReadFile = File.ReadAllLines(filePath);
            var lineList = new List<string>(aReadFile);
            return lineList;
        }

        internal static void ReadIniFile()
            {
                string serverIp = "";
                Loc.DatDir = "";
                Ftp.DatDir = "";
                Ftp.ServerPath = "";
                Ftp.UserName = "";
                
                Ftp.PassWord = "";
                string section = "";

                string scriptpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if(!File.Exists(scriptpath))
                    scriptpath = "C:\\DonsScripts\\H O T S";

                string iniPath = scriptpath + "\\Hite Order Tracking Config.ini";

                var lineList = ReadFileToList(iniPath);

                foreach (string line in lineList)
                {
                    int endBrack = line.IndexOf("]");
                    if (endBrack != -1)
                        section = line.Substring(1, endBrack - 1);

                    if (section == "General")
                    {
                        var split = line.Split('=');
                        if (split[0] == "PathWorkingData")
                            Loc.DatDir = split[1];
                    }

                    if (section == "WebFTP")
                    {
                        var split = line.Split('=');
                        if (split[0] == "FTPServerIp")
                            serverIp = split[1];
                        if (split[0] == "FTPUserName")
                            Ftp.UserName = split[1];
                        if (split[0] == "FTPPassWord")
                            Ftp.PassWord = split[1];
                        if (split[0] == "FTPDatDir")
                            Ftp.DatDir = split[1];
                    }
                }
                Ftp.ServerPath = "ftp://" + serverIp + "/" + Ftp.DatDir + "/";
                if (Ftp.DatDir != "" &&
                    Ftp.ServerPath != "" &&
                    Ftp.UserName != "" &&
                    Ftp.PassWord != "")
                           Ftp.FtpReady = true;

            }
    }
}

