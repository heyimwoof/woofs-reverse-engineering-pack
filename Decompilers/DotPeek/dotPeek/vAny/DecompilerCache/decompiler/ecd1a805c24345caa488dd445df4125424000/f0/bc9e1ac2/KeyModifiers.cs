// Decompiled with JetBrains decompiler
// Type: nlsetup.KeyModifiers
// Assembly: Netlimiter++, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: ECD1A805-C243-45CA-A488-DD445DF41254
// Assembly location: C:\Users\woof\Desktop\Netlimiter++\Netlimiter++\Netlimiter++.exe

using System;

namespace nlsetup
{
  [Flags]
  public enum KeyModifiers
  {
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
    NoRepeat = 16384, // 0x00004000
  }
}
