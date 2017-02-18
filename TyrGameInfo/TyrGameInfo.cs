using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patchwork.AutoPatching;

namespace TyrGameInfo
{
    [AppInfoFactory]
    internal class TyrAppInfoFactory : AppInfoFactory
    {
        public override AppInfo CreateInfo(DirectoryInfo folderInfo)
        {

            string exeFileName;
            string exeFileName64;
            string iconFile;
            string appVersion;

            bool multiarch = true;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                exeFileName = "Tyranny";
                exeFileName64 = ""; //no idea
                iconFile = "Tyranny.png";
                appVersion = null;
            }
            else
            {
                exeFileName = iconFile = "Tyranny32.exe";
                exeFileName64 = iconFile = "Tyranny64.exe";

                appVersion = FileVersionInfo.GetVersionInfo(Path.Combine(folderInfo.FullName, exeFileName)).FileVersion;
            }

            var fileInfos = folderInfo.GetFiles(exeFileName).ToList();

            if (fileInfos.Count == 0)
            {
                throw new FileNotFoundException($"Tyranny's executable file '{exeFileName}' was not found in this directory.", exeFileName);
            }

            if (multiarch)
            {
                fileInfos.AddRange(folderInfo.GetFiles(exeFileName64).ToList());

                if (fileInfos.Count < 2)
                {
                    throw new FileNotFoundException($"Tyranny's executable file '{exeFileName64}' was not found in this directory.", exeFileName64);
                }
            }

            return new AppInfo()
            {
                BaseDirectory = folderInfo,
                Executable = fileInfos[0],
                Executable64 = (fileInfos.Count == 2) ? fileInfos[1] : null,
                AppVersion = appVersion,
                AppName = "Tyranny",
                MultiArch = multiarch,                
                IconLocation = new FileInfo(Path.Combine(folderInfo.FullName, iconFile)),
                IgnorePEVerifyErrors = new[] {
			        //Expected an ObjRef on the stack.(Error: 0x8013185E). 
			        //-you can ignore the following. They are present in the original DLL. I'm not sure if they are actually errors.
			        0x8013185EL,
			        //The 'this' parameter to the call must be the calling method's 'this' parameter.(Error: 0x801318E1)
			        //-this isn't really an issue. PEV is just confused.
			        0x801318E1,
			        //Call to .ctor only allowed to initialize this pointer from within a .ctor. Try newobj.(Error: 0x801318BF)
			        //-this is a *verificaiton* issue is caused by copying the code from an existing constructor to a non-constructor method 
			        //-it contains a call to .ctor(), which is illegal from a non-constructor method.
			        //-There will be an option to fix this at some point, but it's not really an error.
			        0x801318BF,
                }
            };
        }
    }
}
