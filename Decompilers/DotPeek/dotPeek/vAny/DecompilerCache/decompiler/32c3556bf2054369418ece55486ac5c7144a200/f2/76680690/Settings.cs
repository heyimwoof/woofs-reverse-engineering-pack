// Decompiled with JetBrains decompiler
// Type: Vision_Client.Properties.Settings
// Assembly: Vision Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 32C3556B-F205-4369-418E-CE55486AC5C7
// Assembly location: C:\Users\woof\Desktop\Vision Limiter new - Copy (2)\Vision Client\bin\x64\Debug\Vision Client.exe

using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vision_Client.Properties
{
  [CompilerGenerated]
  [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.4.0.0")]
  internal sealed class Settings : ApplicationSettingsBase
  {
    private static Settings \u0002 = (Settings) SettingsBase.Synchronized((SettingsBase) new Settings());

    public static Settings \u0002()
    {
      // ISSUE: reference to a compiler-generated field
      // ISSUE: variable of a compiler-generated type
      Settings settings = Settings.\u0002;
      return settings;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string FileString
    {
      get => (string) this[\u0006\u2006.\u0002(-380313767)];
      set => this[\u0006\u2006.\u0002(-380313767)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string Username
    {
      get => (string) this[\u0006\u2006.\u0002(-380313782)];
      set => this[\u0006\u2006.\u0002(-380313782)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string Password
    {
      get => (string) this[\u0006\u2006.\u0002(-380313735)];
      set => this[\u0006\u2006.\u0002(-380313735)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("False")]
    public bool register
    {
      get => (bool) this[\u0006\u2006.\u0002(-380319045)];
      set => this[\u0006\u2006.\u0002(-380319045)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string User
    {
      get => (string) this[\u0006\u2006.\u0002(-380313752)];
      set => this[\u0006\u2006.\u0002(-380313752)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string Pass
    {
      get => (string) this[\u0006\u2006.\u0002(-380313965)];
      set => this[\u0006\u2006.\u0002(-380313965)] = (object) value;
    }
  }
}
