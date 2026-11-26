namespace CampusNavigation.Navigation
{
    /// <summary>
    /// Lets a MonoBehaviour expose an INavigationService that is a plain C# object
    /// (like MockNavigationService). UI scripts accept either a component that
    /// implements INavigationService directly or one that implements this.
    /// </summary>
    public interface INavigationServiceProvider
    {
        INavigationService NavigationService { get; }
    }
}
