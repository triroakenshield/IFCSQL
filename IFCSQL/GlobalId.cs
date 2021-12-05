using System;
using System.Text;
// ReSharper disable StringLiteralTypo
// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

//https://github.com/buildingSMART/IfcDoc/blob/master/IfcKit/utilities/BuildingSmart.Utilities.Conversion/GlobalIdConverter.cs

public static class GlobalId
{
    private static string cConversionTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";

    public static Guid Parse(string format64)
    {
        int i, j, m;
        var num = new uint[6];

        if (format64 == null) return Guid.Empty;

        if (format64.Contains("-")) return new Guid(format64);

        format64 = format64.Trim('\'');

        if (format64.Length != 22) throw new ArgumentException("Invalid Global ID Length: " + format64);

        j = 0;
        m = 2;

        for (i = 0; i < 6; i++)
        {
            var temp = format64.Substring(j, m);
            j += m;
            m = 4;
            num[i] = cv_from_64(temp);
        }

        var data1 = num[0] * 16777216 + num[1];                 // 16-13. bytes
        var data2 = (ushort)(num[2] / 256);                              // 12-11. bytes
        var data3 = (ushort)((num[2] % 256) * 256 + num[3] / 65536);     // 10-09. bytes

        var data40 = (byte)((num[3] / 256) % 256);                   //    08. byte
        var data41 = (byte)(num[3] % 256);                           //    07. byte
        var data42 = (byte)(num[4] / 65536);                         //    06. byte
        var data43 = (byte)((num[4] / 256) % 256);                   //    05. byte
        var data44 = (byte)(num[4] % 256);                           //    04. byte
        var data45 = (byte)(num[5] / 65536);                         //    03. byte
        var data46 = (byte)((num[5] / 256) % 256);                   //    02. byte
        var data47 = (byte)(num[5] % 256);                           //    01. byte

        return new Guid(data1, data2, data3, data40, data41, data42, data43, data44, data45, data46, data47);
    }

    public static string Format(Guid pGuid)
    {
        var num = new uint[6];
        int i, n;

        var comp = pGuid.ToByteArray();
        var data1 = BitConverter.ToUInt32(comp, 0);
        var data2 = BitConverter.ToUInt16(comp, 4);
        var data3 = BitConverter.ToUInt16(comp, 6);
        var data40 = comp[8];
        var data41 = comp[9];
        var data42 = comp[10];
        var data43 = comp[11];
        var data44 = comp[12];
        var data45 = comp[13];
        var data46 = comp[14];
        var data47 = comp[15];

        //
        // Creation of six 32 Bit integers from the components of the GUID structure
        //
        num[0] = data1 / 16777216;                                      //    16. byte  (pGuid->Data1 / 16777216) is the same as (pGuid->Data1 >> 24)
        num[1] = data1 % 16777216;                                      // 15-13. bytes (pGuid->Data1 % 16777216) is the same as (pGuid->Data1 & 0xFFFFFF)
        num[2] = (uint)(data2 * 256 + data3 / 256);                     // 12-10. bytes
        num[3] = (uint)((data3 % 256) * 65536 + data40 * 256 + data41); // 09-07. bytes
        num[4] = (uint)(data42 * 65536 + data43 * 256 + data44);       // 06-04. bytes
        num[5] = (uint)(data45 * 65536 + data46 * 256 + data47);       // 03-01. bytes

        var buf = new StringBuilder();

        //
        // Conversion of the numbers into a system using a base of 64
        //
        n = 2;
        for (i = 0; i < 6; i++)
        {
            var temp = cv_to_64(num[i], n);
            buf.Append(temp);
            n = 4;
        }

        return buf.ToString();
    }

    //
    // Conversion of an integer into a number with base 64
    // using the coside table cConveronTable
    //
    private static string cv_to_64(uint number, int nDigits)
    {
        int iDigit;
        var result = new char[nDigits];

        var act = number;

        for (iDigit = 0; iDigit < nDigits; iDigit++)
        {
            result[nDigits - iDigit - 1] = cConversionTable[(int)(act % 64)];
            act /= 64;
        }

        if (act != 0) throw new ArgumentException("Number out of range");

        return new string(result);
    }

    //
    // The reverse function to calculate the number from the code
    //
    private static uint cv_from_64(string str)
    {
        int len, i, j, index;

        len = str.Length;
        if (len > 4) throw new ArgumentException("Invalid Global ID Format");

        uint pRes = 0;

        for (i = 0; i < len; i++)
        {
            index = -1;
            for (j = 0; j < 64; j++)
            {
                if (cConversionTable[j] == str[i])
                {
                    index = j;
                    break;
                }
            }

            if (index == -1) throw new ArgumentException("Invalid Global ID Format");

            pRes = (uint)(pRes * 64 + index);
        }

        return pRes;
    }
    
    // guid variant types
    private enum GuidVariant
    {
        ReservedNCS = 0x00,
        Standard = 0x02,
        ReservedMicrosoft = 0x06,
        ReservedFuture = 0x07
    }

    // guid version types
    private enum GuidVersion
    {
        TimeBased = 0x01,
        Reserved = 0x02,
        NameBased = 0x03,
        Random = 0x04
    }

    // constants that are used in the class
    private class Const
    {
        // number of bytes in guid
        public const int ByteArraySize = 16;

        // multiplex variant info
        public const int VariantByte = 8;
        public const int VariantByteMask = 0x3f;
        public const int VariantByteShift = 6;

        // multiplex version info
        public const int VersionByte = 7;
        public const int VersionByteMask = 0x0f;
        public const int VersionByteShift = 4;
    }
    
    public static Guid HashGuid(string uniqueString)
    {
        var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(Encoding.Default.GetBytes(uniqueString));
        return new Guid(hash);
    }
}