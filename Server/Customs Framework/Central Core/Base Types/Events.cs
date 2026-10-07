#region References

using System;

#endregion

namespace CustomsFramework
{
    public delegate void BaseCoreEventHandler(BaseCoreEventArgs e);

    public delegate void BaseModuleEventHandler(BaseModuleEventArgs e);

    public class BaseCoreEventArgs : EventArgs
    {
        public BaseCore Core { get; }

        public BaseCoreEventArgs(BaseCore core)
        {
            Core = core;
        }
    }

    public class BaseModuleEventArgs : EventArgs
    {
        public BaseModule Module { get; }

        public BaseModuleEventArgs(BaseModule module)
        {
            Module = module;
        }
    }
}
