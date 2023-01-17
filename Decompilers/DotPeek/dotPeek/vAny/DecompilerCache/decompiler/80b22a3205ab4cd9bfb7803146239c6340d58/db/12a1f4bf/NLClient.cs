// Decompiled with JetBrains decompiler
// Type: NetLimiter.Service.NLClient
// Assembly: NetLimiter, Version=4.1.14.0, Culture=neutral, PublicKeyToken=null
// MVID: 80B22A32-05AB-4CD9-BFB7-803146239C63
// Assembly location: C:\Users\woof\Desktop\Netlimiter++\Netlimiter++\NetLimiter.dll

using CoreLib.Process;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NetLimiter.Service.Api;
using NetLimiter.Service.AppList;
using NetLimiter.Service.Notifications;
using NLInfoTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NetLimiter.Service
{
  [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
  public class NLClient : INLServiceCallback, IDisposable
  {
    private static readonly ILogger<NLClient> _logger = NetLimiter.Logging.LoggerFactory.CreateLogger<NLClient>();
    private object _ruleListLock = new object();
    private object _filterListLock = new object();
    private List<Filter> _filters;
    private volatile SynchronizationContext _synCtx;
    private List<Rule> _rules;
    private volatile INLSvcCnn _cnn;
    private volatile string _hostName;
    private volatile string _userName;
    private volatile NLLicense _license;
    private volatile string _localhostFootprint;
    private volatile bool _isLocalhost;
    private volatile string _addr;
    private volatile ushort _port;
    private string _svcPath;
    private string _clientId;
    private volatile bool _isBfeRunning;
    private volatile Exception _connectError;

    public bool IsBfeRunning => this._isBfeRunning;

    public int SettingsVersion { get; protected set; }

    public string ServiceVersion { get; protected set; }

    public int ConfirmedSettingsVersion { get; protected set; }

    public event EventHandler<FilterEventArgs> FilterAdded;

    public event EventHandler<FilterUpdateEventArgs> FilterUpdated;

    public event EventHandler<AppInfoEventArgs> AppUpdated;

    public event EventHandler<FilterEventArgs> FilterRemoved;

    public event EventHandler<RuleEventArgs> RuleAdded;

    public event EventHandler<RuleUpdateEventArgs> RuleUpdated;

    public event EventHandler<RuleEventArgs> RuleRemoved;

    public event EventHandler FirewallRequestChange;

    public event EventHandler NetworkChanged;

    public event EventHandler<AppInfoEventArgs> AppAdded;

    public event EventHandler<AppIdEventArgs> AppRemoved;

    public event EventHandler<OptionsChangeEventArgs> OptionsChanged;

    public event EventHandler<StatsToolsCompletedEventArgs> StatsToolsCompleted;

    public event EventHandler<StatsQueryUpdateEventArgs> StatsQueryUpdate;

    public event EventHandler<CnnEventLogUpdateEventArgs> CnnEventLogUpdate;

    public event EventHandler LicenseChanged;

    public event EventHandler StateChanged;

    public event EventHandler RuleOrderChanged;

    public event EventHandler IsBfeRunningChanged;

    public event EventHandler PrioritySettingsChanged;

    public event EventHandler NotificationsChanged;

    public string SvcAddress => this._cnn != null ? this._cnn.SvcAddress : (string) null;

    public NLServiceState State => this._cnn != null ? this._cnn.State : NLServiceState.Initial;

    public string Address => this._addr;

    public ushort Port => this._port;

    public string UserName => this._userName;

    public Version OSVersion { get; protected set; }

    public bool IsWin10OrGreater
    {
      get
      {
        Version osVersion = this.OSVersion;
        return (object) osVersion != null && osVersion.Major >= 10;
      }
    }

    public SynchronizationContext SynchronizationContext
    {
      get => this._synCtx;
      set => this._synCtx = value;
    }

    public Exception ConnectError
    {
      get => this._connectError;
      protected set => this._connectError = value;
    }

    public Filter GetInternetZone() => this.Filters.FirstOrDefault<Filter>((Func<Filter, bool>) (x => x.Id == "{2bc7c021-b058-45dc-b22f-73d8e10e3fef}"));

    public Filter GetLocalNetworkZone() => this.Filters.FirstOrDefault<Filter>((Func<Filter, bool>) (x => x.Id == "{cbe59154-31dc-4cfb-a12c-7fffb87ff400}"));

    public Filter GetAnyFilter() => this.Filters.FirstOrDefault<Filter>((Func<Filter, bool>) (x => x.Id == "{c053e57e-d554-49cb-ac70-e0ce95302faa}"));

    public NLClient() => this.InitInternal();

    private void InitInternal()
    {
      if (SynchronizationContext.Current != null)
        this._synCtx = SynchronizationContext.Current;
      else
        this._synCtx = new SynchronizationContext();
    }

    public void Connect(string hostName = null, ushort port = 0, string userName = null, SecureString password = null)
    {
      this._addr = hostName;
      this._port = port;
      this._userName = userName;
      this.Connect((INLSvcCnn) new NLWcfSvcCnn((INLServiceCallback) this, hostName, port, userName, password));
    }

    internal void Connect(INLSvcCnn cnn)
    {
      if (cnn == null)
        throw new ArgumentNullException();
      lock (this)
      {
        this._cnn = this._cnn == null ? cnn : throw new NLException("Connect already called");
        this._cnn.StateChanged += new EventHandler<NLServiceStateChangedEventArgs>(this.Cnn_StateChanged);
      }
      this.ConnectError = (Exception) null;
      try
      {
        this._cnn.Connect();
      }
      catch (Exception ex)
      {
        this.ConnectError = ex;
        throw ex;
      }
    }

    ~NLClient() => this.Close();

    private T Invoke<T>(Func<T> f)
    {
      try
      {
        return f();
      }
      catch (FaultException<NLFaultContract> ex)
      {
        throw this.OnFault(ex);
      }
    }

    private void Invoke(Action a)
    {
      try
      {
        a();
      }
      catch (FaultException<NLFaultContract> ex)
      {
        throw this.OnFault(ex);
      }
    }

    private Exception OnFault(FaultException<NLFaultContract> fault)
    {
      Exception exception = fault.Detail.ToException();
      NLClient._logger.LogError(exception.Message);
      return exception;
    }

    private void OnConnected(ServiceInfo svcInfo)
    {
      NLClient._logger.LogInformation("Connected to {0}: osver={1}", (object) svcInfo.HostName, (object) svcInfo.OSVersion);
      this._hostName = svcInfo.HostName;
      this._license = svcInfo.License;
      this._localhostFootprint = svcInfo.LocalhostFootprint;
      this._isBfeRunning = svcInfo.IsBfeRunning;
      this.SettingsVersion = svcInfo.SettingsVersion;
      this._svcPath = svcInfo.SvcInstPath;
      this._clientId = svcInfo.ClientId;
      this.ServiceVersion = svcInfo.ServiceVersion;
      Version version = new Version(4, 1, 1);
      Version result;
      if (Version.TryParse(this.ServiceVersion, out result))
      {
        if (!(result < version))
        {
          try
          {
            this.OSVersion = new Version(svcInfo.OSVersion);
          }
          catch (Exception ex)
          {
            NLClient._logger.LogError(ex, "Failed initialize OSVersion: {0}", (object) svcInfo.OSVersion);
          }
          try
          {
            using (RegistryKey registryKey1 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
              using (RegistryKey registryKey2 = registryKey1.OpenSubKey("Software\\Locktime Software\\NetLimiter\\4", false))
                this._isLocalhost = this._localhostFootprint == registryKey2.GetValue("LocalhostFootprint") as string;
            }
          }
          catch (Exception ex)
          {
            NLClient._logger.LogError(ex, "Failed to handle localhost footprint");
          }
          if (this._isLocalhost)
          {
            if (svcInfo.IsElevationRequired)
            {
              try
              {
                svcInfo.IsElevationRequired = this.CheckElevationRequired(svcInfo);
              }
              catch (Exception ex)
              {
                NLClient._logger.LogError(ex, "RequestAdminRights failed");
              }
            }
          }
          lock (this._filterListLock)
          {
            this._filters = new List<Filter>();
            int num = this._cnn.Svc.IsFwEnabled ? 1 : 0;
            List<Filter> filters = this._cnn.Svc.Filters;
            this._cnn.Svc.Filters.ForEach((Action<Filter>) (flt => this._filters.Add(flt)));
            this._filters.ForEach((Action<Filter>) (flt => this.InitFilter(flt)));
          }
          lock (this._ruleListLock)
          {
            this._rules = this._cnn.Svc.Rules;
            return;
          }
        }
      }
      throw new ServiceVersionTooLow(this.ServiceVersion);
    }

    private bool CheckElevationRequired(ServiceInfo svcInfo)
    {
      string str = Path.Combine(svcInfo.SvcInstPath, "NLSvcCliCnnCheck.exe");
      if (!System.IO.File.Exists(str))
      {
        NLClient._logger.LogWarning("NLSvcCliCnnCheck not found: path={path}", (object) str);
        return false;
      }
      System.Diagnostics.Process process = System.Diagnostics.Process.Start(str, svcInfo.ClientId);
      try
      {
        return this._cnn.Svc.CheckElevationRequired();
      }
      finally
      {
        process.Kill();
      }
    }

    public Filter AddFilter(Filter flt)
    {
      if (flt == null)
        throw new ArgumentNullException();
      Filter fltNew = (Filter) null;
      this.Invoke<Filter>((Func<Filter>) (() => fltNew = this._cnn.Svc.AddFilter(flt)));
      this.CopyUpdatedFilter(fltNew, flt);
      this._synCtx.Send((SendOrPostCallback) (s => this.OnFilterAdded(fltNew)), (object) null);
      return fltNew;
    }

    public void RemoveFilter(Filter flt)
    {
      if (flt == null)
        throw new ArgumentNullException();
      this.Invoke((Action) (() => this._cnn.Svc.RemoveFilter(flt.Id)));
      this._synCtx.Send((SendOrPostCallback) (s => this.OnFilterRemoved(flt.Id)), (object) null);
    }

    public FilterUpdateInfo UpdateFilter(Filter flt)
    {
      if (flt == null)
        throw new ArgumentNullException();
      FilterUpdateInfo info = (FilterUpdateInfo) null;
      this.Invoke<FilterUpdateInfo>((Func<FilterUpdateInfo>) (() => info = this._cnn.Svc.UpdateFilter(flt)));
      this.CopyUpdatedFilter(info.Filter, flt);
      if (info.HasChange)
        this._synCtx.Send((SendOrPostCallback) (s => this.OnFilterUpdated(info)), (object) null);
      return info;
    }

    private void CopyUpdatedFilter(Filter from, Filter to)
    {
      to._id = from.Id;
      to._internalId = from.InternalId;
      to._revision = from.Revision;
      to.CreatedTime = from.CreatedTime;
      to.UpdatedTime = from.UpdatedTime;
      to.Name = from.Name;
    }

    public StatsDbFileInfo StatsGetDBFileInfo() => this.Invoke<StatsDbFileInfo>((Func<StatsDbFileInfo>) (() => this._cnn.Svc.StatsGetDbFileInfo()));

    public StatsResultList StatsQuery(NetLimiter.Service.StatsQuery query) => this.Invoke<StatsResultList>((Func<StatsResultList>) (() => this._cnn.Svc.StatsQuery(query)));

    public StatsResultList StatsGetQueryResults(string queryName) => this.Invoke<StatsResultList>((Func<StatsResultList>) (() => this._cnn.Svc.StatsGetQueryResults(queryName)));

    public void StatsStopQuery(string queryName) => this.Invoke((Action) (() => this._cnn.Svc.StatsStopQuery(queryName)));

    public List<string> GetStatsApps() => this.Invoke<List<string>>((Func<List<string>>) (() => this._cnn.Svc.GetStatsApps()));

    public DateTime GetStatsFirstRecordTime() => this.Invoke<DateTime>((Func<DateTime>) (() => this._cnn.Svc.GetStatsFirstRecordTime()));

    public bool ChangeStatsDataLocation(string newLocation, bool moveFiles) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.ChangeStatsDataLocation(newLocation, moveFiles)));

    public bool StatsCompactData(DateTime start, DateTime end, uint interval) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsCompactData(start, end, interval)));

    public bool StatsDeleteData(List<string> appList) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsDeleteData(appList)));

    public bool StatsDeleteDatabase() => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsDeleteDatabase()));

    public bool StatsEnableCompression(bool enableCompression) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsEnableCompression(enableCompression)));

    public bool StatsMergeData(List<string> appList, string mergeApp) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsMergeData(appList, mergeApp)));

    public bool StatsCheckIntegrity(string alternateDbPath = null) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsIntegrityCheck(alternateDbPath)));

    public bool StatsRepairData(StatsBadRowsListInfo badRows) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.StatsRepairData(badRows)));

    public string InstallIp2LocDB(string path) => this.Invoke<string>((Func<string>) (() => this._cnn.Svc.InstallIp2LocDB(path)));

    public Ip2LocVersion GetIp2LocVersion() => this.Invoke<Ip2LocVersion>((Func<Ip2LocVersion>) (() => this._cnn.Svc.GetIp2LocVersion()));

    public Location GetIpLocation(IPAddress ip) => this.Invoke<Location>((Func<Location>) (() => this._cnn.Svc.GetIpLocation(ip)));

    public List<Filter> Filters
    {
      get
      {
        lock (this._filterListLock)
          return this._filters == null ? (List<Filter>) null : this._filters.ToList<Filter>();
      }
    }

    public List<Rule> Rules
    {
      get
      {
        lock (this._ruleListLock)
          return this._rules == null ? (List<Rule>) null : this._rules.ToList<Rule>();
      }
    }

    internal void InitFilter(Filter flt)
    {
    }

    void INLServiceCallback.OnFilterAdded(Filter filter) => this._synCtx.Post((SendOrPostCallback) (s => this.OnFilterAdded(filter)), (object) null);

    private void OnFilterAdded(Filter filter)
    {
      lock (this._filterListLock)
      {
        if (this._filters == null || this._filters.Exists((Predicate<Filter>) (x => x.Id == filter.Id)))
          return;
        this._filters.Add(filter);
        this.InitFilter(filter);
      }
      NLClient._logger.LogInformation("Filter added: id={0} name={1}", (object) filter?.Id, (object) filter?.Name);
      EventHandler<FilterEventArgs> filterAdded = this.FilterAdded;
      if (filterAdded == null)
        return;
      filterAdded((object) this, new FilterEventArgs(filter));
    }

    void INLServiceCallback.OnFilterRemoved(string fltId) => this._synCtx.Post((SendOrPostCallback) (s => this.OnFilterRemoved(fltId)), (object) null);

    private void OnFilterRemoved(string fltId)
    {
      NLClient._logger.LogTrace(">>");
      NLClient._logger.LogInformation("Filter removed: id={0}", (object) fltId);
      Filter filter = (Filter) null;
      lock (this._filterListLock)
      {
        if (this._filters == null)
          return;
        filter = this._filters.Find((Predicate<Filter>) (x => x.Id == fltId));
        if (filter == null)
          return;
        this._filters.Remove(filter);
        NLClient._logger.LogInformation("Filter found: name={0}", (object) filter.Name);
      }
      lock (this._ruleListLock)
      {
        foreach (NLVersioned<Rule> nlVersioned in this._rules.Where<Rule>((Func<Rule, bool>) (x => x.FilterId == fltId)).ToList<Rule>())
          ((INLServiceCallback) this).OnRuleRemoved(nlVersioned.Id);
      }
      EventHandler<FilterEventArgs> filterRemoved = this.FilterRemoved;
      if (filterRemoved != null)
        filterRemoved((object) this, new FilterEventArgs(filter));
      NLClient._logger.LogTrace("<<");
    }

    void INLServiceCallback.OnFilterUpdated(FilterUpdateInfo info) => this._synCtx.Post((SendOrPostCallback) (s => this.OnFilterUpdated(info)), (object) null);

    private void OnFilterUpdated(FilterUpdateInfo info)
    {
      NLClient._logger.LogTrace(">>");
      Filter oldFilter = (Filter) null;
      NLClient._logger.LogInformation("Filter updated: id={0}, name={1}", (object) info.Filter.Id, (object) info.Filter.Name);
      lock (this._filterListLock)
      {
        if (this._filters == null)
          return;
        oldFilter = this._filters.Find((Predicate<Filter>) (x => x.Id == info.Filter.Id));
        if (oldFilter == null)
        {
          NLClient._logger.LogDebug("Filter not found");
          this.OnFilterAdded(info.Filter);
          return;
        }
        if (oldFilter.Revision >= info.Filter.Revision)
        {
          NLClient._logger.LogDebug("Filter revision old: {0}/{1}", (object) oldFilter.Revision, (object) info.Filter.Revision);
          return;
        }
        this._filters[this._filters.IndexOf(oldFilter)] = info.Filter;
        this.InitFilter(info.Filter);
      }
      EventHandler<FilterUpdateEventArgs> filterUpdated = this.FilterUpdated;
      if (filterUpdated != null)
        filterUpdated((object) this, new FilterUpdateEventArgs(info, oldFilter));
      NLClient._logger.LogTrace("<<");
    }

    public void RemoveFilterDeep(Filter filter)
    {
      if (filter == null)
        throw new ArgumentNullException("zone");
      this._cnn.Svc.RemoveFilterDeep(filter.Id);
    }

    public NodeLoader CreateNodeLoader() => new NodeLoader(this._cnn.Svc);

    public List<AppInfo> Apps => this._cnn.Svc.Apps.Where<AppInfo>((Func<AppInfo, bool>) (x => x != null)).ToList<AppInfo>();

    public bool RemoveApp(AppId appId) => this.Invoke<bool>((Func<bool>) (() => this._cnn.Svc.RemoveApp(appId)));

    public void Close()
    {
      if (this._cnn == null)
        return;
      this._cnn.Close();
    }

    public void Dispose() => this.Close();

    public ushort TcpPort
    {
      get => this._cnn.Svc.TcpPort;
      set => this._cnn.Svc.TcpPort = value;
    }

    public string TcpAddress
    {
      get => this._cnn.Svc.TcpAddress;
      set => this._cnn.Svc.TcpAddress = value;
    }

    public bool UseTcpBinding
    {
      get => this._cnn.Svc.UseTcpBinding;
      set => this._cnn.Svc.UseTcpBinding = value;
    }

    public bool UseNpBinding
    {
      get => this._cnn.Svc.UseNpBinding;
      set => this._cnn.Svc.UseNpBinding = value;
    }

    public bool IsStatsEnabled
    {
      get => this._cnn.Svc.IsStatsEnabled;
      set => this._cnn.Svc.IsStatsEnabled = value;
    }

    public bool IsFwEnabled
    {
      get => this._cnn.Svc.IsFwEnabled;
      set => this._cnn.Svc.IsFwEnabled = value;
    }

    public bool RunClientOnFwRqeust
    {
      get => this._cnn.Svc.RunClientOnFwRequest;
      set => this._cnn.Svc.RunClientOnFwRequest = value;
    }

    public bool IsLimiterEnabled
    {
      get => this._cnn.Svc.IsLimiterEnabled;
      set => this._cnn.Svc.IsLimiterEnabled = value;
    }

    public bool IsPriorityEnabled
    {
      get => this._cnn.Svc.IsPriorityEnabled;
      set => this._cnn.Svc.IsPriorityEnabled = value;
    }

    public string Ip2LocFolderName => this._cnn.Svc.Ip2LocFolderName;

    public string SettingsFolderName => this._cnn.Svc.SettingsFolderName;

    public string SettingsFileName => this._cnn.Svc.SettingsFileName;

    public string StatsFolderName
    {
      get => this._cnn.Svc.StatsFolderName;
      set => this._cnn.Svc.StatsFolderName = value;
    }

    public int StatsUpdateTime
    {
      get => this._cnn.Svc.StatsUpdateTime;
      set => this._cnn.Svc.StatsUpdateTime = value;
    }

    public List<FirewallRequest> FirewallRequests => this._cnn.Svc.FirewallRequests;

    public void SetDefaultFwAction(FwAction fwaIn, FwAction fwaOut) => this._cnn.Svc.SetDefaultFwAction(fwaIn, fwaOut);

    public void GetDefaultFwAction(out FwAction fwaIn, out FwAction fwaOut) => this._cnn.Svc.GetDefaultFwAction(out fwaIn, out fwaOut);

    void INLServiceCallback.OnNotification(NLServiceNotificationType type)
    {
      switch (type)
      {
        case NLServiceNotificationType.RuleOrderChanged:
          this.OnRuleOrderChanged();
          break;
        case NLServiceNotificationType.NetworkChange:
          this.RunAsync((Action) (() =>
          {
            EventHandler networkChanged = this.NetworkChanged;
            if (networkChanged == null)
              return;
            networkChanged((object) this, EventArgs.Empty);
          }));
          break;
        case NLServiceNotificationType.FirewallRequest:
          this.RunAsync((Action) (() =>
          {
            EventHandler firewallRequestChange = this.FirewallRequestChange;
            if (firewallRequestChange == null)
              return;
            firewallRequestChange((object) this, EventArgs.Empty);
          }));
          break;
        case NLServiceNotificationType.BfeStateChange:
          this.RunAsync((Action) (() =>
          {
            this._isBfeRunning = this._cnn.Svc.IsBfeRunning;
            EventHandler bfeRunningChanged = this.IsBfeRunningChanged;
            if (bfeRunningChanged == null)
              return;
            bfeRunningChanged((object) this, EventArgs.Empty);
          }));
          break;
        case NLServiceNotificationType.PriSettingsChanged:
          this.RunAsync((Action) (() =>
          {
            EventHandler prioritySettingsChanged = this.PrioritySettingsChanged;
            if (prioritySettingsChanged == null)
              return;
            prioritySettingsChanged((object) this, EventArgs.Empty);
          }));
          break;
        case NLServiceNotificationType.NotificationsChanged:
          this.RunAsync((Action) (() =>
          {
            EventHandler notificationsChanged = this.NotificationsChanged;
            if (notificationsChanged == null)
              return;
            notificationsChanged((object) this, EventArgs.Empty);
          }));
          break;
      }
    }

    private void RunAsync(Action action, int repeatOnTimeout = 5) => this._synCtx.Post((SendOrPostCallback) (s => this.RunAsyncCallback(action, repeatOnTimeout)), (object) null);

    private void RunAsyncCallback(Action action, int repeatOnTimeout)
    {
      try
      {
        action();
      }
      catch (TimeoutException ex)
      {
        if (repeatOnTimeout > 0)
          this.RunAsync(action, repeatOnTimeout--);
        else
          NLClient._logger.LogError((Exception) ex, "RunAsync failed");
      }
      catch (Exception ex)
      {
        NLClient._logger.LogError(ex, "Failed to invoke the action");
      }
    }

    void INLServiceCallback.OnLicenseChange(NLLicense license) => this._synCtx.Post((SendOrPostCallback) (ctx =>
    {
      this._license = license;
      if (this.LicenseChanged == null)
        return;
      this.LicenseChanged((object) this, EventArgs.Empty);
    }), (object) null);

    public void ReplyFirewallRequest(FirewallRequest fwr, FwAction action) => this._cnn.Svc.ReplyFirewallRequest(fwr.Id, new FwRequestReply()
    {
      Action = action
    });

    public void ReplyFirewallRequest(FirewallRequest fwr, FwRequestReply reply) => this._cnn.Svc.ReplyFirewallRequest(fwr.Id, reply);

    public void ApproveNetwork(Network ntw)
    {
      this._cnn.Svc.ApproveNetwork(ntw.Id);
      ntw.IsApproved = true;
    }

    public List<Network> Networks => this._cnn.Svc.Networks;

    void INLServiceCallback.OnAppAdded(AppInfo info) => this._synCtx.Post((SendOrPostCallback) (x =>
    {
      if (info == null)
        return;
      EventHandler<AppInfoEventArgs> appAdded = this.AppAdded;
      if (appAdded == null)
        return;
      appAdded((object) this, new AppInfoEventArgs(info));
    }), (object) null);

    void INLServiceCallback.OnAppRemoved(AppId appId) => this._synCtx.Post((SendOrPostCallback) (x =>
    {
      EventHandler<AppIdEventArgs> appRemoved = this.AppRemoved;
      if (appRemoved == null)
        return;
      appRemoved((object) this, new AppIdEventArgs(appId));
    }), (object) null);

    void INLServiceCallback.OnAppUpdated(AppInfo info) => this._synCtx.Post((SendOrPostCallback) (x =>
    {
      EventHandler<AppInfoEventArgs> appUpdated = this.AppUpdated;
      if (appUpdated == null)
        return;
      appUpdated((object) this, new AppInfoEventArgs(info));
    }), (object) null);

    public void RemoveNetwork(string ntwId) => this.Invoke((Action) (() => this._cnn.Svc.RemoveNetwork(ntwId)));

    void INLServiceCallback.OnRuleAdded(Rule rule) => this._synCtx.Post((SendOrPostCallback) (s => this.OnRuleAdded(rule)), (object) null);

    private void OnRuleAdded(Rule rule)
    {
      NLClient._logger.LogTrace(">>");
      lock (this._ruleListLock)
      {
        if (this._rules == null || this._rules.Exists((Predicate<Rule>) (x => x.Id == rule.Id)))
          return;
        this._rules.Add(rule);
      }
      NLClient._logger.LogInformation("Ruled added: type={0}, id={1}", (object) rule.GetType().Name, (object) rule.Id);
      EventHandler<RuleEventArgs> ruleAdded = this.RuleAdded;
      if (ruleAdded != null)
        ruleAdded((object) this, new RuleEventArgs(rule));
      NLClient._logger.LogTrace("<<");
    }

    void INLServiceCallback.OnRuleRemoved(string ruleId) => this._synCtx.Post((SendOrPostCallback) (s => this.OnRuleRemoved(ruleId)), (object) null);

    private void OnRuleRemoved(string ruleId)
    {
      NLClient._logger.LogTrace(">>");
      Rule rule = (Rule) null;
      lock (this._ruleListLock)
      {
        if (this._rules == null)
          return;
        rule = this._rules.Find((Predicate<Rule>) (x => x.Id == ruleId));
        if (rule == null)
        {
          NLClient._logger.LogInformation("Rule not found: id={0}", (object) ruleId);
          return;
        }
        this._rules.Remove(rule);
        NLClient._logger.LogInformation("Rule removed: type={0}, id={1}", (object) rule.GetType().Name, (object) ruleId);
      }
      EventHandler<RuleEventArgs> ruleRemoved = this.RuleRemoved;
      if (ruleRemoved != null)
        ruleRemoved((object) this, new RuleEventArgs(rule));
      NLClient._logger.LogTrace("<<");
    }

    void INLServiceCallback.OnRuleUpdated(RuleUpdateInfo info) => this._synCtx.Post((SendOrPostCallback) (s => this.OnRuleUpdated(info)), (object) null);

    private void OnRuleUpdated(RuleUpdateInfo info)
    {
      NLClient._logger.LogTrace(">>");
      Rule oldRule = (Rule) null;
      lock (this._ruleListLock)
      {
        if (this._rules == null)
          return;
        oldRule = this._rules.Find((Predicate<Rule>) (x => x.Id == info.Rule.Id));
        if (oldRule != null)
          this._rules[this._rules.IndexOf(oldRule)] = info.Rule;
      }
      if (oldRule != null)
      {
        NLClient._logger.LogInformation("Rule updated: type={0}, id={1}", (object) oldRule.GetType().Name, (object) oldRule.Id);
        EventHandler<RuleUpdateEventArgs> ruleUpdated = this.RuleUpdated;
        if (ruleUpdated != null)
          ruleUpdated((object) this, new RuleUpdateEventArgs(info, oldRule));
      }
      NLClient._logger.LogTrace("<<");
    }

    public Rule AddRule(string filterId, Rule rule)
    {
      if (rule == null)
        throw new ArgumentNullException();
      Rule ruleNew = (Rule) null;
      this.Invoke<Rule>((Func<Rule>) (() => ruleNew = this._cnn.Svc.AddRule(filterId, rule)));
      this.CopyUpdatedRule(ruleNew, rule);
      this._synCtx.Send((SendOrPostCallback) (s => this.OnRuleAdded(ruleNew)), (object) null);
      return ruleNew;
    }

    public void RemoveRule(Rule rule)
    {
      if (rule == null)
        throw new ArgumentNullException();
      this.Invoke((Action) (() => this._cnn.Svc.RemoveRule(rule.Id)));
      this._synCtx.Send((SendOrPostCallback) (s => this.OnRuleRemoved(rule.Id)), (object) null);
    }

    public RuleUpdateInfo UpdateRule(Rule rule)
    {
      if (rule == null)
        throw new ArgumentNullException();
      RuleUpdateInfo info = (RuleUpdateInfo) null;
      this.Invoke<RuleUpdateInfo>((Func<RuleUpdateInfo>) (() => info = this._cnn.Svc.UpdateRule(rule)));
      this.CopyUpdatedRule(info.Rule, rule);
      if (info.HasChange)
        this._synCtx.Send((SendOrPostCallback) (s => this.OnRuleUpdated(info)), (object) null);
      return info;
    }

    private void CopyUpdatedRule(Rule from, Rule to)
    {
      to._id = from.Id;
      to._revision = from.Revision;
      to.InternalId = from.InternalId;
      to._internalFltId = from._internalFltId;
      to.CreatedTime = from.CreatedTime;
      to.UpdatedTime = from.UpdatedTime;
      to.IsEnabled = from.IsEnabled;
      to.IsActive = from.IsActive;
      to.FilterId = from.FilterId;
    }

    public string HostName => this._hostName;

    void INLServiceCallback.OnOptionsChange(OptionsChangeType changeType, object value) => this._synCtx.Post((SendOrPostCallback) (ctx =>
    {
      if (this.OptionsChanged == null)
        return;
      this.OptionsChanged((object) this, new OptionsChangeEventArgs(changeType, value));
    }), (object) null);

    public NLLicense License => this._license;

    public void ResetNodeTotals() => this.ResetNodeTotals(ulong.MaxValue);

    public void ResetNodeTotals(ulong nodeId) => this._cnn.Svc.ResetNodeTotals(nodeId);

    public ChartData GetChart(ulong nodeId, NLFlowDir? dir = null) => this._cnn.Svc.GetChart(nodeId, dir);

    public IEnumerable<AccessTableRow> GetAccessTable() => this._cnn.Svc.GetAccessTable();

    public void SetAccessTable(IEnumerable<AccessTableRow> table) => this._cnn.Svc.SetAccessTable(table);

    public string ResolveIdentity(string userOrGroupName) => this._cnn.Svc.ResolveIdentity(userOrGroupName);

    public bool IsLocalhost => this._isLocalhost;

    public void RestartCommunication()
    {
      try
      {
        this._cnn.Svc.RestartCommunication();
      }
      catch
      {
      }
    }

    private void Cnn_StateChanged(object sender, NLServiceStateChangedEventArgs e)
    {
      this.ConnectError = this._cnn.ConnectError;
      if (this.ConnectError != null)
        NLClient._logger.LogError(this.ConnectError, "ConnectError");
      if (this.State == NLServiceState.Connected)
        this.OnConnected(e.ServiceInfo);
      if (this.StateChanged != null)
        this._synCtx.Send((SendOrPostCallback) (state =>
        {
          try
          {
            EventHandler stateChanged = this.StateChanged;
            if (stateChanged == null)
              return;
            stateChanged((object) this, EventArgs.Empty);
          }
          catch (Exception ex)
          {
            this.Close();
            NLClient._logger.LogError(ex, "Failed to invoke StateChanged event");
          }
        }), (object) null);
      if (e.ServiceInfo == null || !e.ServiceInfo.HasPendingFwr)
        return;
      this._synCtx.Send((SendOrPostCallback) (ctx =>
      {
        try
        {
          EventHandler firewallRequestChange = this.FirewallRequestChange;
          if (firewallRequestChange == null)
            return;
          firewallRequestChange((object) this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
          this.Close();
          NLClient._logger.LogError(ex, "Failed to invoke FirewallRequestChange event");
        }
      }), (object) null);
    }

    public void SetRegistrationData(string regName, string regCode) => this.Invoke<NLLicense>((Func<NLLicense>) (() => this._license = this._cnn.Svc.SetRegistrationData(regName, regCode)));

    public void RemoveRegistrationData() => this.Invoke<NLLicense>((Func<NLLicense>) (() => this._license = this._cnn.Svc.RemoveRegistrationData()));

    void INLServiceCallback.OnStatsToolsCompleted(StatsToolsResult result) => this._synCtx.Post((SendOrPostCallback) (s =>
    {
      if (this.StatsToolsCompleted == null)
        return;
      this.StatsToolsCompleted((object) this, new StatsToolsCompletedEventArgs()
      {
        Result = result
      });
    }), (object) null);

    void INLServiceCallback.OnStatsQueryUpdate(StatsQueryUpdateEventArgs args) => this._synCtx.Post((SendOrPostCallback) (s =>
    {
      if (this.StatsQueryUpdate == null)
        return;
      this.StatsQueryUpdate((object) this, args);
    }), (object) null);

    void INLServiceCallback.OnCnnEventLogUpdate(CnnEventLogUpdateEventArgs args) => this._synCtx.Post((SendOrPostCallback) (s =>
    {
      if (this.CnnEventLogUpdate == null)
        return;
      this.CnnEventLogUpdate((object) this, args);
    }), (object) null);

    public Network UpdateNetwork(Network network) => this._cnn.Svc.UpdateNetwork(network);

    public List<UserInfo> GetUserList() => this._cnn.Svc.GetUserList();

    public bool IsAppInUse(AppId appId) => this._cnn.Svc.IsAppInUse(appId);

    public void KillCnns(ulong nodeId) => this._cnn.Svc.KillCnns(nodeId);

    public void KillCnnsByFilterId(string fltId) => this._cnn.Svc.KillCnnsByFilter(fltId);

    public void KillCnnsByQuota(string ruleId, long quotaPtr = 0) => this._cnn.Svc.KillCnnsByQuota(ruleId, quotaPtr);

    public void SetRuleTable(IEnumerable<Rule> rules) => this._cnn.Svc.SetRuleTable(rules.Select<Rule, string>((Func<Rule, string>) (x => x.Id)));

    private void OnRuleOrderChanged()
    {
      List<Rule> rules = this._cnn.Svc.Rules;
      lock (this._ruleListLock)
      {
        foreach (Rule rule1 in rules)
        {
          Rule newRule = rule1;
          Rule rule2 = this._rules.FirstOrDefault<Rule>((Func<Rule, bool>) (x => x.Id == newRule.Id));
          if (rule2 != null)
          {
            this._rules.Remove(rule2);
            this._rules.Add(rule2);
          }
          else
            this.OnRuleAdded(rule2);
        }
      }
      this._synCtx.Post((SendOrPostCallback) (s =>
      {
        EventHandler ruleOrderChanged = this.RuleOrderChanged;
        if (ruleOrderChanged == null)
          return;
        ruleOrderChanged((object) this, EventArgs.Empty);
      }), (object) null);
    }

    public void ResetQuota(QuotaRule rule, long quotaPtr = 0, bool stop = false) => this._cnn.Svc.ResetQuota(rule.Id, quotaPtr, stop);

    public IEnumerable<QuotaState> GetQuotaStates() => this._cnn.Svc.GetQuotaStates();

    public QuotaStateDetails GetQuotaStateDetails(string quotaRuleId, int ix) => this._cnn.Svc.GetQuotaStateDetails(quotaRuleId, ix);

    public void InitQuotaTotalFromStats(string quotaRuleId) => this._cnn.Svc.InitQuotaTotalFromStats(quotaRuleId);

    public PrioritySettings PrioritySettings
    {
      get => this._cnn.Svc.PrioritySettings;
      set => this._cnn.Svc.PrioritySettings = value;
    }

    public async Task<List<AppListSvcItem>> GetInstalledServicesAsync() => await Task.Run<List<AppListSvcItem>>((Func<List<AppListSvcItem>>) (() => this._cnn.Svc.GetInstalledServices()));

    public async Task<List<AppListStoreItem>> GetInstalledStoreAppsAsync() => await Task.Run<List<AppListStoreItem>>((Func<List<AppListStoreItem>>) (() => this._cnn.Svc.GetInstalledStoreApps()));

    public SidName GetSidName(Sid sid) => this._cnn.Svc.GetSidName(sid);

    public AppInfo GetAppInfo(AppId appId) => this._cnn.Svc.GetAppInfo(appId);

    public void UpdateAppInfo(AppId appId, AppInfo appInfo) => this._cnn.Svc.UpdateAppInfo(appId, appInfo);

    public Filter GetFilterByInternalId(uint id) => this._cnn.Svc.GetFilterByInternalId(id);

    public List<AppId> GetRemovedApps() => this._cnn.Svc.GetRemovedApps();

    public CnnLogEventLists GetCnnLog(CnnLogEventFilter filter) => this._cnn.Svc.GetCnnLog(filter);

    public List<string> GetDeadFilters() => this._cnn.Svc.GetDeadFilters();

    public bool UACElevate()
    {
      INLSvcCnn cnn = this._cnn;
      if ((cnn != null ? (cnn.State != NLServiceState.Connected ? 1 : 0) : 1) != 0)
        return false;
      if (!this.IsLocalhost)
        throw new Exception("Client must be connected to localhost");
      if (ProcessHelper.IsAdmin())
        return true;
      string path = Path.Combine(this._svcPath, "NLCliElevator.exe");
      if (!System.IO.File.Exists(path))
        throw new Exception("NLCliElevator.exe not found");
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.FileName = path;
      startInfo.Arguments = "nlclientid:" + this._clientId;
      startInfo.UseShellExecute = true;
      startInfo.CreateNoWindow = true;
      startInfo.Verb = "runas";
      try
      {
        System.Diagnostics.Process.Start(startInfo).WaitForExit();
      }
      catch
      {
      }
      return !this.GetIsElevationRequired();
    }

    public ClientInfo GetClientInfo(string clientId) => this.Invoke<ClientInfo>((Func<ClientInfo>) (() => this._cnn.Svc.GetClientInfo(clientId)));

    public void Elevate(string clientId) => this.Invoke((Action) (() => this._cnn.Svc.Elevate(clientId)));

    public bool GetIsElevationRequired()
    {
      try
      {
        return this.Invoke<ClientInfo>((Func<ClientInfo>) (() => this._cnn.Svc.GetClientInfo((string) null))).IsElevationRequired;
      }
      catch (AdminRequiredException ex)
      {
        return false;
      }
    }

    public void SubscribeAsFwReqeustHandler(bool subscribe) => this.Invoke((Action) (() => this._cnn.Svc.SubscribeAsFwReqeustHandler(subscribe)));

    public List<ServiceNotification> GetNotifications() => this.Invoke<List<ServiceNotification>>((Func<List<ServiceNotification>>) (() => this._cnn.Svc.GetNotifications()));

    public void ClearNotifications() => this.Invoke((Action) (() => this._cnn.Svc.ClearNotifications()));

    public void MuteNotifications(string[] notifIds) => this.Invoke((Action) (() => this._cnn.Svc.MuteNotifications(notifIds)));
  }
}
