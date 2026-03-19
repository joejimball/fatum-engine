using System;

namespace FatumCommon.Helps
{
    public static class RandomHelps
    {
        [ThreadStatic]
        private static Random rndLocal;

        static RandomHelps()
        {

        }

        public static Random RndLocal
        {
            get
            {
                if (rndLocal == null)
                {
                    rndLocal = new Random(Guid.NewGuid().GetHashCode());
                }
                return rndLocal;
            }
        }

        public static void RellenarAleatorio(short[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (short)RndLocal.Next(short.MaxValue);
            }
        }

        public static void RellenarAleatorio(ushort[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (ushort)RndLocal.Next(ushort.MaxValue);
            }
        }
    }
}
