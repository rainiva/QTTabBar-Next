namespace QTTabBarLib {
    /// <summary>
    /// The composition root exposes one atomic assembly operation.  Controllers
    /// receive their own role-specific hosts; this contract is deliberately not
    /// a forwarding facade for the QTTabBarClass object graph.
    /// </summary>
    internal interface ITabBarCompositionHost {
        void BuildTabBarComponents();
    }
}
