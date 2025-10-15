using DmsProjeckt.Data;

namespace DmsProjeckt.Helpers
{
    public static class FolderExtensions
    {
        public static IEnumerable<DmsFolder> Flatten(this IEnumerable<DmsFolder> folders)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return FlattenInternal(folders, visited);
        }

        private static IEnumerable<DmsFolder> FlattenInternal(IEnumerable<DmsFolder> folders, HashSet<string> visited)
        {
            foreach (var folder in folders)
            {
                if (folder == null || string.IsNullOrWhiteSpace(folder.Path))
                    continue;

                if (!visited.Add(folder.Path))
                    continue; // 🔁 déjà vu

                yield return folder;

                if (folder.SubFolders != null)
                {
                    foreach (var sub in FlattenInternal(folder.SubFolders, visited))
                    {
                        yield return sub;
                    }
                }
            }
        }
    }
}
