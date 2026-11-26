using System.Linq;
using CampusNavigation.Navigation;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Interfaces can't be serialized, so components reference services through a
    /// MonoBehaviour field. These helpers turn that field into the interface, and
    /// fall back to searching the scene when the field is left empty.
    /// </summary>
    public static class SceneServices
    {
        /// <summary>
        /// Accepts a component that implements INavigationService directly (the real
        /// engine) or an INavigationServiceProvider (MockNavigationSimulator).
        /// </summary>
        public static INavigationService ResolveNavigation(MonoBehaviour source)
        {
            if (source != null)
                return FromComponent(source);

            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Select(FromComponent)
                .FirstOrDefault(service => service != null);
        }

        /// <summary>Returns "source" as T, or the first T in the scene if source is null.</summary>
        public static T Resolve<T>(MonoBehaviour source) where T : class
        {
            if (source != null)
                return source as T;

            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OfType<T>()
                .FirstOrDefault();
        }

        private static INavigationService FromComponent(MonoBehaviour component) =>
            component as INavigationService ?? (component as INavigationServiceProvider)?.NavigationService;
    }
}
