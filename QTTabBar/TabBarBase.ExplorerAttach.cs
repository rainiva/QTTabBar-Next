namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        protected bool fProcessingStartups = true;

        protected void FinishExplorerAttached() {
            fProcessingStartups = false;
            OnExplorerAttachActivate();
            base.OnExplorerAttached();
        }

        protected virtual void OnExplorerAttachActivate() {
        }
    }
}
