// Decompiled with JetBrains decompiler
// Type: KeyAuth.encryption
// Assembly: Vision Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 285CD64B-215B-40F4-A9FB-AB9533109878
// Assembly location: C:\Users\woof\Downloads\Vision Client.exe

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace KeyAuth
{
  public static class encryption
  {
    public static string byte_arr_to_str([In] byte[] obj0)
    {
      StringBuilder stringBuilder = new StringBuilder(obj0.Length * 2);
      foreach (byte num in obj0)
        stringBuilder.AppendFormat(\u003CModule\u003E.c(checked (12460 * 2), Type.EmptyTypes.Length + 17341, sizeof (ushort) + 185), (object) num);
      return stringBuilder.ToString();
    }

    public static byte[] str_to_byte_arr([In] string obj0)
    {
      int length;
      int startIndex;
      try
      {
        length = obj0.Length;
        byte[] byteArr = new byte[length / 2];
        for (startIndex = 0; startIndex < length; startIndex += 2)
        {
          byte[] numArray = byteArr;
          int index = startIndex / 2;
          \u003CModule\u003E.h = -1411494653;
          int num = (int) Convert.ToByte(obj0.Substring(startIndex, 2), 16);
          numArray[index] = (byte) num;
        }
        return byteArr;
      }
      catch
      {
        Console.WriteLine(\u003CModule\u003E.c(length << 24 >>> 21 != 1371 || (-(length * -30220) ^ 2579) != 0 ? ((188 * startIndex + 844 + startIndex * 68 & 90) == 72 ? ((146 * startIndex + startIndex * 878 ^ 9004) == 0 ? Type.EmptyTypes.Length - 2003008625 : (((int) ((uint) startIndex / 886125U) | -8193) != -8193 ? (((int) ((uint) length % 5515U) * 816447488 | -7801) != -7801 ? 2012423940 - 563736689 : sizeof (int) + 1820346405) : Type.EmptyTypes.Length + 71434)) : (((int) ((uint) startIndex % 1072250382U) | -1073741825) != -1073741825 ? checked (-1506459910 - 76275919) : 1135421659)) : (((-1 | length) - 4383 & -987355528) == 0 ? Type.EmptyTypes.Length + 1640232194 : checked (-70064729 - 699207318)), Type.EmptyTypes.Length + 80376, Type.EmptyTypes.Length + 83));
        Thread.Sleep(3500);
        Environment.Exit(0);
        return (byte[]) null;
      }
    }

    public static string encrypt_string([In] string obj0, [In] byte[] obj1, [In] byte[] obj2)
    {
      Aes aes = Aes.Create();
      aes.Mode = CipherMode.CBC;
      aes.Key = obj1;
      aes.IV = obj2;
      string str;
      using (MemoryStream memoryStream = new MemoryStream())
      {
        ICryptoTransform encryptor = aes.CreateEncryptor();
        try
        {
          using (CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, encryptor, CryptoStreamMode.Write))
          {
            byte[] bytes = Encoding.Default.GetBytes(obj0);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            str = encryption.byte_arr_to_str(memoryStream.ToArray());
          }
        }
        finally
        {
          if (encryptor != null)
          {
            encryptor.Dispose();
            \u003CModule\u003E.j = -1529522494;
          }
        }
      }
      \u003CModule\u003E.p = -1051365525;
      return str;
    }

    public static string decrypt_string([In] string obj0, [In] byte[] obj1, [In] byte[] obj2)
    {
      Aes aes = Aes.Create();
      aes.Mode = CipherMode.CBC;
      \u003CModule\u003E.e = (object) \u003CModule\u003E.c(Type.EmptyTypes.Length + 24311, 2053355866 ^ 2053373438, checked (1896630579 - 1896630410));
      \u003CModule\u003E.d = (object) 1386028750;
      aes.Key = obj1;
      aes.IV = obj2;
      using (MemoryStream memoryStream1 = new MemoryStream())
      {
        using (ICryptoTransform decryptor = aes.CreateDecryptor())
        {
          MemoryStream memoryStream2 = memoryStream1;
          ICryptoTransform transform = decryptor;
          \u003CModule\u003E.j = 1657774894;
          using (CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream2, transform, CryptoStreamMode.Write))
          {
            byte[] byteArr = encryption.str_to_byte_arr(obj0);
            cryptoStream.Write(byteArr, 0, byteArr.Length);
            cryptoStream.FlushFinalBlock();
            byte[] array = memoryStream1.ToArray();
            return Encoding.Default.GetString(array, 0, array.Length);
          }
        }
      }
    }

    public static string iv_key()
    {
      Guid guid = Guid.NewGuid();
      string str1 = guid.ToString();
      guid = Guid.NewGuid();
      string str2 = guid.ToString();
      int num1 = sizeof (double) + 16387;
      int num2 = Type.EmptyTypes.Length + 25019;
      int h = \u003CModule\u003E.h;
      int num3 = (8 * h << 1) - 78472 == 396 * h + 116 * h - 4110 ? ((2 & h & -8181) == (h - -(h * 3 + h) & 2) ? ((-157257630 | h * 805306368) != -157257630 ? 1351012724 : Type.EmptyTypes.Length + 1842070588) : Type.EmptyTypes.Length + 1242769323) : 17;
      string str3 = \u003CModule\u003E.c(num1, num2, num3);
      int length = str2.IndexOf(str3, StringComparison.Ordinal);
      return str1.Substring(0, length);
    }

    public static string sha256([In] string obj0) => encryption.byte_arr_to_str(new SHA256Managed().ComputeHash(Encoding.Default.GetBytes(obj0)));

    public static string encrypt([In] string obj0, [In] string obj1, [In] string obj2)
    {
      byte[] bytes1 = Encoding.Default.GetBytes(encryption.sha256(obj1).Substring(0, 32));
      byte[] bytes2 = Encoding.Default.GetBytes(encryption.sha256(obj2).Substring(0, 16));
      return encryption.encrypt_string(obj0, bytes1, bytes2);
    }

    public static string decrypt([In] string obj0, [In] string obj1, [In] string obj2)
    {
      byte[] bytes1 = Encoding.Default.GetBytes(encryption.sha256(obj1).Substring(0, 32));
      byte[] bytes2 = Encoding.Default.GetBytes(encryption.sha256(obj2).Substring(0, 16));
      return encryption.decrypt_string(obj0, bytes1, bytes2);
    }
  }
}
