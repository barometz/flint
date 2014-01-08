namespace flint
{
    /// <summary> Media control instructions as understood by Pebble </summary>
    public enum MediaControls
    {
        PlayPause = 1,
        Next = 4,
        Previous = 5,
        // PlayPause also sends 8 for some reason.  To be figured out.
        Other = 8
    }
}