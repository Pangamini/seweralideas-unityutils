namespace SeweralIdeas.ObjectPooling
{
    /// <summary>Something that <see cref="ObjectPoolManager.ReleaseAtEndOfFrame"/> can release once the frame is over.</summary>
    public interface IDelayedRelease
    {
        void ReleaseNow();
    }
}
