namespace CommitDock.Models
{
    public enum DealWithLocalChanges
    {
        DoNothing = 0,
        StashAndReapply,
        Discard,
    }
}
