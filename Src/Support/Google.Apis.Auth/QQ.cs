using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Google.Apis.Auth
{
    public static class QQ
    {
        public static void Test()
        {
            Debug.WriteLine(typeof(EncFSFileProvider.GoogleDrive.BaseClientServiceInitializer).AssemblyQualifiedName);
            Debug.WriteLine(typeof(EncFSFileProvider.GoogleDrive.BaseClientServiceInitializer2).AssemblyQualifiedName);
            Debug.WriteLine(typeof(Google.Apis.Services.BaseClientServiceInitializer).AssemblyQualifiedName);
            var KK = new EncFSFileProvider.GoogleDrive.BaseClientServiceInitializer();
            var KK2 = new EncFSFileProvider.GoogleDrive.BaseClientServiceInitializer2();
            var KK3 = new Google.Apis.Services.BaseClientServiceInitializer();



        }

        public static Google.Apis.Services.BaseClientServiceInitializer Test1()
        {
            //var ii = new Google.Apis.Services.BaseClientServiceInitializer();
            //return ii;
            return WW.Test1();
        }
    }
}
