using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;



namespace CapstoneVS
{
    internal static class ExtensionMethods
    {
        internal static TInterface GetService<TService, TInterface>(this IServiceProvider serviceProvider)
        {
            return (TInterface)serviceProvider.GetService(typeof(TService));
        }

        internal static IOleServiceProvider GetOleServiceProvider(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<SDTE, IOleServiceProvider>();
        }

    }
}
