#if !NET9_0_OR_GREATER
namespace System.Threading.Tasks;

public static class TaskExtensions
{
    extension(Task)
    {
        public static Task<Task> WhenAny(params ReadOnlySpan<Task> tasks) => Task.WhenAny([.. tasks]);
    }
}
#endif