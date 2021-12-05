﻿using System;
using System.Text;

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

        var Data1 = (uint)(num[0] * 16777216 + num[1]);                 // 16-13. bytes
        var Data2 = (ushort)(num[2] / 256);                              // 12-11. bytes
        var Data3 = (ushort)((num[2] % 256) * 256 + num[3] / 65536);     // 10-09. bytes

        var Data4_0 = (byte)((num[3] / 256) % 256);                   //    08. byte
        var Data4_1 = (byte)(num[3] % 256);                           //    07. byte
        var Data4_2 = (byte)(num[4] / 65536);                         //    06. byte
        var Data4_3 = (byte)((num[4] / 256) % 256);                   //    05. byte
        var Data4_4 = (byte)(num[4] % 256);                           //    04. byte
        var Data4_5 = (byte)(num[5] / 65536);                         //    03. byte
        var Data4_6 = (byte)((num[5] / 256) % 256);                   //    02. byte
        var Data4_7 = (byte)(num[5] % 256);                           //    01. byte

        return new Guid(Data1, Data2, Data3, Data4_0, Data4_1, Data4_2, Data4_3, Data4_4, Data4_5, Data4_6, Data4_7);
    }

    public static string Format(Guid pGuid)
    {
        var num = new uint[6];
        int i, n;

        var comp = pGuid.ToByteArray();
        var Data1 = BitConverter.ToUInt32(comp, 0);
        var Data2 = BitConverter.ToUInt16(comp, 4);
        var Data3 = BitConverter.ToUInt16(comp, 6);
        var Data4_0 = comp[8];
        var Data4_1 = comp[9];
        var Data4_2 = comp[10];
        var Data4_3 = comp[11];
        var Data4_4 = comp[12];
        var Data4_5 = comp[13];
        var Data4_6 = comp[14];
        var Data4_7 = comp[15];

        //
        // Creation of six 32 Bit integers from the components of the GUID structure
        //
        num[0] = (uint)(Data1 / 16777216);                                                 //    16. byte  (pGuid->Data1 / 16777216) is the same as (pGuid->Data1 >> 24)
        num[1] = (uint)(Data1 % 16777216);                                                 // 15-13. bytes (pGuid->Data1 % 16777216) is the same as (pGuid->Data1 & 0xFFFFFF)
        num[2] = (uint)(Data2 * 256 + Data3 / 256);                                 // 12-10. bytes
        num[3] = (uint)((Data3 % 256) * 65536 + Data4_0 * 256 + Data4_1);  // 09-07. bytes
        num[4] = (uint)(Data4_2 * 65536 + Data4_3 * 256 + Data4_4);       // 06-04. bytes
        num[5] = (uint)(Data4_5 * 65536 + Data4_6 * 256 + Data4_7);       // 03-01. bytes

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
        uint act;
        int iDigit;
        var result = new char[nDigits];

        act = number;

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
        System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(uniqueString));
        return new Guid(hash);
    }
}