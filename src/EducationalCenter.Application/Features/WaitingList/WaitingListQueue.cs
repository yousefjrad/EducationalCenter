using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Application.Features.WaitingList;

internal static class WaitingListQueue
{
    /// <summary>
    /// Renumbers the Waiting entries of a section as 1..n, skipping the entry that just left the queue.
    /// Changes are tracked; the caller saves.
    /// </summary>
    public static async Task CompactAsync(IUnitOfWork uow, int sectionId, int leavingEntryId, CancellationToken ct)
    {
        var queue = await uow.WaitingList.GetWaitingBySectionAsync(sectionId, ct);

        var position = 1;
        foreach (var entry in queue.Where(x => x.Id != leavingEntryId).OrderBy(x => x.Position))
        {
            if (entry.Position != position)
            {
                entry.Position = position;
                uow.WaitingList.Update(entry);
            }
            position++;
        }
    }
}
