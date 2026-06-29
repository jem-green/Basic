using System;
using System.Diagnostics;
using BasicLibrary;

namespace BasicTest
{
    public class UnitTest1
    {
        public void TestMethod1()
        {
            //int s = 1;
            //int[] d = new int[s+1];
            //uBasicLibrary.Array a = new uBasicLibrary.Array("a",s,d,"10");
            //int[] p = new int[s+1];
            //p[s] = 0;
            //a.Set(p, 1);
            //System.Diagnostics.Debug.WriteLine(a.Get(p));

            int s = 2;
            int[] d = new int[s + 1];
            d[1] = 2;
            d[2] = 2;
            BasicLibrary.Array a = new BasicLibrary.Array("a", s, d, "10");
            int[] p = new int[s + 1];
            p[1] = 1;
            p[2] = 1;
            a.Set(p, 1);
            System.Diagnostics.Debug.WriteLine(a.Get(p));







        }
    }
}
