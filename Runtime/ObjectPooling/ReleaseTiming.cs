namespace SeweralIdeas.ObjectPooling
{
    /// <summary>When a despawn takes effect (see <see cref="Spawnable.DespawnTiming"/>).</summary>
    public enum ReleaseTiming
    {
        /// <summary>At the end of the frame it was asked in, as destroying an object does. For what is only shown.</summary>
        EndOfFrame,

        /// <summary>
        /// At the start of the next physics step (at the end of the frame while the time is stopped and no step comes). For what
        /// takes part in the simulation: a frame may hold no step or several, so a release at the end of it would leave the
        /// object in the world for one more step or not, depending on when the frames fell.
        /// </summary>
        NextStep,
    }
}
