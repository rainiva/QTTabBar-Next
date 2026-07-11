namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerSpecialTravelLogHost {
        bool IExplorerSpecialTravelLogHost.IsNavigatedByCode() => NavigatedByCode;
        void IExplorerSpecialTravelLogHost.AddSpecialTravelLog(int hash) => LogEntryDic[hash] = _explorerControllerModule.GetCurrentLogEntry();
    }
}
