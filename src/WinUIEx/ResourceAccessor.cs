using Microsoft.Windows.ApplicationModel.Resources;

namespace WinUIEx
{
    internal static class ResourceAccessor
    {
        private const string LOC_PREFIX = "WinUIEx";
        private static string c_resourceLoc = "WinUIEx/Resources";
        private static ResourceManager? m_resourceManagerWinRT;

        private static ResourceManager GetResourceManager()
        {
            if (m_resourceManagerWinRT is null)
            {
                m_resourceManagerWinRT = new ResourceManager();
            }
            return m_resourceManagerWinRT;
        }

        private static ResourceMap GetResourceMap()
        {
            return ResourceAccessor.GetResourceManager().MainResourceMap.GetSubtree(c_resourceLoc);
        }

        private static ResourceContext GetResourceContext()
        {
            var m_resourceContextWinRT = GetResourceManager().CreateResourceContext();
            return m_resourceContextWinRT;
        }

        internal static string GetLocalizedStringResource(string resourceName)
        {
            var mrt_lifted_resourceMap = GetResourceMap();
            var mrt_lifted_resourceContext = GetResourceContext();
            return mrt_lifted_resourceMap.GetValue(resourceName, mrt_lifted_resourceContext).ValueAsString;
        }
    }
}